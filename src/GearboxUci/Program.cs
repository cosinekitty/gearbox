using System;
using System.Threading;
using Gearbox;

namespace GearboxUci
{
    class Program
    {
        static bool exit;
        static AutoResetEvent signal = new AutoResetEvent(false);
        static Thinker thinker;

        static int Main(string[] args)
        {
            thinker = new Thinker();
            var thinkerThread = new Thread(ThinkerThreadFunc)
            {
                IsBackground = true,
                Name = "Thinker",
            };
            thinkerThread.Start();

            string line;
            while (null != (line = Console.ReadLine()))
            {
                string[] token = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (token.Length == 0)
                    continue;

                if (token[0] == "quit")
                    break;
            }

            thinker.AbortSearch();
            exit = true;
            signal.Set();
            thinkerThread.Join();
            return 0;
        }

        static void ThinkerThreadFunc()
        {
            while (!exit && signal.WaitOne() && !exit)
            {
            }
        }
    }
}
