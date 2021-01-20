using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Gearbox;

namespace EndgameTableGen
{
    internal class ParallelTableGenerator : TableWorker
    {
        private readonly int max_table_size;
        private readonly int num_threads;
        private readonly Dictionary<long, Table> finished = new();
        private readonly List<long> workList = new();
        private readonly HashSet<long> allConfigIds = new();
        private readonly Stopwatch chrono = new();
        private readonly AutoResetEvent[] waiters;
        private readonly TableSweeper[] sweeperForThread;

        public ParallelTableGenerator(int max_table_size, int num_threads, Func<TableSweeper> sweeperFactory)
        {
            this.max_table_size = max_table_size;
            this.num_threads = num_threads;
            waiters = new AutoResetEvent[num_threads];
            sweeperForThread = new TableSweeper[num_threads];
            for (int i = 0; i < num_threads; ++i)
            {
                waiters[i] = new AutoResetEvent(false);
                sweeperForThread[i] = sweeperFactory();
            }
        }

        public override void Dispose()
        {
            lock (finished)
            {
                foreach (Table table in finished.Values)
                    table.Dispose();

                finished.Clear();
            }
        }

        public override void Start()
        {
            chrono.Restart();
            finished.Clear();
            workList.Clear();
            allConfigIds.Clear();
        }

        public override Table GenerateTable(int[,] config)
        {
            // In this parallel version, we just queue up all the work.
            // When "Finish" is called, we know the queue is complete and we fire up all the threads there.
            long config_id = GetConfigId(config, false);
            workList.Add(config_id);
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
                    Priority = ThreadPriority.BelowNormal,
                };
                threadPool[i].Start(i);
            }

            Log("Started {0} threads.", num_threads);

            // Wait for all threads to finish.
            for (int i = 0; i < num_threads; ++i)
                threadPool[i].Join();

            chrono.Stop();
            Log("Finished after {0} = {1} seconds. Max parallel jobs = {2}.", chrono.Elapsed, chrono.Elapsed.TotalSeconds, MaxParallelJobs);
        }

        private bool GetNextAvailableJob(int thread_number, TableGenerator worker, out long config_id)
        {
            while (true)
            {
                // Search the list of remaining jobs for any that don't have any
                // unsatisfied dependencies.
                lock (finished)
                {
                    if (workList.Count == 0)
                    {
                        worker.Log("Work list is empty. Exiting.");
                        config_id = 0;
                        return false;
                    }

                    foreach (long cid in workList)
                    {
                        if (CanBeWorkedNow(cid))
                        {
                            workList.Remove(cid);
                            worker.Log("Ready to start {0}", ConfigString(cid));
                            config_id = cid;
                            return true;
                        }
                    }
                }

                // This thread can't make progress right now because all
                // pending jobs depend on other jobs that aren't finished yet.
                // Go to sleep until we are signaled that a job has finished.
                worker.Log("Blocked.");
                waiters[thread_number].WaitOne();
            }
        }

        private bool CanBeWorkedNow(long config_id)
        {
            // Compute the set of other jobs this job depends on completing first.
            int[,] config = DecodeConfig(config_id);
            HashSet<long> deps = ConfigDependencySet(config);

            // Prune out redundant/impossible dependencies.
            // This is necessary because the dependency generator finds
            // redundant things like [K vs kq] when we really only need [KQ vs k].
            deps.IntersectWith(allConfigIds);

            // See if any dependencies are not yet finished.
            foreach (long required_config_id in deps)
                if (!finished.ContainsKey(required_config_id))
                    return false;

            return true;
        }

        private int ParallelJobs;
        private int MaxParallelJobs;

        private void ThreadFunc(object arg)
        {
            int thread_number = (int)arg;
            using var worker = new TableGenerator(max_table_size, sweeperForThread[thread_number]);
            worker.LogTag = thread_number.ToString("00");
            worker.Log("Thread starting");
            while (GetNextAvailableJob(thread_number, worker, out long config_id))
            {
                // Update the worker's dictionary of finished endgame tables,
                // so it definitely has all the dependencies it needs.
                // Once we release the lock, the worker's independent copy of
                // the dictionary will be isolated and thread-safe.
                lock (finished)
                {
                    foreach (var kv in finished)
                        worker.AddToFinishedTable(kv.Key, kv.Value);

                    ++ParallelJobs;
                    if (MaxParallelJobs < ParallelJobs)
                        MaxParallelJobs = ParallelJobs;
                    worker.Log("Incremented parallel jobs={0}, max={1}", ParallelJobs, MaxParallelJobs);
                }

                // Generate the table!
                int[,] config = DecodeConfig(config_id);
                Table table = worker.GenerateTable(config);

                // Reflect this new work in the shared 'finished' table so other threads can use it.
                lock (finished)
                {
                    --ParallelJobs;
                    worker.Log("Decremented parallel jobs: {0}", ParallelJobs);
                    finished.Add(config_id, table);
                }

                // Now that new work is finished, signal all blocked
                // threads that they might be able to make progress.
                // We don't signal ourselves, so that we block immediately
                // in the call to GetNextAvailableJob() if it can't find a ready job.
                // If we signal a thread that is currently running, it will cause
                // an extra loop if it is stalled next time, but that causes no harm.
                for (int i = 0; i < num_threads; ++i)
                    if (i != thread_number)
                        waiters[i].Set();
            }
        }
    }
}
