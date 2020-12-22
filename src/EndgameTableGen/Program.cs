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

            Console.WriteLine(" Qq Rr Bb Nn Pp");

            var config = new int[2, 5];
            for (int n=0; n <= nonkings; ++n)
                PlanDistribute(config, n, 0, true);

            return 0;
        }

        const int NumNonKings = 5;    // 0=Q, 1=R, 2=B, 3=N, 4=P
        const int Q_INDEX = 0;
        const int R_INDEX = 1;
        const int B_INDEX = 2;
        const int N_INDEX = 3;
        const int P_INDEX = 4;

        const int WHITE = 0;
        const int BLACK = 1;

        static void PlanDistribute(int[,] config, int remaining, int mover, bool equal)
        {
            if (mover < NumNonKings)
            {
                for (int w = remaining; w >= 0; --w)
                {
                    config[WHITE, mover] = w;    // allocate w of these nonkings to White

                    // Enforce the integer [WQ, WR, WB, WN, WP] >= [BQ, BR, BB, BN, BP].
                    // For example, if there are 3 White Queens, 1 Black Knight, and 2 Black Bishops,
                    // 30000 > 00210, so we are OK.
                    int limit = remaining - w;
                    if (equal && limit > w)
                        limit = w;

                    // But if this is the last slot (Black Pawns)
                    // we must spend all of the remaining count if possible.

                    if (mover+1 == NumNonKings)
                    {
                        if (limit == remaining - w)
                        {
                            config[BLACK, mover] = limit;
                            PlanDistribute(config, 0, 1 + mover, equal);
                            config[BLACK, mover] = 0;
                        }
                    }
                    else
                    {
                        for (int b = limit; b >= 0; --b)
                        {
                            config[BLACK, mover] = b;       // allocate b of these nonkings to Black
                            PlanDistribute(config, remaining-(w+b), 1 + mover, equal && w==b);
                        }
                    }
                }
            }
            else
            {
                // Leaf node of the recursive search tree.
                if (IsCheckmatePossible(config))
                    PrintConfig(config);
            }
        }

        static void PrintConfig(int[,] config)
        {
            for (int m=0; m < NumNonKings; ++m)
                Console.Write(" {0}{1}", config[0,m], config[1,m]);

            Console.WriteLine();
        }

        static bool IsCheckmatePossible(int[,] config)
        {
            // This is based on the corresponding logic in Board.GetGameResult().
            int q = config[WHITE, Q_INDEX] + config[BLACK, Q_INDEX];
            int r = config[WHITE, R_INDEX] + config[BLACK, R_INDEX];
            int b = config[WHITE, B_INDEX] + config[BLACK, B_INDEX];
            int n = config[WHITE, N_INDEX] + config[BLACK, N_INDEX];
            int p = config[WHITE, P_INDEX] + config[BLACK, P_INDEX];
            return (q+r+p > 0) || (n+b > 1);
        }
    }
}
