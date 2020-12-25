using System;
using System.IO;
using System.Numerics;
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
            for (int mover=0; mover < 5; ++mover)
                for (int side=0; side < 2; ++side)
                    if (0 <= config[side,mover] && config[side,mover] <= 9)
                        sb.Append(config[side,mover]);
                    else
                        throw new ArgumentException("Invalid mover count in config[]");

            return sb.ToString();
        }

        protected string ConfigFileName(int[,] config)
        {
            string dir = Environment.GetEnvironmentVariable("GEARBOX_TABLEBASE_DIR") ?? Environment.CurrentDirectory;
            string fn = ConfigSymbol(config) + ".endgame";
            return Path.Combine(dir, fn);
        }

        protected BigInteger TableSize(int[,] config)
        {
            BigInteger size = 1;
            int wp = config[WHITE,P_INDEX];
            int bp = config[BLACK,P_INDEX];
            int p = wp + bp;

            // Are there any pawns on the board?
            if (p > 0)
            {
                // Use left/right symmetry to force the White King to the left side of the board.
                size *= 32;
            }
            else
            {
                // The White King can go in any of 10 unique squares,
                // thanks to eight-fold symmetry.
                size *= 10;
            }

            // The Black King can go in any of 64 squares.
            size *= 64;

            // Pawns may appear in 48 different squares.
            // But when both sides have at least one pawn, en passant captures are possible.
            // In that case, there are 48 + 8 = 56 possible states for each pawn.
            int pawn_factor = (wp > 0 && bp > 0) ? 56 : 48;
            while (p-- > 0)
                size *= pawn_factor;

            // Add up the total count of all movers that are neither king nor pawn.
            int m = 0;
            for (int i=0; i < P_INDEX; ++i)
                m += config[WHITE,i] + config[BLACK,i];

            // Each of these remaining movers can be in any of the 64 squares.
            while (m-- > 0)
                size *= 64;

            return size;
        }

        internal static void Log(string format, params object[] args)
        {
            string now = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            now = now.Substring(0, now.Length-6) + "Z";     // convert "...:29.2321173Z" to "...:29.23Z"
            Console.Write(now);
            Console.Write("  ");
            Console.WriteLine(format, args);
            Console.Out.Flush();    // in case being redirected to a file, so 'tail -f' or 'tee' works.
        }
    }
}
