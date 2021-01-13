using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Timers;

namespace Gearbox
{
    public class Thinker
    {
        private int maxSearchLimit;
        private int searchTimeMillis;
        private System.Timers.Timer searchTimer = new();
        private int quiescentCheckLimit = 3;
        private List<Stratum> stratumList = new(100);
        private HashTable xpos;
        private int evalCount;
        private readonly object searchMutex = new();
        private bool searchInProgress;
        private bool abort;
        private AutoResetEvent abortSignal = new(false);
        private ISearchInfoSink sink;       // Supports sending notifications about the search to a user interface or debug log
        private readonly Dictionary<long, IEndgameTable> endgameTableForConfigId = new();
        private IPositionEvaluator evaluator;

        public Thinker(int hashTableSize, IPositionEvaluator evaluator)
        {
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));
            this.evaluator = evaluator;
            NewHashTable(hashTableSize);
            searchTimer.Elapsed += OnSearchTimeElapsed;
            Name = "Gearbox";
        }

        public Thinker(int hashTableSize)
            : this(hashTableSize, new FullEvaluator())
        {
        }

        public string Name { get; set; }    // an arbitrary name of the thinker; defaults to "Gearbox".

        public int HashTableSize => xpos.Size;

        public void NewHashTable(int size)
        {
            xpos = new HashTable(size);
        }

        public void ClearHashTable()
        {
            xpos.Clear();
        }

        public void SetInfoSink(ISearchInfoSink sink)
        {
            this.sink = sink;
        }

        public void SetSearchLimit(int maxSearchLimit)
        {
            this.maxSearchLimit = maxSearchLimit;
            this.searchTimeMillis = 0;
        }

        public void SetSearchTime(int millis)
        {
            this.maxSearchLimit = 100;
            this.searchTimeMillis = Math.Max(1, millis);
        }

        public int EvalCount
        {
            get { return evalCount; }
        }

        public void UnloadEndgameTables()
        {
            endgameTableForConfigId.Clear();
        }

        public int LoadEndgameTables(string dir)    // returns the number of table files loaded
        {
            UnloadEndgameTables();

            int count = 0;

            if (dir == null)
                dir = Directory.GetCurrentDirectory();

            if (Directory.Exists(dir))
            {
                dir = Path.GetFullPath(dir);

                // Currently support endgame tables that can fit in memory only.
                // This means nonking piece count <= 2.

                foreach (string fn in Directory.EnumerateFiles(dir))
                {
                    // Valid filenames look like this: 1001000000.endgame.
                    // The first is the configuration ID, which is a decimal number
                    // whose digits represent how many of each piece there are,
                    // in the order QqRrBbNnPp.
                    if (Path.GetExtension(fn) == ".endgame")
                    {
                        string config_text = Path.GetFileNameWithoutExtension(fn);
                        if (long.TryParse(config_text, out long config_id) && config_id >= 0 && config_id <= 9999999999)
                        {
                            // Currently only in-memory tables are supported.
                            // Tables larger than 2 nonking pieces will not fit in memory.
                            // Later I will consider support tables based on seeking in a file.
                            int nonking = NonKingPieceCount(config_id);
                            switch (nonking)
                            {
                                case 1:
                                case 2:
                                    endgameTableForConfigId[config_id] = MemoryEndgameTable.Load(fn);
                                    ++count;
                                    break;

                                default:
                                    // do nothing
                                    break;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private int NonKingPieceCount(long config_id)
        {
            int count = 0;
            while (config_id > 0)
            {
                count += (int)(config_id % 10);
                config_id /= 10;
            }
            return count;
        }

        public Move Search(Board board)
        {
            lock (searchMutex)
            {
                abort = false;
                searchInProgress = true;
            }

            if (searchTimeMillis > 0)
            {
                searchTimer.Interval = (double)searchTimeMillis;
                searchTimer.Enabled = true;
            }

            try
            {
                // Iterative deepening search.
                evalCount = 0;
                var stratum = StratumForDepth(0);
                board.GenMoves(stratum.legal);
                if (stratum.legal.nmoves == 0)
                    throw new Exception("There are no legal moves in this position.");

                if (stratum.legal.nmoves == 1)
                {
                    // There is only one legal move, so return it.
                    // However, if possible, calculate the exact value of the starting position.
                    // This way, the endgame tables can still help us report forced mates.
                    // But if we don't know the exact score, return Score.Undefined;
                    // this helps test the code, to find places where we should have found
                    // an endgame score but didn't.
                    Move only = stratum.legal.array[0];
                    board.PushMove(only);
                    if (TerminalEval(out int terminalScore, board, 0))
                        only.score = Score.OnePlyDelay(-terminalScore);
                    board.PopMove();
                    return only;
                }

                // Shuffle the move list to eliminate any bias
                // introduced by the legal move generator.
                // This is a way of giving the computer more interesting
                // play when there are different options judged equal.
                stratum.legal.Shuffle();

                Move bestMove = SearchRoot(board, 1);
                if (bestMove.IsNull())
                    return stratum.legal.array[0];    // search was aborted. return random legal move.

                for (int limit = 2; limit < maxSearchLimit; ++limit)
                {
                    if (bestMove.score >= Score.WonForFriend)
                        break;      // we found an optimal forced mate, so there is no reason to keep working

                    if (bestMove.score <= Score.WonForEnemy)
                        break;      // we are going to lose... give up and cry!

                    // Sort in descending order by score.
                    stratum.legal.Sort();

                    // Even though we sorted, pruning can make unequal moves appear equal.
                    // Therefore, always put the very best move we found previously at
                    // the front of the list. This helps improve pruning for the deeper search
                    // we are about to do.
                    stratum.legal.MoveToFront(bestMove);

                    // Search one level deeper.
                    Move tryMove = SearchRoot(board, limit);
                    if (tryMove.IsNull())
                        break;  // search was aborted; keep the best move we found from the previous search

                    bestMove = tryMove;
                }
                return bestMove;
            }
            finally
            {
                searchTimer.Enabled = false;

                lock (searchMutex)
                {
                    searchInProgress = false;
                    if (abort)
                    {
                        abort = false;
                        abortSignal.Set();
                    }
                }
            }
        }

        private void OnSearchTimeElapsed(object source, ElapsedEventArgs e)
        {
            AbortSearch();
        }

        public bool AbortSearch()
        {
            // If a search is in progress, tell the thinker thread to abort the search.
            bool initiatedAbort = false;
            lock (searchMutex)
            {
                if (searchInProgress && !abort)
                {
                    abort = true;
                    initiatedAbort = true;
                }
            }

            // If we initiated the abort, wait for it to complete and return true.
            // Otherwise do nothing and return false.
            return initiatedAbort && abortSignal.WaitOne();
        }

        public BestPath GetBestPath(Board board)
        {
            var nodeList = new List<BestPathNode>();
            var legal = new MoveList();
            var scratch = new MoveList();
            bool isCircular = false;

            // Use the transposition table to find the principal variation.
            // Go as far into the future as possible.
            // Use the board to calculate hash codes for the lookup,
            // and keep making the predicted moves into the board.
            while (!isCircular)
            {
                HashValue hash = board.Hash();
                HashEntry entry = xpos.Read(hash);
                if (entry.verify != hash.b)
                    break;

                board.GenMoves(legal);
                if (!legal.Contains(entry.move))
                    break;

                foreach (BestPathNode prior in nodeList)
                {
                    if (prior.hash.a == hash.a && prior.hash.b == hash.b)
                    {
                        isCircular = true;
                        break;
                    }
                }

                string san = board.MoveNotation(entry.move, legal, scratch);
                string uci = entry.move.ToString();

                board.PushMove(entry.move);
                nodeList.Add(new BestPathNode { move = entry.move, uci = uci, san = san, hash = hash });
            }

            // Rewind all the changes we made to the board.
            for (int i = 0; i < nodeList.Count; ++i)
                board.PopMove();

            return new BestPath
            {
                isCircular = isCircular,
                nodes = nodeList.ToArray(),
            };
        }

        private Move SearchRoot(Board board, int limit)
        {
            HashValue hash = board.Hash();
            Stratum stratum = StratumForDepth(0);
            MoveList legal = stratum.legal;
            Move bestMove = Move.Null;
            for (int i=0; i < legal.nmoves; ++i)
            {
                if (sink != null)
                    sink.OnBeginSearchMove(board, legal.array[i], limit);
                board.PushMove(legal.array[i]);
                legal.array[i].score = Score.OnePlyDelay(-NegaMax(board, 1, limit, Score.NegInf, -bestMove.score, 0));
                board.PopMove();
                if (legal.array[i].score == Score.Undefined)
                    return Move.Null;   // signal aborted search
                if (legal.array[i].score > bestMove.score)
                {
                    bestMove = legal.array[i];
                    xpos.Update(hash, bestMove, Score.NegInf, Score.PosInf, limit);
                    if (sink != null)
                    {
                        BestPath path = GetBestPath(board);
                        sink.OnBestPath(board, path);
                    }
                }
            }
            if (bestMove.IsNull())
                throw new Exception("SearchRoot failed to find a move.");
            return bestMove;
        }

        public bool EndgameEval(out int score, Board board)
        {
            if (endgameTableForConfigId.Count > 0)
            {
                // We have at least one endgame table loaded.
                // Check to see if we can use an endgame table in this position.
                // How many non-king pieces exist on the board?
                int table_index;
                IEndgameTable table;

                long config_id = board.GetEndgameConfigId(false);
                if (endgameTableForConfigId.TryGetValue(config_id, out table))
                {
                    table_index = board.GetEndgameTableIndex(false);
                    score = table.GetScore(table_index, board.IsWhiteTurn);
                    return true;
                }

                // Try again, swapping Black and White.
                config_id = board.GetEndgameConfigId(true);
                if (endgameTableForConfigId.TryGetValue(config_id, out table))
                {
                    table_index = board.GetEndgameTableIndex(true);
                    score = table.GetScore(table_index, board.IsBlackTurn);
                    return true;
                }
            }

            score = Score.Undefined;
            return false;
        }

        private bool TerminalEval(out int score, Board board, int depth)
        {
            // Is the game over? Score immediately if so.
            switch (board.GetGameResult())
            {
                case GameResult.BlackWon:
                case GameResult.WhiteWon:
                    score = Score.FriendMated;
                    return true;

                case GameResult.Draw:
                    score = Score.Draw;
                    return true;
            }

            // If we are deeper than the moves generated by SearchRoot,
            // then treat any repeated position as a draw.
            // This keeps us from going in circles when we are winning,
            // and motivates us to force a draw when losing.
            // It also helps prune out entire chunks of the tree.
            if (depth > 1 && board.RepCount() == 1)
            {
                score = Score.Draw;
                return true;
            }

            // Check to see if this is a known endgame position.
            // If so, we can immediately determine the value of the position
            // without searching any deeper in the tree.
            return EndgameEval(out score, board);
        }

        private int NegaMax(Board board, int depth, int limit, int alpha, int beta, int checkCount)
        {
            if (abort)
            {
                // The search has been aborted.
                // Start an upward cascade of undoing moves to the board
                // and popping out of all levels of recursion.
                return Score.Undefined;
            }

            // Look for situations where we can immediately evaluate the position with certainty.
            // Examples: game is over, or endgame tables know the score.
            if (TerminalEval(out int terminalScore, board, depth))
                return terminalScore;

            // If we have seen this position before, see if we can recycle its value.
            HashValue hash = board.Hash();
            HashEntry entry = xpos.Read(hash);

            if (entry.verify == hash.b)
            {
                if (entry.height >= limit - depth && entry.alpha <= alpha && entry.beta >= beta)
                {
                    // Recycling forced-mate scores was messing up picking moves that lead to forced mates.
                    // The problem: sometimes Search() receives a partial view of the forced mates
                    // and stops searching at a small limit, before brute force search has the
                    // opportunity to find alternative moves that mate even faster.
                    // Better alternatives not in the hash table require a deeper limit.
                    // So now we recycle scores only when they can't possibly trigger an early exit.

                    if (entry.move.score > Score.WonForEnemy && entry.move.score < Score.WonForFriend)
                        return entry.move.score;
                }
            }

            Move bestMove = Move.Null;
            Stratum stratum = StratumForDepth(depth);
            MoveList legal = stratum.legal;
            MoveGen opt;
            if (depth < limit)
            {
                // Full-width search: generate all legal moves for this position.
                opt = MoveGen.All;
            }
            else
            {
                // Quiescence search.
                // Start with the evaluation of the current node only.
                bestMove.score = Eval(board);

                // Consider "doing nothing" a move; it is a valid way to interpret quiescence.
                if (bestMove.score >= beta)
                    goto prune;

                if (bestMove.score > alpha)
                    alpha = bestMove.score;

                // Examine "special" moves only: all captures and a limited number of checks.
                if (checkCount < quiescentCheckLimit)
                    opt = MoveGen.ChecksAndCaptures;
                else
                    opt = MoveGen.Captures;
            }

            board.GenMoves(legal, opt);
            int mateMoveIndex = OrderMoves(board, legal);
            if (mateMoveIndex >= 0)
            {
                // There is at least one move in the list that causes checkmate.
                // Store the move in the hash table, just so it shows up in the best path.
                // Set the alpha, beta, and height to be as inclusive as possible, because
                // this score is absolutely reliable in all cases.
                legal.array[mateMoveIndex].score = Score.OnePlyDelay(Score.EnemyMated);
                xpos.Update(hash, legal.array[mateMoveIndex], Score.NegInf, Score.PosInf, 1000);
                return legal.array[mateMoveIndex].score;
            }

            // We did not find a move that causes an immediate checkmate.
            // See if we can improve move ordering using previous work saved in the hash table.
            if (entry.verify == hash.b)
                legal.MoveToFront(entry.move);

            for (int i = 0; i < legal.nmoves; ++i)
            {
                Move move = legal.array[i];
                board.PushMove(move);
                int nextCheckCount = checkCount;
                if (opt == MoveGen.ChecksAndCaptures)
                {
                    // We generated a list that contains three categories of moves:
                    // 1. Non-capture moves that cause check.
                    // 2. Captures that cause check.
                    // 3. Captures that do not cause check.
                    // In case #1 only, we "burn" one of our chances to explore these
                    // non-captures in the quiescence search.
                    if (0 == (move.flags & MoveFlags.Capture))
                        ++nextCheckCount;
                }
                move.score = Score.OnePlyDelay(-NegaMax(board, 1 + depth, limit, -beta, -alpha, nextCheckCount));
                board.PopMove();

                if (move.score == Score.Undefined)
                    return Score.Undefined;     // Continue unwinding from an aborted search.

                if (move.score > bestMove.score)
                    bestMove = move;

                if (move.score >= beta)
                    goto prune;      // This move is TOO GOOD... opponent has better (or equal) options than this position.

                if (move.score > alpha)
                    alpha = move.score;
            }
        prune:
            xpos.Update(hash, bestMove, alpha, beta, limit - depth);
            return bestMove.score;
        }

        private static int OrderMoves(Board board, MoveList legal)
        {
            // Preliminary sort: assume captures cause more pruning than non-captures.
            // Try more valuable captures/promotions before less valuable ones.
            for (int i = 0; i < legal.nmoves; ++i)
            {
                Move move = legal.array[i];
                int score = 0;

                if (0 != (move.flags & MoveFlags.Check))
                {
                    if (0 != (move.flags & MoveFlags.Immobile))
                    {
                        // Immediate checkmate! There is no better possible move.
                        // There is no need to look at any other moves, or to sort the list.
                        // Tell the caller the index of the move that causes checkmate.
                        return i;
                    }
                    score += Score.CheckBonus;      // small bonus for checking moves
                }
                else if (0 != (move.flags & MoveFlags.Immobile))
                {
                    // Immediate stalemate. No other factors matter (material, etc.)
                    legal.array[i].score = Score.Draw;
                    continue;
                }

                // Give a material bonus for pawn promotions.
                switch (move.prom)
                {
                    case 'q': score += Score.Queen;  break;
                    case 'r': score += Score.Rook;   break;
                    case 'b': score += Score.Bishop; break;
                    case 'n': score += Score.Knight; break;
                }

                // Give a material bonus for captures.
                switch (board.square[move.dest] & Square.PieceMask)
                {
                    case Square.Queen:  score += Score.Queen;  break;
                    case Square.Rook:   score += Score.Rook;   break;
                    case Square.Bishop: score += Score.Bishop; break;
                    case Square.Knight: score += Score.Knight; break;
                    case Square.Pawn:   score += Score.Pawn;   break;
                    case Square.Empty:
                        if ((move.prom == '\0') && 0 != (move.flags & MoveFlags.Capture))
                            score += Score.Pawn;    // en passant capture
                        break;
                }

                // Give a reduced material penalty for capturing with larger pieces.
                if (0 != (move.flags & MoveFlags.Capture))
                {
                    const int denom = 100;
                    switch (board.square[move.source] & Square.PieceMask)
                    {
                        case Square.Queen:  score -= Score.Queen / denom;  break;
                        case Square.Rook:   score -= Score.Rook / denom;   break;
                        case Square.Bishop: score -= Score.Bishop / denom; break;
                        case Square.Knight: score -= Score.Knight / denom; break;
                        case Square.Pawn:   score -= Score.Pawn / denom;   break;
                    }
                }

                legal.array[i].score = score;
            }
            legal.Sort();
            return -1;       // did not find checkmate
        }

        private int Eval(Board board)
        {
            ++evalCount;

            // Evaluate the board with scores relative to White.
            int score = evaluator.Eval(board);

            // Convert the White-relative score to player-relative, negamax score.
            return board.IsBlackTurn ? -score : score;
        }

        private Stratum StratumForDepth(int depth)
        {
            while (stratumList.Count <= depth)
                stratumList.Add(new Stratum());

            return stratumList[depth];
        }
    }

    internal class Stratum
    {
        public MoveList legal = new MoveList();
    }
}
