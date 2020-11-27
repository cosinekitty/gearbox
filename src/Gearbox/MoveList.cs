namespace Gearbox
{
    public class MoveList
    {
        // http://www.stmintz.com/ccc/index.php?id=424966
        // http://www.chess.com/forum/view/fun-with-chess/what-chess-position-has-the-most-number-of-possible-moves
        public const int MAX_MOVES = 256;

        public readonly Move[] array = new Move[MAX_MOVES];
        public int nmoves;

        public void Add(Move m)
        {
            array[nmoves++] = m;
        }

        // TODO: add Shuffle()
    }
}
