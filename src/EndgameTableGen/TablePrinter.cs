using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EndgameTableGen
{
    internal class TablePrinter : TableWorker
    {
        private BigInteger totalSize;
        private List<long> configIdList = new();

        public override void Start()
        {
            Console.WriteLine("[Qq Rr Bb Nn Pp]{0,22}", "[size]");
            totalSize = 0;
            configIdList.Clear();
        }

        public override Table GenerateTable(int[,] config)
        {
            BigInteger size = TableSize(config);
            totalSize += size;
            long config_id = GetConfigId(config, false);
            configIdList.Add(config_id);
            Console.WriteLine(" {0} {1,22:n0}", ConfigString(config), size);
            return null;
        }

        public override void Finish()
        {
            Console.WriteLine("---------------   --------------------");
            Console.WriteLine("{0,15} {1,22:n0}", configIdList.Count, totalSize);
            Console.WriteLine();

            PrintDependencies();
        }

        private void PrintDependencies()
        {
            // Print the list of dependencies among the configurations.
            // This will help me figure out how to run the endgame generator
            // on multiple threads, so it can finish faster.
            Console.WriteLine("Dependencies:");
            for (int index2=0; index2 < configIdList.Count; ++index2)
            {
                long config2 = configIdList[index2];
                Console.WriteLine("{0}:", ConfigString(config2));
                HashSet<long> deps = ConfigDependencySet(config2);
                long[] deplist = deps.OrderByDescending(id => id).ToArray();
                foreach (long config1 in deplist)
                {
                    int index1 = configIdList.IndexOf(config1);
                    if (index1 >= 0)
                    {
                        Console.WriteLine("    {0}", ConfigString(config1));
                        if (index1 >= index2)
                            throw new Exception($"Internal error: index {index2} should not depend on index {index1}.");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
