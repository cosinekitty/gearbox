using System;

namespace Gearbox
{
    public class FullEvaluator : IPositionEvaluator
    {
        public int Eval(Board board)
        {
            int score =
                Score.Pawn   * (board.inventory[(int)Square.WP] - board.inventory[(int)Square.BP]) +
                Score.Knight * (board.inventory[(int)Square.WN] - board.inventory[(int)Square.BN]) +
                Score.Bishop * (board.inventory[(int)Square.WB] - board.inventory[(int)Square.BB]) +
                Score.Rook   * (board.inventory[(int)Square.WR] - board.inventory[(int)Square.BR]) +
                Score.Queen  * (board.inventory[(int)Square.WQ] - board.inventory[(int)Square.BQ]);

            if (board.whiteBishopsOnColor[0] > 0 && board.whiteBishopsOnColor[1] > 0)
                score += Score.BishopsOnOppositeColors;

            if (board.blackBishopsOnColor[0] > 0 && board.blackBishopsOnColor[1] > 0)
                score -= Score.BishopsOnOppositeColors;

            score += PawnScore(board, Square.WP, Square.BP, 7, -1, Direction.N);
            score -= PawnScore(board, Square.BP, Square.WP, 2, +1, Direction.S);

            return score;
        }

        private static readonly int[] PawnPositionScore = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

            //        a          b          c          d          e          f          g          h
            0,  -400000,   -100000,    -50000,         0,         0,    -50000,   -100000,   -400000,   0,   // 2
            0,  -350000,   +100000,   +120000,   +150000,   +150000,   +120000,   +100000,   -350000,   0,   // 3
            0,  -300000,   +120000,   +150000,   +200000,   +200000,   +150000,   +120000,   -300000,   0,   // 4
            0,  -200000,   +150000,   +250000,   +300000,   +300000,   +250000,   +150000,   -200000,   0,   // 5
            0,  -100000,   +250000,   +400000,   +500000,   +500000,   +400000,   +250000,   -100000,   0,   // 6
            0,  +150000,   +350000,   +500000,   +700000,   +700000,   +500000,   +350000,   +150000,   0    // 7
        };

        private static readonly int[] PassedPawnPositionScore = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

            //        a          b          c          d          e          f          g          h
            0,  -200000,   +200000,   +250000,         0,         0,   +250000,   +200000,   -200000,   0,   // 2
            0,  -150000,   +300000,   +350000,   +450000,   +450000,   +350000,   +300000,   -150000,   0,   // 3
            0,  -100000,   +350000,   +450000,   +550000,   +550000,   +450000,   +350000,   -100000,   0,   // 4
            0,        0,   +500000,   +550000,   +700000,   +700000,   +550000,   +500000,         0,   0,   // 5
            0,  +100000,   +700000,   +850000,  +1500000,  +1500000,   +850000,   +700000,   +100000,   0,   // 6
            0,  +250000,  +1200000,  +1500000,  +2000000,  +2000000,  +1500000,  +1200000,   +250000,   0    // 7
        };

        private const int IsolationPenalty     = -300000;
        private const int DoubledPenalty       = -300000;
        private const int PawnProtectPawnBonus = +200000;
        private const int PawnProtectPassedPawnBonus = +400000;

        private int[] FriendPawnsInColumn = new int[10];
        private int[] EnemyPawnsInColumn = new int[10];

        private int PawnScore(
            Board board,
            Square friendPawn,
            Square enemyPawn,
            int enemyHomeRank,
            int rankIncrement,
            int dir)
        {
            int score = 0;
            Square[] square = board.GetSquaresArray();

            Array.Clear(FriendPawnsInColumn, 0, 10);
            Array.Clear(EnemyPawnsInColumn, 0, 10);

            // The 't_rank' loop is from the point of view of the given side.
            // We start from the enemy side of the board and work toward the friend side,
            // to make it more efficient to detect passed pawns.
            // b_rank is the "board rank", used to calculate the actual board offset.
            int b_rank = enemyHomeRank;
            for (int t_rank = 7; t_rank <= 2; --t_rank)
            {
                for (int file = 1; file <= 8; ++file)
                {
                    int b_ofs = 10*(b_rank + 1) + file;     // board offset
                    int t_ofs = 10*(t_rank + 1) + file;     // lookup table offset
                    if (square[b_ofs] == friendPawn)
                    {
                        ++FriendPawnsInColumn[file];

                        // Is this a passed pawn?
                        int blockers = EnemyPawnsInColumn[file-1] + EnemyPawnsInColumn[file] + EnemyPawnsInColumn[file+1];
                        if (blockers == 0)
                        {
                            // Yes, a passed pawn.
                            score += PassedPawnPositionScore[t_ofs];

                            // Bonuses for other pawn(s) protecting this passed pawn.
                            if (square[b_ofs - dir + Direction.E] == friendPawn)
                                score += PawnProtectPassedPawnBonus;

                            if (square[b_ofs - dir + Direction.W] == friendPawn)
                                score += PawnProtectPassedPawnBonus;
                        }
                        else
                        {
                            // Not a passed pawn.

                            // Bonuses for other pawn(s) protecting this non-passed pawn.
                            if (square[b_ofs - dir + Direction.E] == friendPawn)
                                score += PawnProtectPawnBonus;

                            if (square[b_ofs - dir + Direction.W] == friendPawn)
                                score += PawnProtectPawnBonus;
                        }
                    }
                }

                // Second pass through this rank: count enemy pawns per column.
                // We do this in a separate pass and AFTER looking for passed
                // friendly pawns in this rank, so we don't count enemy pawns
                // as blocking friendly pawns in the same rank.
                for (int file = 1; file <= 8; ++file)
                {
                    int b_ofs = 10*(b_rank + 1) + file;     // board offset
                    if (square[b_ofs] == enemyPawn)
                        ++EnemyPawnsInColumn[file];
                }

                b_rank += rankIncrement;
            }

            for (int file=1; file <= 8; ++file)
            {
                if (FriendPawnsInColumn[file] > 0)
                {
                    // Isolation penalty if a column of pawns has no possible assistance on either side.
                    if (FriendPawnsInColumn[file-1] == 0 && FriendPawnsInColumn[file+1] == 0)
                        score += FriendPawnsInColumn[file] * IsolationPenalty;

                    // Penalty for doubling up pawns.
                    if (FriendPawnsInColumn[file] > 1)
                        score += (FriendPawnsInColumn[file] - 1) * DoubledPenalty;
                }
            }

            return score;
        }
    }
}
