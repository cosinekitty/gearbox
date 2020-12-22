using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EndgameTableGen
{
    internal class TableGenerator : TableWorker
    {
        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<string, Table> finished = new Dictionary<string, Table>();

        public override void Start()
        {
            chrono.Restart();
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
                Log("Loaded: {0}", filename);
            }
            else
            {
                Log("Generating: {0}", filename);

                // Generate the table.
                table = new Table(size);

                // Save the table to disk.
                table.Save(filename);
                Log("Saved: {0}", filename);
            }

            // Store the finished table in memory.
            string symbol = ConfigSymbol(config);
            finished.Add(symbol, table);
        }

        public override void Finish()
        {
            chrono.Stop();
            Log("Finished after {0} = {1} seconds.", chrono.Elapsed, chrono.Elapsed.TotalSeconds);
        }
    }
}
