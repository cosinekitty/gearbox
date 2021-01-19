using System;
using Gearbox;

namespace BoardTest
{
    internal class EndgameWalker : IEndgamePositionVisitor
    {
        private Thinker egThinker;      // thinker that has endgame tables loaded
        private Square[] nonKingPieces;
        private int nodeCount;
        private int whiteWonCount, blackWonCount, drawCount;    // count game-terminal positions
        private MoveList legalMoveList = new();

        public EndgameWalker(Thinker egThinker)
        {
            this.egThinker = egThinker;
        }

        public int NodeCount => nodeCount;
        public int WhiteWonCount => whiteWonCount;
        public int BlackWonCount => blackWonCount;
        public int DrawCount => drawCount;

        static string ConfigText(Square[] nonKingPieces)
        {
            return "[" + string.Join(", ", nonKingPieces) + "]";
        }

        public bool Start(Square[] nonKingPieces)
        {
            this.nonKingPieces = nonKingPieces;
            this.nodeCount = 0;
            this.whiteWonCount = 0;
            this.blackWonCount = 0;
            this.drawCount = 0;
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

            GameResult result = board.GetGameResult();

            switch (result)
            {
                case GameResult.WhiteWon:  ++whiteWonCount;  break;
                case GameResult.BlackWon:  ++blackWonCount;  break;
                case GameResult.Draw:      ++drawCount;      break;
            }

            switch (result)
            {
                case GameResult.InProgress:
                    if (parentScore == Score.FriendMated)
                    {
                        Console.WriteLine("EndgameWalker{0} @ {1}: checkmate score for game in progress.", ConfigText(nonKingPieces), nodeCount);
                        return false;
                    }

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
                            // Technically, checkmates can be POSSIBLE in cases like KB:kb, but not forcible.
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
                        Console.WriteLine("EndgameWalker{0} @ {1}: best score {2} ({3}) [{4}] does not match parent score {5} ({6}) in {7}",
                            ConfigText(nonKingPieces),
                            nodeCount,
                            bestmove.score,
                            Score.Format(bestmove.score),
                            bestmove,
                            parentScore,
                            Score.Format(parentScore),
                            board.ForsythEdwardsNotation());

                        return false;
                    }
                    return true;

                case GameResult.WhiteWon:
                case GameResult.BlackWon:
                    // The score must reflect that the current player has been checkmated.
                    if (parentScore != Score.FriendMated)
                    {
                        Console.WriteLine("EndgameWalker{0} @ {1}: Expected checkmated score {2} but found {3}", ConfigText(nonKingPieces), nodeCount, Score.FriendMated, parentScore);
                        return false;
                    }
                    return true;

                case GameResult.Draw:
                    // The score must reflect that the game has ended in a draw.
                    if (parentScore != Score.Draw)
                    {
                        Console.WriteLine("EndgameWalker{0} @ {1}: Expected draw score but found {2}", ConfigText(nonKingPieces), nodeCount, parentScore);
                        return false;
                    }
                    return true;

                default:
                    Console.WriteLine("EndgameWalker{0} @ {1}: Unknown game result {2}", ConfigText(nonKingPieces), nodeCount, result);
                    return false;
            }
        }
    }
}