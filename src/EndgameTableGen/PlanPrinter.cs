using System;

namespace EndgameTableGen
{
    internal class TablePrinter : ITableWorker
    {
        private int tableCount;

        public void Start()
        {
            Console.WriteLine("    table   Qq Rr Bb Nn Pp");
        }

        public void GenerateTable(int[,] config)
        {
            Console.Write("{0,9}  ", ++tableCount);

            for (int m=0; m < WorkPlanner.NumNonKings; ++m)
                Console.Write(" {0}{1}", config[0,m], config[1,m]);

            Console.WriteLine();
        }

        public void Finish()
        {
        }
    }
}