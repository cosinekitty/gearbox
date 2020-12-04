namespace Gearbox
{
    internal struct HashEntry
    {
        public ulong verify;    // the "b" value of the total hash code, for verifying the entry
        public Move  move;      // the best response (or null move) and score (or the one that caused pruning)
        public int   alpha;     // the negamax alpha value when this node was evaluated
        public int   beta;      // the negamax beta value when this node was evaluated
        public int   height;    // the search depth beneath this node
    }

    internal class HashTable
    {
        private HashEntry[] array;

        public HashTable(int size)
        {
            array = new HashEntry[size];
        }

        public HashEntry Read(HashValue hash)
        {
            int index = (int)(hash.a % (ulong)array.Length);
            return array[index];
        }

        public void Update(HashValue hash, Move move, int alpha, int beta, int height)
        {
            int index = (int)(hash.a % (ulong)array.Length);
            array[index] = new HashEntry
            {
                verify = hash.b,
                move = move,
                alpha = alpha,
                beta = beta,
                height = height,
            };
        }
    }
}
