using System;
using Gearbox;

namespace BoardTest
{
    internal class EndgameWalker : IEndgamePositionVisitor
    {
        private Thinker egThinker;      // thinker that has endgame tables loaded
        private Square[] nonKingPieces;
        private int nodeCount;
        private MoveList legalMoveList = new();

        public EndgameWalker(Thinker egThinker)
        {
            this.egThinker = egThinker;
        }

        public int NodeCount => nodeCount;

        static string ConfigText(Square[] nonKingPieces)
        {
            return "[" + string.Join(", ", nonKingPieces) + "]";
        }

        public bool Start(Square[] nonKingPieces)
        {
            this.nonKingPieces = nonKingPieces;
            this.nodeCount = 0;
            return true;
        }

        public bool Finish()
        {
            return true;
        }

        public bool Visit(Board board)
        {
            ++nodeCount;

            // Find the endgame table's score for the current position.

            if (!egThinker.EndgameEval(out int parentScore, board))
            {
                Console.WriteLine("EndgameWalker{0}: EndgameEval failed for PARENT {1}", ConfigText(nonKingPieces), board.ForsythEdwardsNotation());
                return false;
            }

            if (board.GetGameResult() == GameResult.InProgress)
            {
                Move bestmove = Move.Null;

                // Verify that all children have a consistent score.
                board.GenMoves(legalMoveList);
                for (int i = 0; i < legalMoveList.nmoves; ++i)
                {
                    Move move = legalMoveList.array[i];
                    board.PushMove(move);
                    if (!egThinker.EndgameEval(out int opponentScore, board))
                    {
                        // This can happen if the game is a draw, e.g. a capture results in insufficient mating material.
                        if (board.GetGameResult() != GameResult.Draw)
                        {
                            Console.WriteLine("EndgameWalker{0} @ {1}: EndgameEval failed for CHILD {2} in {3}",
                                ConfigText(nonKingPieces),
                                nodeCount,
                                move,
                                board.ForsythEdwardsNotation());

                            return false;
                        }
                        opponentScore = Score.Draw;
                    }
                    board.PopMove();

                    move.score = Score.OnePlyDelay(-opponentScore);
                    if (move.score > bestmove.score)
                        bestmove = move;
                }

                if (bestmove.IsNull())
                {
                    Console.WriteLine("EndgameWalker{0} @ {1}: best move was NULL", ConfigText(nonKingPieces), nodeCount);
                    return false;
                }

                if (bestmove.score != parentScore)
                {
                    Console.WriteLine("EndgameWalker{0} @ {1}: best score {2} does not match parent score {3} in {4}",
                        ConfigText(nonKingPieces),
                        nodeCount,
                        bestmove.score,
                        parentScore,
                        board.ForsythEdwardsNotation());

                    return false;
                }
            }

            return true;
        }
    }
}