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
