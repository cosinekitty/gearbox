using System;
using System.Collections.Generic;
using Gearbox;

namespace EndgameTableGen
{
    internal static class ParentPositionGenerator
    {
        public static void GenParentList(List<int> parentTableIndexList, Board board)
        {
            parentTableIndexList.Clear();

            // We are given the board of a "child" position.
            // We need to find the unique set of table indexes of all the positions that
            // lead to this child position via a legal move.
            // Each parent position must itself be a legal position.
            // If it is White's turn to move in the child position, then
            // we arrive there via a list moves made by Black, from every
            // position where it was Black's turn to move.
            // We are staying inside the same table, which means we ignore
            // the possibility of undoing captures or pawn promotions.

            Square[] square = board.GetSquaresArray();
            Square parentSide = board.IsWhiteTurn ? Square.Black : Square.White;
            Square childSide  = board.IsWhiteTurn ? Square.White : Square.Black;

            int ep = board.GetEpTarget();
            if (ep == 0)
            {
                // No en passant target is set.
                // Find every parent piece that could have moved.
                foreach (int ofs in TableGenerator.WholeBoardOffsetTable)
                {
                    if (0 != (square[ofs] & parentSide))
                    {
                        switch (square[ofs] & Square.PieceMask)
                        {
                            case Square.King:
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.E);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NE);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.N);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.W);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.S);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SE);
                                break;

