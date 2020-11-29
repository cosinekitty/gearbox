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
        public bool isPlayerInCheck;
        public Ternary playerCanMove;
        public bool whiteCanCastleKingside;
        public bool whiteCanCastleQueenside;
        public bool blackCanCastleKingside;
        public bool blackCanCastleQueenside;
    }
}
