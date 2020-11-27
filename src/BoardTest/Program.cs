using System;
using Gearbox;

namespace BoardTest
{
    class Program
    {
        static int Main(string[] args)
        {
            var board = new Board();
            string fen = board.ForsythEdwardsNotation();
            Console.WriteLine(fen);
            if (fen != Board.StandardSetup)
            {
                Console.WriteLine("FAIL: does not match standard setup.");
                return 1;
            }
            Console.WriteLine("PASS");
            return 0;
        }
    }
}
