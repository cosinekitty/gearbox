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
        public const int Undefined   = int.MinValue;

        // Helper functions.
        public static int CheckmateLoss(int depth)
        {
            return FriendMated + depth;
        }

        public static int CheckmateWin(int depth)
        {
            return EnemyMated - depth;
        }

        public static string Format(int score)
        {
            if (score > WonForFriend)
                return string.Format("#{0}", 1 + (EnemyMated - score)/2);

            if (score < WonForEnemy)
                return string.Format("#-{0}", 1 + (FriendMated + score)/2);

            if (score == 0)
                return "0";

            string text;
            if (score < 0)
            {
                text = "-";
                score = -score;
            }
            else
                text = "+";

            text += string.Format("{0}.{1}", score / 1000000, ((score % 1000000 + 500) / 1000).ToString("000"));
            return text;
        }
    }

    public enum MoveFlags : byte
    {
        None = 0x00,
        Valid = 0x01,       // the Check & Immobile flags are valid
        Check = 0x02,       // move causes check to the opponent
        Immobile = 0x04,    // move leaves opponent with no legal moves (stalemate or checkmate)
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
            if (source == 0)
                return "----";  // this is a null move
            string alg = Board.Algebraic(source) + Board.Algebraic(dest);
            return (prom == '\0') ? alg : (alg + prom);
        }
    }
}
