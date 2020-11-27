using System;
using System.Collections.Generic;
using System.Text;

namespace Gearbox
{
    public class Board
    {
        // See this document for board layout:
        // https://docs.google.com/spreadsheets/d/12mNHhBPNH66jUZ6dGeRYKiSsRXGTedCG1qHCAgAaifk/edit?usp=sharing

        private readonly Square[] square = new Square[120];
        private readonly UnmoveStack unmoveStack = new UnmoveStack();
        private int wkofs;
        private int bkofs;
        private bool isWhiteTurn;
        private int fullMoveNumber;
        private int halfMoveClock;
        private bool isPlayerInCheck;
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

            // Determine whether the current player is in check.
            isPlayerInCheck = isWhiteTurn ? IsAttackedBy(wkofs, Square.Black) : IsAttackedBy(bkofs, Square.White);
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
            return isPlayerInCheck;
        }

        public bool PlayerCanMove()
        {
            // Return true if the current player has at least one legal move.
            // This is faster than generating all legal moves.
            if (isWhiteTurn)
                return PlayerCanMove(Square.White, Square.Black, Direction.N, 2, 7);

            return PlayerCanMove(Square.Black, Square.White, Direction.S, 7, 2);
        }

        public void PushMove(Move move)
        {
            // Preserve information about the current board state.
            var unmove = new Unmove();
            unmove.move = move;
            unmove.capture = square[move.dest];
            unmove.epTargetOffset = epTargetOffset;
            unmove.halfMoveClock = halfMoveClock;
            unmove.isPlayerInCheck = isPlayerInCheck;
            unmove.whiteCanCastleKingside = whiteCanCastleKingside;
            unmove.whiteCanCastleQueenside = whiteCanCastleQueenside;
            unmove.blackCanCastleKingside = blackCanCastleKingside;
            unmove.blackCanCastleQueenside = blackCanCastleQueenside;
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
                        Lift(epTargetOffset + Direction.S);     // remove the captured black pawn from the board
                    break;

                case Square.BP:
                    if (move.dest == epTargetOffset)
                        Lift(epTargetOffset + Direction.N);     // remove the captured white pawn from the board
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
                            Lift(28);
                            Drop(26, Square.WR);
                        }
                        else if (move.dest == 23)
                        {
                            // White O-O-O : move the queenside rook around the king.
                            Lift(21);
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
                            Lift(98);
                            Drop(96, Square.BR);
                        }
                        else if (move.dest == 93)
                        {
                            // Black O-O-O : move the queenside rook around the king.
                            Lift(91);
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

            // Determine whether the current player is in check.
            if (isWhiteTurn)
            {
                isPlayerInCheck = IsAttackedBy(wkofs, Square.Black);
                ++fullMoveNumber;
            }
            else
            {
                isPlayerInCheck = IsAttackedBy(bkofs, Square.White);
            }
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
            isPlayerInCheck = unmove.isPlayerInCheck;

            switch (piece)
            {
                case Square.WK:
                    wkofs = unmove.move.source;
                    if (unmove.move.source == 25)
                    {
                        if (unmove.move.dest == 27)
                        {
                            // Undo rook movement in White O-O.
                            Lift(26);
                            Drop(28, Square.WR);
                        }
                        else if (unmove.move.dest == 23)
                        {
                            // Undo rook movement in White O-O-O.
                            Lift(25);
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
                            Lift(96);
                            Drop(98, Square.BR);
                        }
                        else if (unmove.move.dest == 93)
                        {
                            // Undo rook movement in Black O-O-O.
                            Lift(95);
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
        }

        private Square Lift(int ofs)
        {
            Square piece = square[ofs];
            square[ofs] = Square.Empty;
            return piece;
        }

        private void Drop(int ofs, Square piece)
        {
            square[ofs] = piece;
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
                                    GenCastleKingside(movelist, ofs, friend, enemy);
                                if (GenMove_Single(movelist, ofs, Direction.W, friend))
                                    GenCastleQueenside(movelist, ofs, friend, enemy);
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
            PopMove();
            if (!illegal)
                movelist.Add(move);
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

        private void GenCastleKingside(MoveList movelist, int source, Square friend, Square enemy)
        {
            // not allowed to castle while in check
            if (isPlayerInCheck)
                return;

            // not allowed to castle if the king or the involved rook have moved/captured
            if (friend == Square.White)
            {
                if (!whiteCanCastleKingside)
                    return;
            }
            else
            {
                if (!blackCanCastleKingside)
                    return;
            }

            // not allowed to castle unless squares between king and rook are empty
            int dest = source + 2*Direction.E;
            if (square[source + Direction.E] != Square.Empty || square[dest] != Square.Empty)
                return;

            // Caller has already determined that we would not be castling *through* check.
            // The caller verified that moving the king one square east is legal.
            // If not, we don't call this function in the first place!
            // AddMove() will filter out the case of castling *into* check,
            // just like it tests any other move.
            AddMove(movelist, source, dest);
        }

        private void GenCastleQueenside(MoveList movelist, int source, Square friend, Square enemy)
        {
            // not allowed to castle while in check
            if (isPlayerInCheck)
                return;

            // not allowed to castle if the king or the involved rook have moved/captured
            if (friend == Square.White)
            {
                if (!whiteCanCastleQueenside)
                    return;
            }
            else
            {
                if (!blackCanCastleQueenside)
                    return;
            }

            // not allowed to castle unless squares between king and rook are empty
            int dest = source + 2*Direction.W;
            if (square[source + Direction.W] != Square.Empty || square[dest] != Square.Empty || square[source + 3*Direction.W] != Square.Empty)
                return;

            // Caller has already determined that we would not be castling *through* check.
            // The caller verified that moving the king one square west is legal.
            // If not, we don't call this function in the first place!
            // AddMove() will filter out the case of castling *into* check,
            // just like it tests any other move.
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
            {
                if (rank == promrank)
                {
                    if (IsLegalMove(source, dest, 'q')) return true;
                }
                else
                {
                    if (IsLegalMove(source, dest)) return true;
                }
            }

            // Check for diagonal capture toward the west.
            dest = source + pawndir + Direction.W;
            if (0 != (square[dest] & enemy) || dest == epTargetOffset)
            {
                if (rank == promrank)
                {
                    if (IsLegalMove(source, dest, 'q')) return true;
                }
                else
                {
                    if (IsLegalMove(source, dest)) return true;
                }
            }

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
