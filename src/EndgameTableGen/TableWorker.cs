using System;
using System.Text;

namespace EndgameTableGen
{
    internal abstract class TableWorker : ITableWorker
    {
        public const int NumNonKings = 5;    // 0=Q, 1=R, 2=B, 3=N, 4=P
        public const int Q_INDEX = 0;
        public const int R_INDEX = 1;
        public const int B_INDEX = 2;
        public const int N_INDEX = 3;
        public const int P_INDEX = 4;
        public const int WHITE = 0;
        public const int BLACK = 1;

        public abstract void Start();
        public abstract void GenerateTable(int[,] config);
        public abstract void Finish();

        protected string ConfigSymbol(int[,] config)
        {
            // Return a string in the form "QqRrBbNnPp",
            // where each is a decimal digit.
            // Fail if any piece count is greater than 9.
            // (Possible only when all pawns promoted to knights/bishops... LOL.)
            var sb = new StringBuilder();
            for (int side=0; side < 2; ++side)
                for (int mover=0; mover < 5; ++mover)
                    if (0 <= config[side,mover] && config[side,mover] <= 9)
                        sb.Append(config[side,mover]);
                    else
                        throw new ArgumentException("Invalid mover count in config[]");

            return sb.ToString();
        }

        protected string ConfigFileName(int[,] config)
        {
            return ConfigSymbol(config) + ".endgame";
        }

        protected long TableSize(int[,] config)
        {
            long size;
            int p = config[WHITE,P_INDEX] + config[BLACK,P_INDEX];

            if (p > 0)
            {
                // If there are any pawns, we use left/right symmetry
                // to force one of them to the left side of the board.
                // Therefore, that one pawn can be in any of 6*4 = 24 squares.
                --p;    // consume one of the pawns
                size = 24 * 64 * 64;     // consumed pawn, White King, Black King
            }
            else
            {
                // The White King can go in any of 10 unique squares.
                // The Black King can go in any of the 64 squares.
                size = 10 * 64;
            }

            // Any remaining pawns may appear in 48 different squares.
            while (p-- > 0)
                size *= 48;

            // All other movers can appear in any of the 64 different squares.
            // Add them all up.
            int m = 0;
            for (int i=0; i < P_INDEX; ++i)
                m += config[WHITE,i] + config[BLACK,i];

            while (m-- > 0)
                size *= 64;

            return size;
        }
    }
}
