using System;
using System.Text;

namespace Gearbox
{
    public class Board
    {
        // See this document for board layout:
        // https://docs.google.com/spreadsheets/d/12mNHhBPNH66jUZ6dGeRYKiSsRXGTedCG1qHCAgAaifk/edit?usp=sharing

        private readonly Square[] square = new Square[10*12];
        private int wkofs;
        private int bkofs;
        private bool isWhiteTurn;
        private int fullMoveNumber;
        private int halfMoveClock;
        private bool whiteCanCastleKingside;
        private bool whiteCanCastleQueenside;
        private bool blackCanCastleKingside;
        private bool blackCanCastleQueenside;
        private int epTargetOffset;     // offset behind pawn that just moved 2 squares; otherwise 0.

        public const string StandardSetup = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Board(string fen = null)
        {
            for (int ofs=0; ofs<10; ++ofs)
                square[ofs] = square[ofs+10] = square[ofs+100] = square[ofs+110] = Square.Offboard;

            for (int ofs=20; ofs < 100; ofs += 10)
                square[ofs] = square[ofs+9] = Square.Offboard;

            SetPosition(fen ?? StandardSetup);
        }

        public string ForsythEdwardsNotation()
        {
            var fen = new StringBuilder();
            int empty = 0;
            for (char rank='8'; rank >= '1'; --rank)
            {
                if (rank != '8')
                    fen.Append('/');

                for (char file = 'a'; file <= 'h'; ++file)
                {
                    Square piece = square[Offset(file, rank)];
                    if (piece == Square.Empty)
                    {
                        ++empty;
                    }
                    else
                    {
                        if (empty > 0)
                        {
                            fen.Append((char)(empty + '0'));
                            empty = 0;
                        }
                        char c;
                        switch (piece)
                        {
                            case Square.BP: c = 'p'; break;
                            case Square.BN: c = 'n'; break;
                            case Square.BB: c = 'b'; break;
                            case Square.BR: c = 'r'; break;
                            case Square.BQ: c = 'q'; break;
                            case Square.BK: c = 'k'; break;
                            case Square.WP: c = 'P'; break;
                            case Square.WN: c = 'N'; break;
                            case Square.WB: c = 'B'; break;
                            case Square.WR: c = 'R'; break;
                            case Square.WQ: c = 'Q'; break;
                            case Square.WK: c = 'K'; break;
                            default:
                                throw new Exception("Invalid contents of square in chessboard.");
                        }
                        fen.Append(c);
                    }
                }
                if (empty > 0)
                {
                    fen.Append((char)(empty + '0'));
                    empty = 0;
                }
            }

            fen.Append(isWhiteTurn ? " w " : " b ");
            int pos = fen.Length;
            if (whiteCanCastleKingside)
                fen.Append('K');
            if (whiteCanCastleQueenside)
                fen.Append('Q');
            if (blackCanCastleKingside)
                fen.Append('k');
            if (blackCanCastleQueenside)
                fen.Append('q');
            if (fen.Length == pos)
                fen.Append('-');

            if (epTargetOffset == 0)
                fen.Append(" - ");
            else
                fen.AppendFormat(" {0} ", Algebraic(epTargetOffset));

            fen.AppendFormat("{0} {1}", halfMoveClock, fullMoveNumber);
            return fen.ToString();
        }

        public static string Algebraic(int offset)
        {
            if (offset < 0 || offset >= 120)
                throw new ArgumentException("Offset is out of bounds.");

            var array = new char[2]
            {
                (char)((offset % 10) - 1 + 'a'),
                (char)((offset / 10) - 2 + '1')
            };

            if (array[0] < 'a' || array[0] > 'h' || array[1] < '1' || array[1] > '8')
                throw new ArgumentException("Invalid offset.");

            return new string(array);
        }

        private static bool TryGetOffset(string algebraic, out int offset)
        {
            if (algebraic != null && algebraic.Length == 2)
            {
                char file = algebraic[0];
                char rank = algebraic[1];
                if (file >= 'a' && file <= 'h' && rank >= '1' && rank <= '8')
                {
                    offset = (file - 'a') + 10*(rank - '1') + 21;
                    return true;
                }
            }
            offset = 0;
            return false;
        }

