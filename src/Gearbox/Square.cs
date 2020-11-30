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
    public enum Square
    {
        Empty    = 0x00,
        Pawn     = 0x01,
        Knight   = 0x02,
        Bishop   = 0x03,
        Rook     = 0x04,
        Queen    = 0x05,
        King     = 0x06,
        White    = 0x08,
        Black    = 0x10,
        Offboard = 0x20,

        PieceMask = 0x07,
        SideMask = White | Black,

        WP = White | Pawn,
        WN = White | Knight,
        WB = White | Bishop,
        WR = White | Rook,
        WQ = White | Queen,
        WK = White | King,

        BP = Black | Pawn,
        BN = Black | Knight,
        BB = Black | Bishop,
        BR = Black | Rook,
        BQ = Black | Queen,
        BK = Black | King,
    }
}