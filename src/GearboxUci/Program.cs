// Implements the UCI protocol as described at
// http://wbec-ridderkerk.nl/html/UCIProtocol.html
// The Stockfish UCI parser has some helpful hints:
// https://github.com/mcostalba/Stockfish/blob/master/src/uci.cpp

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Gearbox;

namespace GearboxUci
{
    class Program
    {
        static bool debugMode;
        static bool exit;
        static AutoResetEvent signal = new AutoResetEvent(false);
        static Thinker thinker = new Thinker();
        static Board board = new Board();
        static SearchLimits searchLimits;

        static void WriteLine(StreamWriter log, string line)
        {
            log.WriteLine("  {0}", line);
            Console.WriteLine(line);
            Console.Out.Flush();
        }

        static void WriteLine(StreamWriter log, string format, params object[] args)
        {
            WriteLine(log, string.Format(format, args));
        }

        static int Main(string[] args)
        {
            using (StreamWriter log = File.CreateText("gearbox.log"))
            {
                var sink = new UciSearchInfoSink();
                thinker.SetInfoSink(sink);
                var thinkerThread = new Thread(ThinkerThreadFunc)
                {
                    IsBackground = true,
                    Name = "Gearbox Thinker",
                };
                thinkerThread.Start();

                string line;
                while (null != (line = Console.ReadLine()))
                {
                    log.WriteLine("> {0}", line);
                    string[] token = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (token.Length == 0)
                        continue;

                    if (token[0] == "quit")
                        break;

                    switch (token[0])
                    {
                        case "uci":
                            WriteLine(log, "id name Gearbox");
                            WriteLine(log, "id author Don Cross");
                            WriteLine(log, "uciok");
                            break;

                        case "isready":
                            WriteLine(log, "readyok");
                            break;

                        case "ucinewgame":
                            break;  // do nothing

                        case "position":
                            SetPosition(token);
                            break;

                        case "go":
                            Go(token);
                            break;

                        case "stop":
                            thinker.AbortSearch();
                            break;

                        case "debug":
                            debugMode = (token.Length == 2) && (token[1] == "on");
                            break;
                    }
                    log.Flush();
                }

                thinker.AbortSearch();
                exit = true;
                signal.Set();
                thinkerThread.Join();
                return 0;
            }
        }

        static void SetPosition(string[] token)
        {
            if (token.Length < 2)
                return;     // illegal UCI syntax for the "position" command.

            int index = 2;
            string fen = "";
            switch (token[1])
            {
                case "fen":
                    while (index < token.Length && token[index] != "moves")
                    {
                        if (fen.Length > 0)
                            fen += " ";
                        fen += token[index];
                        ++index;
                    }
                    break;

                case "startpos":
                    index = 2;
                    fen = Board.StandardSetup;
                    break;

                default:
                    return;     // cannot parse this command
            }

            if (index < token.Length && token[index] == "moves")
            {
                var legal = new MoveList();
                board.SetPosition(fen);
                while (++index < token.Length)
                {
                    board.GenMoves(legal);
                    Move move = legal.ToMoveArray().FirstOrDefault(m => m.ToString() == token[index]);
                    if (move.IsNull())
                        return;     // illegal move... give up
                    board.PushMove(move);
                }
            }
        }

        static void Go(string[] token)
        {
            // Parse the "go" command line.
            var limits = new SearchLimits();
            int index = 0;
            while (++index < token.Length)
            {
                if (token[index] == "searchmoves")
                {
                    // The remainder of the command is a list of moves to which to constrain our search.
                    var legal = new MoveList();
                    board.GenMoves(legal);
                    Move[] legalArray = legal.ToMoveArray();
                    while (++index < token.Length)
                    {
                        Move move = legalArray.FirstOrDefault(m => m.ToString() == token[index]);
                        if (!move.IsNull())
                            limits.searchMoves.Add(move);
                    }
                }
                else if (token[index] == "wtime")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.wtime);
                }
                else if (token[index] == "btime")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.btime);
                }
                else if (token[index] == "winc")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.winc);
                }
                else if (token[index] == "binc")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.binc);
                }
                else if (token[index] == "movestogo")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.movesToGo);
                }
                else if (token[index] == "depth")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.depth);
                }
                else if (token[index] == "nodes")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.nodes);
                }
                else if (token[index] == "movetime")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.moveTime);
                }
                else if (token[index] == "mate")
                {
                    if (++index < token.Length)
                        int.TryParse(token[index], out limits.mate);
                }
                else if (token[index] == "infinite")
                {
                    limits.infinite = true;
                }
                else if (token[index] == "ponder")
                {
                    limits.ponder = true;
                }
            }

            // FIXFIXFIX - Apply the settings to the search.
            searchLimits = limits;

            // Start thinking!
            signal.Set();
        }

        static void ThinkerThreadFunc()
        {
            using (StreamWriter log = File.CreateText("thinker.log"))
            {
                while (!exit && signal.WaitOne() && !exit)
                {
                    if (searchLimits != null)
                    {
                        int remainingMillis;
                        int incrementMillis;
                        if (board.IsWhiteTurn)
                        {
                            remainingMillis = searchLimits.wtime;
                            incrementMillis = searchLimits.winc;
                        }
                        else
                        {
                            remainingMillis = searchLimits.btime;
                            incrementMillis = searchLimits.binc;
                        }
                        log.WriteLine("# remainingMillis={0}, incrementMillis={1}", remainingMillis, incrementMillis);
                        log.Flush();
                        thinker.SetSearchTime(remainingMillis / 40);
                    }
                    Move move = thinker.Search(board);
                    WriteLine(log, "bestmove {0}", move);
                    log.Flush();
                }
            }
        }
    }
}
