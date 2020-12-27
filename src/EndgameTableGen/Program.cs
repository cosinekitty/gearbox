using System;
using System.IO;
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
";

        static int Main(string[] args)
        {
            if ((args.Length == 2) && (args[0] == "list"))
            {
                return ListTable(args[1]);
            }

            if ((args.Length == 2) && (MakeWorker(args[0]) is ITableWorker worker))
            {
                int nonkings;
                if (int.TryParse(args[1], out nonkings) && (nonkings >= 1) && (nonkings <= 9))
                {
                    var planner = new WorkPlanner(worker);
                    planner.Plan(nonkings);
                    return 0;
                }

                Console.WriteLine("Invalid number of non-kings: {0}", args[1]);
                return 1;
            }

            Console.WriteLine(UsageText);
            return 1;
        }

        static ITableWorker MakeWorker(string kind)
        {
            switch (kind)
            {
                case "plan":
                    return new TablePrinter();

                case "gen":
                    return new TableGenerator { EnableTableGeneration = true, EnableSelfCheck = true };

                case "test":
                    return new TableGenerator { EnableTableGeneration = false, EnableSelfCheck = true };

                default:
                    return null;
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
            var worker = new TableGenerator();
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
