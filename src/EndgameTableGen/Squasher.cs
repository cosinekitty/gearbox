using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Gearbox;

namespace EndgameTableGen
{
    internal static class Squasher
    {
        private const int BlockSizeEntries = 1024;
        private const int BlockSizeBytes = Table.BytesPerPosition * BlockSizeEntries;

        public static int Compress(int tableSize, string inFileName, string outFileName)
        {
            if (File.Exists(outFileName))
            {
                Console.WriteLine("Deleting existing file: {0}", outFileName);
                File.Delete(outFileName);
            }

            if (!File.Exists(inFileName))
            {
                Console.WriteLine("ERROR: Input file does not exist: {0}", inFileName);
                return 1;
            }

            long fileSizeBytes = (long)Table.BytesPerPosition * tableSize;
            using (FileStream infile = File.OpenRead(inFileName))
            {
                // Verify the file length matches what is expected for this configuration.
                if (infile.Length != fileSizeBytes)
                {
                    Console.WriteLine("ERROR: Expected '{0}' to be {1} bytes long, but found {2} bytes.", inFileName, fileSizeBytes, infile.Length);
                    return 1;
                }

                // The very first (White, Black) table entry of 3 bytes should be [80 18 01], or (0x801, 0x801),
                // which indicates a pair of unreachable positions. This is because the White King and Black King
                // cannot both be at index 0, which is the square a1 (regardless of whether we are using 8-fold symmetry
                // or left-right symmetry for the White King).
                // I will use ASCII characters as a signature for the front of a compressed file.
                var inData = new byte[BlockSizeBytes];
                int nread = infile.Read(inData, 0, Table.BytesPerPosition);
                if (nread != Table.BytesPerPosition)
                {
                    Console.WriteLine("ERROR: Cannot read {0} bytes from the front of file {1}", Table.BytesPerPosition, inFileName);
                    return 1;
                }

                if (inData[0] != 0x80 || inData[1] != 0x18 || inData[2] != 0x01)
                {
                    Console.WriteLine("ERROR: The first {0} bytes of file {1} indicate it is not a raw endgame file.", Table.BytesPerPosition, inFileName);
                    return 1;
                }

                // Create histograms, one for White and one for Black, of how many times different scores
                // occur. These will be used for an efficient encoding of the scores in each block.
                // We also need to save the histogram in the header so the decompressor can reconstruct
                // the same encoding.
                const int HistSize = 1 + (TableGenerator.EnemyMatedScore - TableGenerator.FriendMatedScore);
                var whiteHist = new int[HistSize];
                var blackHist = new int[HistSize];

                infile.Seek(0, SeekOrigin.Begin);
                long bytesRemaining = fileSizeBytes;
                while (bytesRemaining > 0)
                {
                    int attempt = (bytesRemaining < BlockSizeBytes) ? (int)bytesRemaining : BlockSizeBytes;
                    if (attempt % Table.BytesPerPosition != 0)
                        throw new Exception($"Invalid residual size: {attempt} bytes.");
                    nread = infile.Read(inData, 0, attempt);
                    if (nread != attempt)
                        throw new Exception($"Tried to read {attempt} bytes, but received {nread}");
                    int nslots = attempt / Table.BytesPerPosition;
                    for (int i=0; i < nslots; ++i)
                    {
                        DecodeScores(inData, 3*i, out int wscore, out int bscore);
                        ++whiteHist[wscore - TableGenerator.FriendMatedScore];
                        ++blackHist[bscore - TableGenerator.FriendMatedScore];
                    }
                    bytesRemaining -= attempt;
                }

                // Convert the histograms to frequency tables.
                int[][] whiteFrequency = MakeFrequencyTable(whiteHist);
                int[][] blackFrequency = MakeFrequencyTable(blackHist);

                // Build Huffman trees from the frequency tables.
                HuffmanNode wtree = HuffmanEncoder.Compile(whiteFrequency);
                HuffmanNode btree = HuffmanEncoder.Compile(blackFrequency);

                // Build dictionaries of bit strings for each tree.
                Dictionary<int, string> wdict = HuffmanEncoder.MakeEncoding(wtree);
                Dictionary<int, string> bdict = HuffmanEncoder.MakeEncoding(btree);

                PrintHuffmanCode(wdict, "White");
                PrintHuffmanCode(bdict, "Black");

                // Open the output file.
                using (FileStream outfile = File.Create(outFileName))
                {
                    // Create a JSON blob and write it to the front of the output file.
                    // Terminate the JSON blob with '\n'.
                    var header = new CompressedTableHeader
                    {
                        Signature = CompressedTableHeader.CorrectSignature,
                        TableSize = tableSize,
                        BlockSize = BlockSizeEntries,
                        WhiteTree = wtree.Compact(),
                        BlackTree = btree.Compact(),
                    };

                    var options = new JsonSerializerOptions
                    {
                        IncludeFields = true,
                        IgnoreNullValues = true,
                    };
                    string json = JsonSerializer.Serialize(header, options) + "\n";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(json);
                    outfile.Write(headerBytes, 0, headerBytes.Length);

                    // Reserve space in the output file to hold the table of compressed block lengths.
                    // We will come back and write the block length table once we finish compressing all the blocks.
                    long blockLengthTablePosition = outfile.Position;
                    int numBlocks = (tableSize + (BlockSizeEntries-1)) / BlockSizeEntries;
                    outfile.SetLength(outfile.Length + 2*numBlocks);    // 16-bit unsigned integer for each block length
                    outfile.Seek(0, SeekOrigin.End);

                    var blockLengthTable = new short[numBlocks];
                    var blockBuffer = new byte[BlockSizeBytes];

                    // Read the entire input file again, compressing each block.
                    infile.Seek(0, SeekOrigin.Begin);
                    bytesRemaining = fileSizeBytes;
                    int block = 0;
                    var writer = new BitWriter(outfile);

                    long prevBlockOffset = outfile.Position;
                    long minBlockLength = long.MaxValue;
                    long maxBlockLength = long.MinValue;
                    while (bytesRemaining > 0)
                    {
                        int attempt = (bytesRemaining < BlockSizeBytes) ? (int)bytesRemaining : BlockSizeBytes;
                        if (attempt % Table.BytesPerPosition != 0)
                            throw new Exception($"Invalid residual size: {attempt} bytes.");
                        nread = infile.Read(inData, 0, attempt);
                        if (nread != attempt)
                            throw new Exception($"Tried to read {attempt} bytes, but received {nread}");
                        int nslots = attempt / Table.BytesPerPosition;
                        for (int i=0; i < nslots; ++i)
                        {
                            DecodeScores(inData, 3*i, out int wscore, out int bscore);
                            writer.Write(wdict[wscore]);
                            writer.Write(bdict[bscore]);
                        }
                        writer.Flush();
                        long offset = outfile.Position;
                        long length = offset - prevBlockOffset;
                        prevBlockOffset = offset;
                        if ((length & 0x7fff) != length)
                            throw new Exception($"Block length {length} cannot be represented as a 16-bit integer.");
                        blockLengthTable[block] = (short)length;
                        if (length > maxBlockLength)
                            maxBlockLength = length;
                        if (length < minBlockLength)
                            minBlockLength = length;
                        bytesRemaining -= attempt;
                        ++block;
                    }
                    if (block != numBlocks)
                        throw new Exception($"Expected {numBlocks} blocks, but found {block}");

                    Console.WriteLine("Min block length = {0}, max block length = {1}", minBlockLength, maxBlockLength);

                    // Seek backwards and write the table of block lengths.
                    outfile.Seek(blockLengthTablePosition, SeekOrigin.Begin);
                    var outBuffer = new byte[2];
                    for (int i = 0; i < numBlocks; ++i)
                    {
                        outBuffer[0] = (byte)blockLengthTable[i];
                        outBuffer[1] = (byte)(blockLengthTable[i] >> 8);
                        outfile.Write(outBuffer, 0, 2);
                    }
                }
            }

            return 0;
        }

