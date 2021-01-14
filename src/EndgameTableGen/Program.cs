﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Gearbox;

namespace EndgameTableGen
{
    // See notes in document:
    // https://docs.google.com/document/d/1oe3dbQlsrfpdWhEvbcRAmaDjr0vOFTpFWAfDFGgTfes

    static class Program
    {
        const int MaxNonKings = 9;
        const int MaxThreads = 32;
        static readonly string UsageText = string.Format(@"
Endgame tablebase generator for the Gearbox chess engine.

USAGE:

EndgameTableGen plan N
    Prints the series of endgame database tables to be generated,
    in dependency order, for all possible configurations of
    up to N non-king pieces/pawns, where N = 1..{0}.

EndgameTableGen gen N
    Generates all tables for up to N non-king movers.
    If the program is interrupted and resumed, will
    load all the tables that were completed and resume
    at the first unwritten table.

EndgameTableGen pgen N T
    Like 'gen N', only runs the generator in parallel
    using T = 1..{1} threads.

EndgameTableGen test N
    Perform a self-test on generating distinct positions
    without duplicates, consistent table indexes, etc.
    Does not write any tables to disk.

EndgameTableGen list filename
    Given a table filename, generates a text listing
    of each position in the file, and the associated scores for
    White and Black. The filename must be of the form:
    [dir/]dddddddddd.endgame
    where the 'd' are decimal digits that combine to form
    a valid endgame configuration ID. This is because the
    configuration ID is fundamental for interpreting the
    contents of the file.

EndgameTableGen stats filename
    Analyze the given endgame table and print statistics.

EndgameTableGen decode config_id table_index side_to_move
    Prints the FEN of a board configuration corresponding to
    the given configuration and table index.
    config_id    = decimal integer QqRrBbNnPp.
    table_index  = integer offset into the table.
    side_to_move = 'w' or 'b'.

EndgameTableGen diff a.endgame b.endgame
    Compares the scores in the two endgame table files.

EndgameTableGen edge_check
    Runs unit tests on edge/index file generator, sorter, reader.
", MaxNonKings, MaxThreads);

        static int Main(string[] args)
        {
            if ((args.Length == 2) && (args[0] == "list"))
            {
                return ListTable(args[1]);
            }

            if ((args.Length == 2) && (args[0] == "stats"))
            {
                return PrintTableStats(args[1]);
            }

            if ((args.Length == 4) && (args[0] == "decode"))
            {
                string side_to_move = args[3];
                if (side_to_move != "w" && side_to_move != "b")
                {
                    Console.WriteLine("Invalid side_to_move: {0}", side_to_move);
                    return 1;
                }

                if (long.TryParse(args[1], out long config_id) && (config_id >= 0) && (config_id <= 9999999999))
                {
                    if (int.TryParse(args[2], out int table_index))
                    {
                        var board = new Board(false);
                        TableGenerator.DecodePosition(board, config_id, table_index, side_to_move == "w");
                        Console.WriteLine(board.ForsythEdwardsNotation());
                        return 0;
                    }

                    Console.WriteLine("Invalid table_index: {0}", args[2]);
                    return 1;
                }
                Console.WriteLine("Invalid config_id: {0}", args[1]);
                return 1;
            }

            if (args.Length == 2)       // must handle all other 2-arg commands before here
            {
                if (!int.TryParse(args[1], out int nonkings) || (nonkings < 1) && (nonkings > MaxNonKings))
                {
                    Console.WriteLine("Invalid number of non-kings: {0}", args[1]);
                    return 1;
                }

                ITableWorker worker;
                switch (args[0])
                {
                    case "plan":
                        worker = new TablePrinter();
                        break;

                    case "gen":
                        // Figure out the maximum possible table size up front.
                        int max_table_size = MaxTableSize(nonkings);

                        // Now the generator can pre-allocate the worst-case memory usage.
                        worker = new TableGenerator(max_table_size) { EnableTableGeneration = true };
                        break;

                    case "test":
                        worker = new TableGenerator(0) { EnableTableGeneration = false };
                        break;

                    default:
                        Console.WriteLine("ERROR: Unknown type of worker: {0}", args[0]);
                        return 1;
                }

                using (var planner = new WorkPlanner(worker))
                {
                    planner.Plan(nonkings);
                }
                return 0;
            }

            if (args.Length == 3 && args[0] == "pgen")
            {
                if (!int.TryParse(args[1], out int nonkings) || (nonkings < 1) && (nonkings > MaxNonKings))
                {
                    Console.WriteLine("Invalid number of non-kings: {0}", args[1]);
                    return 1;
                }

                if (!int.TryParse(args[2], out int num_threads) || (num_threads < 1) || (num_threads > MaxThreads))
                {
                    Console.WriteLine("Invalid number of threads: {0}. Must be 1..{1}.", args[2], MaxThreads);
                    return 1;
                }

                int max_table_size = MaxTableSize(nonkings);
                var worker = new ParallelTableGenerator(max_table_size, num_threads);
                using (var planner = new WorkPlanner(worker))
                {
                    planner.Plan(nonkings);
                }
                return 0;
            }

            if (args.Length == 3 && args[0] == "diff")
            {
                return DiffEndgameTables(args[1], args[2]);
            }

            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "edge_check":
                        return EdgeCheck();
                }
            }

