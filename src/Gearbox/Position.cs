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

        private void TransformFrom(Position origin, Transform transform)
        {
            EpTargetOffset = origin.EpTargetOffset;
            for (int k = 0; k < NumPieceTypes; ++k)
                PieceList[k].TransformFrom(origin.PieceList[k], transform);
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

        public int GetEndgameTableIndex(Position scratch)
        {
            Debug.Assert(PieceList[Position.WK].Count == 1);
            Debug.Assert(PieceList[Position.BK].Count == 1);

            int wp = PieceList[Position.WP].Count;
            int bp = PieceList[Position.BP].Count;
            int pawns = wp + bp;

            Transform maxTransform;
            int[] whiteKingSymmetryLookup;

            if (pawns == 0)
            {
                // When there are no pawns, we get the most symmetry benefit
                // from forcing the white king from a realm of 64 squares
                // into a realm of 10 squares.
                // Then we can use any of the 8 possible symmetry transforms.
                whiteKingSymmetryLookup = EightfoldSymmetryLookup;
                maxTransform = Transform.Maximum;
            }
            else
            {
                // With any pawns on the board, we are constrained to use
                // left/right symmetry only.
                whiteKingSymmetryLookup = LeftRightSymmetryLookup;
                maxTransform = Transform.LeftRight;
            }

            int wkindex = PieceList[Position.WK].Array[0].Index;
            int best_table_index = -1;
            for (Transform transform = Transform.Identity; transform <= maxTransform; ++transform)
            {
                int wkflip = Symmetry.ForwardTransform(wkindex, transform);
                int wkcode = whiteKingSymmetryLookup[wkflip];
                if (wkcode >= 0)
                {
                    scratch.TransformFrom(this, transform);
                    int table_index = scratch.GetEndgameTableIndex(wkcode);
                    if (best_table_index < 0 || table_index < best_table_index)
                        best_table_index = table_index;
                }
            }

            if (best_table_index < 0)
                throw new Exception("Unable to find any transform that generates a valid table index.");

            return best_table_index;
        }

        private int GetEndgameTableIndex(int wk_code)
        {
            int tindex = 64*wk_code + PieceList[Position.BK].Array[0].Index;

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
            int wp = PieceList[Position.WP].Count;
            int bp = PieceList[Position.BP].Count;
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

        internal void Sort()
        {
            for (int p = 0; p < NumPieceTypes; ++p)
                if (PieceList[p].Count >= 2)
                    PieceList[p].Sort();
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

        internal void TransformFrom(PieceLocationList origin, Transform transform)
        {
            Count = origin.Count;
            for (int i = 0; i < Count; ++i)
            {
                // Transform the 0..63 indexes, but NOT the 21..91 offsets (we need the original offsets for en passant).
                Array[i].Index = Symmetry.ForwardTransform(origin.Array[i].Index, transform);
                Array[i].Offset = origin.Array[i].Offset;
            }
            Sort();
        }

        internal void Sort()
        {
            // We have to keep the index values in sorted order, to match
            // redundancy elimination logic in the endgame table generator:
            // When there are multiple identical pieces on the board, swapping
            // any pair of them creates different table indexes.
            // To eliminate the redundancy, we keep the smallest of all the
            // N! table indexes created by having N identical pieces on the board.
            // Because I will probably never have more than 4 identical pieces on the board,
            // an O(n^2) selection sort is probably the fastest approach.

            for (int i=0; i+1 < Count; ++i)
            {
                int b = i;
                for (int k=i+1; k < Count; ++k)
                    if (Array[k].Index < Array[b].Index)
                        b = k;

                if (b != i)
                {
                    var swap = Array[i];
                    Array[i] = Array[b];
                    Array[b] = swap;
                }
            }
        }
    }

    public struct PieceLocation
    {
        public int Offset;
        public int Index;
    }
}
