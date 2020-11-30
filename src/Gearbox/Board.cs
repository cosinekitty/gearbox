using System;
using System.Collections.Generic;
using System.Text;

namespace Gearbox
{
    public class Board
    {
        // See this document for board layout:
        // https://docs.google.com/spreadsheets/d/12mNHhBPNH66jUZ6dGeRYKiSsRXGTedCG1qHCAgAaifk/edit?usp=sharing

        private readonly Square[] square = MakeEmptyBoard();
        private readonly UnmoveStack unmoveStack = new UnmoveStack();
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
        private string initialFen;      // needed for saving game to PGN file
        private Ternary playerInCheck;
        private Ternary playerCanMove;
        private HashValue hash;

        public const string StandardSetup = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static Square[] MakeEmptyBoard()
        {
            var square = new Square[120];

            for (int ofs=0; ofs<10; ++ofs)
                square[ofs] = square[ofs+10] = square[ofs+100] = square[ofs+110] = Square.Offboard;

            for (int ofs=20; ofs < 100; ofs += 10)
                square[ofs] = square[ofs+9] = Square.Offboard;

            return square;
        }

        public Board(string fen = null)
        {
            SetPosition(fen ?? StandardSetup);
        }

        public void Reset()
        {
            SetPosition(StandardSetup);
        }

        public HashValue Hash()
        {
            return hash;
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
            int fenLengthBeforeCastling = fen.Length;
            if (whiteCanCastleKingside)
                fen.Append('K');
            if (whiteCanCastleQueenside)
                fen.Append('Q');
            if (blackCanCastleKingside)
                fen.Append('k');
            if (blackCanCastleQueenside)
                fen.Append('q');
            if (fen.Length == fenLengthBeforeCastling)
                fen.Append('-');

            if (epTargetOffset == 0)
                fen.Append(" - ");
            else
                fen.AppendFormat(" {0} ", Algebraic(epTargetOffset));

            fen.AppendFormat("{0} {1}", halfMoveClock, fullMoveNumber);
            return fen.ToString();
        }

        public static void Algebraic(int offset, out char file, out char rank)
        {
            file = (char)((offset % 10) - 1 + 'a');
            rank = (char)((offset / 10) - 2 + '1');
            if (offset < 0 || offset >= 120)
                throw new ArgumentException("Offset is out of bounds.");
            if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
                throw new ArgumentException("Offset is outside the board.");
        }

        public static string Algebraic(int offset)
        {
            char file, rank;
            Algebraic(offset, out file, out rank);
            return new string(new char[] {file, rank});
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
                        Drop(ofs, piece);
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

            playerInCheck = Ternary.Unknown;
            playerCanMove = Ternary.Unknown;

            unmoveStack.Reset();
            initialFen = fen;
        }