            Console.WriteLine(UsageText);
            return 1;
        }

        private static int DiffEndgameTables(string filename1, string filename2)
        {
            if (!ConfigIdFromFileName(filename1, out long config_id_1))
                return 1;

            if (!ConfigIdFromFileName(filename2, out long config_id_2))
                return 1;

            if (config_id_1 != config_id_2)
            {
                Console.WriteLine("ERROR: The files have different configuration IDs.");
                return 1;
            }

            int[,] config = TableWorker.DecodeConfig(config_id_1);
            int size = (int)TableWorker.TableSize(config);

            var table1 = MemoryTable.MemoryLoad(filename1, size);
            var table2 = MemoryTable.MemoryLoad(filename2, size);
            if (table1.Size != table2.Size)
            {
                Console.WriteLine("Tables are different sizes.");
                return 1;
            }

            var board = new Board(false);
            int diffcount = 0;
            for (int tindex = 0; tindex < table1.Size; ++tindex)
            {
                int w1 = table1.GetWhiteScore(tindex);
                int w2 = table2.GetWhiteScore(tindex);
                diffcount += PrintDiff(board, config_id_1, tindex, w1, w2, true);

                int b1 = table1.GetBlackScore(tindex);
                int b2 = table2.GetBlackScore(tindex);
                diffcount += PrintDiff(board, config_id_1, tindex, b1, b2, false);
            }
            return (diffcount == 0) ? 0 : 1;
        }

        private static int PrintDiff(Board board, long config_id, int tindex, int score1, int score2, bool white_to_move)
        {
            if (score1 == score2)
                return 0;

            TableGenerator.DecodePosition(board, config_id, tindex, white_to_move);
            string fen = board.ForsythEdwardsNotation();

            Console.WriteLine("{0,10} {1,6} {2,6}  {3}", tindex, score1, score2, fen);
            return 1;
        }


        private static int MaxTableSize(int nonkings)
        {
            var worker = new MaxTableSizeFinder();
            using (var planner = new WorkPlanner(worker))
            {
                planner.Plan(nonkings);
                worker.Log("For nonkings={0}, max table size = {1:n0}", nonkings, worker.MaxTableSize);
                return (int)worker.MaxTableSize;
            }
        }

        private class MaxTableSizeFinder : TableWorker
        {
            public BigInteger MaxTableSize;

            public override void Start()
            {
                MaxTableSize = 0;
            }

            public override Table GenerateTable(int[,] config)
            {
                BigInteger size = TableSize(config);
                if (size > MaxTableSize)
                    MaxTableSize = size;
                return null;
            }

            public override void Finish()
            {
            }

            public override void Dispose()
            {
            }
        }

