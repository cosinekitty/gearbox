using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Gearbox
{
    public class Thinker
    {
        private int maxSearchLimit;
        private int searchTimeMillis;
        private System.Timers.Timer searchTimer;
        private int quiescentCheckLimit = 3;
        private List<Stratum> stratumList = new List<Stratum>(100);
        private HashTable xpos = new HashTable(50000000);
        private int evalCount;
        private readonly object searchMutex = new object();
        private bool searchInProgress;
        private bool abort;
        private AutoResetEvent abortSignal = new AutoResetEvent(false);

        public Thinker()
        {
            searchTimer = new System.Timers.Timer();
            searchTimer.Elapsed += OnSearchTimeElapsed;
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
                    return stratum.legal.array[0];    // There is only one legal move, so return it.

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
            Stratum stratum = StratumForDepth(0);
            MoveList legal = stratum.legal;
            Move bestMove = Move.Null;
            for (int i=0; i < legal.nmoves; ++i)
            {
                board.PushMove(legal.array[i]);
                legal.array[i].score = -NegaMax(board, 1, limit, Score.NegInf, -bestMove.score, 0);
                board.PopMove();
                if (legal.array[i].score == Score.Undefined)
                    return Move.Null;   // signal aborted search
                if (legal.array[i].score > bestMove.score)
                    bestMove = legal.array[i];
            }
            if (bestMove.IsNull())
                throw new Exception("SearchRoot failed to find a move.");
            HashValue hash = board.Hash();
            xpos.Update(hash, bestMove, Score.NegInf, Score.PosInf, limit);
            return bestMove;
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

            // Is the game over? Score immediately if so.
            if (!board.PlayerCanMove())
            {
                if (board.IsPlayerInCheck())
                    return Score.CheckmateLoss(depth);

                return Score.Draw;
            }

            // If we have seen this position before,
            // see if we can recycle its value.
            // CONCERNS:
            // 1. draw by repetition may be missed.
            // 2. incorrect evaluation due to alpha-beta pruning.
            HashValue hash = board.Hash();
            HashEntry entry = xpos.Read(hash);
            if (entry.verify == hash.b)
            {
                if (entry.height >= limit - depth && entry.alpha <= alpha && entry.beta >= beta)
                    return entry.move.score;
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
                bestMove.score = Eval(board, depth);

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
                legal.array[mateMoveIndex].score = Score.EnemyMated;
                xpos.Update(hash, legal.array[mateMoveIndex], Score.NegInf, Score.PosInf, 1000);
                return Score.CheckmateWin(depth + 1);
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
                move.score = -NegaMax(board, 1 + depth, limit, -beta, -alpha, nextCheckCount);
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

        private int Eval(Board board, int depth)
        {
            ++evalCount;

            // Evaluate the board with scores relative to White.
            int score = 0;

            // Super simple material evaluation.
            for (int y = 21; y <= 91; y += 10)
            {
                for (int x = 0; x < 8; ++x)
                {
                    int ofs = x + y;
                    switch (board.square[ofs])
                    {
                        case Square.WP: score += Score.Pawn;    break;
                        case Square.BP: score -= Score.Pawn;    break;
                        case Square.WN: score += Score.Knight;  break;
                        case Square.BN: score -= Score.Knight;  break;
                        case Square.WB: score += Score.Bishop;  break;
                        case Square.BB: score -= Score.Bishop;  break;
                        case Square.WR: score += Score.Rook;    break;
                        case Square.BR: score -= Score.Rook;    break;
                        case Square.WQ: score += Score.Queen;   break;
                        case Square.BQ: score -= Score.Queen;   break;
                    }
                }
            }

            // Then, if it is actually Black's turn, negate the score for NegaMax.
            if (board.IsBlackTurn)
                score = -score;

            return score;
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
