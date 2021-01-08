using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Gearbox;

namespace EndgameTableGen
{
    // I copied class TableGenerator to ParallelTableGenerator,
    // then made changes to get it to work in multiple threads.
    internal class ParallelTableGenerator : TableWorker
    {
        private readonly int max_table_size;
        private readonly int num_threads;
        private readonly Dictionary<long, Table> finished = new();
        private readonly Queue<long> workQueue = new();
        private readonly HashSet<long> allConfigIds = new();

        public ParallelTableGenerator(int max_table_size, int num_threads)
        {
            this.max_table_size = max_table_size;
            this.num_threads = num_threads;
        }

        public override void Start()
        {
        }

        public override Table GenerateTable(int[,] config)
        {
            // In this parallel version, we just queue up all the work.
            // When "Finish" is called, we know the queue is complete and we fire up all the threads there.
            long config_id = GetConfigId(config, false);
            workQueue.Enqueue(config_id);
            allConfigIds.Add(config_id);
            Log("Queued: {0}", ConfigString(config));
            return null;
        }

        public override void Finish()
        {
            var threadPool = new Thread[num_threads];

            for (int i = 0; i < num_threads; ++i)
            {
                threadPool[i] = new Thread(ThreadFunc)
                {
                    IsBackground = false,
                    Name = $"Table Gen {i}/{num_threads}",
                };
                threadPool[i].Start(i);
            }
        }

        private void ThreadFunc(object arg)
        {
            int thread_number = (int)arg;
            var worker = new TableGenerator(max_table_size)
            {
                LogTag = thread_number.ToString("00"),
            };
            worker.Log("Thread starting");
            while (true)
            {
                long config_id;
                lock (workQueue)
                {
                    if (workQueue.Count == 0)
                    {
                        worker.Log("Work queue is empty. Exiting.");
                        return;
                    }

                    config_id = workQueue.Dequeue();
                }

                // Before starting this job, wait for all of its dependencies to finish.
                int[,] config = DecodeConfig(config_id);
                worker.Log("Waiting for dependencies of: {0}", ConfigString(config));
                WaitForDependencies(thread_number, config);

                // Update the worker's dictionary of finished endgame tables,
                // so it definitely has all the dependencies it needs.
                // Once we release the lock, the worker's independent copy of
                // the dictionary will be isolated and thread-safe.
                lock (finished)
                {
                    foreach (var kv in finished)
                        worker.AddToFinishedTable(kv.Key, kv.Value);
                }

                // Generate the table!
                Table table = worker.GenerateTable(config);

                // Reflect this new work in the shared 'finished' table so other threads can use it.
                lock (finished)
                {
                    finished.Add(config_id, table);
                }
            }
        }

        private void WaitForDependencies(int thread_number, int[,] config)
        {
            // Compute the dependency set for this job.
            HashSet<long> deps = ConfigDependencySet(config);

            // Prune out redundant/impossible dependencies.
            // This is necessary because the dependency generator finds
            // redundant things like [K vs kq] when we really only need [KQ vs k].
            deps.IntersectWith(allConfigIds);

            // Poll the dictionary of finished tables until all dependencies are completed.
            while (true)
            {
                lock (finished)
                {
                    bool ready = true;
                    foreach (long required_config_id in deps)
                        if (!finished.ContainsKey(required_config_id))
                            ready = false;

                    if (ready)
                        return;     // All the dependencies are satisfied.
                }

                Thread.Sleep(1000);     // FIXFIXFIX - is there a more elegant way to wait for the table to change?
            }
        }
    }
}