        static bool ConfigIdFromFileName(string filename, out long config_id)
        {
            config_id = -1;
            string config_text = Path.GetFileNameWithoutExtension(filename);
            if (config_text.Length != 10 || !long.TryParse(config_text, out config_id))
            {
                Console.WriteLine("ERROR: Filename does not contain a valid configuration ID: {0}", filename);
                return false;
            }
            return true;
        }

        static int ListTable(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("ERROR: File does not exist: {0}", filename);
                return 1;
            }

            if (!ConfigIdFromFileName(filename, out long config_id))
                return 1;

            int[,] config = TableWorker.DecodeConfig(config_id);
            int size = (int) TableWorker.TableSize(config);
            Table table = MemoryTable.MemoryLoad(filename, size);
            var worker = new TableGenerator(0);
            worker.ForEachPosition(table, config, PrintNode);
            return 0;
        }

        static int PrintNode(Table table, Board board, int tindex)
        {
            int score = board.IsWhiteTurn ? table.GetWhiteScore(tindex) : table.GetBlackScore(tindex);
            Console.WriteLine("{0,9} {1,5} {2}", tindex, score, board.ForsythEdwardsNotation());
            return 1;
        }

        static int PrintTableStats(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("ERROR: File does not exist: {0}", filename);
                return 1;
            }

            string config_text = Path.GetFileNameWithoutExtension(filename);
            if (config_text.Length != 10 || !long.TryParse(config_text, out long config_id))
            {
                Console.WriteLine("ERROR: Filename does not contain a valid configuration ID.");
                return 1;
            }

            int[,] config = TableWorker.DecodeConfig(config_id);
            int size = (int) TableWorker.TableSize(config);
            Table table = MemoryTable.MemoryLoad(filename, size);
            Console.WriteLine(filename);
            Console.WriteLine("{0,12:n0} total position capacity", size);
            int whiteWins=0, whiteLosses=0, blackWins=0, blackLosses=0, occupied=0;
            int longestForcedMatePlies = -1;
            for (int tindex=0; tindex < size; ++tindex)
            {
                int wscore = table.GetWhiteScore(tindex);
                int bscore = table.GetBlackScore(tindex);
                if (wscore != 0 || bscore != 0)
                {
                    ++occupied;

                    if (wscore != 0)
                    {
                        if (wscore < 0)
                            ++whiteLosses;
                        else
                            ++whiteWins;

                        longestForcedMatePlies = Math.Max(longestForcedMatePlies, TableGenerator.EnemyMatedScore - Math.Abs(wscore));
                    }

                    if (bscore != 0)
                    {
                        if (bscore < 0)
                            ++blackLosses;
                        else
                            ++blackWins;

                        longestForcedMatePlies = Math.Max(longestForcedMatePlies, TableGenerator.EnemyMatedScore - Math.Abs(bscore));
                    }
                }
            }

            Console.WriteLine("{0,12:n0} occupied score slots; ratio = {1}", occupied, ((double)occupied / size).ToString("F6"));
            Console.WriteLine("{0,12:n0} White wins", whiteWins);
            Console.WriteLine("{0,12:n0} White losses", whiteLosses);
            Console.WriteLine("{0,12:n0} Black wins", blackWins);
            Console.WriteLine("{0,12:n0} Black losses.", blackLosses);
            Console.WriteLine("Longest forced mate plies = {0}", longestForcedMatePlies);
            Console.WriteLine();

            return 0;
        }

        private class UniqueRandomGenerator
        {
            private readonly HashSet<int> used = new();
            private readonly Random rand = new(8675309);    // Ask Jenny for the pseudorandom numbers.

            public int Next(int denom)
            {
                while (true)
                {
                    int r = rand.Next(denom);
                    if (used.Add(r))
                        return r;
                }
            }
        }

        private static void Shuffle<T>(int seed, T[] array)
        {
            var shuffle = new Random(seed);
            for (int i = 1; i < array.Length; ++i)
            {
                int r = shuffle.Next(i+1);
                if (r < i)
                {
                    T swap = array[i];
                    array[i] = array[r];
                    array[r] = swap;
                }
            }
        }

        private static int EdgeCheck()
        {
            // We want to create a list of edges where
            // each after_tindex is referenced by multiple before_index,
            // to verify that the sorting and batching work properly.
            const int BatchSize = 10;     // how many before_tindex map to each after_tindex
            const int NumBatches = 10000;
            const int NumEdges = BatchSize * NumBatches;
            const int TableSize = 4000000;
            const string EdgeFileName = "test.edge";
            const string IndexFileName = "test.index";

            int i, b, n;
            var urand = new UniqueRandomGenerator();
            var edgeList = new GraphEdge[NumEdges];
            for (b = n = 0; b < NumBatches; ++b)
            {
                int after_tindex = urand.Next(TableSize);
                for (i = 0; i < BatchSize; ++i)
                {
                    edgeList[n].before_tindex = urand.Next(TableSize);
                    edgeList[n].after_tindex = after_tindex;
                    ++n;
                }
            }
            if (n != NumEdges)
            {
                Console.WriteLine($"FAIL EdgeCheck: internal error -- n={n}, NumEdges={NumEdges}");
                return 1;
            }

            // Shuffle the edgelist so the matching after_tindex values are no longer contiguous.
            Shuffle(1740417, edgeList);

            // Write the list of edges to the output file.
            using (var writer = new EdgeWriter(EdgeFileName))
            {
                for (i = 0; i < NumEdges; ++i)
                    writer.WriteEdge(edgeList[i]);
            }

            if (!VerifyEdgeReader(EdgeFileName, edgeList))
                return 1;

            // Sort the edge file.
            string WorkDir = Directory.GetCurrentDirectory();
            const int Radix = 16;
            using (var sorter = new EdgeFileSorter(WorkDir, Radix, TableSize))
            {
                sorter.Sort(EdgeFileName, IndexFileName);
            }

            if (!VerifyEdgeSort(EdgeFileName, edgeList, NumBatches, BatchSize))
                return 1;

            if (!VerifyEdgeIndex(EdgeFileName, IndexFileName, edgeList, BatchSize))
                return 1;

            File.Delete(EdgeFileName);
            File.Delete(IndexFileName);

            Console.WriteLine("PASS: EdgeCheck");
            return 0;
        }

        static bool VerifyEdgeReader(string edgeFileName, GraphEdge[] list)
        {
            using (var reader = new EdgeReader(edgeFileName))
            {
                int i;
                for (i = 0; reader.ReadEdge(out GraphEdge edge); ++i)
                {
                    if (list[i].before_tindex != edge.before_tindex || list[i].after_tindex != edge.after_tindex)
                    {
                        Console.WriteLine($"FAIL VerifyEdges: edgeList[{i}]={list[i]}, but read edge={edge}");
                        return false;
                    }
                }
                if (i != list.Length)
                {
                    Console.WriteLine($"FAIL VerifyEdges: expected to read {list.Length} edges, but found {i}");
                    return false;
                }
            }
            return true;
        }

        static bool VerifyEdgeSort(string edgeFileName, GraphEdge[] list, int numBatches, int batchSize)
        {
            if (list.Length != numBatches * batchSize)
            {
                Console.WriteLine($"FAIL VerifyEdgeSort: list.Length={list.Length} but {numBatches}*{batchSize} = {numBatches*batchSize}");
                return false;
            }

            // Confirm that the edge file is sorted by after_tindex.
            // Compute the expected series of after_tindex values.
            // Each one should occur 'batchSize' times, but the before_tindex can be any order.
            var after_set = new HashSet<int>(list.Select(e => e.after_tindex));
            var after_list = after_set.OrderBy(a => a).ToArray();
            if (after_list.Length != numBatches)
            {
                Console.WriteLine($"FAIL VerifyEdgeSort: after_list.Length should have been {numBatches}, but is {after_list.Length}");
                return false;
            }

            var dict = new Dictionary<int, SortedSet<int>>();
            foreach (GraphEdge e in list)
            {
                if (!dict.TryGetValue(e.after_tindex, out var before_list))
                    dict.Add(e.after_tindex, before_list = new());

                if (!before_list.Add(e.before_tindex))
                {
                    Console.WriteLine($"FAIL VerifyEdgeSort: could not add {e} to dictionary.");
                    return false;
                }
            }

            using (var reader = new EdgeReader(edgeFileName))
            {
                var before_list = new List<int>();
                for (int n = 0; n < numBatches; ++n)
                {
                    before_list.Clear();
                    for (int b = 0; b < batchSize; ++b)
                    {
                        if (!reader.ReadEdge(out GraphEdge edge))
                        {
                            Console.WriteLine($"FAIL VerifyEdgeSort: could not read edge for n={n}, b={b}");
                            return false;
                        }
                        if (edge.after_tindex != after_list[n])
                        {
                            Console.WriteLine($"FAIL VerifyEdgeSort: Expected after_tindex={after_list[n]}, but found {edge}");
                            return false;
                        }
                        before_list.Add(edge.before_tindex);
                    }

                    int[] before_sort = before_list.OrderBy(x => x).ToArray();
                    if (before_sort.Length != batchSize)
                    {
                        Console.WriteLine($"FAIL VerifyEdgeSort: expected before_sort.Length to be {batchSize}, but found {before_sort.Length}");
                        return false;
                    }

                    int[] before_check = dict[after_list[n]].ToArray();
                    if (before_check.Length != before_sort.Length)
                    {
                        Console.WriteLine($"FAIL VerifyEdgeSort: before_check.Length={before_check.Length}, but before_sort.Length={before_sort.Length}");
                        return false;
                    }

                    for (int k = 0; k < batchSize; ++k)
                    {
                        if (before_sort[k] != before_check[k])
                        {
                            Console.WriteLine($"FAIL VerifyEdgeSort: before_sort[{k}]={before_sort[k]}, but before_check[{k}]={before_check[k]}.");
                            return false;
                        }
                    }
                }

                if (reader.ReadEdge(out GraphEdge trash))
                {
                    Console.WriteLine($"FAIL VerifyEdgeSort: should have exhausted edges, but found {trash}");
                    return false;
                }
            }

            Console.WriteLine("PASS: VerifyEdgeSort");
            return true;
        }

        struct EdgeBatch
        {
            public int after_tindex;
            public int[] before_list;
        }

        static bool VerifyEdgeIndex(string edgeFileName, string indexFileName, GraphEdge[] list, int batchSize)
        {
            var batchList = new List<EdgeBatch>();
            foreach (var group in list.GroupBy(edge => edge.after_tindex))
            {
                int after_tindex = group.Key;
                int[] before_list = group.Select(edge => edge.before_tindex).OrderBy(b => b).ToArray();
                batchList.Add(new EdgeBatch{after_tindex = after_tindex, before_list = before_list});
            }
            EdgeBatch[] batchArray = batchList.ToArray();

            // Shuffle the list to make sure we can access the batches in random after_tindex order.
            Shuffle(57346385, batchArray);

            // Exercise the indexer.
            using (var indexer = new EdgeIndexer(indexFileName, edgeFileName))
            {
                var before_list = new List<int>();
                foreach (EdgeBatch batch in batchArray)
                {
                    //Console.WriteLine("BATCH {0} => [{1}]", batch.after_tindex, string.Join(", ", batch.before_list));
                    indexer.GetBeforeTableIndexes(before_list, batch.after_tindex);
                    if (before_list.Count != batch.before_list.Length || before_list.Count != batchSize)
                    {
                        Console.WriteLine($"FAIL VerifyEdgeIndex: before_list.Count={before_list.Count}, batch length=${batch.before_list.Length}, expected {batchSize}");
                        return false;
                    }
                    before_list.Sort();
                    for (int i=0; i < batchSize; ++i)
                    {
                        if (before_list[i] != batch.before_list[i])
                        {
                            Console.WriteLine($"FAIL VerifyEdgeIndex: before_list[{i}]={before_list[i]}, but expected {batch.before_list[i]}");
                            return false;
                        }
                    }
                }
            }

            Console.WriteLine("PASS: VerifyEdgeIndex");
            return true;
        }
    }
}