        public string MoveNotation(Move move, MoveList legalMoves, MoveList scratch)
        {
            var san = new StringBuilder(7, 7);      // SAN moves can never be more than 7 characters long.

            char file1, rank1;
            Algebraic(move.source, out file1, out rank1);

            char file2, rank2;
            Algebraic(move.dest, out file2, out rank2);

            Square piece = square[move.source] & Square.PieceMask;
            if ((piece == Square.King) && (move.dest - move.source == 2*Direction.E))
            {
                san.Append("O-O");
            }
            else if ((piece == Square.King) && (move.dest - move.source == 2*Direction.W))
            {
                san.Append("O-O-O");
            }
            else
            {
                Square capture = square[move.dest] & Square.PieceMask;
                if ((piece == Square.Pawn) && (file1 != file2) && (capture == Square.Empty))
                    capture = Square.Pawn;      // adjust for en passant capture

                // Central to PGN is the concept of "ambiguous" notation.
                // We want to figure out the minimum number of characters needed
                // to unambiguously encode the chess move.
                // Make a list of the subset of legal moves that have the
                // same piece moving and the same destination square.
                // Include only pawn promotions to the same promoted piece.
                scratch.nmoves = 0;
                for (int i=0; i < legalMoves.nmoves; ++i)
                {
                    Move lm = legalMoves.array[i];
                    Square lp = square[lm.source] & Square.PieceMask;
                    if (lp == piece && lm.dest == move.dest && lm.prom == move.prom)
                        scratch.Add(lm);
                }
                if (scratch.nmoves == 0)
                    throw new ArgumentException("Cannot format an illegal move.");

                bool need_source_file = false;
                bool need_source_rank = false;

                if (scratch.nmoves > 1)
                {
                    /*
                        [The following is quoted from http://www.very-best.de/pgn-spec.htm, section 8.2.3.]

                        In the case of ambiguities (multiple pieces of the same type moving to the same square),
                        the first appropriate disambiguating step of the three following steps is taken:

                        First, if the moving pieces can be distinguished by their originating files,
                        the originating file letter of the moving piece is inserted immediately after
                        the moving piece letter.

                        Second (when the first step fails), if the moving pieces can be distinguished by
                        their originating ranks, the originating rank digit of the moving piece is inserted
                        immediately after the moving piece letter.

                        Third (when both the first and the second steps fail), the two character square
                        coordinate of the originating square of the moving piece is inserted immediately
                        after the moving piece letter.
                    */

                    // Check for distinct files and ranks for other moves that end up at 'dest'.
                    int file_count = 0;
                    int rank_count = 0;
                    for (int i=0; i < scratch.nmoves; ++i)
                    {
                        char mfile, mrank;
                        Algebraic(scratch.array[i].source, out mfile, out mrank);
                        if (mfile == file1)
                            ++file_count;
                        if (mrank == rank1)
                            ++rank_count;
                    }

                    if (file_count == 1)
                    {
                        need_source_file = true;
                    }
                    else
                    {
                        need_source_rank = true;
                        if (rank_count > 1)
                            need_source_file = true;
                    }
                }

                if (piece == Square.Pawn)
                {
                    if (capture != Square.Empty)    // NOTE:  capture was set to PAWN above if this move is en passant.
                        need_source_file = true;
                }
                else
                {
                    san.Append(SanPieceSymbol(piece));
                }

                if (need_source_file)
                    san.Append(file1);

                if (need_source_rank)
                    san.Append(rank1);

                if (capture != Square.Empty)
                    san.Append('x');

                san.Append(file2);
                san.Append(rank2);

                if (move.prom != '\0')
                {
                    san.Append('=');
                    san.Append(char.ToUpperInvariant(move.prom));
                }
            }

            if (0 != (move.flags & MoveFlags.Check))
                san.Append((0 != (move.flags & MoveFlags.Immobile)) ? '#' : '+');

            return san.ToString();
        }

        private static char SanPieceSymbol(Square piece)
        {
            switch (piece)
            {
                case Square.King:   return 'K';
                case Square.Queen:  return 'Q';
                case Square.Rook:   return 'R';
                case Square.Bishop: return 'B';
                case Square.Knight: return 'N';
                default:
                    throw new ArgumentException(string.Format("No SAN symbol for {0}", piece));
            }
        }

        public void GenMoves(MoveList movelist)
        {
            if (isWhiteTurn)
                GenMoves(movelist, Square.White, Square.Black, Direction.N, 2, 7);
            else
                GenMoves(movelist, Square.Black, Square.White, Direction.S, 7, 2);
        }

        private bool IsIllegalPosition()
        {
            // Return true if we have arrived at a position where a move just made
            // places that same side king in check.
            return isWhiteTurn ? IsAttackedBy(bkofs, Square.White) : IsAttackedBy(wkofs, Square.Black);
        }

        public bool IsPlayerInCheck()
        {
            if (playerInCheck != Ternary.Unknown)
                return playerInCheck == Ternary.Yes;

            bool check = isWhiteTurn ? IsAttackedBy(wkofs, Square.Black) : IsAttackedBy(bkofs, Square.White);
            playerInCheck = check ? Ternary.Yes : Ternary.No;
            return check;
        }

        public bool PlayerCanMove()
        {
            // If we have already figured out whether the player can move, recycle that information.
            if (playerCanMove != Ternary.Unknown)
                return playerCanMove == Ternary.Yes;

            // Return true if the current player has at least one legal move.
            // This is faster than generating all legal moves.
            bool canMove = isWhiteTurn
                ? PlayerCanMove(Square.White, Square.Black, Direction.N, 2, 7)
                : PlayerCanMove(Square.Black, Square.White, Direction.S, 7, 2);

            // Cache the work so we don't have to do it again for this position.
            playerCanMove = canMove ? Ternary.Yes : Ternary.No;
            return canMove;
        }

