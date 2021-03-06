using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Gearbox
{
    public class CompressedEndgameTable : IEndgameTable
    {
        private const int EnemyMatedScore = +2000;
        private const int FriendMatedScore = -2000;

        private string filename;
        private FileStream infile;
        private CompressedTableHeader header;
        private int currentBlockNumber = -1;
        private int[] whiteBlock;
        private int[] blackBlock;
        private byte[] compressedBlockBuffer;
        private int numBlocks;
        private long[] blockOffsetTable;
        private short[] blockLengthTable;
        private BitReader reader = new();

        public CompressedEndgameTable(string filename)
        {
            this.infile = File.OpenRead(filename);
            this.filename = filename;
            LoadHeader();
        }

        public int TableSize => header.TableSize;
        public int BlockSize => header.BlockSize;

        public void Dispose()
        {
            if (infile != null)
            {
                infile.Dispose();
                infile = null;
            }
        }

        private void LoadHeader()
        {
            // The front of a compressed endgame file is a JSON blob followed by '\n'.
            var headerBytes = new List<byte>();
            var buffer = new byte[512];
            infile.Seek(0, SeekOrigin.Begin);
            while (true)
            {
                int nread = infile.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < nread; ++i)
                {
                    if (buffer[i] == (byte)'\n')
                        goto loaded_json_header;
                    if (buffer[i] < 0x20 || buffer[i] > 0x7f)
                        throw new Exception($"Invalid binary character found in JSON header of file: {filename}");
                    headerBytes.Add(buffer[i]);
                }
                if (nread != buffer.Length)
                    throw new Exception($"Could not find end of JSON header in file: {filename}");
            }
loaded_json_header:
            string json = Encoding.UTF8.GetString(headerBytes.ToArray());
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                IgnoreNullValues = true,
            };
            this.header = JsonSerializer.Deserialize<CompressedTableHeader>(json, options);
            if (this.header.Signature != CompressedTableHeader.CorrectSignature)
                throw new Exception($"Incorrect header signature in file: {filename}");
            whiteBlock = new int[header.BlockSize];
            blackBlock = new int[header.BlockSize];
            numBlocks = (header.TableSize + header.BlockSize - 1) / header.BlockSize;
            blockOffsetTable = new long[numBlocks];
            blockLengthTable = new short[numBlocks];
            long blockTableOffset = headerBytes.Count + 1;      // skip just past the '\n'
            infile.Seek(blockTableOffset, SeekOrigin.Begin);

            var blBuffer = new byte[2 * numBlocks];
            int blread = infile.Read(blBuffer, 0, blBuffer.Length);
            if (blread != blBuffer.Length)
                throw new Exception($"Could not read {blBuffer.Length} bytes for block length table from: {filename}");

            // Decode block lengths and block offsets from blBuffer.
            long nextBlockOffset = blockTableOffset + blBuffer.Length;
            int maxCompressedBlockLength = 0;
            for (int i = 0; i < numBlocks; ++i)
            {
                blockLengthTable[i] = (short)(blBuffer[2*i] | (blBuffer[2*i+1] << 8));
                blockOffsetTable[i] = nextBlockOffset;
                nextBlockOffset += blockLengthTable[i];
                if (blockLengthTable[i] > maxCompressedBlockLength)
                    maxCompressedBlockLength = blockLengthTable[i];
            }
            compressedBlockBuffer = new byte[maxCompressedBlockLength];
        }

        public int GetScore(int tindex, bool whiteToMove)
        {
            int score = GetRawScore(tindex, whiteToMove);

            // Scores stored in the table are in the range -2048..+2047.
            // The score 0 is a draw (or unreachable position).
            // Other scores are mate-in-plies values.
            // -2000 means the player to move is in a checkmate right now.
            // +1999 means the side to move can checkmate in one ply, etc.
            // Adjust for Gearbox score encoding.

            if (score < 0)
                score += (Score.FriendMated - FriendMatedScore);
            else if (score > 0)
                score += (Score.EnemyMated - EnemyMatedScore);

            return score;
        }

        public int GetRawScore(int tindex, bool whiteToMove)
        {
            if (tindex < 0 || tindex >= header.TableSize)
                throw new ArgumentException($"Invalid table index {tindex} for table of size {header.TableSize}");

            LoadBlock(tindex / header.BlockSize);
            return (whiteToMove ? whiteBlock : blackBlock)[tindex % header.BlockSize];
        }

        private void LoadBlock(int block)
        {
            if (block == currentBlockNumber)
                return;     // no need to do anything -- the block is already loaded

            // Read the raw compressed data.
            infile.Seek(blockOffsetTable[block], SeekOrigin.Begin);
            int nread = infile.Read(compressedBlockBuffer, 0, blockLengthTable[block]);
            if (nread != blockLengthTable[block])
                throw new Exception($"Cannot read {blockLengthTable[block]} bytes for block {block} from file {filename}");

            // Decompress the data into whiteBlock and blackBlock.
            // Process 1 bit at a time, traversing the Huffman tree, until each score is decoded.
            // Alternate between appending the scores between the tables whiteBlock and blackBlock.

            int blockLength = header.BlockSize;
            if (block == numBlocks - 1)
            {
                // The final block generally will not be full.
                blockLength = header.TableSize % header.BlockSize;
                if (blockLength == 0)
                    blockLength = header.BlockSize;
            }

            reader.Init(compressedBlockBuffer, nread);
            DecodeBlockForSide(header.WhiteTree, reader, whiteBlock, blockLength);
            DecodeBlockForSide(header.BlackTree, reader, blackBlock, blockLength);
            currentBlockNumber = block;
        }

        private void DecodeBlockForSide(int[][] tree, BitReader reader, int[] block, int blockLength)
        {
            int blockIndex = 0;
            while (blockIndex < blockLength)
            {
                // Read 1 bit to determine whether the next item is a run or a sequence.
                int kind = reader.ReadBit();
                int length = 1 + reader.ReadInteger(10);
                if (blockIndex + length > blockLength)
                    throw new Exception($"Block overflow in decoder: {blockIndex + length} > {blockLength}");

                if (kind == 0)
                {
                    for (int i = 0; i < length; ++i)
                        block[blockIndex++] = DecodeScore(tree, reader);
                }
                else
                {
                    int score = DecodeScore(tree, reader);
                    for (int i = 0; i < length; ++i)
                        block[blockIndex++] = score;
                }
            }
        }

        private int DecodeScore(int[][] tree, BitReader reader)
        {
            int treeIndex = 0;

            while (tree[treeIndex].Length == 2)
            {
                int bit = reader.ReadBit();
                treeIndex = tree[treeIndex][bit];
            }

            if (tree[treeIndex].Length != 1)
                throw new Exception($"Decoder error: unexpected length {tree[treeIndex].Length} at index {treeIndex}.");

            return tree[treeIndex][0];
        }
    }
}
