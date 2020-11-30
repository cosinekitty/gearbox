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
        public Ternary epCaptureIsLegal;   // lazy-evaluated existence of at least one legal en passant capture
        public int halfMoveClock;
        public Ternary playerInCheck;
        public Ternary playerCanMove;
        public CastlingFlags castling;
        public HashValue pieceHash;         // used for sanity checking, not restoring, after each PopMove()
    }
}
