using System;
using Gearbox;

namespace PossibleMates
{
    class Program
    {
        static void Main(string[] args)
        {
            // Find all legal board positions with combinations
            // of two kings, zero or one white bishop/knight,
            // and zero or one black bishop/knight.
            int count = FindCheckmatePositions(Square.WQ);
            Console.WriteLine("Found {0} checkmates.", count);
        }

        static int FindCheckmatePositions(params Square[] nonKingPieces)
        {
            int count = 0;
            var board = new Board();
            board.Clear();
            board.SetTurn(false);
            for (char wk_file = 'a'; wk_file <= 'h'; ++wk_file)
            {
                for (char wk_rank = '1'; wk_rank <= '8'; ++wk_rank)
                {
                    board.Drop(Square.WK, wk_file, wk_rank);
                    for (char bk_file = 'a'; bk_file <= 'h'; ++bk_file)
                    {
                        int dx = Math.Abs(bk_file - wk_file);
                        for (char bk_rank = '1'; bk_rank <= '8'; ++bk_rank)
                        {
                            int dy = Math.Abs(bk_rank - wk_rank);
                            if (Math.Max(dx, dy) > 1)
                            {
                                board.Drop(Square.BK, bk_file, bk_rank);
                                count += Search(board, nonKingPieces, 0);
                                board.Lift(bk_file, bk_rank);
                            }
                        }
                    }
                    board.Lift(wk_file, wk_rank);
                }
            }
            return count;
        }

        static int Search(Board board, Square[] nonKingPieces, int depth)
        {
            if (depth < nonKingPieces.Length)
            {
                int count = 0;
                for (char file = 'a'; file <= 'h'; ++file)
                {
                    for (char rank = '1'; rank <= '8'; ++rank)
                    {
                        if (Square.Empty == board.Contents(file, rank))
                        {
                            board.Drop(nonKingPieces[depth], file, rank);
                            count += Search(board, nonKingPieces, 1+depth);
                            board.Lift(file, rank);
                        }
                    }
                }
                return count;
            }

            if (board.IsValidPosition())
            {
                if (!board.UncachedPlayerCanMove() && board.UncachedPlayerInCheck())
                {
                    Console.WriteLine(board.ForsythEdwardsNotation());
                    return 1;
                }
            }

            return 0;
        }
    }
}
