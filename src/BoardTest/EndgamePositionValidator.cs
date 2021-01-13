using System;
using Gearbox;

namespace BoardTest
{
    internal class EndgamePositionValidator : IEndgamePositionVisitor
    {
        private const int EndgameMaxSearchPlies = 5;

        private Thinker egThinker;      // thinker that has endgame tables loaded
        private Thinker bfThinker;      // thinker for brute force search only
        private int nodeCount;
        private Square[] nonKingPieces;

        public EndgamePositionValidator(Thinker egThinker)
        {
            this.egThinker = egThinker;
            this.bfThinker = new Thinker(10000000, new NullEvaluator()) { Name = "Brute Force Thinker" };
            this.bfThinker.SetSearchLimit(EndgameMaxSearchPlies);
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

            if (!board.UncachedPlayerCanMove())
                return true;        // avoid calling Thinker.Search() when the game is over: it can't find a move!

            // Consult the thinker that knows about endgames.
            Move egMove = egThinker.Search(board);
            if (egMove.score == Score.Undefined)
            {
                Console.WriteLine("FAIL EndgamePositionValidator{0} @{1}: Undefined score returned by endgame thinker.", ConfigText(nonKingPieces), nodeCount);
                return false;
            }

            int plies;
            if (egMove.score > Score.WonForFriend)
            {
                // This side has a forced win, according to the endgame tables.
                // Is this within the search horizon we are willing to search with brute force?
                plies = Score.EnemyMated - egMove.score;
            }
            else if (egMove.score < Score.WonForEnemy)
            {
                // This side has a forced loss, according to the endgame tables.
                // Is this within the search horizon we are willing to search with brute force?
                plies = egMove.score - Score.FriendMated;
            }
            else
            {
                if (egMove.score != Score.Draw)
                {
                    Console.WriteLine("FAIL EndgamePositionValidator{0} @{1}: Unexpected score {2} from endgame thinker.", ConfigText(nonKingPieces), nodeCount, egMove.score);
                    return false;
                }
                // For drawn positions, we can't figure out the search horizon from the score.
                // Search these positions to maximum plies, and try to confirm that we find a draw after all.
                plies = EndgameMaxSearchPlies;
            }

            if (plies <= EndgameMaxSearchPlies)
            {
                // Reproduce the search without using endgame table knowledge.
                Move bfMove = bfThinker.Search(board);

                // Note: we DO NOT compare the moves, just the scores.
                // Because of random movelist shuffling, two different moves can be just as good.
                // But the scores have to match, or something is wrong.
                // Special case: if there is only one legal move, the score will be undefined.
                if (bfMove.score != egMove.score && bfMove.score != Score.Undefined)
                {
                    Console.WriteLine("FAIL EndgamePositionValidator{0} @{1}: brute force score = {2} ({3}), endgame table score = {4} ({5}), for {6}",
                        ConfigText(nonKingPieces),
                        nodeCount,
                        Score.Format(bfMove.score),
                        bfMove.score,
                        Score.Format(egMove.score),
                        egMove.score,
                        board.ForsythEdwardsNotation());

                    Console.WriteLine("brute force move   = {0}", bfMove);
                    Console.WriteLine("endgame table move = {0}", egMove);
                    return false;
                }
            }

            return true;
        }
    }
}