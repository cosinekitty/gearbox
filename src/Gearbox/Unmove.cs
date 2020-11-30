namespace Gearbox
{
    internal enum Ternary
    {
        Unknown,
        No,
        Yes,
    }

    internal struct Unmove
    {
        public Move move;
        public Square capture;
        public int epTargetOffset;
        public int halfMoveClock;
        public Ternary playerInCheck;
        public Ternary playerCanMove;
        public CastlingFlags castling;
        public HashValue hash;      // used for sanity checking, not restoring, after each PopMove()
    }
}
