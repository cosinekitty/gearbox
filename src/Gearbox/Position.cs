using System;
using System.Diagnostics;

namespace Gearbox
{
    // Represents the board by a normalized view of its piece locations.
    public class Position
    {
        public const int WK =  0;
        public const int BK =  1;
        public const int WQ =  2;
        public const int BQ =  3;
        public const int WR =  4;
        public const int BR =  5;
        public const int WB =  6;
        public const int BB =  7;
        public const int WN =  8;
        public const int BN =  9;
        public const int WP = 10;
        public const int BP = 11;
        public const int NumPieceTypes = 12;

        public readonly PieceLocationList[] PieceList;
        public int EpTargetOffset;

        public Position()
        {
            PieceList = new PieceLocationList[NumPieceTypes];
            for (int i=0; i < NumPieceTypes; ++i)
                PieceList[i] = new PieceLocationList();
        }

        public void Clear()
        {
            for (int i=0; i < NumPieceTypes; ++i)
                PieceList[i].Clear();
        }

        public void Append(Square piece, int ofs)
        {
            int slot = ArraySlotForPiece(piece);
            PieceList[slot].Append(ofs);
        }

        public void ApplyTransform(Transform transform)
        {
            foreach (PieceLocationList list in PieceList)
                list.ApplyTransform(transform);
        }

        public static int ArraySlotForPiece(Square piece)
        {
            switch (piece)
            {
                case Square.WK: return WK;
                case Square.BK: return BK;
                case Square.WQ: return WQ;
                case Square.BQ: return BQ;
                case Square.WR: return WR;
                case Square.BR: return BR;
                case Square.WB: return WB;
                case Square.BB: return BB;
                case Square.WN: return WN;
                case Square.BN: return BN;
                case Square.WP: return WP;
                case Square.BP: return BP;
                default:
                    throw new ArgumentException(string.Format("Invalid piece: {0}", piece));
            }
        }

        public static Square PieceForArraySlot(int slot)
        {
            switch (slot)
            {
                case WK: return Square.WK;
                case BK: return Square.BK;
                case WQ: return Square.WQ;
                case BQ: return Square.BQ;
                case WR: return Square.WR;
                case BR: return Square.BR;
                case WB: return Square.WB;
                case BB: return Square.BB;
                case WN: return Square.WN;
                case BN: return Square.BN;
                case WP: return Square.WP;
                case BP: return Square.BP;
                default:
                    throw new ArgumentException(string.Format("Invalid slot index: {0}", slot));
            }
        }

