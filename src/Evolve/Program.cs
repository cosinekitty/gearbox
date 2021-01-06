using System;
using System.IO;
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
            int a_wins = 0, a_losses = 0;   // from thinker1's point of view
            int w_wins = 0, w_losses = 0;   // from White's point of view
            int draws = 0;

            for (int g=0; g < ngames; ++g)
            {
                GameResult result = PlayGame(thinker1, thinker2, g & 1, g + 1);
                switch (result)
                {
                    case GameResult.WhiteWon:
                        ++w_wins;
                        if ((g & 1) == 0)
                            ++a_wins;
                        else
                            ++a_losses;
                        break;

                    case GameResult.BlackWon:
                        ++w_losses;
                        if ((g & 1) == 0)
                            ++a_losses;
                        else
                            ++a_wins;
                        break;

                    case GameResult.Draw:
                        ++draws;
                        break;

                    default:
                        throw new Exception($"Unknown game result {result}");
                }
                Console.WriteLine($"{TimeStamp()} STATS  games={1+g}, a_wins={a_wins}, a_losses={a_losses}, w_wins={w_wins}, w_losses={w_losses}, draws={draws}");
                Console.Out.Flush();    // in case output is redirected and we want to monitor with 'tail -f'.
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
            thinker.Name = $"Gearbox {geneFileName}";
            return thinker;
        }

        static GameResult PlayGame(Thinker thinker1, Thinker thinker2, int firstToMove, int round)
        {
            thinker1.ClearHashTable();
            thinker2.ClearHashTable();
            var thinkers = new Thinker[] { thinker1, thinker2 };
            var board = new Board();
            var legalMoves = new MoveList();
            var scratch = new MoveList();
            var utcStart = DateTime.UtcNow;

            GameResult result;
            for (int turn = firstToMove; (result = board.GetGameResult()) == GameResult.InProgress; turn ^= 1)
            {
                Move move = thinkers[turn].Search(board);
                board.GenMoves(legalMoves);
                string san = board.MoveNotation(move, legalMoves, scratch);
                board.PushMove(move);
                Console.WriteLine($"{TimeStamp()} MOVE  {san,-7} {Score.Format(move.score),8} {board.ForsythEdwardsNotation()}");
                Console.Out.Flush();    // in case output is redirected and we want to monitor with 'tail -f'.
            }

            // Append the game to a pgn file.
            // Use the tags to record which side was playing White, Black.
            var tags = new GameTags
            {
                Event = "Gene Battle",
                White = thinkers[firstToMove].Name,
                Black = thinkers[firstToMove ^ 1].Name,
                Round = round.ToString(),
                Date = $"{utcStart.Year:0000}.{utcStart.Month:00}.{utcStart.Day:00}",
            };
            tags.SetTag("Time", $"{utcStart.Hour:00}:{utcStart.Minute:00}:{utcStart.Second:00} UTC");

            string pgn = board.PortableGameNotation(tags);
            using (StreamWriter outfile = File.AppendText("battle.pgn"))
                outfile.WriteLine(pgn);

            return result;
        }

        static string TimeStamp()
        {
            DateTime utc = DateTime.UtcNow;
            return utc.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