        private static void PrintHuffmanCode(Dictionary<int, string> dict, string side)
        {
            Console.WriteLine("Huffman encoding for {0} scores:", side);
            foreach (int score in dict.Keys.OrderBy(score => score))
            {
                Console.WriteLine("{0,5} = {1}", score, dict[score]);
            }
            Console.WriteLine();
        }

        private static int[][] MakeFrequencyTable(int[] histogram)
        {
            var list = new List<int[]>();
            for (int score = TableGenerator.FriendMatedScore; score <= TableGenerator.EnemyMatedScore; ++score)
            {
                int count = histogram[score - TableGenerator.FriendMatedScore];
                if (count > 0)
                    list.Add(new int[] { score, count });
            }
            return list.ToArray();
        }

        private static void DecodeScores(byte[] data, int offset, out int wscore, out int bscore)
        {
            wscore = AdjustScore(((int)data[offset] << 4) | ((int)data[offset+1] >> 4));
            bscore = AdjustScore((((int)data[offset+1] & 0x0f) << 8) | (int)data[offset+2]);
        }

        private static int AdjustScore(int score)
        {
            // Sign extend the raw score to represent negative scores correctly.
            if (0 != (score & 0x800))
                score |= ~0xfff;

            // Treat unreachable scores as draws (0), because their value shouldn't matter anyway.
            // This is to enhance data compression.
            if (score == TableGenerator.UnreachablePos)
                score = 0;

            if (score < TableGenerator.FriendMatedScore || score > TableGenerator.EnemyMatedScore - 1)
                throw new Exception($"Invalid score: {score}");

            return score;
        }
    }
}
