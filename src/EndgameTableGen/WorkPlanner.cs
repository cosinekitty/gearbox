using System;
using System.Text;

namespace EndgameTableGen
{
    internal class WorkPlanner: IDisposable
    {
        public const int NumSides = 2;
        public const int WHITE = 0;
        public const int BLACK = 1;

        public const int NumNonKings = 5;
        public const int Q_INDEX = 0;
        public const int R_INDEX = 1;
        public const int B_INDEX = 2;
        public const int N_INDEX = 3;
        public const int P_INDEX = 4;

        private readonly ITableWorker worker;
        private readonly int[,] config = new int [NumSides, NumNonKings];

        public WorkPlanner(ITableWorker worker)
        {
            this.worker = worker;
        }

        public void Dispose()
        {
            worker.Dispose();
        }

        public void Plan(int nonkings)
        {
            if (nonkings < 0 || nonkings > 9)
                throw new ArgumentException("Invalid number of nonking pieces: " + nonkings);

            worker.Start();
            for (int n=0; n <= nonkings; ++n)
                PlanDistribute(n, 0, true);
            worker.Finish();
        }

        private void PlanDistribute(int remaining, int mover, bool equal)
        {
            if (mover < NumNonKings)
            {
                for (int w = remaining; w >= 0; --w)
                {
                    config[WHITE, mover] = w;    // allocate w of these nonkings to White

                    // Enforce the integer [WQ, WR, WB, WN, WP] >= [BQ, BR, BB, BN, BP].
                    // For example, if there are 3 White Queens, 1 Black Knight, and 2 Black Bishops,
                    // 30000 > 00210, so we are OK.
                    int limit = remaining - w;
                    if (equal && limit > w)
                        limit = w;

                    // But if this is the last slot (Black Pawns)
                    // we must spend all of the remaining count if possible.

                    if (mover+1 == NumNonKings)
                    {
                        if (limit == remaining - w)
                        {
                            config[BLACK, mover] = limit;
                            PlanDistribute(0, 1 + mover, equal);
                            config[BLACK, mover] = 0;
                        }
                    }
                    else
                    {
                        for (int b = limit; b >= 0; --b)
                        {
                            config[BLACK, mover] = b;       // allocate b of these nonkings to Black
                            PlanDistribute(remaining-(w+b), 1 + mover, equal && w==b);
                        }
                    }
                }
            }
            else
            {
                // Leaf node of the recursive search tree.
                if (IsForcedCheckmatePossible(config))
                    worker.GenerateTable(config);
            }
        }

        public static bool IsForcedCheckmatePossible(int[,] config)
        {
            // This function returns false only when it is certain
            // that forced checkmate is impossible for the given configuration.
            // Otherwise it must return true, so that we generate
            // an endgame table for the configuration.

            int q = config[WHITE, Q_INDEX] + config[BLACK, Q_INDEX];
            int r = config[WHITE, R_INDEX] + config[BLACK, R_INDEX];
            int p = config[WHITE, P_INDEX] + config[BLACK, P_INDEX];
            if (q + r + p > 0)
                return true;

            int wb = config[WHITE, B_INDEX];
            int wn = config[WHITE, N_INDEX];
            int bb = config[BLACK, B_INDEX];
            int bn = config[BLACK, N_INDEX];

            if (wn + wb + bn + bb > 1)
            {
                // Checkmate is possible, but not proven forcible.
                // Exclude some known cases where checkmates occur but are not forced.
                // Others may exist, but it's better to err on the side of returning true.
                string dec = Decimal(wb, bb, wn, bn);
                switch (dec)
                {
                    //    BbNn
                    case "1100":
                    case "1001":
                    case "0110":
                    case "0011":
                    case "0020":
                    case "0002":
                        // Add more material draw cases here as they are discovered.
                        return false;

                    default:
                        return true;
                }
            }

            return false;
        }

        private static string Decimal(params int[] digits)
        {
            var sb = new StringBuilder(digits.Length);
            foreach (int d in digits)
            {
                if (d < 0 || d > 9)
                    throw new ArgumentException("Invalid digit: " + d);
                sb.Append(d);
            }
            return sb.ToString();
        }
    }
}
