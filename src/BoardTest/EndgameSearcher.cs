using System;
using System.Collections.Generic;
using Gearbox;

namespace BoardTest
{
    internal interface IEndgamePositionVisitor
    {
        bool Start(Square[] nonKingPieces);
        bool Visit(Board board);        // returns false to signal failure
        bool Finish();
    }

    internal class EndgameSearcher
    {
        private Square[] nonKingPieces;
        private IEndgamePositionVisitor visitor;
        private Board board = new Board(true);      // create an empty board

        private static readonly int[] WholeBoardOffsetTable = MakeOffsetTable('a', 'h', '1', '8');

        public EndgameSearcher()
        {
        }

        public bool Search(Square[] nonKingPieces, IEndgamePositionVisitor visitor)
        {
            this.nonKingPieces = nonKingPieces;
            this.visitor = visitor;
            board.Clear(true);

            if (!visitor.Start(nonKingPieces))
                return false;

            // Generate every legal board configuration for this set of pieces.
            // Do a shallow search using egThinker, which has endgame tables loaded.
            // If it reports a forced win, and the forced win is within a reasonable
            // search horizon, repeat the search with a brute force search (bfThinker).
            // Verify that both scores match.

            foreach (int wkofs in WholeBoardOffsetTable)
            {
                board.PlaceWhiteKing(wkofs);
                foreach (int bkofs in WholeBoardOffsetTable)
                {
                    if (KingDistance(wkofs, bkofs) > 1)
                    {
                        board.PlaceBlackKing(bkofs);
                        if (!EndgameSearchDepth(0))
                            return false;   // FAILURE!
                    }
                }
            }

            return visitor.Finish();
        }

        private bool EndgameSearchDepth(int depth)
        {
            if (depth == nonKingPieces.Length)
            {
                // We have placed all the pieces on the board.
                // Assess this position with both White and Black to move.
                board.RefreshAfterDangerousChanges();

                // What if it is White's turn to move?
                board.SetTurn(true);
                if (board.IsValidPosition())
                    if (!visitor.Visit(board))
                        return false;

                // What if it is Black's turn to move?
                board.SetTurn(false);
                if (board.IsValidPosition())
                    if (!visitor.Visit(board))
                        return false;
            }
            else
            {
                // Recurse to put the remaining pieces on the board in every possible configuration.
                Square[] square = board.GetSquaresArray();
                foreach (int ofs in WholeBoardOffsetTable)
                {
                    if (square[ofs] == Square.Empty)
                    {
                        square[ofs] = nonKingPieces[depth];
                        if (!EndgameSearchDepth(1 + depth))
                            return false;
                        square[ofs] = Square.Empty;
                    }
                }
            }

            return true;
        }


        private static int[] MakeOffsetTable(char file1, char file2, char rank1, char rank2)
        {
            var table = new List<int>();
            for (char rank = rank1; rank <= rank2; ++rank)
                for (char file = file1; file <= file2; ++file)
                    table.Add(Board.Offset(file, rank));
            return table.ToArray();
        }

        private static int KingDistance(int ofs1, int ofs2)
        {
            int dx = Math.Abs((ofs1 % 10) - (ofs2 % 10));
            int dy = Math.Abs((ofs1 / 10) - (ofs2 / 10));
            return Math.Max(dx, dy);
        }
    }
}