        public void PushMove(Move move)
        {
            if (0 == (square[move.source] & Square.SideMask))
                throw new ArgumentException("Attempt to move a non-piece");

            // Preserve information about the current board state.
            var unmove = new Unmove();
            unmove.move = move;
            unmove.capture = square[move.dest];
            unmove.epTargetOffset = epTargetOffset;
            unmove.halfMoveClock = halfMoveClock;
            unmove.playerInCheck = playerInCheck;
            unmove.playerCanMove = playerCanMove;
            unmove.whiteCanCastleKingside = whiteCanCastleKingside;
            unmove.whiteCanCastleQueenside = whiteCanCastleQueenside;
            unmove.blackCanCastleKingside = blackCanCastleKingside;
            unmove.blackCanCastleQueenside = blackCanCastleQueenside;
            unmove.hash = hash;
            unmoveStack.Push(unmove);

            // Capturing an unmoved rook destroys castling on that side.
            // If the rook has already moved away and back, the castling
            // flag for that side is false, and setting it to false again is harmless.
            switch (unmove.capture)
            {
                case Square.WR:
                    if (move.dest == 28)
                        whiteCanCastleKingside = false;
                    else if (move.dest == 21)
                        whiteCanCastleQueenside = false;
                    break;

                case Square.BR:
                    if (move.dest == 98)
                        blackCanCastleKingside = false;
                    else if (move.dest == 91)
                        blackCanCastleQueenside = false;
                    break;

                case Square.WK:
                case Square.BK:
                    throw new ArgumentException("Move would capture a king!");
            }

            Lift(move.dest);    // remove captured piece (if any) from the board.
            Square piece = Lift(move.source);

            Square dropped;
            switch (move.prom)
            {
                case 'q':   dropped = (piece & Square.SideMask) | Square.Queen;   break;
                case 'r':   dropped = (piece & Square.SideMask) | Square.Rook;    break;
                case 'b':   dropped = (piece & Square.SideMask) | Square.Bishop;  break;
                case 'n':   dropped = (piece & Square.SideMask) | Square.Knight;  break;
                case '\0':  dropped = piece;    break;
                default:    throw new ArgumentException("Invalid promotion piece in move.");
            }
            Drop(move.dest, dropped);

            // See if this move is an en passant capture.
            switch (piece)
            {
                case Square.WP:
                    if (move.dest == epTargetOffset)
                        Lift(epTargetOffset + Direction.S, Square.BP);
                    break;

                case Square.BP:
                    if (move.dest == epTargetOffset)
                        Lift(epTargetOffset + Direction.N, Square.WP);
                    break;
            }

            // Assume no en passant target, unless code below finds a pawn moving two squares forward.
            epTargetOffset = 0;

            switch (piece)
            {
                case Square.WK:
                    // White cannot castle after moving his King.
                    whiteCanCastleKingside = whiteCanCastleQueenside = false;
                    wkofs = move.dest;

                    // Check for White castling move.
                    if (move.source == 25)
                    {
                        if (move.dest == 27)
                        {
                            // White O-O : move the kingside rook around the king.
                            Lift(28, Square.WR);
                            Drop(26, Square.WR);
                        }
                        else if (move.dest == 23)
                        {
                            // White O-O-O : move the queenside rook around the king.
                            Lift(21, Square.WR);
                            Drop(24, Square.WR);
                        }
                    }
                    break;

                case Square.BK:
                    // Black cannot castle after moving his King.
                    blackCanCastleKingside = blackCanCastleQueenside = false;
                    bkofs = move.dest;

                    // Check for Black castling move.
                    if (move.source == 95)
                    {
                        if (move.dest == 97)
                        {
                            // Black O-O : move the kingside rook around the king.
                            Lift(98, Square.BR);
                            Drop(96, Square.BR);
                        }
                        else if (move.dest == 93)
                        {
                            // Black O-O-O : move the queenside rook around the king.
                            Lift(91, Square.BR);
                            Drop(94, Square.BR);
                        }
                    }
                    break;

                case Square.WR:
                    // Moving a rook prevents castling on the same side.
                    if (move.source == 28)
                        whiteCanCastleKingside = false;
                    else if (move.source == 21)
                        whiteCanCastleQueenside = false;
                    break;

                case Square.BR:
                    // Moving a rook prevents castling on the same side.
                    if (move.source == 98)
                        blackCanCastleKingside = false;
                    else if (move.source == 91)
                        blackCanCastleQueenside = false;
                    break;

                case Square.WP:
                    // If a pawn moves two squares forward, it provides an en passant target.
                    if (move.dest - move.source == 2 * Direction.N)
                        epTargetOffset = move.source + Direction.N;
                    break;

                case Square.BP:
                    // If a pawn moves two squares forward, it provides an en passant target.
                    if (move.dest - move.source == 2 * Direction.S)
                        epTargetOffset = move.source + Direction.S;
                    break;
            }

            if (unmove.capture != Square.Empty || (piece & Square.PieceMask) == Square.Pawn)
                halfMoveClock = 0;
            else
                ++halfMoveClock;

            isWhiteTurn = !isWhiteTurn;
            if (isWhiteTurn)
                ++fullMoveNumber;

            if (0 != (move.flags & MoveFlags.Valid))
            {
                // We already determined whether this move causes check, so no need to repeat that work.
                playerInCheck = (0 != (move.flags & MoveFlags.Check)) ? Ternary.Yes : Ternary.No;

                // We also already determined whether the opponent has at least one legal move.
                playerCanMove = (0 != (move.flags & MoveFlags.Immobile)) ? Ternary.No : Ternary.Yes;
            }
            else
            {
                // Start undecided about whether the player is in check or can move.
                // Lazy-evaluate these facts only when they are first needed.
                playerInCheck = Ternary.Unknown;
                playerCanMove = Ternary.Unknown;
            }

            if (square[wkofs] != Square.WK)
                throw new Exception("White King is misplaced");

            if (square[bkofs] != Square.BK)
                throw new Exception("Black King is misplaced");
        }

