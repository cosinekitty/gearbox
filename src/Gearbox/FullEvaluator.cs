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

            return score;
        }
    }
}
