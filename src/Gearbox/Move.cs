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
    public static class Score
    {
        // Scores are roughly in units of "micropawns".
        public const int Draw = 0;

        // Scores relative to White/Black...
        public const int WonForWhite = +1000000000;
        public const int WonForBlack = -1000000000;
        public const int WhiteMate   = +1100000000;
        public const int BlackMate   = -1100000000;

        // Negamax scores: relative to current player / opposite player...
        public const int WonForFriend = +1000000000;
        public const int WonForEnemy  = -1000000000;
        public const int EnemyMated   = +1100000000;
        public const int FriendMated  = -1100000000;

        public const int NegInf      = -2000000000;
        public const int PosInf      = +2000000000;
        public const int Undefined   = int.MinValue;    // SUBTLE: -int.MinValue is the same as +int.MinValue, thanks to two's complement math

        // Raw piece values... must be adjusted for tradoff bonus/penalty, etc.
        internal const int Pawn   = 1000000;
        internal const int Knight = 2900000;
        internal const int Bishop = 3100000;
        internal const int Rook   = 5000000;
        internal const int Queen  = 9000000;

        internal const int CheckBonus = 100000;

        public static int OnePlyDelay(int score)
        {
            // For each ply we send a score up the search,
            // we need to slightly penalize delay of beneficial positions
            // and slightly reward delay of adverse positions.
            // But avoid adjusting any score extremely close to being a draw,
            // so that we always choose a draw (or avoid it) when beneficial.
            if (score != Undefined)     // never change an undefined score; it is a signal of a null move.
            {
                if (score > +100)
                    return score - 1;
                if (score < -100)
                    return score + 1;
            }
            return score;
        }

        public static string Format(int score)
        {
            if (score > WonForFriend)
                return string.Format("#{0}", 1 + (EnemyMated - score)/2);

            if (score < WonForEnemy)
                return string.Format("#-{0}", (score - FriendMated)/2);

            if (score == 0)
                return "0.000";

            string text;
            if (score < 0)
            {
                text = "-";
                score = -score;
            }
            else
                text = "+";

            int whole = score / 1000000;
            int frac = ((score % 1000000 + 500) / 1000);
            if (frac >= 1000)
            {
                ++whole;
                frac -= 1000;
            }

            text += string.Format("{0}.{1}", whole, frac.ToString("000"));
            return text;
        }
    }

    public enum MoveFlags : byte
    {
        None     = 0x00,
        Valid    = 0x01,    // the Check & Immobile flags are valid
        Check    = 0x02,    // move causes check to the opponent
        Immobile = 0x04,    // move leaves opponent with no legal moves (stalemate or checkmate)
        Capture  = 0x08,    // move is a capture (includes en passant and promotions)
    }

    public struct Move
    {
        public byte source;
        public byte dest;
        public char prom;       // 'qrbn' if promotion, '\0' otherwise
        public MoveFlags flags;
        public int  score;

        public static readonly Move Null = new Move { score = Score.NegInf };

        public Move(int source, int dest)
        {
            this.source = (byte)source;
            this.dest = (byte)dest;
            this.prom = '\0';
            this.flags = MoveFlags.None;
            this.score = Score.Undefined;
        }

        public Move(int source, int dest, char prom)
        {
            this.source = (byte)source;
            this.dest = (byte)dest;
            this.prom = prom;
            this.flags = MoveFlags.None;
            this.score = Score.Undefined;
        }

        public override string ToString()
        {
            if (IsNull())
                return "----";
            string alg = Board.Algebraic(source) + Board.Algebraic(dest);
            return (prom == '\0') ? alg : (alg + prom);
        }

        public bool IsNull()
        {
            return source == 0;
        }

        public bool IsValidMove()
        {
            return (source != 0) && (0 != (flags & MoveFlags.Valid));
        }

        public Move Validate()
        {
            if (!IsValidMove())
                throw new Exception($"Not a valid move: source={source}, flags={flags}");

            return this;
        }

        public bool IsCaptureOrPromotion()
        {
            Validate();
            return ('\0' != prom) || (0 != (flags & MoveFlags.Capture));
        }

        public bool IsPromotion()
        {
            Validate();
            return '\0' != prom;
        }
    }
}