        public void PopMove()
        {
            Unmove unmove = unmoveStack.Pop();
            Square dropped = Lift(unmove.move.dest);
            Square piece;
            if (unmove.move.prom == '\0')
                piece = dropped;
            else
                piece = (dropped & Square.SideMask) | Square.Pawn;
            Drop(unmove.move.source, piece);
            Drop(unmove.move.dest, unmove.capture);

            whiteCanCastleKingside = unmove.whiteCanCastleKingside;
            whiteCanCastleQueenside = unmove.whiteCanCastleQueenside;
            blackCanCastleKingside = unmove.blackCanCastleKingside;
            blackCanCastleQueenside = unmove.blackCanCastleQueenside;
            epTargetOffset = unmove.epTargetOffset;
            halfMoveClock = unmove.halfMoveClock;
            playerInCheck = unmove.playerInCheck;
            playerCanMove = unmove.playerCanMove;

            switch (piece)
            {
                case Square.WK:
                    wkofs = unmove.move.source;
                    if (unmove.move.source == 25)
                    {
                        if (unmove.move.dest == 27)
                        {
                            // Undo rook movement in White O-O.
                            Lift(26, Square.WR);
                            Drop(28, Square.WR);
                        }
                        else if (unmove.move.dest == 23)
                        {
                            // Undo rook movement in White O-O-O.
                            Lift(24, Square.WR);
                            Drop(21, Square.WR);
                        }
                    }
                    break;

                case Square.BK:
                    bkofs = unmove.move.source;
                    if (unmove.move.source == 95)
                    {
                        if (unmove.move.dest == 97)
                        {
                            // Undo rook movement in Black O-O.
                            Lift(96, Square.BR);
                            Drop(98, Square.BR);
                        }
                        else if (unmove.move.dest == 93)
                        {
                            // Undo rook movement in Black O-O-O.
                            Lift(94, Square.BR);
                            Drop(91, Square.BR);
                        }
                    }
                    break;

                case Square.WP:
                    // Undo en passant captures: put black pawn back on the board.
                    if (unmove.move.dest == epTargetOffset)
                        Drop(epTargetOffset + Direction.S, Square.BP);
                    break;

                case Square.BP:
                    // Undo en passant captures: put white pawn back on the board.
                    if (unmove.move.dest == epTargetOffset)
                        Drop(epTargetOffset + Direction.N, Square.WP);
                    break;
            }

            if (isWhiteTurn)
                --fullMoveNumber;

            isWhiteTurn = !isWhiteTurn;

            if (square[wkofs] != Square.WK)
                throw new Exception("White King is misplaced");

            if (square[bkofs] != Square.BK)
                throw new Exception("Black King is misplaced");

            if (hash.a != unmove.hash.a || hash.b != unmove.hash.b)
                throw new Exception("Hash value was not preserved");
        }

