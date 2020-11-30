﻿/*
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
using System.Text.RegularExpressions;
using Gearbox;

namespace BoardTest
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc;
            if (0 != (rc = TestGameTags())) return rc;
            if (0 != (rc = TestGameListing())) return rc;
            if (0 != (rc = TestStandardSetup())) return rc;
            if (0 != (rc = TestLegalMoves("gearbox_move_test.txt"))) return rc;
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
                Result = GameResult.BlackWon,
            };
            tags.SetTag("UTCDate", "2020.10.01");
            tags.SetTag("UTCTime", "07:57:31");
            tags.SetTag("Opening", "Caro-Kann Defense");
            tags.SetTag("TimeControl", "600+0");

            string listing = tags.ToString();

            Board board = BoardFromGame(@"e4 c6 d4 d5 Nc3 Nf6 e5 Ng8 f4 g6 Nf3 h5 Bd3 Nh6 O-O Bf5
                Nh4 Bxd3 Qxd3 e6 Bd2 Be7 Nf3 Nf5 Rae1 Nd7 Nd1 a6 Ne3 c5 c3 Rc8 Nxf5 gxf5
                Ng5 Nf8 h4 Ng6 g3 b5 Kh2 Qb6 b4 cxd4 cxd4 Rc4 Nf3 Bxb4 Rc1 Bxd2 Qxd2 O-O
                Rfd1 Rfc8 Rxc4 Rxc4 Qe2 Qa5 Ng5 Kf8 Qxh5 Qxa2+ Kh3 Rc2 Nf3 Rf2 Rh1 Qe2
                Qh6+ Ke8 Ng5 Qg4#");

            GameHistory history = board.GetGameHistory();
            listing += history.FormatMoveList(80);

            string expected =
                "[Event \"Rated Rapid game\"]\n" +
                "[Site \"https://lichess.org/JDY63HRY\"]\n" +
                "[Date \"2020.10.01\"]\n" +
                "[Round \"-\"]\n" +
                "[White \"Montzer\"]\n" +
                "[Black \"shotachkonia\"]\n" +
                "[Result \"0-1\"]\n" +
                "[Opening \"Caro-Kann Defense\"]\n" +
                "[TimeControl \"600+0\"]\n" +
                "[UTCDate \"2020.10.01\"]\n" +
                "[UTCTime \"07:57:31\"]\n" +
                "\n" +
                "1. e4 c6 2. d4 d5 3. Nc3 Nf6 4. e5 Ng8 5. f4 g6 6. Nf3 h5 7. Bd3 Nh6 8. O-O Bf5\n" +
                "Nh4 Bxd3 10. Qxd3 e6 11. Bd2 Be7 12. Nf3 Nf5 13. Rae1 Nd7 14. Nd1 a6 15. Ne3 c5\n" +
                "c3 Rc8 17. Nxf5 gxf5 18. Ng5 Nf8 19. h4 Ng6 20. g3 b5 21. Kh2 Qb6 22. b4 cxd4\n" +
                "cxd4 Rc4 24. Nf3 Bxb4 25. Rc1 Bxd2 26. Qxd2 O-O 27. Rfd1 Rfc8 28. Rxc4 Rxc4 29.\n" +
                "Qa5 30. Ng5 Kf8 31. Qxh5 Qxa2+ 32. Kh3 Rc2 33. Nf3 Rf2 34. Rh1 Qe2 35. Qh6+ Ke8\n" +
                "Ng5 Qg4#\n";

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

        private static Board BoardFromGame(string text)
        {
            var board = new Board();
            var legalMoves = new MoveList();
            var scratch = new MoveList();
            string[] list = Split(text);
            foreach (string san in list)
            {
                board.GenMoves(legalMoves);
                Move move = legalMoves.ToMoveArray().First(m => san == board.MoveNotation(m, legalMoves, scratch));
                board.PushMove(move);
            }
            return board;
        }

        static int TestGameTags()
        {
            const string expectedEmptyText = "[Event \"?\"]\n[Site \"?\"]\n[Date \"????.??.??\"]\n[Round \"?\"]\n[White \"?\"]\n[Black \"?\"]\n[Result \"*\"]\n\n";

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

        static int TestLegalMoves(string filename)
        {
            int gameCount = 0;
            var board = new Board();
            var movelist = new MoveList();
            var scratch = new MoveList();
            var reFlags = new Regex(@"check=([01]) mobile=([01])");
            using (StreamReader infile = File.OpenText(filename))
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
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    string[] token = Split(line);
                    if (token.Length != 2 || !int.TryParse(token[0], out ply))
                        throw new Exception("Bad input format in file: " + filename);
                    string moveAlg = token[1];

                    if (ply == 0)
                    {
                        ++gameCount;
                        board.Reset();
                    }

                    string fenBefore = board.ForsythEdwardsNotation();
                    board.GenMoves(movelist);
                    string fenAfter = board.ForsythEdwardsNotation();
                    if (fenBefore != fenAfter)
                    {
                        Console.WriteLine("FAIL({0} line {1}): GenMoves corrupted the board.", filename, lnum);
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
                    ++lnum;
                    string calcFen = board.ForsythEdwardsNotation();
                    if (correctFen != calcFen)
                    {
                        Console.WriteLine("FAIL({0} line {1}): FEN mismatch", filename, lnum);
                        Console.WriteLine("correct = {0}", correctFen);
                        Console.WriteLine("calc    = {0}", calcFen);
                        return 1;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    Match m = reFlags.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("FAIL({0} line {1}): did not match flags regex", filename, lnum);
                        return 1;
                    }
                    bool correctCheck = (m.Groups[1].Value == "1");
                    bool correctMobility = (m.Groups[2].Value == "1");
                    if (correctCheck != board.IsPlayerInCheck())
                    {
                        Console.WriteLine("FAIL({0} line {1}): check should have been {2}", filename, lnum, correctCheck);
                        return 1;
                    }

                    if (correctMobility != board.PlayerCanMove())
                    {
                        Console.WriteLine("FAIL({0} line {1}): mobility should have been {2}", filename, lnum, correctMobility);
                        return 1;
                    }

                    line = infile.ReadLine();
                    ++lnum;
                    if (line != "")
                    {
                        Console.WriteLine("FAIL({0} line {1}): expected blank line.", filename, lnum);
                        return 1;
                    }
                }
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
    }
}
