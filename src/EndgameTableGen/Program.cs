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

EndgameTableGen compress path/<config_id>.endgame ...
    Opens the specified raw endgame file(s) and writes a compressed
    version of each to: path/x_<config_id>.endgame.

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

            if ((args.Length >= 2) && (args[0] == "compress"))
            {
                long totalCompressedBytes = 0;
                for (int i = 1; i < args.Length; ++i)
                {
                    int rc = CompressEndgameTable(args[i], ref totalCompressedBytes);
                    if (rc != 0)
                        return rc;
                }
                Console.WriteLine("Total Compressed Bytes = {0}", totalCompressedBytes.ToString("n0"));
                return 0;
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
                        worker = new TableGenerator(max_table_size);
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

            Console.WriteLine(UsageText);
            return 1;
        }

        private static int CompressEndgameTable(string inFileName, ref long totalCompressedBytes)
        {
            const string RawEndgameSuffix = ".endgame";
            const string CompressedEndgameSuffix = ".egm";

            if (!inFileName.EndsWith(RawEndgameSuffix))
            {
                Console.WriteLine("ERROR: Input endgame filename must end with '{0}'.", RawEndgameSuffix);
                return 1;
            }

            if (!ConfigIdFromFileName(inFileName, out long config_id))
                return 1;

            int[,] config = TableWorker.DecodeConfig(config_id);
            int size = (int)TableWorker.TableSize(config);

            // Convert the input filename to a matching output filename.
            // For example, "../../tables/1100000010.endgame" becomes "../../tables/1100000010.egm".
            string outFileName = inFileName.Substring(0, inFileName.Length - RawEndgameSuffix.Length) + CompressedEndgameSuffix;

            int rc = Squasher.Compress(size, inFileName, outFileName, ref totalCompressedBytes);
            if (rc != 0)
                return rc;

            rc = Squasher.Verify(inFileName, outFileName);
            return rc;
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
            worker.ForEachPosition(table, config, PrintNode, null);
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
            Console.WriteLine("size = {0,12:n0}", size);
            Console.WriteLine();

            const int HistogramSize = (TableGenerator.EnemyMatedScore - TableGenerator.FriendMatedScore) + 1;
            var whiteHistogram = new int[HistogramSize];
            var blackHistogram = new int[HistogramSize];
            for (int tindex=0; tindex < size; ++tindex)
            {
                int wscore = table.GetWhiteScore(tindex);
                int bscore = table.GetBlackScore(tindex);
                if (wscore >= TableGenerator.FriendMatedScore && wscore <= TableGenerator.EnemyMatedScore)
                    ++whiteHistogram[wscore - TableGenerator.FriendMatedScore];

                if (bscore >= TableGenerator.FriendMatedScore && bscore <= TableGenerator.EnemyMatedScore)
                    ++blackHistogram[bscore - TableGenerator.FriendMatedScore];
            }

            PrintHistogram("White", whiteHistogram);
            PrintHistogram("Black", blackHistogram);

            return 0;
        }

        private static void PrintHistogram(string title, int[] histogram)
        {
            Console.WriteLine("{0} histogram", title);
            int total = 0;
            int sum = 0;
            for (int score = TableGenerator.FriendMatedScore; score <= TableGenerator.EnemyMatedScore; ++score)
            {
                int count = histogram[score - TableGenerator.FriendMatedScore];
                if (count != 0)
                {
                    ++total;
                    sum += count;
                    Console.WriteLine("{0,5}  {1,12:n0}", score, count);
                }
            }
            Console.WriteLine("-----  ------------");
            Console.WriteLine("{0,5}  {1,12:n0}", total, sum);
            Console.WriteLine();
        }
    }
}
