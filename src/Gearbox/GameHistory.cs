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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gearbox
{
    public class GameHistory
    {
        public readonly string InitialState;    // If null, assume standard chess. Otherwise, starting configuration in Forsyth Edwards Notation (FEN).
        public readonly Move[] MoveHistory;     // The list of moves from the starting configuration to the current configuration.
        public readonly GameResult Result;      // whether the game ended, and what the outcome was

        internal GameHistory(string initialState, Move[] moveHistory, GameResult result)
        {
            this.InitialState = initialState;
            this.MoveHistory = moveHistory;
            this.Result = result;
        }

        public string FormatMoveList(int maxLineLength)
        {
            var tlist = new List<string>();
            var board = new Board(InitialState);
            var legalMoves = new MoveList();
            var scratch = new MoveList();

            if (board.IsBlackTurn)
            {
                tlist.Add(string.Format("{0}.", board.FullMoveNumber));
                tlist.Add("...");   // placeholder for missing White move.
            }

            foreach (Move move in MoveHistory)
            {
                if (board.IsWhiteTurn)
                    tlist.Add(string.Format("{0}.", board.FullMoveNumber));

                board.GenMoves(legalMoves);
                string san = board.MoveNotation(move, legalMoves, scratch);
                tlist.Add(san);
                board.PushMove(move);
            }

            tlist.Add(GameTags.FormatResult(Result));

            // Format the list of tokens by delimiting with a mixture
            // of spaces and newlines so as to best satisfy maxLineLength.
            if (maxLineLength <= 0)
            {
                // Just make one long line with no line ending terminator.
                // This is suitable for display environments that support word wrapping.
                return string.Join(" ", tlist);
            }

            // Do our own word wrapping, doing our best to obey the maximum line length.
            // However, it is possible we will exceed maxLineLength if it is unreasonably
            // small (especially if less than 7 characters).
            // Always end with a line terminator "\n".
            var sb = new StringBuilder();
            int len = 0;
            foreach (string t in tlist)
            {
                int extra = t.Length;
                if (len > 0)
                    ++extra;    // include room for a space delimiter

                if (len + extra > maxLineLength)
                {
                    sb.AppendLine();
                    len = 0;
                }
                else
                {
                    if (len > 0)
                        sb.Append(' ');
                    sb.Append(t);
                    len += extra;
                }
            }

            if (len > 0)
                sb.AppendLine();

            return sb.ToString();
        }
    }
}
