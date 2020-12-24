using System;

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

        public void ForwardTransform(Transform transform)
        {
            foreach (PieceLocationList list in PieceList)
                list.ForwardTransform(transform);
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
                    int height = Board.DiagonalHeight(flip);
                    if (height != 0)
                        return height;
                }
            }
            return 0;       // Everything is on the a1..h8 diagonal.
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
                Index = Board.IndexFromOffset(offset),
            };
        }

        internal void ForwardTransform(Transform transform)
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