        private void Lift(int ofs, Square expected)
        {
            Square lifted = Lift(ofs);
            if (lifted != expected)
                throw new Exception(string.Format("Expected to lift {0} from {1} but found {2}.", expected, Algebraic(ofs), lifted));
        }

        private static void PieceHashValues(Square piece, int ofs, out ulong a, out ulong b)
        {
            int pindex = (int)(piece & Square.PieceMask) - 1;
            if (0 != (piece & Square.Black))
                pindex += 6;

            int file = (ofs % 10) - 1;
            int rank = (ofs / 10) - 2;
            int sindex = 8*rank + file;

            a = HashSalt.Data[pindex, sindex, 0];
            b = HashSalt.Data[pindex, sindex, 1];
        }

        private Square Lift(int ofs)
        {
            if (square[ofs] == Square.Offboard)
                throw new ArgumentException("Attempt to lift from offboard offset " + ofs);
            Square piece = square[ofs];
            square[ofs] = Square.Empty;
            if (piece != Square.Empty)
            {
                ulong a, b;
                PieceHashValues(piece, ofs, out a, out b);
                hash.a -= a;
                hash.b -= b;
            }
            return piece;
        }

        private void Drop(int ofs, Square piece)
        {
            if (square[ofs] == Square.Offboard)
                throw new ArgumentException("Attempt to drop to offboard offset " + ofs);

            square[ofs] = piece;

            if (piece != Square.Empty)
            {
                ulong a, b;
                PieceHashValues(piece, ofs, out a, out b);
                hash.a += a;
                hash.b += b;
            }
        }

        private bool IsAttackedBy(int ofs, Square side)
        {
            if (side == Square.White)
            {
                if (square[ofs + Direction.SE]  == Square.WP)   return true;
                if (square[ofs + Direction.SW]  == Square.WP)   return true;
                if (square[ofs + Direction.NEE] == Square.WN)   return true;
                if (square[ofs + Direction.NNE] == Square.WN)   return true;
                if (square[ofs + Direction.NNW] == Square.WN)   return true;
                if (square[ofs + Direction.NWW] == Square.WN)   return true;
                if (square[ofs + Direction.SWW] == Square.WN)   return true;
                if (square[ofs + Direction.SSW] == Square.WN)   return true;
                if (square[ofs + Direction.SSE] == Square.WN)   return true;
                if (square[ofs + Direction.SEE] == Square.WN)   return true;
            }
            else
            {
                if (square[ofs + Direction.NE]  == Square.BP)   return true;
                if (square[ofs + Direction.NW]  == Square.BP)   return true;
                if (square[ofs + Direction.NEE] == Square.BN)   return true;
                if (square[ofs + Direction.NNE] == Square.BN)   return true;
                if (square[ofs + Direction.NNW] == Square.BN)   return true;
                if (square[ofs + Direction.NWW] == Square.BN)   return true;
                if (square[ofs + Direction.SWW] == Square.BN)   return true;
                if (square[ofs + Direction.SSW] == Square.BN)   return true;
                if (square[ofs + Direction.SSE] == Square.BN)   return true;
                if (square[ofs + Direction.SEE] == Square.BN)   return true;
            }

            if (AttackRay(ofs, Direction.E,  side, Square.Rook,   Square.Queen))  return true;
            if (AttackRay(ofs, Direction.N,  side, Square.Rook,   Square.Queen))  return true;
            if (AttackRay(ofs, Direction.W,  side, Square.Rook,   Square.Queen))  return true;
            if (AttackRay(ofs, Direction.S,  side, Square.Rook,   Square.Queen))  return true;
            if (AttackRay(ofs, Direction.NE, side, Square.Bishop, Square.Queen))  return true;
            if (AttackRay(ofs, Direction.NW, side, Square.Bishop, Square.Queen))  return true;
            if (AttackRay(ofs, Direction.SW, side, Square.Bishop, Square.Queen))  return true;
            if (AttackRay(ofs, Direction.SE, side, Square.Bishop, Square.Queen))  return true;

            return false;
        }

        private bool AttackRay(int source, int dir, Square side, Square piece1, Square piece2)
        {
            int dest = source + dir;
            if (square[dest] == (Square.King | side))
                return true;

            while (square[dest] == Square.Empty)
                dest += dir;

            return square[dest] == (piece1 | side) || square[dest] == (piece2 | side);
        }

