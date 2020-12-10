using Gearbox;

namespace GearboxUci
{
    internal class SearchLimits
    {
        public MoveList searchMoves = new MoveList();
        public bool ponder; // start searching in "ponder" mode.
        public int wtime;   // remaining time for White, in milliseconds.
        public int btime;   // remaining time for Black, in milliseconds.
        public int winc;    // White increment per move in milliseconds, if positive.
        public int binc;    // Bhite increment per move in milliseconds, if positive.
        public int movesToGo;
        public int depth;
        public int nodes;
        public int mate;        // search for mate within this many moves.
        public int moveTime;    // search exactly this many milliseconds.
        public bool infinite;   // search until the "stop" command.
    }
}
