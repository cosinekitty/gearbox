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

            int ep = board.GetEpTarget();
            if (ep == 0)
            {
                // No en passant target is set.
                // This can happen for two reasons:
                // 1. The previous move was not a pawn moving two squares forward.
                // 2. We are in a configuration where at least one side doesn't have any pawns,
                //    in which case the table indexes are generated without regard to en passant.
                //    We have to know the difference, so we know whether to include or exclude
                //    parent positions which moved a pawn two squares forward to reach this position.
                // See logic inside AppendPawn() that handles both cases.

                // Find every parent piece that could have moved.
                foreach (int ofs in TableGenerator.WholeBoardOffsetTable)
                {
                    if (0 != (square[ofs] & parentSide))
                    {
                        switch (square[ofs] & Square.PieceMask)
                        {
                            case Square.King:
                                AppendSingle(parentTableIndexList, board, ofs, Direction.E);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NE);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.N);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.W);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.S);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SE);
                                break;

                            case Square.Queen:
                                AppendRay(parentTableIndexList, board, ofs, Direction.E);
                                AppendRay(parentTableIndexList, board, ofs, Direction.NE);
                                AppendRay(parentTableIndexList, board, ofs, Direction.N);
                                AppendRay(parentTableIndexList, board, ofs, Direction.NW);
                                AppendRay(parentTableIndexList, board, ofs, Direction.W);
                                AppendRay(parentTableIndexList, board, ofs, Direction.SW);
                                AppendRay(parentTableIndexList, board, ofs, Direction.S);
                                AppendRay(parentTableIndexList, board, ofs, Direction.SE);
                                break;

                            case Square.Rook:
                                AppendRay(parentTableIndexList, board, ofs, Direction.E);
                                AppendRay(parentTableIndexList, board, ofs, Direction.N);
                                AppendRay(parentTableIndexList, board, ofs, Direction.W);
                                AppendRay(parentTableIndexList, board, ofs, Direction.S);
                                break;

                            case Square.Bishop:
                                AppendRay(parentTableIndexList, board, ofs, Direction.NE);
                                AppendRay(parentTableIndexList, board, ofs, Direction.NW);
                                AppendRay(parentTableIndexList, board, ofs, Direction.SW);
                                AppendRay(parentTableIndexList, board, ofs, Direction.SE);
                                break;

                            case Square.Knight:
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NEE);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NNE);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NNW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.NWW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SWW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SSW);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SSE);
                                AppendSingle(parentTableIndexList, board, ofs, Direction.SEE);
                                break;

                            case Square.Pawn:
                                AppendPawn(parentTableIndexList, board, ofs);
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
                // that could have possibly preceeded this one is pawn moving two squares from its home rank.
                AppendPawnDoubleMove(parentTableIndexList, board, ep);
            }
            board.SetEpTarget(ep);      // always restore the original board state, including ep target
        }

        private static void AppendPawnDoubleMove(List<int> parentTableIndexList, Board board, int ep)
        {
            Square[] square = board.GetSquaresArray();
            int ep_target_rank = (ep / 10) - 1;
            if (board.IsBlackTurn)      // are parents of this position White?
            {
                if (ep_target_rank != 3)
                    throw new Exception($"Invalid en passant target {ep} for White.");

                if (square[ep + Direction.N] != Square.WP)
                    throw new Exception($"Expected White pawn in front of en passant target {ep}");

                if (square[ep] != Square.Empty || square[ep + Direction.S] != Square.Empty)
                    throw new Exception($"Squares behind White pawn are not empty.");

                AppendSingle(parentTableIndexList, board, ep + Direction.N, 2 * Direction.S);
            }
            else
            {
                if (ep_target_rank != 6)
                    throw new Exception($"Invalid en passant target {ep} for Black.");

                if (square[ep + Direction.S] != Square.BP)
                    throw new Exception($"Expected Black pawn in front of en passant target {ep}");

                if (square[ep] != Square.Empty || square[ep + Direction.N] != Square.Empty)
                    throw new Exception($"Squares behind Black pawn are not empty.");

                AppendSingle(parentTableIndexList, board, ep + Direction.S, 2 * Direction.N);
            }
        }

        private static void AppendPawn(List<int> parentTableIndexList, Board board, int ofs)
        {
            Square[] square = board.GetSquaresArray();
            int rank = (ofs / 10) - 1;
            if (board.IsBlackTurn)      // are parents of this position White?
            {
                // White pawns on rank 2 cannot have moved there (that's their home rank).
                // Also, check for empty square, redundantly with first AppendSingle call's internal logic,
                // so that we don't have to check again before second call.
                if (rank != 2 && square[ofs + Direction.S] == Square.Empty)
                {
                    AppendSingle(parentTableIndexList, board, ofs, Direction.S);

                    // If both sides have pawns, then the table generator would have set en passant target
                    // when a pawn double-move was implied. Therefore, we can only generate double moves here
                    // when only one side has pawn(s).
                    if (rank == 4 && square[ofs + 2 * Direction.S] == Square.Empty && !board.BothSidesHavePawns())
                        AppendSingle(parentTableIndexList, board, ofs, 2 * Direction.S);
                }
            }
            else
            {
                // Black pawns on rank 7 cannot have moved there (that's their home rank).
                // Also, check for empty square, redundantly with first AppendSingle call's internal logic,
                // so that we don't have to check again before second call.
                if (rank != 7 && square[ofs + Direction.N] == Square.Empty)
                {
                    AppendSingle(parentTableIndexList, board, ofs, Direction.N);

                    // If both sides have pawns, then the table generator would have set en passant target
                    // when a pawn double-move was implied. Therefore, we can only generate double moves here
                    // when only one side has pawn(s).
                    if (rank == 5 && square[ofs + 2 * Direction.N] == Square.Empty && !board.BothSidesHavePawns())
                        AppendSingle(parentTableIndexList, board, ofs, 2 * Direction.N);
                }
            }
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

            // Unless both sides have at least one pawn, it is a waste of time
            // to try to find different parent positions based on en passant state.
            // This is because the endgame table generator does not treat a position
            // as different just because a pawn moved two squares, when only one
            // side has pawns, because en passant captures are not possible anyway.
            // Without the call to board.BothSidesHavePawns(), the code will not
            // do anything different; it will just be slower, because
            // parentTableIndexList.Contains(pindex) will always be true below;
            // the pindex will be the same value as we just added above.
            if (board.BothSidesHavePawns())
            {
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
        }

        private static void AppendSingle(
            List<int> parentTableIndexList,
            Board board,
            int ofs,
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
