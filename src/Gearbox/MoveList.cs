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

        public void Sort(int dir)
        {
            // Because move lists are fairly short, I believe
            // a cache-friendly O(n^2) selection sort is fine.
            // I may revisit this assumption later.
            for (int i=0; i+1 < nmoves; ++i)
            {
                int bestIndex = i;
                int bestScore = dir * array[i].score;
                for (int j=i+1; j < nmoves; ++j)
                {
                    int score = dir * array[j].score;
                    if (score < bestScore)
                    {
                        bestIndex = j;
                        bestScore = score;
                    }
                }
                if (bestIndex > i)
                {
                    Move swap = array[i];
                    array[i] = array[bestIndex];
                    array[bestIndex] = swap;
                }
            }
        }

        // TODO: add Shuffle()
    }
}
