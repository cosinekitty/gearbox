/*
    MIT License

    Copyright (c) 2020 Don Cross

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gearbox;

namespace BoardTest
{
    class Program
    {
        static readonly Dictionary<string, Func<string[], bool>> TestTable = new()
        {
            ["all"] = TestAll,
            ["puzzles"] = TestPuzzles,
            ["endgame"] = TestEndgames,
        };

        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (TestTable.TryGetValue(args[0], out var func))
                    return func(args.Skip(1).ToArray()) ? 0 : 1;
            }
            Console.WriteLine("ERROR: Invalid command line arguments.");
            Console.WriteLine("The following command line verbs are supported:");
            foreach (string key in TestTable.Keys.OrderBy(k => k.ToUpperInvariant()))
                Console.WriteLine(key);
            return 1;
        }

        static bool TestAll(string[] args)
        {
            var emptyArgs = new string[0];

            return
                TestStandardSetup() &&
                TestDraws() &&
                TestGameTags() &&
                TestEndgames(emptyArgs) &&
                TestPuzzles(emptyArgs) &&
                TestGameListing() &&
                TestPgn()
            ;
        }

        struct EndgamePositionTest
        {
            public readonly string fen;
            public readonly string score;
            public EndgamePositionTest(string fen, string score)
            {
                this.fen = fen;
                this.score = score;
            }
        }

        static bool TestEndgames(string[] args)
        {
            const string TableDirVarName = "GEARBOX_TABLEBASE_DIR";
            string dir = Environment.GetEnvironmentVariable(TableDirVarName);
            if (dir == null)
            {
                Console.WriteLine("Skipping endgame tests because environment variable is not set: {0}", TableDirVarName);
                return true;
            }

            var thinker = new Thinker(1000);
            thinker.SetSearchLimit(1);
            int loadCount = thinker.LoadEndgameTables(dir);
            Console.WriteLine("Loaded {0} endgame tables.", loadCount);
            if (loadCount == 0)
            {
                Console.WriteLine("Skipping endgame tests because there are no endgame tables in: {0}", dir);
                return true;
            }

            var testList = new EndgamePositionTest[]
            {
                new EndgamePositionTest("8/8/8/8/5kp1/8/4K3/8 b - - 9 5", "#11"),
                new EndgamePositionTest("1k6/8/1K6/8/8/1R6/8/8 w - - 1 1", "#2"),
            };

            foreach (EndgamePositionTest t in testList)
                if (!TestEndgamePosition(thinker, t.fen, t.score))
                    return false;

            Console.WriteLine("PASS: TestEndgames");
            return true;
        }

        static bool TestEndgamePosition(Thinker thinker, string fen, string expectedScore)
        {
            var board = new Board(fen);
            Move move = thinker.Search(board);
            string scoreFormat = Score.Format(move.score);
            Console.WriteLine("{0} {1}", move, scoreFormat);
            if (scoreFormat != expectedScore)
            {
                Console.WriteLine("FAIL(TestEndgamePosition): expected score {0} but found {1} for {2}", expectedScore, scoreFormat, fen);
                return false;
            }
            Console.WriteLine("PASS: TestEndgamePosition: [{0}] {1}", scoreFormat, fen);
            return true;
        }

        static bool TestPgn()
        {
            int gameCount;
            const string fenFileName = "final_positions.txt";
            return (
                TestLegalMoves("gearbox_move_test.txt", fenFileName, out gameCount) &&
                TestPgnLoader("testgames.pgn", fenFileName, gameCount)
            );
        }

        static bool TestPgnLoader(string pgnFileName, string fenFileName, int expectedGameCount)
        {
            int count = 0;
            using (StreamReader fenfile = File.OpenText(fenFileName))
            {
                foreach (Game game in Game.FromTextFile(pgnFileName))
                {
                    ++count;
                    var board = Board.FromGame(game);
                    string calcFen = board.ForsythEdwardsNotation();
                    string expectedFen = fenfile.ReadLine();
                    if (expectedFen == null)
                    {
                        Console.WriteLine("FAIL(TestPgnLoader): hit end of file {0}", fenFileName);
                        return false;
                    }
                    if (calcFen != expectedFen)
                    {
                        Console.WriteLine("FAIL(TestPgnLoader): calculated FEN:");
                        Console.WriteLine(calcFen);
                        Console.WriteLine("Expected FEN:");
                        Console.WriteLine(expectedFen);
                        return false;
                    }
                }
            }
            if (count != expectedGameCount)
            {
                Console.WriteLine("FAIL(TestPgnLoader): expected {0} games, found {1}.", expectedGameCount, count);
                return false;
            }
            Console.WriteLine("PASS: PGN Loader ({0} games)", count);
            return true;
        }

        static bool TestGameListing()
        {
            var tags = new GameTags
            {
                Event = "Rated Rapid game",
                Site = "https://lichess.org/JDY63HRY",
                Date = "2020.10.01",
                Round = "-",
                White = "Montzer",
                Black = "shotachkonia",
            };
            tags.SetTag("UTCDate", "2020.10.01");
            tags.SetTag("UTCTime", "07:57:31");
            tags.SetTag("Opening", "Caro-Kann Defense");
            tags.SetTag("TimeControl", "600+0");

            Board board = Board.FromPgnText(@"e4 c6 d4 d5 Nc3 Nf6 e5 Ng8 f4 g6 Nf3 h5 Bd3 Nh6 O-O Bf5
                Nh4 Bxd3 Qxd3 e6 Bd2 Be7 Nf3 Nf5 Rae1 Nd7 Nd1 a6 Ne3 c5 c3 Rc8 Nxf5 gxf5
                Ng5 Nf8 h4 Ng6 g3 b5 Kh2 Qb6 b4 cxd4 cxd4 Rc4 Nf3 Bxb4 Rc1 Bxd2 Qxd2 O-O
                Rfd1 Rfc8 Rxc4 Rxc4 Qe2 Qa5 Ng5 Kf8 Qxh5 Qxa2+ Kh3 Rc2 Nf3 Rf2 Rh1 Qe2
                Qh6+ Ke8 Ng5 Qg4#");

            string listing = board.PortableGameNotation(tags);

            string expected = NormalizeLineEndings(
@"[Event ""Rated Rapid game""]
[Site ""https://lichess.org/JDY63HRY""]
[Date ""2020.10.01""]
[Round ""-""]
[White ""Montzer""]
[Black ""shotachkonia""]
[Result ""0-1""]
[Opening ""Caro-Kann Defense""]
[TimeControl ""600+0""]
[UTCDate ""2020.10.01""]
[UTCTime ""07:57:31""]

1. e4 c6 2. d4 d5 3. Nc3 Nf6 4. e5 Ng8 5. f4 g6 6. Nf3 h5 7. Bd3 Nh6 8. O-O Bf5
9. Nh4 Bxd3 10. Qxd3 e6 11. Bd2 Be7 12. Nf3 Nf5 13. Rae1 Nd7 14. Nd1 a6 15. Ne3
c5 16. c3 Rc8 17. Nxf5 gxf5 18. Ng5 Nf8 19. h4 Ng6 20. g3 b5 21. Kh2 Qb6 22. b4
cxd4 23. cxd4 Rc4 24. Nf3 Bxb4 25. Rc1 Bxd2 26. Qxd2 O-O 27. Rfd1 Rfc8 28. Rxc4
Rxc4 29. Qe2 Qa5 30. Ng5 Kf8 31. Qxh5 Qxa2+ 32. Kh3 Rc2 33. Nf3 Rf2 34. Rh1 Qe2
35. Qh6+ Ke8 36. Ng5 Qg4# 0-1
");

            if (listing != expected)
            {
                Console.WriteLine("FAIL: game listing is wrong:");
                Console.WriteLine(listing);
                Console.WriteLine("Expected:");
                Console.WriteLine(expected);
                return false;
            }

            Console.WriteLine("PASS: Game Listing");
            return true;
        }

        static bool TestGameTags()
        {
            string expectedEmptyText = NormalizeLineEndings(
@"[Event ""?""]
[Site ""?""]
[Date ""????.??.??""]
[Round ""?""]
[White ""?""]
[Black ""?""]
[Result ""*""]

");

            var emptyTags = new GameTags();
            string actualText = emptyTags.ToString();
            if (actualText != expectedEmptyText)
            {
                Console.WriteLine("FAIL: empty tag text is incorrect:\n{0}", actualText);
                Console.WriteLine("Correct is:\n{0}", expectedEmptyText);
                return false;
            }
            Console.WriteLine("PASS: Game Tags");
            return true;
        }

        static bool TestStandardSetup()
        {
            var board = new Board();
            HashValue hash1 = board.Hash();
            Console.WriteLine(hash1);
            string fen = board.ForsythEdwardsNotation();
            Console.WriteLine(fen);
            if (fen != Board.StandardSetup)
            {
                Console.WriteLine("FAIL: does not match standard setup.");
                return false;
            }
            board.SetPosition(Board.StandardSetup);
            HashValue hash2 = board.Hash();
            Console.WriteLine(hash2);
            if (hash1.a != hash2.a || hash1.b != hash2.b)
            {
                Console.WriteLine("FAIL: inconsistent hash values for standard setup.");
                return false;
            }
            board.RefreshAfterDangerousChanges();
            HashValue hash3 = board.Hash();
            Console.WriteLine(hash3);
            if (hash3.a != hash1.a || hash3.b != hash1.b)
            {
                Console.WriteLine("FAIL: inconsistent hash values after RefreshAfterDangerousChanges().");
                return false;
            }
            Console.WriteLine("PASS: Standard Setup");
            return true;
        }

        static string[] Split(string line)
        {
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        static bool TestLegalMoves(string inFileName, string outFileName, out int gameCount)
        {
            gameCount = 0;
            var board = new Board();
            var movelist = new MoveList();
            var scratch = new MoveList();
            var reFlags = new Regex(@"check=([01]) mobile=([01])");

            using (StreamWriter outfile = File.CreateText(outFileName))
            using (StreamReader infile = File.OpenText(inFileName))
            {
                // The input is generated by a custom option in the "portable" version of Chenard.
                // The original raw data came from the wonder lichess.org website:
                // https://database.lichess.org/standard/lichess_db_standard_rated_2020-10.pgn.bz2
                //
                //    don@spearmint:~/github/chenard/linux $ ./build && ./chenard --gearbox lichess_db_standard_rated_2020-10.pgn gearbox.txt
                //
                //    Chenard - a portable chess program, v 2020.11.27
                //    by Don Cross  -  http://cosinekitty.com/chenard/
                //    Linux version; compiled with GNU C++ 6.3.0 20170516
                //
                //    Kept 1000 of 1476710 games.
                //
                // Each input record consists of 6 lines:
                //      ply longmove
                //      long_move_list
                //      san_move_list
                //      fen
                //      check=[01] mobile=[01]
                //      <blank>
                //
                string line;
                int ply;
                int lnum = 0;
                string prevFen = null;
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    string[] token = Split(line);
                    if (token.Length != 2 || !int.TryParse(token[0], out ply))
                        throw new Exception("Bad input format in file: " + inFileName);
                    string moveAlg = token[1];

                    if (ply == 0)
                    {
                        ++gameCount;
                        board.SetPosition(null);
                        if (prevFen != null)
                        {
                            outfile.WriteLine(prevFen);
                            prevFen = null;
                        }
                    }

                    string fenBefore = board.ForsythEdwardsNotation();
                    board.GenMoves(movelist);
                    string fenAfter = board.ForsythEdwardsNotation();
                    if (fenBefore != fenAfter)
                    {
                        Console.WriteLine("FAIL({0} line {1}): GenMoves corrupted the board.", inFileName, lnum);
                        Console.WriteLine("before = {0}", fenBefore);
                        Console.WriteLine("after  = {0}", fenAfter);
                        return false;
                    }
                    Move[] marray = movelist.ToMoveArray();
                    foreach (Move move in marray)
                        move.Validate();

                    line = infile.ReadLine();
                    ++lnum;
                    string correctSanMoveList = string.Join(' ', Split(line).OrderBy(t => t));
                    string calcSanMoveList = SanMoveList(board, movelist, scratch);

                    Move moveToMake = marray.First(m => m.ToString() == moveAlg);
                    board.PushMove(moveToMake);

                    string correctFen = infile.ReadLine();
                    prevFen = correctFen;
                    ++lnum;
                    string calcFen = board.ForsythEdwardsNotation();
                    if (correctFen != calcFen)
                    {
                        Console.WriteLine("FAIL({0} line {1}): FEN mismatch", inFileName, lnum);
                        Console.WriteLine("correct = {0}", correctFen);
                        Console.WriteLine("calc    = {0}", calcFen);
                        return false;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    Match m = reFlags.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("FAIL({0} line {1}): did not match flags regex", inFileName, lnum);
                        return false;
                    }
                    bool correctCheck = (m.Groups[1].Value == "1");
                    bool correctMobility = (m.Groups[2].Value == "1");
                    if (correctCheck != board.IsPlayerInCheck())
                    {
                        Console.WriteLine("FAIL({0} line {1}): check should have been {2}", inFileName, lnum, correctCheck);
                        return false;
                    }

                    if (correctMobility != board.PlayerCanMove())
                    {
                        Console.WriteLine("FAIL({0} line {1}): mobility should have been {2}", inFileName, lnum, correctMobility);
                        return false;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    if (line != "")
                    {
                        Console.WriteLine("FAIL({0} line {1}): expected blank line.", inFileName, lnum);
                        return false;
                    }
                }

                if (prevFen != null)
                    outfile.WriteLine(prevFen);
            }
            Console.WriteLine("PASS: LegalMoveTest for {0} games.", gameCount);
            return true;
        }

        static string SanMoveList(Board board, MoveList movelist, MoveList scratch)
        {
            var sanlist = new string[movelist.nmoves];
            for (int i=0; i < movelist.nmoves; ++i)
                sanlist[i] = board.MoveNotation(movelist.array[i], movelist, scratch);
            return string.Join(' ', sanlist.OrderBy(t => t));
        }

        static string NormalizeLineEndings(string raw)
        {
            // Handle the fact that hardcoded unit tests in this source code can
            // have their line endings changed on different operating systems,
            // and the editor format may not match the native OS format.
            bool r = raw.Contains('\r');
            bool n = raw.Contains('\n');

            if (r && n)     // Windows
                return raw.Replace("\r", "").Replace("\n", Environment.NewLine);

            if (r)          // Mac OS
                return raw.Replace("\r", Environment.NewLine);

            // Linux
            return raw.Replace("\n", Environment.NewLine);
        }

        static Puzzle[] PuzzleList =
        {
            new Puzzle(100, "Rc8#",  1, "6k1/5ppp/8/8/8/8/2R2K2/8 w - - 10 6"),                             // simple back-rank checkmate
            new Puzzle(3.500, "Rb8",   3, "5Q2/3bp3/p2q2k1/P1pP1ppp/1rP1p2P/4P1P1/5PN1/R5K1 b - - 0 36"),     // https://lichess.org/rntVQfLj/black#71
            new Puzzle(4.750, "Qxe7",  1, "r6k/p1qnrQ1p/2pb4/1p6/3P1BR1/2P3P1/PP3P1P/R5K1 w - - 1 22"),       // https://lichess.org/imjiXGEj/white#42
            new Puzzle(3.500, "Qxf7+", 3, "3rr1k1/R4ppp/8/1p2b3/3P4/1Q3N1q/1P3P2/5RK1 w - - 0 27"),           // https://lichess.org/8kw3bPuD/white#52
            new Puzzle(2.500, "d8h4",  3, "rn1q1rk1/ppp2ppp/3bp3/8/2B5/2P2Q2/P1PP1PPP/R1B2RK1 b - - 0 10"),   // https://lichess.org/training/62387
            new Puzzle(0.750, "f6c6",  5, "8/pp2k1rp/2b2R2/1p2pPP1/5n2/1PP5/P2K4/6R1 w - - 3 34"),            // https://lichess.org/training/62394
            new Puzzle(2.500, "a6b5",  5, "rn1qkb1r/3ppppp/b4n2/1NpP4/8/4P3/PP3PPP/R1BQKBNR b KQkq - 0 7"),   // https://lichess.org/training/62398
            new Puzzle(0.750, "a1e1",  3, "8/5pkp/4p1p1/6q1/P1r5/2N1n2P/1P2Q1P1/R5K1 w - - 1 28"),            // https://lichess.org/training/62414
            new Puzzle(3.750, "f2c5",  7, "4k2r/1p3p2/pn2pPp1/4B2p/5P2/P7/1P3QKP/1q6 w k - 0 32"),            // https://lichess.org/training/62417
            new Puzzle(3.000, "Be1",   8, "3r3k/p4Bbp/4Qnp1/2p1p3/3qP3/5PP1/Pr1B3P/R2R3K w - - 3 31"),        // https://lichess.org/UulmeeB6/white#60
            new Puzzle(100, "h4h2", 10, "7k/p1pn2p1/bp2p2r/4p2n/2PPNp1q/2PB1P2/P4QPP/R4RK1 b - - 8 20"),    // https://lichess.org/training/62644
            new Puzzle(100, "b3f7", 10, "5rk1/2R2ppp/1p3n2/6r1/3Pp2q/1Q2P2B/PN2bPPP/R6K w - - 5 26"),       // https://lichess.org/training/62646
            new Puzzle(3.250, "g6g5", 10, "8/1N6/3P1kp1/5p1p/R1P4P/6K1/3r4/6n1 b - - 0 42"),                  // https://lichess.org/training/62789
            new Puzzle(1.750, "d4g7",  9, "2rr2k1/p3ppbp/1p4p1/3n1b2/2NB4/6P1/PP2PPBP/R2R2K1 w - - 1 16"),    // https://lichess.org/training/62818
            new Puzzle(2.750, "d7f6", 10, "4k1r1/1pqnpp2/8/p1pRP2Q/P1P5/1P5P/1B4p1/6K1 b - - 0 36"),          // https://lichess.org/training/62867
        };

        static bool TestPuzzles(string[] args)
        {
            int HashTableSize = (args.Length > 0) ? int.Parse(args[0]) : 50000000;
            var board = new Board();
            var thinker = new Thinker(HashTableSize, new SimpleEvaluator());
            var legal = new MoveList();
            var scratch = new MoveList();
            int count = 0;
            int totalEvalCount = 0;
            var puzzleTime = new Stopwatch();
            var totalTime = Stopwatch.StartNew();
            Console.WriteLine("        move       score     time    evaluated [starting position]");
            foreach (Puzzle puzzle in PuzzleList)
            {
                board.SetPosition(puzzle.fen);
                board.GenMoves(legal);
                thinker.SetSearchLimit(puzzle.searchLimit);
                puzzleTime.Restart();
                Move move = thinker.Search(board);
                puzzleTime.Stop();
                string elapsed = puzzleTime.Elapsed.TotalSeconds.ToString("F3");
                string score = Score.Format(move.score);
                string san = board.MoveNotation(move, legal, scratch);
                string uci = move.ToString();
                string bestPathFormat = FormatBestPath(thinker, board);
                Console.WriteLine("PUZZLE: {0,-7} {1,8} {2,8} {3,12:n0} [{4}] {5}", san, score, elapsed, thinker.EvalCount, puzzle.fen, bestPathFormat);
                totalEvalCount += thinker.EvalCount;
                if (puzzle.movetext != san && puzzle.movetext != uci)
                {
                    Console.WriteLine("FAIL(TestPuzzles): expected {0}, found san={1}, uci={2}", puzzle.movetext, san, uci);
                    return false;
                }
                if (move.score < puzzle.minScore)
                {
                    Console.WriteLine("FAIL(TestPuzzles): expected min score {0}", Score.Format(puzzle.minScore));
                    return false;
                }
                ++count;
            }
            totalTime.Stop();
            Console.WriteLine("        -------          -------- ------------");
            Console.WriteLine("PASS:   {0,7}          {1,8} {2,12:n0}", count, totalTime.Elapsed.TotalSeconds.ToString("0.000"), totalEvalCount);

            if (args.Length > 1)
            {
                // Append statistics to an output csv file.
                string outFileName = args[1];

                if (!File.Exists(outFileName))
                    using (StreamWriter output = File.CreateText(outFileName))
                        output.WriteLine("\"HashTableSize\",\"TimeInSeconds\",\"EvalCount\"");

                using (StreamWriter output = File.AppendText(outFileName))
                    output.WriteLine("{0},{1},{2}", HashTableSize, totalTime.Elapsed.TotalSeconds, totalEvalCount);
            }

            return true;
        }

        private static string FormatBestPath(Thinker thinker, Board board)
        {
            BestPath bp = thinker.GetBestPath(board);
            string text = string.Join(" ", bp.nodes.Select(node => node.san));
            if (bp.isCircular)
                text += " .";
            return "(" + text + ")";
        }

        static bool TestDraws()
        {
            var board = Board.FromPgnText("1. Nf3 Nc6 2. Ng1 Nb8 3. Nf3 Nc6 4. Ng1 Nb8 5. Nf3");
            GameResult result = board.GetGameResult();
            if (result != GameResult.Draw)
            {
                Console.WriteLine("FAIL(TestDraws): Expected draw by repetition but found {0}", result);
                return false;
            }
            Console.WriteLine("PASS: Draws");
            return true;
        }
    }

    internal class Puzzle
    {
        public int minScore;
        public string movetext;     // can be either SAN (Qxb5+) or UCI (c4b5).
        public int searchLimit;
        public string fen;

        public Puzzle(double minScore, string movetext, int searchLimit, string fen)
        {
            this.minScore = (int)Math.Round(minScore * 1.0e6) - 20;     // allow up to depth 20 delay penalty
            this.movetext = movetext;
            this.searchLimit = searchLimit;
            this.fen = fen;
        }
    }
}
