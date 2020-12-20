using System;
using Gearbox;

namespace PossibleMates
{
    class Program
    {
        const string Usage = @"
USAGE:  PossibleMates [--terse] config

Where config is one or more of the following characters:

    P = White Pawn
    N = White Knight
    B = White Bishop
    R = White Rook
    Q = White Queen

    p = Black Pawn
    n = Black Knight
    b = Black Bishop
    r = Black Rook
    q = Black Queen

This program tries every legal combination of the specified
pieces on the board, with Black to move. It then finds all
positions where Black is checkmated.

By default, all the FEN positions are printed, followed by
the number of checkmates.

With the --terse option, only the number of checkmates is printed.

";

        static readonly int[] OffsetTable = MakeOffsetTable();
        static bool TerseMode;

        static int Main(string[] args)
        {
            string config;
            if (args.Length == 1)
            {
                TerseMode = false;
                config = args[0];
            }
            else if (args.Length == 2 && args[0] == "--terse")
            {
                TerseMode = true;
                config = args[1];
            }
            else
            {
                Console.WriteLine(Usage);
                return 1;
            }

            Square[] nonKingPieces = ParseNonKingPieces(config);
            int count = FindCheckmatePositions(nonKingPieces);
            if (TerseMode)
                Console.WriteLine("{0}", count);
            else
                Console.WriteLine("Found {0} checkmates for Kk{1}.", count, config);
            return 0;
        }

        static Square[] ParseNonKingPieces(string config)
        {
            var pieces = new Square[config.Length];
            for (int i=0; i < config.Length; ++i)
            {
                switch (config[i])
                {
                    case 'P':   pieces[i] = Square.WP;  break;
                    case 'N':   pieces[i] = Square.WN;  break;
                    case 'B':   pieces[i] = Square.WB;  break;
                    case 'R':   pieces[i] = Square.WR;  break;
                    case 'Q':   pieces[i] = Square.WQ;  break;
                    case 'p':   pieces[i] = Square.BP;  break;
                    case 'n':   pieces[i] = Square.BN;  break;
                    case 'b':   pieces[i] = Square.BB;  break;
                    case 'r':   pieces[i] = Square.BR;  break;
                    case 'q':   pieces[i] = Square.BQ;  break;
                    default:
                        throw new ArgumentException("Invalid non-king piece character: " + config[i]);
                }
            }
            return pieces;
        }

        static int KingDistance(int ofs1, int ofs2)
        {
            int dx = Math.Abs((ofs1 % 10) - (ofs2 % 10));
            int dy = Math.Abs((ofs1 / 10) - (ofs2 / 10));
            return Math.Max(dx, dy);
        }

        static int[] MakeOffsetTable()
        {
            var table = new int[64];
            int index = 0;
            for (char file = 'a'; file <= 'h'; ++file)
                for (char rank = '1'; rank <= '8'; ++rank)
                    table[index++] = Board.Offset(file, rank);
            return table;
        }

        static int FindCheckmatePositions(params Square[] nonKingPieces)
        {
            int count = 0;
            var board = new Board(false);   // make an empty board where it is Black's turn

            foreach (int wkofs in OffsetTable)
            {
                board.PlaceWhiteKing(wkofs);
                foreach (int bkofs in OffsetTable)
                {
                    if (KingDistance(wkofs, bkofs) > 1)
                    {
                        board.PlaceBlackKing(bkofs);
                        count += Search(board, nonKingPieces, 0);
                    }
                }
            }
            return count;
        }

        static int Search(Board board, Square[] nonKingPieces, int depth)
        {
            if (depth < nonKingPieces.Length)
            {
                int count = 0;
                Square[] square = board.GetSquaresArray();
                foreach (int ofs in OffsetTable)
                {
                    if (square[ofs] == Square.Empty)
                    {
                        square[ofs] = nonKingPieces[depth];
                        count += Search(board, nonKingPieces, 1+depth);
                        square[ofs] = Square.Empty;
                    }
                }
                return count;
            }

            if (board.IsValidPosition() && board.IsCheckmate())
            {
                if (!TerseMode)
                    Console.WriteLine(board.ForsythEdwardsNotation());
                return 1;
            }

            return 0;
        }
    }
}
