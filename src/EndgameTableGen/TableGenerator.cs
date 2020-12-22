using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace EndgameTableGen
{
    internal class TableGenerator : TableWorker
    {
        private Dictionary<string, Table> finished = new Dictionary<string, Table>();

        public override void Start()
        {
        }

        public override void GenerateTable(int[,] config)
        {
            Table table;

            string filename = ConfigFileName(config);
            int size = (int)TableSize(config);
            if (File.Exists(filename))
            {
                // We have already calculated this endgame table. Load it from disk.
                table = Table.Load(filename, size);
            }
            else
            {
                // Generate the table.
                table = new Table(size);

                // Save the table to disk.
                table.Save(filename);
            }

            // Store the finished table in memory.
            string symbol = ConfigSymbol(config);
            finished.Add(symbol, table);
        }

        public override void Finish()
        {
        }
    }
}
