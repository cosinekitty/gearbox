using System;
using Gearbox;

namespace PossibleMates
{
    class Program
    {
        static readonly int[] OffsetTable = MakeOffsetTable();

        static int Main(string[] args)
        {
            // Find all legal board positions with combinations
            // of two kings, zero or one white bishop/knight,
            // and zero or one black bishop/knight.
            int count = FindCheckmatePositions(Square.WQ);
            Console.WriteLine("Found {0} checkmates.", count);
            return 0;
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
                Console.WriteLine(board.ForsythEdwardsNotation());
                return 1;
            }

            return 0;
        }
    }
}
