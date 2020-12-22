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
";

        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "plan")
            {
                int nonkings;
                if (int.TryParse(args[1], out nonkings) && (nonkings >= 1) && (nonkings <= 9))
                    return Plan(nonkings);

                Console.WriteLine("Invalid number of non-kings: {0}", args[1]);
                return 1;
            }

            Console.WriteLine(UsageText);
            return 1;
        }

        static int Plan(int nonkings)
        {
            // Terminology: there are three kinds of objects on the chess board.
            // Just in this code, I use these words very specifically: king, piece, pawn.
            // king  = A king; White or Black.
            // piece = A queen, rook, bishop, or knight; White or Black.
            // pawn  = A pawn; White or Black.
            // I use the word "mover" to represent any of the above objects.
            // A "nonking" is any mover that is not a king.

            // Find every possible way to spread the specified number of nonkings.
            // Generate configurations for a given nonking count in ascending order of pawn count.
            // Thus all configurations with 0 pawns must be generated before all with 1 pawn,
            // all with 1 pawn before all with 2 pawns, etc.
            // This is because a pawn can be promoted to a piece, so any configuration
            // with N pawns and M pieces depends on all configurations with
            // (N-1) pawns and (M+1) pieces.
            // A pawn or a piece can be captured, so each [N, M] depends on [N-1, M] and [N, M-1] also.

            Console.WriteLine("    table   Qq Rr Bb Nn Pp");

            var planner = new WorkPlanner();
            planner.Plan(nonkings);
            return 0;
        }
    }
}
