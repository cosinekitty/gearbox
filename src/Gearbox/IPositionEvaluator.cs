namespace Gearbox
{
    public interface IPositionEvaluator
    {
        int Eval(Board board);      // Evaluates relative to White. Caller must adjust for negamax.
    }
}
