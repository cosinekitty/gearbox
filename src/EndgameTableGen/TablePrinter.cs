using System;

namespace EndgameTableGen
{
    internal class TablePrinter : TableWorker
    {
        private int tableCount;
        private long totalSize;

        public override void Start()
        {
            Console.WriteLine("    table   Qq Rr Bb Nn Pp {0,20}", "size");
            tableCount = 0;
            totalSize = 0;
        }

        public override void GenerateTable(int[,] config)
        {
            Console.Write("{0,9}  ", ++tableCount);

            for (int m=0; m < WorkPlanner.NumNonKings; ++m)
                Console.Write(" {0}{1}", config[0,m], config[1,m]);

            long size = TableSize(config);
            Console.WriteLine(" {0,20:n0}", size);

            totalSize += size;
        }

        public override void Finish()
        {
            Console.WriteLine("                             ------------------");
            Console.WriteLine("                           {0,20:n0}", totalSize);
        }
    }
}
