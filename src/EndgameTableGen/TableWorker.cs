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
            long size = 1;
            int wp = config[WHITE,P_INDEX];
            int bp = config[BLACK,P_INDEX];
            int p = wp + bp;

            // Are there any pawns on the board?
            if (p > 0)
            {
                // Consume one of the pawns.
                --p;

                // Use left/right symmetry to force the consumed pawn
                // to the left side of the board.

                if (wp > 0 && bp > 0)
                {
                    // En passant captures are possible.
                    // We treat a pawn that has just moved 2 squares forward
                    // as if it rests on an imaginary square hovering above
                    // the actual square it landed on.
                    // There can be at most one such pawn on the board at any time.
                    // Therefore, if any pawn just moved two squares, it becomes
                    // the "primary" pawn. Otherwise, the primary pawn is the pawn
                    // (White or Black) having the alphabetically lowest algebraic coordinates.
                    // Accounting for left/right symmetry, the primary pawn can be in
                    // one of 6*4 + 4 = 28 states.
                    size *= 28;
                }
                else
                {
                    // No en passant captures are possible.
                    // So there is no difference between a pawn that has just
                    // moved two squares forward and one that hasn't just done so.
                    size *= 24;
                }

                // The White King can be anywhere on the board (no symmetry exploits for it).
                size *= 64;
            }
            else
            {
                // The White King can go in any of 10 unique squares,
                // thanks to eight-fold symmetry.
                size *= 10;
            }

            // The Black King can go in any of 64 squares.
            size *= 64;

            // Any remaining pawns may appear in 48 different squares.
            while (p-- > 0)
                size *= 48;

            // Add up the total count of all movers that are neither king nor pawn.
            int m = 0;
            for (int i=0; i < P_INDEX; ++i)
                m += config[WHITE,i] + config[BLACK,i];

            // Each of these remaining movers can be in any of the 64 squares.
            while (m-- > 0)
                size *= 64;

            return size;
        }
    }
}
