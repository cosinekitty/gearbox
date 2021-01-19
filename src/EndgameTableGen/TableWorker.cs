using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public const int NumSides = 2;
        public const int WHITE = 0;
        public const int BLACK = 1;

        public abstract void Start();
        public abstract Table GenerateTable(int[,] config);
        public abstract void Finish();
        public abstract void Dispose();

        public static string ConfigString(int[,] config)
        {
            var sb = new StringBuilder();
            for (int m=0; m < WorkPlanner.NumNonKings; ++m)
            {
                if (m > 0)
                    sb.Append(" ");
                sb.AppendFormat("{0}{1}", config[0,m], config[1,m]);
            }
            return sb.ToString();
        }

        public static string ConfigString(long config_id)
        {
            int[,] config = DecodeConfig(config_id);
            return ConfigString(config);
        }

        public static long GetConfigId(int[,] config, bool reverseSides)      // same idea as board.GetConfigId
        {
            // Calculate the decimal integer QqRrBbNnPp.
            // This identifies the lookup table to use.
            long id = 0;
            int w = reverseSides ? BLACK : WHITE;
            int b = reverseSides ? WHITE : BLACK;
            for (int mover=0; mover < NumNonKings; ++mover)
                id = 100*id + 10*config[w,mover] + config[b,mover];

            return id;
        }

        public static int[,] DecodeConfig(long id)
        {
            long n = id;
            var config = new int[NumSides, NumNonKings];
            for (int mover = NumNonKings-1; mover >= 0; --mover)
            {
                config[BLACK, mover] = (int)(n % 10);
                n /= 10;
                config[WHITE, mover] = (int)(n % 10);
                n /= 10;
            }
            Debug.Assert(GetConfigId(config, false) == id);
            return config;
        }

        public static string OutputDirectory()
        {
            string dir = Environment.GetEnvironmentVariable("GEARBOX_TABLEBASE_DIR") ?? Environment.CurrentDirectory;
            return dir;
        }

        protected string ConfigFileName(int[,] config)
        {
            long id = GetConfigId(config, false);
            if (id <= 0)
                throw new ArgumentException("Invalid endgame configuration.");
            string dir = OutputDirectory();
            string fn = id.ToString("D10") + ".endgame";
            return Path.Combine(dir, fn);
        }

        public static BigInteger TableSize(int[,] config)
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

        internal string LogTag;     // needed for tagging the threads in multithreaded workloads

        private static readonly object LogMutex = new object();

        internal void Log(string format, params object[] args)
        {
            lock (LogMutex)     // in multithreaded cases, make sure we don't scramble lines together
            {
                string now = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                now = now.Substring(0, now.Length-6) + "Z";     // convert "...:29.2321173Z" to "...:29.23Z"
                Console.Write(now);
                Console.Write("  ");
                if (!string.IsNullOrEmpty(LogTag))
                    Console.Write("[" + LogTag + "]  ");
                Console.WriteLine(format, args);
                Console.Out.Flush();    // in case being redirected to a file, so 'tail -f' or 'tee' works.
            }
        }

        public static bool DependsOn(long config_id_1, long config_id_2)
        {
            // config1 depends on config2 when we must wait for config2 to be finished
            // before we can start calculating config1.
            // For example, [KP vs k] depends on [KQ vs k] because the white pawn
            // might get promoted to a white queen.

            // Strategy: compute the set of all config_ids config1 could depend on.
            // Return true if config2 is in that set.
            int[,] config1 = DecodeConfig(config_id_1);
            var dependencies = new HashSet<long>();
            ConfigDependencySet(dependencies, config1);
            return dependencies.Contains(config_id_2);
        }

        private static void ConfigDependencySet(HashSet<long> deps, int[,] config)
        {
            for (int side=0; side < NumSides; ++side)
            {
                for (int mover=0; mover < NumNonKings; ++mover)
                {
                    if (config[side, mover] > 0)
                    {
                        // One of these piece(s) could be captured.
                        --config[side, mover];
                        if (WorkPlanner.IsForcedCheckmatePossible(config))
                        {
                            deps.Add(GetConfigId(config, false));
                            deps.Add(GetConfigId(config, true));
                            ConfigDependencySet(deps, config);
                        }
                        ++config[side, mover];
                    }
                }

                if (config[side, P_INDEX] > 0)
                {
                    // One of these pawn(s) could be promoted to Q, R, B, N.
                    --config[side, P_INDEX];
                    for (int prom = Q_INDEX; prom <= N_INDEX; ++prom)
                    {
                        ++config[side, prom];
                        if (WorkPlanner.IsForcedCheckmatePossible(config))
                        {
                            deps.Add(GetConfigId(config, false));
                            deps.Add(GetConfigId(config, true));
                            ConfigDependencySet(deps, config);
                        }
                        --config[side, prom];
                    }
                    ++config[side, P_INDEX];
                }
            }
        }

        public static HashSet<long> ConfigDependencySet(int[,] config)
        {
            var deps = new HashSet<long>();
            ConfigDependencySet(deps, config);
            return deps;
        }

        public static HashSet<long> ConfigDependencySet(long config_id)
        {
            int[,] config = DecodeConfig(config_id);
            return ConfigDependencySet(config);
        }
    }
}