        public static int Offset(char file, char rank)
        {
            if (file < 'a' || file > 'h')
                throw new ArgumentException("Invalid file letter. Must be a..h.");

            if (rank < '1' || rank > '8')
                throw new ArgumentException("Invalid rank number. Must be 1..8.");

            return (file - 'a') + 10*(rank - '1') + 21;
        }

        public void SetPosition(string fen)
        {
            // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            // https://ia902908.us.archive.org/26/items/pgn-standard-1994-03-12/PGN_standard_1994-03-12.txt

            string[] token = fen.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (token.Length != 6)
                throw new ArgumentException("FEN record must contain 6 space-delimited fields.");

            // token[0] = layout
            Square piece;
            int count;
            int total = 0;      // total number of squares filled (must end up 64)
            char file = 'a';
            char rank = '8';
            this.bkofs = this.wkofs = 0;    // detect missing king(s)

            foreach (char c in token[0])
            {
                if (c == '/')
                {
                    if (file != 'i')
                        throw new ArgumentException("FEN rank did not contain exactly 8 squares.");

                    file = 'a';
                    --rank;

                    if (rank < '1')
                        throw new ArgumentException("FEN layout contains too many ranks.");
                }
                else
                {
                    int ofs = Offset(file, rank);
                    if (c >= '1' && c <= '8')
                    {
                        count = c - '0';
                        piece = Square.Empty;
                    }
                    else
                    {
                        count = 1;
                        switch (c)
                        {
                            case 'p':   piece = Square.BP;  break;
                            case 'n':   piece = Square.BN;  break;
                            case 'b':   piece = Square.BB;  break;
                            case 'r':   piece = Square.BR;  break;
                            case 'q':   piece = Square.BQ;  break;
                            case 'k':   piece = Square.BK;  this.bkofs = ofs;  break;
                            case 'P':   piece = Square.WP;  break;
                            case 'N':   piece = Square.WN;  break;
                            case 'B':   piece = Square.WB;  break;
                            case 'R':   piece = Square.WR;  break;
                            case 'Q':   piece = Square.WQ;  break;
                            case 'K':   piece = Square.WK;  this.wkofs = ofs;  break;
                            default:
                                throw new ArgumentException("Invalid character in Forsyth Edwards Notation (FEN).");
                        }

                        if ((c == 'p' || c == 'P') && (rank == '1' || rank == '8'))
                            throw new ArgumentException("FEN pawn on invalid rank.");
                    }

                    total += count;
                    while (count-- > 0)
                    {
                        if (square[ofs] == Square.Offboard)
                            throw new ArgumentException("FEN content went outside the board.");
                        square[ofs] = piece;
                        ++ofs;
                        ++file;
                    }
                }
            }

            if (total != 64)
                throw new ArgumentException("FEN layout did not contain exactly 64 squares.");

            if (this.wkofs == 0 || this.bkofs == 0)
                throw new ArgumentException("FEN does not include both kings.");

            // token[1] = turn to move
            switch (token[1])
            {
                case "w":   isWhiteTurn = true;     break;
                case "b":   isWhiteTurn = false;    break;
                default:
                    throw new ArgumentException("FEN side to move must be 'w' or 'b'.");
            }

            // token[2] = castling availability
            whiteCanCastleKingside = whiteCanCastleQueenside = false;
            blackCanCastleKingside = blackCanCastleQueenside = false;
            if (token[2] != "-")
            {
                foreach (char c in token[2])
                {
                    switch (c)
                    {
                        case 'K':   whiteCanCastleKingside  = true; break;
                        case 'Q':   whiteCanCastleQueenside = true; break;
                        case 'k':   blackCanCastleKingside  = true; break;
                        case 'q':   blackCanCastleQueenside = true; break;
                        default:
                            throw new ArgumentException("FEN castling availability is invalid.");
                    }
                }
            }

            // token[3] = en passant target
            if (token[3] == "-")
                epTargetOffset = 0;
            else if (!TryGetOffset(token[3], out epTargetOffset))
                throw new ArgumentException("FEN invalid en passant target.");

            if (!int.TryParse(token[4], out halfMoveClock) || halfMoveClock < 0)
                throw new ArgumentException("FEN invalid halfmove clock.");

            if (!int.TryParse(token[5], out fullMoveNumber) || fullMoveNumber < 1)
                throw new ArgumentException("FEN invalid fullmove number.");
        }
    }
}
