namespace Gearbox
{
    public class NullEvaluator : IPositionEvaluator
    {
        public int Eval(Board board)
        {
            // This evaluator can be used for solving checkmate puzzles,
            // where material and positional considerations do not matter.
            return 0;
        }
    }
}
