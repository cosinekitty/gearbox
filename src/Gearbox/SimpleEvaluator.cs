namespace Gearbox
{
    public class SimpleEvaluator : IPositionEvaluator
    {
        public int Eval(Board board)
        {
            return
                Score.Pawn   * (board.inventory[(int)Square.WP] - board.inventory[(int)Square.BP]) +
                Score.Knight * (board.inventory[(int)Square.WN] - board.inventory[(int)Square.BN]) +
                Score.Bishop * (board.inventory[(int)Square.WB] - board.inventory[(int)Square.BB]) +
                Score.Rook   * (board.inventory[(int)Square.WR] - board.inventory[(int)Square.BR]) +
                Score.Queen  * (board.inventory[(int)Square.WQ] - board.inventory[(int)Square.BQ]);
        }
    }
}
