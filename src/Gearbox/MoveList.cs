/*
    MIT License

    Copyright (c) 2020 Don Cross

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;

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

        public Move[] ToMoveArray()
        {
            var copy = new Move[nmoves];
            Array.Copy(array, copy, nmoves);
            return copy;
        }

        public void Sort()
        {
            // Sort move in descending order of their 'score' fields.
            // Because move lists are fairly short, I believe
            // a cache-friendly O(n^2) selection sort is fine.
            // I may revisit this assumption later.
            // Also reconsider later whether it matters that this function
            // shuffles moves having equal scores.
            for (int i=0; i+1 < nmoves; ++i)
            {
                int bestIndex = i;
                int bestScore = array[i].score;
                for (int j=i+1; j < nmoves; ++j)
                {
                    int score = array[j].score;
                    if (score > bestScore)
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

        public void Shuffle()
        {
            if (nmoves > 1)
            {
                var rand = new Random();
                for (int i = 1; i < nmoves; ++i)
                {
                    int r = rand.Next(i+1);
                    if (r < i)
                    {
                        Move swap = array[r];
                        array[r] = array[i];
                        array[i] = swap;
                    }
                }
            }
        }

        public bool MoveToFront(Move best)
        {
            // Find the move matching 'best' in the list.
            // Move the matching move to the front of the list without disturbing
            // the relative order of the other moves.
            for (int i=0; i < nmoves; ++i)
            {
                Move move = array[i];
                if (best.source == move.source && best.dest == move.dest && best.prom == move.prom)
                {
                    for (int k=i; k > 0; --k)
                        array[k] = array[k-1];
                    array[0] = move;
                    return true;
                }
            }

            return false;       // did not find the move in the list
        }

        public bool Contains(Move move)
        {
            for (int i=0; i < nmoves; ++i)
                if (array[i].source == move.source && array[i].dest == move.dest && array[i].prom == move.prom)
                    return true;

            return false;
        }
    }
}
