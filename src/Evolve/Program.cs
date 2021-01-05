using System;
using Gearbox;

namespace Evolve
{
    class Program
    {
        const string UsageText = @"
USAGE:

Evolve battle ngames gene1.json gene2.json
    Play the specified number of games between thinkers configured
    with the two evaluation tuning genes.
";

        static int PrintUsage()
        {
            Console.WriteLine(UsageText);
            return 1;
        }

        static int Main(string[] args)
        {
            if (args.Length == 4 && args[0] == "battle")
            {
                int ngames = int.Parse(args[1]);
                string geneFileName1 = args[2];
                string geneFileName2 = args[3];
                return Battle(ngames, geneFileName1, geneFileName2);
            }
            return PrintUsage();
        }

        static int Battle(int ngames, string geneFileName1, string geneFileName2)
        {
            Thinker thinker1 = MakeThinker(geneFileName1);
            Thinker thinker2 = MakeThinker(geneFileName2);
            int wins = 0, draws = 0, losses = 0;    // from thinker1's point of view

            for (int g=0; g < ngames; ++g)
            {
                GameResult result = PlayGame(thinker1, thinker2, g & 1);
                switch (result)
                {
                    case GameResult.WhiteWon:
                        if ((g & 1) == 0)
                            ++wins;
                        else
                            ++losses;
                        break;

                    case GameResult.BlackWon:
                        if ((g & 1) == 0)
                            ++losses;
                        else
                            ++wins;
                        break;

                    case GameResult.Draw:
                        ++draws;
                        break;

                    default:
                        throw new Exception($"Unknown game result {result}");
                }
                Console.WriteLine($"STATS: games={1+g}, wins={wins}, losses={losses}, draws={draws}");
            }

            return 0;
        }

        static Thinker MakeThinker(string geneFileName)
        {
            // Gene files are not yet implemented. Just create a stock thinker for now.
            const int HashTableSize = 10000000;    // smaller table than usual, to save memory
            const int ThinkTimeMillis = 1000;
            var thinker = new Thinker(HashTableSize);
            thinker.SetSearchTime(ThinkTimeMillis);
            return thinker;
        }

        static GameResult PlayGame(Thinker thinker1, Thinker thinker2, int firstToMove)
        {
            thinker1.ClearHashTable();
            thinker2.ClearHashTable();
            var thinkers = new Thinker[] { thinker1, thinker2 };
            int turn = firstToMove;
            var board = new Board();
            var legalMoves = new MoveList();
            var scratch = new MoveList();
            while (true)
            {
                GameResult result = board.GetGameResult();
                if (result != GameResult.InProgress)
                    return result;

                Move move = thinkers[turn].Search(board);
                board.GenMoves(legalMoves);
                string san = board.MoveNotation(move, legalMoves, scratch);
                Console.WriteLine($"{san} {Score.Format(move.score)}");
                board.PushMove(move);
                Console.WriteLine(board.ForsythEdwardsNotation());
                turn ^= 1;
            }
        }
    }
}