        public int FirstOffDiagonalHeight(Transform transform)
        {
            foreach (PieceLocationList list in PieceList)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    int flip = Symmetry.ForwardTransform(list.Array[i].Index, transform);
                    int height = DiagonalHeight(flip);
                    if (height != 0)
                        return height;
                }
            }
            return 0;       // Everything is on the a1..h8 diagonal.
        }

        private static readonly int[] EightfoldSymmetryLookup = MakeKingSymmetryLookup();
        private static int[] MakeKingSymmetryLookup()
        {
            var lookup = new int[64];

            for (int i=0; i < lookup.Length; ++i)
                lookup[i] = -1;

            int count = 0;
            for (int y = 0; y < 4; ++y)
                for (int x = y; x < 4; ++x)
                    lookup[x + 8*y] = count++;

            return lookup;
        }

        private static readonly int[] LeftRightSymmetryLookup = MakeLeftRightSymmetryLookup();
        private static int[] MakeLeftRightSymmetryLookup()
        {
            var lookup = new int[64];
            for (int i=0; i < lookup.Length; ++i)
                lookup[i] = -1;

            int count = 0;
            for (int y=0; y < 8; ++y)
                for (int x=0; x < 4; ++x)
                    lookup[x + 8*y] = count++;

            return lookup;
        }

        public static int DiagonalHeight(int index)
        {
            // Returns how far above or below the a1..h8 diagonal the given index is.
            // If the return value is 0, the index is on that diagonal.
            // If it is positive, the index is northwest of the diagonal.
            // If it is negative, the index is southeast of the diagonal.
            if (index < 0 || index > 63)
                throw new ArgumentException(string.Format("Invalid index {0}", index));
            int y = index / 8;
            int x = index % 8;
            return y - x;
        }

        public static int IndexFromOffset(int offset)
        {
            // Convert square[] offset 21..98 to tablebase index 0..63.
            int x = (offset % 10) - 1;
            int y = (offset / 10) - 2;
            return 8*y + x;
        }

        public static int OffsetFromIndex(int index)
        {
            // Convert tablebase index 0..63 to square[] offset 21..98.
            int x = index % 8;
            int y = index / 8;
            return 21 + x + 10*y;
        }

        public int GetEndgameTableIndex()
        {
            Debug.Assert(PieceList[Position.WK].Count == 1);
            Debug.Assert(PieceList[Position.BK].Count == 1);

            int wkofs = PieceList[Position.WK].Array[0].Offset;
            int bkofs = PieceList[Position.BK].Array[0].Offset;

            int wp = PieceList[Position.WP].Count;
            int bp = PieceList[Position.BP].Count;
            int pawns = wp + bp;

            int wkindex = IndexFromOffset(wkofs);
            int bkindex = IndexFromOffset(bkofs);
            int wkflip = -1;
            int bkflip = -1;
            Transform best_transform = Transform.Undefined;
            int tindex;
            if (pawns == 0)
            {
                // When there are no pawns, we get the most symmetry benefit
                // from forcing the white king from a realm of 64 squares
                // int a realm of 10 squares.
                for (int t = 0; t < 8; ++t)
                {
                    Transform transform = (Transform)t;
                    int try_wkflip = Symmetry.ForwardTransform(wkindex, transform);
                    int try_bkflip = Symmetry.ForwardTransform(bkindex, transform);
                    if (EightfoldSymmetryLookup[try_wkflip] >= 0)
                    {
                        // Depending on the position, there are either 1 or 2 distinct
                        // transforms that flip the white king onto one of the magic group of 10 squares.
                        // The double case is when the white king is on the diagonal a1..h8.
                        // In that case, look for the first piece that is off the diagonal.
                        // If such a piece exists, pick the transform
                        // that keeps it on or below the diagonal. If no such piece exists
                        // (everything is on the diagonal), then there is no ambiguity.

                        int wk_diag = DiagonalHeight(try_wkflip);
                        Debug.Assert(wk_diag <= 0);
                        if (wk_diag == 0)
                        {
                            int p_diag = FirstOffDiagonalHeight(transform);
                            if (p_diag > 0)
                                continue;   // Eliminate this redundant transform... try the next one.
                        }

                        // We have found the best transform.
                        wkflip = try_wkflip;
                        bkflip = try_bkflip;
                        best_transform = transform;
                    }
                }

                if (wkflip == -1)
                    throw new Exception("Did not find a solution.");

                tindex = 64*EightfoldSymmetryLookup[wkflip] + bkflip;
            }
            else
            {
                // When there are pawns on the board, we can only use left/right symmetry.
                // If the White King is on the right side of the board (files e..h),
                // flip the board so that it moves to the left side (files a..d).
                if (LeftRightSymmetryLookup[wkindex] >= 0)
                    best_transform = Transform.Identity;
                else
                    best_transform = Transform.LeftRight;

                wkflip = Symmetry.ForwardTransform(wkindex, best_transform);
                bkflip = Symmetry.ForwardTransform(bkindex, best_transform);
                tindex = 64*LeftRightSymmetryLookup[wkflip] + bkflip;
            }

            // Apply the best transform to all the pieces in the position.
            ApplyTransform(best_transform);

            // The pieces that are not kings and not pawns are all the same.
            // They all contribute a base-64 digit to the table index.
            for (int p = Position.WQ; p < Position.WP; ++p)
            {
                PieceLocationList list = PieceList[p];
                for (int i = 0; i < list.Count; ++i)
                    tindex = 64*tindex + list.Array[i].Index;
            }

            // Pawns are different for two reasons:
            // 1. They exist in only one of 48 squares, not one of 64.
            // 2. But they can be in an en passant target state, in which case
            //    we treat them as existing in an additional 8 possible states,
            //    for a total of 56 quasi-squares.
            // So we save some table space by using a base-56 digit rather
            // than a base-64 digit for each pawn.
            // I considered adding an extra base-9 digit to encode the
            // en passant target file (none, or a..h), but even if there
            // were 8 pawns on the board, this would add more to the table size:
            // (56/48)^8 = 3.432213672648988 < 9.
            // This puzzled me at first, but then I realized that the
            // former approach benefits from the fact that only pawns
            // on their player's fourth rank can be in the en passant state.

            // Optimization: en passant is NOT possible unless both sides have at least one pawn.
            bool isEnPassantPossible = (wp > 0 && bp > 0);
            int pawnFactor = isEnPassantPossible ? 56 : 48;

            // Encode the white pawns.
            PieceLocationList wp_list = PieceList[Position.WP];
            for (int i=0; i < wp_list.Count; ++i)
            {
                PieceLocation loc = wp_list.Array[i];
                int digit = loc.Index - 8;     // pawns can't be on first rank
                if (isEnPassantPossible && (loc.Offset + Direction.S == EpTargetOffset))
                {
                    // Any white pawn that has just moved two squares forward
                    // (whether or not Black can capture it en passant)
                    // must have its digit adjusted from White's fourth rank
                    // to a virtual eighth rank.
                    digit += 32;    // adjust to the range 48..55.
                }
                tindex = (pawnFactor * tindex) + digit;
            }

            // Encode the black pawns.
            PieceLocationList bp_list = PieceList[Position.BP];
            for (int i=0; i < bp_list.Count; ++i)
            {
                PieceLocation loc = bp_list.Array[i];
                int digit = loc.Index - 8;    // pawns can't be on first rank
                if (isEnPassantPossible && (loc.Offset + Direction.N == EpTargetOffset))
                {
                    // Any black pawn that has just moved two squares forward
                    // (whether or not White can capture it en passant)
                    // must have its digit adjusted from Black's fourth rank
                    // to a virtual eighth rank.
                    // Tricky: Black's fourth rank is White's fifth rank,
                    // so the adjustment is a little different!
                    digit += 24;    // adjust to the range 48..55.
                }
                tindex = (pawnFactor * tindex) + digit;
            }

            return tindex;
        }
    }

    public class PieceLocationList
    {
        public const int MaxCount = 10;     // Example: if all 8 pawns are promoted to rooks, you could have 10 rooks.
        public int Count;
        public readonly PieceLocation[] Array = new PieceLocation[MaxCount];

        public void Clear()
        {
            Count = 0;
        }

        public void Append(int offset)
        {
            Array[Count++] = new PieceLocation
            {
                Offset = offset,
                Index = Position.IndexFromOffset(offset),
            };
        }

        internal void ApplyTransform(Transform transform)
        {
            // Transform the 0..63 indexes, but NOT the 21..91 offsets (we need the original offsets for en passant).
            for (int i=0; i < Count; ++i)
                Array[i].Index = Symmetry.ForwardTransform(Array[i].Index, transform);
        }
    }

    public struct PieceLocation
    {
        public int Offset;
        public int Index;
    }
}
