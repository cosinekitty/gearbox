using System;
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
";

        static int Main(string[] args)
        {
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
                    return new TableGenerator();

                default:
                    return null;
            }
        }
    }
}