        private bool PlayerCanMove(Square friend, Square enemy, int pawndir, int homerank, int promrank)
        {
            for (int rank = 1; rank <= 8; ++rank)
            {
                int ofs = 10*rank + 11;
                for (int x = 0; x < 8; ++x, ++ofs)
                {
                    Square p = square[ofs];
                    if (0 != (p & friend))
                    {
                        switch (p & Square.PieceMask)
                        {
                            case Square.Pawn:
                                if (CanMove_Pawn(ofs, friend, enemy, pawndir, homerank, promrank)) return true;
                                break;

                            case Square.Knight:
                                if (CanMove_Single(ofs, Direction.NEE, friend)) return true;
                                if (CanMove_Single(ofs, Direction.NNE, friend)) return true;
                                if (CanMove_Single(ofs, Direction.NNW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.NWW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SWW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SSW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SSE, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SEE, friend)) return true;
                                break;

                            case Square.Bishop:
                                if (CanMove_Ray(ofs, Direction.NE, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.NW, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.SW, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.SE, friend)) return true;
                                break;

                            case Square.Rook:
                                if (CanMove_Ray(ofs, Direction.N, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.W, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.S, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.E, friend)) return true;
                                break;

                            case Square.Queen:
                                if (CanMove_Ray(ofs, Direction.NE, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.NW, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.SW, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.SE, friend)) return true;
                                if (CanMove_Ray(ofs, Direction.N,  friend)) return true;
                                if (CanMove_Ray(ofs, Direction.W,  friend)) return true;
                                if (CanMove_Ray(ofs, Direction.S,  friend)) return true;
                                if (CanMove_Ray(ofs, Direction.E,  friend)) return true;
                                break;

                            case Square.King:
                                if (CanMove_Single(ofs, Direction.NE, friend)) return true;
                                if (CanMove_Single(ofs, Direction.NW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SW, friend)) return true;
                                if (CanMove_Single(ofs, Direction.SE, friend)) return true;
                                if (CanMove_Single(ofs, Direction.N,  friend)) return true;
                                if (CanMove_Single(ofs, Direction.W,  friend)) return true;
                                if (CanMove_Single(ofs, Direction.S,  friend)) return true;
                                if (CanMove_Single(ofs, Direction.E,  friend)) return true;
                                // We don't need to check for castling, because if castling
                                // is legal, then moving the king one square toward the rook is also legal.
                                // Therefore, the code never gets here when castling is legal anyway.
                                break;

                            default:
                                throw new Exception("Invalid board contents!");
                        }
                    }
                }
            }

            return false;
        }