                            case Square.Queen:
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.E);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NE);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.N);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NW);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.W);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SW);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.S);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SE);
                                break;

                            case Square.Rook:
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.E);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.N);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.W);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.S);
                                break;

                            case Square.Bishop:
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NE);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NW);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SW);
                                AppendRay(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SE);
                                break;

                            case Square.Knight:
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NEE);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NNE);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NNW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.NWW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SWW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SSW);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SSE);
                                AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.SEE);
                                break;

                            case Square.Pawn:
                                // Because en passant target is not set, we must exclude pawn moves of two squares.
                                // The pawn could only have moved one square to get where it is.
                                if (parentSide == Square.White)
                                {
                                    if (ofs / 10 != 3)      // White pawns on rank 2 cannot have moved there (that's their home rank).
                                        AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.S);
                                }
                                else
                                {
                                    if (ofs / 10 != 8)      // Black pawns on rank 7 cannot have moved there (that's their home rank).
                                        AppendSingle(parentTableIndexList, board, ofs, parentSide, childSide, Direction.N);
                                }
                                break;

                            default:
                                throw new Exception($"Invalid square contents {square[ofs]} at offset {ofs}");
                        }
                    }
                }
            }
            else
            {
                // Special case: the en passant target is set. Therefore the only move
                // that could have possibly preceeded this one is a double-pawn-move.
                if (parentSide == Square.White)
                {
                    if (ep / 10 != 4)
                        throw new Exception($"Invalid en passant target {ep} for White.");

                    if (square[ep + Direction.N] != Square.WP)
                        throw new Exception($"Expected White pawn in front of en passant target {ep}");

                    if (square[ep] != Square.Empty || square[ep + Direction.S] != Square.Empty)
                        throw new Exception($"Squares behind White pawn are not empty.");

                    AppendSingle(parentTableIndexList, board, ep + Direction.N, parentSide, childSide, 2 * Direction.S);
                }
                else
                {
                    if (ep / 10 != 7)
                        throw new Exception($"Invalid en passant target {ep} for Black.");

                    if (square[ep + Direction.S] != Square.BP)
                        throw new Exception($"Expected Black pawn in front of en passant target {ep}");

                    if (square[ep] != Square.Empty || square[ep + Direction.N] != Square.Empty)
                        throw new Exception($"Squares behind Black pawn are not empty.");

                    AppendSingle(parentTableIndexList, board, ep + Direction.S, parentSide, childSide, 2 * Direction.N);
                }
            }
            board.SetEpTarget(ep);      // always restore the original board state, including ep target
        }

        private static void AppendIndexesForPosition(
            List<int> parentTableIndexList,
            Board board)
        {
            Square[] square = board.GetSquaresArray();

            // There can be more than one table index that corresponds with
            // a given arrangement of pieces, because of en passant.
            // The en passant target can be 0 (meaning a pawn was not just moved 2 squares),
            // or it can be set to be behind any pawn on the player's fourth rank, when
            // both squares behind the pawn are empty.

            // Start with the always-available option: no en passant.
            // Note that the original en passant state has already been preserved,
            // and will be restored, so we don't need to do so here.
            board.SetEpTarget(0);
            int pindex = board.GetEndgameTableIndex(false);
            if (!parentTableIndexList.Contains(pindex))
                parentTableIndexList.Add(pindex);

            if (board.IsWhiteTurn)
            {
                // Look for every Black pawn that could have just moved two squares forward.
                // There are only 8 squares where they could be.
                for (int ofs = 61; ofs <= 68; ++ofs)
                {
                    if (square[ofs] == Square.BP &&
                        square[ofs + Direction.N] == Square.Empty &&
                        square[ofs + 2*Direction.N] == Square.Empty)
                    {
                        board.SetEpTarget(ofs + Direction.N);
                        pindex = board.GetEndgameTableIndex(false);
                        if (!parentTableIndexList.Contains(pindex))
                            parentTableIndexList.Add(pindex);
                    }
                }
            }
            else
            {
                // Look for every White pawn that could have just moved two squares forward.
                // There are only 8 squares where they could be.
                for (int ofs = 51; ofs <= 58; ++ofs)
                {
                    if (square[ofs] == Square.WP &&
                        square[ofs + Direction.S] == Square.Empty &&
                        square[ofs + 2*Direction.S] == Square.Empty)
                    {
                        board.SetEpTarget(ofs + Direction.S);
                        pindex = board.GetEndgameTableIndex(false);
                        if (!parentTableIndexList.Contains(pindex))
                            parentTableIndexList.Add(pindex);
                    }
                }
            }
        }

        private static void AppendSingle(
            List<int> parentTableIndexList,
            Board board,
            int ofs,
            Square parentSide,
            Square childSide,
            int dir)
        {
            Square[] square = board.GetSquaresArray();
            if (square[ofs + dir] != Square.Empty)
                return;

            board.SetTurn(!board.IsWhiteTurn);
            switch (square[ofs])
            {
                case Square.WK:
                    board.PlaceWhiteKing(ofs + dir);
                    if (board.IsValidPosition())
                        AppendIndexesForPosition(parentTableIndexList, board);
                    board.PlaceWhiteKing(ofs);
                    break;

                case Square.BK:
                    board.PlaceBlackKing(ofs + dir);
                    if (board.IsValidPosition())
                        AppendIndexesForPosition(parentTableIndexList, board);
                    board.PlaceBlackKing(ofs);
                    break;

                case Square.Empty:
                case Square.Offboard:
                    throw new Exception($"Square at {ofs} contains invalid value {square[ofs]}.");

                default:
                    square[ofs + dir] = square[ofs];
                    square[ofs] = Square.Empty;
                    if (board.IsValidPosition())
                        AppendIndexesForPosition(parentTableIndexList, board);
                    square[ofs] = square[ofs + dir];
                    square[ofs + dir] = Square.Empty;
                    break;
            }
            board.SetTurn(!board.IsWhiteTurn);
        }

        private static void AppendRay(
            List<int> parentTableIndexList,
            Board board,
            int ofs,
            Square parentSide,
            Square childSide,
            int dir)
        {
            board.SetTurn(!board.IsWhiteTurn);
            Square[] square = board.GetSquaresArray();
            Square mover = square[ofs];

            switch (mover)
            {
                case Square.WK:
                    for (int origin = ofs + dir; square[origin] == Square.Empty; origin += dir)
                    {
                        board.PlaceWhiteKing(origin);
                        if (board.IsValidPosition())
                            AppendIndexesForPosition(parentTableIndexList, board);
                    }
                    board.PlaceWhiteKing(ofs);
                    break;

                case Square.BK:
                    for (int origin = ofs + dir; square[origin] == Square.Empty; origin += dir)
                    {
                        board.PlaceBlackKing(origin);
                        if (board.IsValidPosition())
                            AppendIndexesForPosition(parentTableIndexList, board);
                    }
                    board.PlaceBlackKing(ofs);
                    break;

                case Square.Empty:
                case Square.Offboard:
                    throw new Exception($"Square at {ofs} contains invalid value {square[ofs]}.");

                default:
                    square[ofs] = Square.Empty;
                    for (int origin = ofs + dir; square[origin] == Square.Empty; origin += dir)
                    {
                        square[origin] = mover;
                        if (board.IsValidPosition())
                            AppendIndexesForPosition(parentTableIndexList, board);
                        square[origin] = Square.Empty;
                    }
                    square[ofs] = mover;
                    break;
            }

            board.SetTurn(!board.IsWhiteTurn);
        }
    }
}
