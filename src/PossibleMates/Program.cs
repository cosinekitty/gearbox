using System;
using System.Collections.Generic;
using System.Linq;
using Gearbox;

namespace PossibleMates
{
    class Program
    {
        const string Usage = @"
USAGE:  PossibleMates [--terse | --exist] config

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

With the --exist option, only the FEN of the first checkmate found is printed.
If no checkmates are found, prints ""X"" and exits.

";

        static readonly int[] OffsetTable = MakeOffsetTable('1', '8');
        static readonly int[] PawnOffsetTable = MakeOffsetTable('2', '7');
        static bool TerseMode;
        static bool ExistenceCheck;

        static int Main(string[] args)
        {
            string config;
            if (args.Length == 1)
            {
                config = args[0];
            }
            else if (args.Length == 2 && args[0] == "--terse")
            {
                TerseMode = true;
                config = args[1];
            }
            else if (args.Length == 2 && args[0] == "--exist")
            {
                ExistenceCheck = true;
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
            {
                Console.WriteLine("{0}", count);
            }
            else if (ExistenceCheck)
            {
                if (count == 0)
                    Console.WriteLine("X");
            }
            else
            {
                Console.WriteLine("Found {0} checkmates for Kk{1}.", count, config);
            }
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
            return pieces.OrderBy(p => p).ToArray();        // sort to collect duplicates together
        }

        static int KingDistance(int ofs1, int ofs2)
        {
            int dx = Math.Abs((ofs1 % 10) - (ofs2 % 10));
            int dy = Math.Abs((ofs1 / 10) - (ofs2 / 10));
            return Math.Max(dx, dy);
        }

        static int[] MakeOffsetTable(char rank1, char rank2)
        {
            var table = new List<int>();
            for (char file = 'a'; file <= 'h'; ++file)
                for (char rank = rank1; rank <= rank2; ++rank)
                    table.Add(Board.Offset(file, rank));
            return table.ToArray();
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
                        count += Search(board, nonKingPieces, 0, -1);
                        if (ExistenceCheck && count > 0)
                            return count;
                    }
                }
            }
            return count;
        }

        static int Search(Board board, Square[] nonKingPieces, int depth, int prevTableIndex)
        {
            if (depth < nonKingPieces.Length)
            {
                Square piece = nonKingPieces[depth];
                int[] offsetTable = (piece == Square.WP || piece == Square.BP) ? PawnOffsetTable : OffsetTable;
                int count = 0;
                Square[] square = board.GetSquaresArray();
                int startIndex = 0;
                if (depth > 0 && nonKingPieces[depth-1] == piece)
                    startIndex = prevTableIndex + 1;    // avoid duplicate positions for duplicate pieces
                for (int i = startIndex; i < offsetTable.Length; ++i)
                {
                    int ofs = offsetTable[i];
                    if (square[ofs] == Square.Empty)
                    {
                        square[ofs] = piece;
                        count += Search(board, nonKingPieces, 1+depth, i);
                        square[ofs] = Square.Empty;
                        if (count > 0 && ExistenceCheck)
                            break;
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