        private void GenMoves(MoveList movelist, Square friend, Square enemy, int pawndir, int homerank, int promrank)
        {
            movelist.nmoves = 0;
            for (int rank = 1; rank <= 8; ++rank)
            {
                int ofs = 10*rank + 11;
                for (int x = 0; x < 8; ++x, ++ofs)
                {
                    Square p = square[ofs];
                    if (0 != (p & friend))
                    {
                        switch (p & Square.PieceMask)
                        {
                            case Square.Pawn:
                                GenMoves_Pawn(movelist, ofs, friend, enemy, pawndir, homerank, promrank);
                                break;

                            case Square.Knight:
                                GenMove_Single(movelist, ofs, Direction.NEE, friend);
                                GenMove_Single(movelist, ofs, Direction.NNE, friend);
                                GenMove_Single(movelist, ofs, Direction.NNW, friend);
                                GenMove_Single(movelist, ofs, Direction.NWW, friend);
                                GenMove_Single(movelist, ofs, Direction.SWW, friend);
                                GenMove_Single(movelist, ofs, Direction.SSW, friend);
                                GenMove_Single(movelist, ofs, Direction.SSE, friend);
                                GenMove_Single(movelist, ofs, Direction.SEE, friend);
                                break;

                            case Square.Bishop:
                                GenMoves_Ray(movelist, ofs, Direction.NE, friend);
                                GenMoves_Ray(movelist, ofs, Direction.NW, friend);
                                GenMoves_Ray(movelist, ofs, Direction.SW, friend);
                                GenMoves_Ray(movelist, ofs, Direction.SE, friend);
                                break;

                            case Square.Rook:
                                GenMoves_Ray(movelist, ofs, Direction.N, friend);
                                GenMoves_Ray(movelist, ofs, Direction.W, friend);
                                GenMoves_Ray(movelist, ofs, Direction.S, friend);
                                GenMoves_Ray(movelist, ofs, Direction.E, friend);
                                break;

                            case Square.Queen:
                                GenMoves_Ray(movelist, ofs, Direction.NE, friend);
                                GenMoves_Ray(movelist, ofs, Direction.NW, friend);
                                GenMoves_Ray(movelist, ofs, Direction.SW, friend);
                                GenMoves_Ray(movelist, ofs, Direction.SE, friend);
                                GenMoves_Ray(movelist, ofs, Direction.N,  friend);
                                GenMoves_Ray(movelist, ofs, Direction.W,  friend);
                                GenMoves_Ray(movelist, ofs, Direction.S,  friend);
                                GenMoves_Ray(movelist, ofs, Direction.E,  friend);
                                break;

                            case Square.King:
                                GenMove_Single(movelist, ofs, Direction.NE, friend);
                                GenMove_Single(movelist, ofs, Direction.NW, friend);
                                GenMove_Single(movelist, ofs, Direction.SW, friend);
                                GenMove_Single(movelist, ofs, Direction.SE, friend);
                                GenMove_Single(movelist, ofs, Direction.N,  friend);
                                GenMove_Single(movelist, ofs, Direction.S,  friend);
                                if (GenMove_Single(movelist, ofs, Direction.E, friend))
                                {
                                    // Moving the king one square east is legal.
                                    // See if castling kingside (O-O) is also legal.
                                    if ((isWhiteTurn ? whiteCanCastleKingside : blackCanCastleKingside) && !IsPlayerInCheck())
                                    {
                                        // Not allowed to castle unless both squares between king and rook are empty.
                                        int dest = ofs + 2*Direction.E;
                                        if ((square[ofs + Direction.E] | square[dest]) == Square.Empty)
                                            AddMove(movelist, ofs, dest);
                                    }
                                }
                                if (GenMove_Single(movelist, ofs, Direction.W, friend))
                                {
                                    // Moving the king one square west is legal.
                                    // See if castling queenside (O-O-O) is also legal.
                                    if ((isWhiteTurn ? whiteCanCastleQueenside : blackCanCastleQueenside) && !IsPlayerInCheck())
                                    {
                                        // Not allowed to castle unless all 3 squares between king and rook are empty.
                                        int dest = ofs + 2*Direction.W;
                                        if ((square[ofs + Direction.W] | square[dest] | square[ofs + 3*Direction.W]) == Square.Empty)
                                            AddMove(movelist, ofs, dest);
                                    }
                                }
                                break;

                            default:
                                throw new Exception("Invalid board contents!");
                        }
                    }
                }
            }
        }

        private bool AddMove(MoveList movelist, int source, int dest, char prom = '\0')
        {
            // Append 'move' to 'movelist', but only if making that move doesn't cause leave the mover in check.
            var move = new Move(source, dest, prom);
            PushMove(move);
            bool illegal = IsIllegalPosition();
            if (!illegal)
            {
                move.flags = MoveFlags.Valid;

                if (IsPlayerInCheck())
                    move.flags |= MoveFlags.Check;

                if (!PlayerCanMove())
                    move.flags |= MoveFlags.Immobile;

                movelist.Add(move);
            }
            PopMove();
            return !illegal;
        }

        private bool IsLegalMove(int source, int dest, char prom = '\0')
        {
            PushMove(new Move(source, dest, prom));
            bool illegal = IsIllegalPosition();
            PopMove();
            return !illegal;
        }

        private bool CanMove_Single(int source, int dir, Square friend)
        {
            int dest = source + dir;
            if (0 == (square[dest] & (friend | Square.Offboard)))
                return IsLegalMove(source, dest);
            return false;
        }

        private bool GenMove_Single(MoveList movelist, int source, int dir, Square friend)
        {
            int dest = source + dir;
            if (0 == (square[dest] & (friend | Square.Offboard)))
                return AddMove(movelist, source, dest);
            return false;
        }

        private bool CanMove_Ray(int source, int dir, Square friend)
        {
            int dest;
            for (dest = source + dir; square[dest] == Square.Empty; dest += dir)
                if (IsLegalMove(source, dest))
                    return true;

            if (0 == (square[dest] & (friend | Square.Offboard)))
                if (IsLegalMove(source, dest))
                    return true;

            return false;
        }

        private void GenMoves_Ray(MoveList movelist, int source, int dir, Square friend)
        {
            int dest;
            for (dest = source + dir; square[dest] == Square.Empty; dest += dir)
                AddMove(movelist, source, dest);

            if (0 == (square[dest] & (friend | Square.Offboard)))
                AddMove(movelist, source, dest);
        }

