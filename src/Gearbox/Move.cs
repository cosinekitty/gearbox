namespace Gearbox
{
    public static class Score
    {
        // Scores are roughly in units of "micropawns".
        public const int Draw = 0;
        public const int WonForWhite = +1000000000;
        public const int WonForBlack = -1000000000;
        public const int WhiteMate   = +1100000000;
        public const int BlackMate   = -1100000000;
        public const int Undefined   = -2000000000;
    }

    public struct Move
    {
        public byte source;
        public byte dest;
        public char prom;       // 'qrbn' if promotion
        public int  score;

        public Move(int source, int dest)
        {
            this.source = (byte)source;
            this.dest = (byte)dest;
            this.prom = '\0';
            this.score = Score.Undefined;
        }

        public Move(int source, int dest, char prom)
        {
            this.source = (byte)source;
            this.dest = (byte)dest;
            this.prom = prom;
            this.score = Score.Undefined;
        }

        public override string ToString()
        {
            string alg = Board.Algebraic(source) + Board.Algebraic(dest);
            return (prom == '\0') ? alg : (alg + prom);
        }
    }
}
