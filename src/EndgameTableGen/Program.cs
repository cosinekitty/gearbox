﻿using System;
using System.IO;
using System.Numerics;
using Gearbox;

namespace EndgameTableGen
{
    // See notes in document:
    // https://docs.google.com/document/d/1oe3dbQlsrfpdWhEvbcRAmaDjr0vOFTpFWAfDFGgTfes

    class Program
    {
        const string UsageText = @"
Endgame tablebase generator for the Gearbox chess engine.

USAGE:

EndgameTableGen plan N
    Prints the series of endgame database tables to be generated,
    in dependency order, for all possible configurations of
    up to N non-king pieces/pawns, where N = 1..9.

EndgameTableGen gen N
    Generates all tables for up to N non-king movers.
    If the program is interrupted and resumed, will
    load all the tables that were completed and resume
    at the first unwritten table.

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

EndgameTableGen decode config_id table_index side_to_move
    Prints the FEN of a board configuration corresponding to
    the given configuration and table index.
    config_id    = decimal integer QqRrBbNnPp.
    table_index  = integer offset into the table.
    side_to_move = 'w' or 'b'.
";

        static int Main(string[] args)
        {
            if ((args.Length == 2) && (args[0] == "list"))
            {
                return ListTable(args[1]);
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
                if (!int.TryParse(args[1], out int nonkings) || (nonkings < 1) && (nonkings > 9))
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

                var planner = new WorkPlanner(worker);
                planner.Plan(nonkings);
                return 0;
            }

            Console.WriteLine(UsageText);
            return 1;
        }

        private static int MaxTableSize(int nonkings)
        {
            var worker = new MaxTableSizeFinder();
            var planner = new WorkPlanner(worker);
            planner.Plan(nonkings);
            TableWorker.Log("For nonkings={0}, max table size = {1:n0}", nonkings, worker.MaxTableSize);
            return (int)worker.MaxTableSize;
        }

        private class MaxTableSizeFinder : TableWorker
        {
            public BigInteger MaxTableSize;

            public override void Start()
            {
                MaxTableSize = 0;
            }

            public override void GenerateTable(int[,] config)
            {
                BigInteger size = TableSize(config);
                if (size > MaxTableSize)
                    MaxTableSize = size;
            }

            public override void Finish()
            {
            }
        }

        static int ListTable(string filename)
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
            Table table = Table.Load(filename, size);
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
    }
}