        private bool CanMove_Pawn(int source, Square friend, Square enemy, int pawndir, int homerank, int promrank)
        {
            int rank = (source / 10) - 1;   // calculate starting rank of the pawn to be moved, 1..8.

            // A pawn can move forward one square if that square is empty.
            int dest = source + pawndir;
            if (square[dest] == Square.Empty)
            {
                if (rank == promrank)
                {
                    // A pawn reaching the opponent's home rank may promote to Queen, Rook, Bishop, or Knight.
                    // However, we only need to check one promotion to see if any of them are legal.
                    if (IsLegalMove(source, dest, 'q')) return true;
                }
                else
                {
                    if (IsLegalMove(source, dest)) return true;
                    dest += pawndir;

                    // A pawn may move two squares forward on its first move, if both squares are empty.
                    if (rank == homerank && square[dest] == Square.Empty)
                        if (IsLegalMove(source, dest))
                            return true;
                }
            }

            // Check for diagonal capture toward the east.
            dest = source + pawndir + Direction.E;
            if (0 != (square[dest] & enemy) || dest == epTargetOffset)
                if (IsLegalMove(source, dest, (rank == promrank) ? 'q' : '\0'))
                    return true;

            // Check for diagonal capture toward the west.
            dest = source + pawndir + Direction.W;
            if (0 != (square[dest] & enemy) || dest == epTargetOffset)
                if (IsLegalMove(source, dest, (rank == promrank) ? 'q' : '\0'))
                    return true;

            return false;
        }

        private void GenMoves_Pawn(MoveList movelist, int source, Square friend, Square enemy, int pawndir, int homerank, int promrank)
        {
            int rank = (source / 10) - 1;   // calculate starting rank of the pawn to be moved, 1..8.

            // A pawn can move forward one square if that square is empty.
            int dest = source + pawndir;
            if (square[dest] == Square.Empty)
            {
                if (rank == promrank)
                {
                    // A pawn reaching the opponent's home rank may promote to Queen, Rook, Bishop, or Knight.
                    // If a promotion to a queen is legal, so is a promotion to rook, bishop, or knight,
                    // so save time by avoiding redundant legality checks.
                    if (AddMove(movelist, source, dest, 'q'))
                    {
                        movelist.Add(new Move(source, dest, 'r'));
                        movelist.Add(new Move(source, dest, 'b'));
                        movelist.Add(new Move(source, dest, 'n'));
                    }
                }
                else
                {
                    AddMove(movelist, source, dest);
                    dest += pawndir;

                    // A pawn may move two squares forward on its first move, if both squares are empty.
                    if (rank == homerank && square[dest] == Square.Empty)
                        AddMove(movelist, source, dest);
                }
            }

            // Check for diagonal capture toward the east.
            dest = source + pawndir + Direction.E;
            if (0 != (square[dest] & enemy) || dest == epTargetOffset)
            {
                if (rank == promrank)
                {
                    // A pawn reaching the opponent's home rank may promote to Queen, Rook, Bishop, or Knight.
                    // If a promotion to a queen is legal, so is a promotion to rook, bishop, or knight,
                    // so save time by avoiding redundant legality checks.
                    if (AddMove(movelist, source, dest, 'q'))
                    {
                        movelist.Add(new Move(source, dest, 'r'));
                        movelist.Add(new Move(source, dest, 'b'));
                        movelist.Add(new Move(source, dest, 'n'));
                    }
                }
                else
                {
                    AddMove(movelist, source, dest);
                }
            }

            // Check for diagonal capture toward the west.
            dest = source + pawndir + Direction.W;
            if (0 != (square[dest] & enemy) || dest == epTargetOffset)
            {
                if (rank == promrank)
                {
                    // A pawn reaching the opponent's home rank may promote to Queen, Rook, Bishop, or Knight.
                    // If a promotion to a queen is legal, so is a promotion to rook, bishop, or knight,
                    // so save time by avoiding redundant legality checks.
                    if (AddMove(movelist, source, dest, 'q'))
                    {
                        movelist.Add(new Move(source, dest, 'r'));
                        movelist.Add(new Move(source, dest, 'b'));
                        movelist.Add(new Move(source, dest, 'n'));
                    }
                }
                else
                {
                    AddMove(movelist, source, dest);
                }
            }
        }
    }
}
