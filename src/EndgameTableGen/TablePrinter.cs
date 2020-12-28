using System;
using System.Numerics;

namespace EndgameTableGen
{
    internal class TablePrinter : TableWorker
    {
        private int tableCount;
        private BigInteger totalSize;

        public override void Start()
        {
            Console.WriteLine("[Qq Rr Bb Nn Pp]{0,22}", "[size]");
            tableCount = 0;
            totalSize = 0;
        }

        public override void GenerateTable(int[,] config)
        {
            for (int m=0; m < WorkPlanner.NumNonKings; ++m)
                Console.Write(" {0}{1}", config[0,m], config[1,m]);

            BigInteger size = TableSize(config);
            Console.WriteLine(" {0,22:n0}", size);

            totalSize += size;
            ++tableCount;
        }

        public override void Finish()
        {
            Console.WriteLine("---------------   --------------------");
            Console.WriteLine("{0,15} {1,22:n0}", tableCount, totalSize);
        }
    }
}
