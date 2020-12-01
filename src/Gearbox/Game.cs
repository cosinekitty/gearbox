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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gearbox
{
    public class PortableGameNotationException : Exception
    {
        public PortableGameNotationException(int lineNumber, string message)
            : base(string.Format("PGN syntax error on line {0}: {1}", lineNumber, message))
            {}
    }

    public class Game
    {
        public readonly GameTags Tags;
        public readonly Move[] MoveHistory;

        public Game(GameTags tags, Move[] history)
        {
            this.Tags = tags;
            this.MoveHistory = history;
        }

        public static IEnumerable<Game> FromTextFile(string filename)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                foreach (Game game in FromStream(reader))
                    yield return game;
            }
        }

        public static IEnumerable<Game> FromString(string text)
        {
            using (var reader = new StringReader(text))
            {
                foreach (Game game in FromStream(reader))
                    yield return game;
            }
        }

        public static IEnumerable<Game> FromStream(TextReader reader)
        {
            ParseState state = ParseState.InHeader;
            string tagName = null;
            string tagValue = null;
            GameTags tags = new GameTags();
            var history = new List<Move>();
            var board = new Board();
            var legalMoves = new MoveList();
            var scratch = new MoveList();
            bool startedAnotherGame = false;
            int lnum = 1;

            foreach (Token t in Tokens(reader))
            {
                lnum = t.LineNumber;
                switch (state)
                {
                    case ParseState.InHeader:
                        t.RequireSymbol();
                        startedAnotherGame = true;

                        // Expect either '[' to begin a tag, or anything else to indicate the game has started.
                        if (t.Text == "[")
                        {
                            state = ParseState.ExpectTagName;
                        }
                        else
                        {
                            board.SetPosition(tags.GetTag("FEN"));
                            state = ParseState.InGame;
                            goto case ParseState.InGame;
                        }
                        break;

                    case ParseState.ExpectTagName:
                        tagName = t.RequireTagName().Text;
                        state = ParseState.ExpectTagValue;
                        break;

                    case ParseState.ExpectTagValue:
                        tagValue = t.RequireStringLiteral().Text;
                        tags.SetTag(tagName, tagValue);
                        state = ParseState.ExpectCloseBracket;
                        break;

                    case ParseState.ExpectCloseBracket:
                        t.RequireSymbol("]");
                        state = ParseState.InHeader;
                        break;

                    case ParseState.InGame:
                        // Expect a chess move or a game termination symbol.
                        switch (t.RequireSymbol().Text)
                        {
                            case "0-1":
                            case "1-0":
                            case "1/2-1/2":
                            case "*":
                                // Game termination. Emit the game!
                                yield return new Game(tags, history.ToArray());
                                history.Clear();
                                tags = new GameTags();
                                startedAnotherGame = false;
                                state = ParseState.InHeader;
                                break;

                            case ".":   // ignore periods after move numbers, "...", etc.
                            case "?":   // ignore bad-move annotations
                            case "!":   // ignore good-move annotations
                                break;

                            default:
                                if (t.StartsWithLetter())
                                {
                                    // Assume this is a SAN move.
                                    board.GenMoves(legalMoves);
                                    bool found = false;
                                    foreach (Move move in legalMoves.ToMoveArray())
                                    {
                                        string san = board.MoveNotation(move, legalMoves, scratch);
                                        if (san == t.Text)
                                        {
                                            board.PushMove(move);
                                            history.Add(move);
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                        throw new PortableGameNotationException(t.LineNumber, string.Format("Illegal move {0}", t.Text));
                                }
                                else
                                {
                                    // Ignore move numbers, but report bad syntax on any other token.
                                    t.RequireMoveNumber();
                                }
                                break;
                        }
                        break;

                    default:
                        throw new PortableGameNotationException(t.LineNumber, "Unknown parse state near: " + t.Text);
                }
            }

            switch (state)
            {
                case ParseState.InHeader:
                case ParseState.InGame:
                    if (startedAnotherGame)
                    {
                        // We hit end of input after starting to parse another game.
                        // Although it is not conformant with the PGN spec, tolerate a missing game terminator.
                        yield return new Game(tags, history.ToArray());
                    }
                    break;

                default:
                    // We were probably in the middle of parsing a tag when we ran out of tokens.
                    // The input was likely truncated or corrupted.
                    throw new PortableGameNotationException(lnum, "Unexpected end of input");
            }
        }

        private static IEnumerable<Token> Tokens(TextReader reader)
        {
            // Reference:
            // http://www.saremba.de/chessgml/standards/pgn/pgn-complete.htm

            TokenKind state = TokenKind.None;
            var sb = new StringBuilder();
            bool backslash = false;
            int lnum = 0;
            string line;
            while (null != (line = reader.ReadLine()))
            {
                ++lnum;
                if (line.Length > 0 && line[0] == '%')
                    continue;   // Section 6: Escape mechanism

                int i = 0;
                while (i < line.Length)
                {
                    char c = line[i++];

                    if (state == TokenKind.None)
                    {
                        if (c == ';')
                            break;      // a "rest of line" comment

                        switch (c)
                        {
                            case '{':
                                state = TokenKind.BraceComment;
                                break;

                            case '"':
                                state = TokenKind.StringLiteral;
                                break;

                            case '$':
                                state = TokenKind.NumericAnnotationGlyph;
                                break;

                            default:
                                if (char.IsWhiteSpace(c))
                                {
                                    // do nothing
                                }
                                else if (char.IsLetterOrDigit(c))
                                {
                                    state = TokenKind.Symbol;
                                    sb.Append(c);
                                }
                                else
                                {
                                    // All other characters are tokens all by themselves.
                                    yield return new Token(lnum, TokenKind.Symbol, c.ToString());
                                }
                                break;
                        }
                    }
                    else if (state == TokenKind.BraceComment)
                    {
                        // Section 5: Commentary
                        // Stay inside the brace comment until we see the first closing brace.
                        if (c == '}')
                            state = TokenKind.None;
                    }
                    else if (state == TokenKind.Symbol)
                    {
                        if (char.IsLetterOrDigit(c) || "_+#-/=:".Contains(c))
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            yield return new Token(lnum, TokenKind.Symbol, sb.ToString());
                            sb.Clear();
                            state = TokenKind.None;
                            --i;    // back up so we process this character on the next iteration
                        }
                    }
                    else if (state == TokenKind.StringLiteral)
                    {
                        // Keep scanning one string character at a time.
                        if (backslash)
                        {
                            sb.Append(c);
                            backslash = false;
                        }
                        else
                        {
                            if (c == '"')
                            {
                                // We found the end of a string token.
                                yield return new Token(lnum, TokenKind.StringLiteral, sb.ToString());
                                sb.Clear();
                                state = TokenKind.None;
                            }
                            else if (c == '\\')
                                backslash = true;
                            else
                                sb.Append(c);
                        }
                    }
                    else if (state == TokenKind.NumericAnnotationGlyph)
                    {
                        if (c < '0' || c > '9')
                        {
                            state = TokenKind.None;
                            --i;    // back up so we process this character on the next iteration
                        }
                    }
                    else
                        throw new PortableGameNotationException(lnum, "unknown state");
                }

                // We have hit the end of the text line.
                // Flush the end of any token now, as needed.
                switch (state)
                {
                    case TokenKind.StringLiteral:
                        throw new PortableGameNotationException(lnum, "Unterminated string constant");

                    case TokenKind.Symbol:
                        yield return new Token(lnum, state, sb.ToString());
                        break;

                    case TokenKind.None:
                        break;

                    default:
                        throw new PortableGameNotationException(lnum, "unhandled state at end of line");
                }

                sb.Clear();
                state = TokenKind.None;
            }
        }
    }

    internal enum TokenKind
    {
        None,               // skipping whitespace, still looking for a token
        BraceComment,       // inside "{...}"
        StringLiteral,      // inside "..."
        NumericAnnotationGlyph,
        Symbol,
    }

    internal enum ParseState
    {
        InHeader,
        ExpectTagName,
        ExpectTagValue,
        ExpectCloseBracket,
        InGame,
    }

    internal struct Token
    {
        internal readonly int LineNumber;
        internal readonly TokenKind Kind;
        internal readonly string Text;

        public Token(int lnum, TokenKind kind, string text)
        {
            this.LineNumber = lnum;
            this.Kind = kind;
            this.Text = text;
        }

        internal Token RequireSymbol()
        {
            if (Kind != TokenKind.Symbol)
                throw new PortableGameNotationException(LineNumber, string.Format("Expected symbol but found '{0}'", Text));
            if (string.IsNullOrWhiteSpace(Text))
                throw new PortableGameNotationException(LineNumber, "Scanned a blank symbol!");
            return this;
        }

        internal bool StartsWithLetter()
        {
            if (Text.Length == 0)
                return false;

            char c = char.ToUpperInvariant(Text[0]);
            return c >= 'A' && c <= 'Z';
        }

        internal Token RequireMoveNumber()
        {
            RequireSymbol();

            int i;
            for (i=0; i < Text.Length; ++i)
            {
                char c = Text[i];
                if (c < '0' || c > '9')
                    throw new PortableGameNotationException(LineNumber, string.Format("Invalid move syntax near '{0}'", Text));
            }

            return this;
        }

        internal Token RequireTagName()
        {
            RequireSymbol();

            if (!StartsWithLetter())
                throw new PortableGameNotationException(LineNumber, string.Format("First character of tag name must be a letter, but found '{0}'", Text));

            for (int i=1; i < Text.Length; ++i)
            {
                char c = char.ToUpperInvariant(Text[i]);
                bool valid = (c == '_') || (c >= 'A' && c <= 'Z') || (c >= '0' || c <= '9');
                if (!valid)
                    throw new PortableGameNotationException(LineNumber, string.Format("Invalid character(s) in tag name '{0}'", Text));
            }

            return this;
        }

        internal Token RequireSymbol(string text)
        {
            RequireSymbol();
            if (Text != text)
                throw new PortableGameNotationException(LineNumber, string.Format("Expected symbol '{0}' but found '{1}'", text, Text));
            return this;
        }

        internal Token RequireStringLiteral()
        {
            if (Kind != TokenKind.StringLiteral)
                throw new PortableGameNotationException(LineNumber, "Expected string literal but found: " + Text);
            return this;
        }
    }
}
