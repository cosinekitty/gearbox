﻿using System;
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
            if (0 != (rc = TestStandardSetup())) return rc;
            if (0 != (rc = TestLegalMoves("gearbox_move_test.txt"))) return rc;
            return 0;
        }

        static int TestStandardSetup()
        {
            var board = new Board();
            string fen = board.ForsythEdwardsNotation();
            Console.WriteLine(fen);
            if (fen != Board.StandardSetup)
            {
                Console.WriteLine("FAIL: does not match standard setup.");
                return 1;
            }
            Console.WriteLine("PASS: StandardSetup");
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
