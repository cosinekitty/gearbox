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
        private const int MaxThreshold = 23;    // maximum value that was ever chosen in 1..50: probably because 2*11+1 = 23.

        private struct Run
        {
            public int Score;
            public int Length;
        }

        public static int Compress(int tableSize, string inFileName, string outFileName, ref long totalCompressedBytes)
        {
            var histogram = new int[MaxThreshold+1];

            if (File.Exists(outFileName))
            {
                //Console.WriteLine("Deleting existing file: {0}", outFileName);
                File.Delete(outFileName);
            }

            if (!File.Exists(inFileName))
            {
                Console.WriteLine("ERROR: Input file does not exist: {0}", inFileName);
                return 1;
            }

            Console.WriteLine("Compressing: {0}", inFileName);

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
                    int prev_wscore = int.MinValue;
                    int prev_bscore = int.MinValue;
                    int wrun = 0;
                    int brun = 0;
                    const int MeanThreshold = 11;
                    for (int i=0; i < nslots; ++i)
                    {
                        DecodeScores(inData, 3*i, out int wscore, out int bscore);

                        if (wscore == prev_wscore)
                        {
                            ++wrun;
                        }
                        else
                        {
                            // More accurate histograms for compression: if there are enough in a row, count as a single instance, because the score will likely be run-length encoded.
                            if (wrun > 0)
                                whiteHist[prev_wscore - TableGenerator.FriendMatedScore] += ((wrun > MeanThreshold) ? 1 : wrun);
                            prev_wscore = wscore;
                            wrun = 1;
                        }

                        if (bscore == prev_bscore)
                        {
                            ++brun;
                        }
                        else
                        {
                            if (brun > 0)
                                blackHist[prev_bscore - TableGenerator.FriendMatedScore] += ((brun > MeanThreshold) ? 1 : brun);
                            prev_bscore = bscore;
                            brun = 1;
                        }
                    }

                    if (wrun > 0)
                        whiteHist[prev_wscore - TableGenerator.FriendMatedScore] += ((wrun > MeanThreshold) ? 1 : wrun);

                    if (brun > 0)
                        blackHist[prev_bscore - TableGenerator.FriendMatedScore] += ((brun > MeanThreshold) ? 1 : brun);

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
                    var wblock = new int[BlockSizeEntries];
                    var bblock = new int[BlockSizeEntries];
                    var writer = new BitWriter(outfile);
                    var runlist = new List<Run>();

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
                            DecodeScores(inData, 3*i, out wblock[i], out bblock[i]);

                        WriteBlock(writer, nslots, wblock, wdict, runlist, histogram);
                        WriteBlock(writer, nslots, bblock, bdict, runlist, histogram);
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

                    outfile.Flush();
                    long inFileLength = infile.Length;
                    long outFileLength = outfile.Length;
                    double ratio = (double)inFileLength / (double)outFileLength;
                    Console.WriteLine("Compressed {0} bytes to {1} bytes. Ratio = {2}", inFileLength.ToString("n0"), outFileLength.ToString("n0"), ratio.ToString("0.0000"));
                    for (int i = 1; i < histogram.Length; ++i)
                        Console.WriteLine("histogram[{0,4}] = {1,20}", i, histogram[i].ToString("n0"));

                    totalCompressedBytes += outFileLength;
                }
            }

            return 0;
        }

        private static void WriteBlock(
            BitWriter writer,
            int nslots,
            int[] block,
            Dictionary<int, string> dict,
            List<Run> runlist,
            int[] histogram)
        {
            // A combination of Huffman encoding and run-length encoding.
            // We alternate between "run mode" and "individual score" mode.
            // Each mode begins with a bit indicating which kind it is:
            // 0 = sequence of individual scores
            // 1 = run of identical scores
            // Following the type bit is a 10-bit integer telling its length.
            // A run then has a single score value, whereas a sequence has
            // the specified number of scores.

            // Convert the scores in 'block' into a run list.
            runlist.Clear();

            int f = 0;
            for (int i = 1; i < nslots; ++i)
            {
                if (block[i] != block[f])
                {
                    // A run just ended.
                    runlist.Add(new Run { Score = block[f], Length = i - f });
                    f = i;
                }
            }

            int length = nslots - f;
            if (length > 0)
                runlist.Add(new Run { Score = block[f], Length = length });

            int runLengthThreshold = BestRunLengthThreshold(runlist, dict);
            ++histogram[runLengthThreshold];

            // Go back and encode every run whose length is at least runLengthThreshold.
            // All other runs get expanded back to a sequence of individual scores.
            int sequenceFrontIndex = 0;
            int sequenceLength = 0;
            int checkLength = 0;
            for (int i = 0; i < runlist.Count; ++i)
            {
                int n = runlist[i].Length;
                if (n >= runLengthThreshold)
                {
                    if (sequenceLength > 0)
                    {
                        checkLength += sequenceLength;
                        WriteSequence(writer, runlist, sequenceLength, sequenceFrontIndex, i, dict);
                        sequenceLength = 0;
                    }
                    checkLength += n;
                    writer.Write(1, 1);                     // signal a run
                    writer.Write(n-1, 10);                  // encode run length (subtract 1 to fit in 10 bits)
                    writer.Write(dict[runlist[i].Score]);   // encode the score to be repeated
                    sequenceFrontIndex = i + 1;
                }
                else
                {
                    sequenceLength += n;
                }
            }

            if (sequenceLength > 0)
            {
                checkLength += sequenceLength;
                WriteSequence(writer, runlist, sequenceLength, sequenceFrontIndex, runlist.Count, dict);
            }

            if (checkLength != nslots)
                throw new Exception($"Internal error: nslots={nslots}, checkLength={checkLength}");
        }

        private static int BestRunLengthThreshold(List<Run> runlist, Dictionary<int, string> dict)
        {
            int bestThresh = -1;
            int bestBitCount = int.MaxValue;
            for (int thresh = 1; thresh <= MaxThreshold; ++thresh)
            {
                int bitcount = 0;
                int sequenceBits = 0;
                for (int i = 0; i < runlist.Count; ++i)
                {
                    int n = runlist[i].Length;
                    if (n >= thresh)
                    {
                        if (sequenceBits > 0)
                        {
                            bitcount += 11 + sequenceBits;
                            sequenceBits = 0;
                        }
                        bitcount += 11 + dict[runlist[i].Score].Length;
                    }
                    else
                    {
                        sequenceBits += n * dict[runlist[i].Score].Length;
                    }
                }

                if (sequenceBits > 0)
                {
                    bitcount += 11 + sequenceBits;
                }

                if (bitcount < bestBitCount)
                {
                    bestBitCount = bitcount;
                    bestThresh = thresh;
                }
            }
            return bestThresh;
        }

        private static void WriteSequence(
            BitWriter writer,
            List<Run> runlist,
            int sequenceLength,
            int front,
            int back,
            Dictionary<int, string> dict)
        {
            writer.Write(0, 1);                     // signal a sequence
            writer.Write(sequenceLength - 1, 10);   // write total number of scores
            int checkLength = 0;
            for (int s = front; s < back; ++s)
            {
                checkLength += runlist[s].Length;
                // Report all the scores, repeating each the required number of times.
                for (int k = 0; k < runlist[s].Length; ++k)
                    writer.Write(dict[runlist[s].Score]);
            }
            if (checkLength != sequenceLength)
                throw new Exception($"Internal error: checkLength={checkLength}, sequenceLength={sequenceLength}");
        }

        internal static int Verify(string rawFileName, string compressedFileName)
        {
            // Verify that we can decompress the compressed file and obtain
            // an exact match for the original scores.
            // Inaccessible/invalid positions will have a score of 0 in
            // the decompressed data, so they don't need to match the original scores.
            int tindex = 0;
            using (var cmp = new CompressedEndgameTable(compressedFileName))
            {
                using (FileStream infile = File.OpenRead(rawFileName))
                {
                    byte[] buffer = new byte[Table.BytesPerPosition * cmp.BlockSize];
                    while (true)
                    {
                        int nbytes = infile.Read(buffer, 0, buffer.Length);
                        int nslots = nbytes / Table.BytesPerPosition;
                        for (int i = 0; i < nslots; ++i)
                        {
                            DecodeScores(buffer, 3*i, out int wscore, out int bscore);
                            int wcheck = cmp.GetRawScore(tindex, true);
                            int bcheck = cmp.GetRawScore(tindex, false);
                            ++tindex;
                            if (wcheck != wscore || bcheck != bscore)
                            {
                                Console.WriteLine("FAIL(Verify): wscore={0}, wcheck={1}, bscore={2}, bcheck={3} tindex={4}", wscore, wcheck, bscore, bcheck, tindex);
                                return 1;
                            }
                        }

                        if (nbytes < buffer.Length)
                            break;  // EOF
                    }
                }
            }
            Console.WriteLine("Verified {0} entries in file {1}", tindex.ToString("n0"), compressedFileName);
            Console.WriteLine();
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
