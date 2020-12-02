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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gearbox;

namespace BoardTest
{
    class Program
    {
        static int Main(string[] args)
        {
            const string fenFileName = "final_positions.txt";

            int rc;
            if (0 != (rc = TestStandardSetup())) return rc;
            if (0 != (rc = TestGameTags())) return rc;
            if (0 != (rc = TestGameListing())) return rc;
            int gameCount;
            if (0 != (rc = TestLegalMoves("gearbox_move_test.txt", fenFileName, out gameCount))) return rc;
            if (0 != (rc = TestPgnLoader("testgames.pgn", fenFileName, gameCount))) return rc;
            if (0 != (rc = TestPuzzles())) return rc;
            return 0;
        }

        static int TestPgnLoader(string pgnFileName, string fenFileName, int expectedGameCount)
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
                        return 1;
                    }
                    if (calcFen != expectedFen)
                    {
                        Console.WriteLine("FAIL(TestPgnLoader): calculated FEN:");
                        Console.WriteLine(calcFen);
                        Console.WriteLine("Expected FEN:");
                        Console.WriteLine(expectedFen);
                        return 1;
                    }
                }
            }
            if (count != expectedGameCount)
            {
                Console.WriteLine("FAIL(TestPgnLoader): expected {0} games, found {1}.", expectedGameCount, count);
                return 1;
            }
            Console.WriteLine("PASS: PGN Loader ({0} games)", count);
            return 0;
        }

        static int TestGameListing()
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
                return 1;
            }

            Console.WriteLine("PASS: Game Listing");
            return 0;
        }

        static int TestGameTags()
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
                return 1;
            }
            Console.WriteLine("PASS: Game Tags");
            return 0;
        }

        static int TestStandardSetup()
        {
            var board = new Board();
            Console.WriteLine(board.Hash());
            string fen = board.ForsythEdwardsNotation();
            Console.WriteLine(fen);
            if (fen != Board.StandardSetup)
            {
                Console.WriteLine("FAIL: does not match standard setup.");
                return 1;
            }
            Console.WriteLine("PASS: Standard Setup");
            return 0;
        }

        static string[] Split(string line)
        {
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        static int TestLegalMoves(string inFileName, string outFileName, out int gameCount)
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
                        return 1;
                    }
                    Move[] marray = movelist.ToMoveArray();

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
                        return 1;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    Match m = reFlags.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("FAIL({0} line {1}): did not match flags regex", inFileName, lnum);
                        return 1;
                    }
                    bool correctCheck = (m.Groups[1].Value == "1");
                    bool correctMobility = (m.Groups[2].Value == "1");
                    if (correctCheck != board.IsPlayerInCheck())
                    {
                        Console.WriteLine("FAIL({0} line {1}): check should have been {2}", inFileName, lnum, correctCheck);
                        return 1;
                    }

                    if (correctMobility != board.PlayerCanMove())
                    {
                        Console.WriteLine("FAIL({0} line {1}): mobility should have been {2}", inFileName, lnum, correctMobility);
                        return 1;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    if (line != "")
                    {
                        Console.WriteLine("FAIL({0} line {1}): expected blank line.", inFileName, lnum);
                        return 1;
                    }
                }

                if (prevFen != null)
                    outfile.WriteLine(prevFen);
            }
            Console.WriteLine("PASS: LegalMoveTest for {0} games.", gameCount);
            return 0;
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
            new Puzzle("Rc8#", 1, "6k1/5ppp/8/8/8/8/2R2K2/8 w - - 10 6"),   // simple back-rank checkmate
            //new Puzzle("Be1", 6, "3r3k/p4Bbp/4Qnp1/2p1p3/3qP3/5PP1/Pr1B3P/R2R3K w - - 3 31"),  // https://lichess.org/UulmeeB6/white#60
        };

        static int TestPuzzles()
        {
            var board = new Board();
            var thinker = new Thinker();
            var legal = new MoveList();
            var scratch = new MoveList();
            int count = 0;
            foreach (Puzzle puzzle in PuzzleList)
            {
                board.SetPosition(puzzle.fen);
                board.GenMoves(legal);
                thinker.SetSearchLimit(puzzle.searchLimit);
                Move move = thinker.Search(board);
                string san = board.MoveNotation(move, legal, scratch);
                if (san != puzzle.san)
                {
                    Console.WriteLine("FAIL(TestPuzzles): {0}", puzzle.fen);
                    Console.WriteLine("Expected: {0}, Calculated: {1}", puzzle.san, san);
                    return 1;
                }
                ++count;
            }
            Console.WriteLine("PASS: {0} puzzles.", count);
            return 0;
        }
    }

    internal class Puzzle
    {
        public string san;
        public int searchLimit;
        public string fen;

        public Puzzle(string san, int searchLimit, string fen)
        {
            this.san = san;
            this.fen = fen;
            this.searchLimit = searchLimit;
        }
    }
}
