using System;

namespace EndgameTableGen
{
    internal class WorkPlanner
    {
        public const int NumNonKings = 5;    // 0=Q, 1=R, 2=B, 3=N, 4=P
        public const int Q_INDEX = 0;
        public const int R_INDEX = 1;
        public const int B_INDEX = 2;
        public const int N_INDEX = 3;
        public const int P_INDEX = 4;
        public const int WHITE = 0;
        public const int BLACK = 1;

        private readonly ITableWorker worker;
        private readonly int[,] config = new int [2, 5];

        public WorkPlanner(ITableWorker worker)
        {
            this.worker = worker;
        }

        public void Plan(int nonkings)
        {
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
                if (IsCheckmatePossible())
                    worker.GenerateTable(config);
            }
        }


        private bool IsCheckmatePossible()
        {
            // This is based on the corresponding logic in Board.GetGameResult().
            int q = config[WHITE, Q_INDEX] + config[BLACK, Q_INDEX];
            int r = config[WHITE, R_INDEX] + config[BLACK, R_INDEX];
            int b = config[WHITE, B_INDEX] + config[BLACK, B_INDEX];
            int n = config[WHITE, N_INDEX] + config[BLACK, N_INDEX];
            int p = config[WHITE, P_INDEX] + config[BLACK, P_INDEX];
            return (q+r+p > 0) || (n+b > 1);
        }
    }
}
