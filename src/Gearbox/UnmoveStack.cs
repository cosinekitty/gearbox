using System;

namespace Gearbox
{
    internal class UnmoveStack
    {
        public Unmove[] array = new Unmove[400];    // start with enough for a game with 200 moves
        public int height;

        public void Push(Unmove unmove)
        {
            if (height == array.Length)
            {
                // The array is already full.
                // Double the length of the array.
                var longer = new Unmove[2 * array.Length];
                Array.Copy(array, longer, height);
                array = longer;
            }
            array[height++] = unmove;
        }

        public Unmove Pop()
        {
            if (height == 0)
                throw new Exception("Attempt to pop from empty unmove stack.");

            return array[--height];
        }

        public void Reset()
        {
            height = 0;
        }
    }
}
