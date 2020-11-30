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

namespace Gearbox
{
    public static class Direction
    {
        // Directions for K, Q, R, B ...
        public const int E  =   1;
        public const int NE =  11;
        public const int N  =  10;
        public const int NW =   9;
        public const int W  =  -1;
        public const int SW = -11;
        public const int S  = -10;
        public const int SE =  -9;

        // Knight directions...
        public const int NEE =  12;
        public const int NNE =  21;
        public const int NNW =  19;
        public const int NWW =   8;
        public const int SWW = -12;
        public const int SSW = -21;
        public const int SSE = -19;
        public const int SEE =  -8;
    }
}
