﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gearbox
{
    public class Thinker
    {
        private int maxSearchLimit;
        private List<Stratum> stratumList = new List<Stratum>();

        public void SetSearchLimit(int maxSearchLimit)
        {
            this.maxSearchLimit = maxSearchLimit;
        }

        public Move Search(Board board)
        {
            // Iterative deepening search.
            var stratum = StratumForDepth(0);
            board.GenMoves(stratum.legal);
            if (stratum.legal.nmoves == 0)
                throw new Exception("There are no legal moves in this position.");

            if (stratum.legal.nmoves == 1)
                return stratum.legal.array[0];    // There is only one legal move, so return it.

            Move bestMove = SearchRoot(board, 1);
            for (int limit = 2; limit < maxSearchLimit; ++limit)
            {
                bestMove = SearchRoot(board, limit);
            }
            return bestMove;
        }

        private Move SearchRoot(Board board, int limit)
        {
            Stratum stratum = StratumForDepth(0);
            MoveList legal = stratum.legal;
            Move bestMove = new Move();
            for (int i=0; i < legal.nmoves; ++i)
            {
                Move move = legal.array[i];
                board.PushMove(move);
                move.score = -NegaMax(board, 1, limit);
                board.PopMove();
                if (move.score > bestMove.score)
                    bestMove = move;
            }
            return bestMove;
        }

        private int NegaMax(Board board, int depth, int limit)
        {
            // Is the game over? Score immediately if so.
            if (!board.PlayerCanMove())
            {
                if (board.IsPlayerInCheck())
                    return Score.Checkmate(depth);

                return Score.Draw;
            }

            if (depth >= limit)
                return Eval(board, depth);

            Stratum stratum = StratumForDepth(depth);
            MoveList legal = stratum.legal;
            board.GenMoves(legal);
            int bestScore = Score.NegInf;
            for (int i = 0; i < legal.nmoves; ++i)
            {
                Move move = legal.array[i];
                board.PushMove(move);
                int score = -NegaMax(board, 1 + depth, limit);
                board.PopMove();
                if (score > bestScore)
                    bestScore = score;
            }

            return bestScore;
        }

        private int Eval(Board board, int depth)
        {
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
                        case Square.WP: score += 1000000; break;
                        case Square.BP: score -= 1000000; break;
                        case Square.WN: score += 2900000; break;
                        case Square.BN: score -= 2900000; break;
                        case Square.WB: score += 3100000; break;
                        case Square.BB: score -= 3100000; break;
                        case Square.WR: score += 5000000; break;
                        case Square.BR: score -= 5000000; break;
                        case Square.WQ: score += 9000000; break;
                        case Square.BQ: score -= 9000000; break;
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