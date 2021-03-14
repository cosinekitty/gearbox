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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Gearbox
{
    public class Board
    {
        // See this document for board layout:
        // https://docs.google.com/spreadsheets/d/12mNHhBPNH66jUZ6dGeRYKiSsRXGTedCG1qHCAgAaifk/edit?usp=sharing

        internal readonly Square[] square = InitSquaresArray();
        internal readonly int[] inventory = new int[1 + (int)Square.BK];
        internal readonly int[] whiteBishopsOnColor = new int[2];
        internal readonly int[] blackBishopsOnColor = new int[2];
        private readonly UnmoveStack unmoveStack = new UnmoveStack();
        private int wkofs;              // location of the White King
        private int bkofs;              // location of the Black King
        private bool isWhiteTurn;
        private int fullMoveNumber;
        private int halfMoveClock;
        private CastlingFlags castling;     // tracks movement of kings, movement/capture of rooks, for castling availability
        private int epTargetOffset;         // offset behind pawn that just moved 2 squares; otherwise 0.
        private Ternary epCaptureIsLegal;   // lazy-evaluated existence of at least one legal en passant capture
        private string initialFen;          // needed for saving game to PGN file
        private Ternary playerInCheck;
        private Ternary playerCanMove;
        private HashValue pieceHash;

        public const string StandardSetup = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static Square[] InitSquaresArray()
        {
            var square = new Square[120];

            for (int ofs=0; ofs<10; ++ofs)
                square[ofs] = square[ofs+10] = square[ofs+100] = square[ofs+110] = Square.Offboard;

            for (int ofs=20; ofs < 100; ofs += 10)
                square[ofs] = square[ofs+9] = Square.Offboard;

            return square;
        }

        public Board(string fen = StandardSetup)
        {
            SetPosition(fen);
        }

        public Board(bool whiteToMove)     // makes an empty (and invalid!) board state
        {
            Clear(whiteToMove);
        }

        public static Board FromGame(Game game)
        {
            if (game == null)
                return new Board();

            var board = new Board(game.Tags.InitialState);
            foreach (Move move in game.MoveHistory)
                board.PushMove(move);

            return board;
        }

        public void LoadGame(Game game)
        {
            if (game != null)
            {
                SetPosition(game.Tags.InitialState);
                foreach (Move move in game.MoveHistory)
                    PushMove(move);
            }
        }

        public static Board FromPgnText(string pgn)
        {
            // Extract the first full game from the PGN string.
            // This function won't work for processing multiple games from a string.
            // For that, do something like:
            // foreach (Game game in Game.FromString(pgn)) { Board b = Board.FromGame(game); ... }

            Game game = Game.FromString(pgn).FirstOrDefault();
            return Board.FromGame(game);
        }

        public Square GetSquareContents(int ofs)
        {
            if (ofs < 21 || ofs > 98 || square[ofs] == Square.Offboard)
                throw new ArgumentException(string.Format("Invalid board offset: {0}", ofs));

            return square[ofs];
        }

        public int FullMoveNumber { get { return fullMoveNumber; } }
        public bool IsWhiteTurn { get { return isWhiteTurn; } }
        public bool IsBlackTurn { get { return !isWhiteTurn; } }

        private HashValue FastHash()
        {
            // Start with the hash of the configuration of pieces on the board.
            // The pieceHash is always kept up to date by PushMove() and PopMove().
            HashValue hash = pieceHash;

            // Adjust for whether it is White's turn or Black's turn.
            if (isWhiteTurn)
            {
                hash.a ^= HashSalt.Data[0, 0, 0];
                hash.b ^= HashSalt.Data[0, 0, 1];
            }

            // Adjust for castling availability for both sides.
            int c = (int)castling;
            hash.a ^= HashSalt.Castling[c, 0];
            hash.b ^= HashSalt.Castling[c, 1];
            return hash;
        }

        public HashValue Hash()
        {
            // The hash value must identify the unique tactical situation.
            // All factors that affect the tree of possible future game states
            // must be represented in the returned pair of hash values.
            HashValue hash = FastHash();

            // Adjust for the ability to make an en passant capture, but only when it is actually possible.
            // Just because epTargetOffset is set, doesn't mean there is a pawn that can capture there.
            // We don't want to think two positions are different just because a pawn moved two squares,
            // when the respective trees of future moves emanating from those positions are identical.
            if (LegalEnPassantCaptureExists())
            {
                int file = 55 + (epTargetOffset % 10);      // 56..63
                hash.a ^= HashSalt.Data[0, file, 0];
                hash.b ^= HashSalt.Data[0, file, 1];
            }

            return hash;
        }

        private bool LegalEnPassantCaptureExists()
        {
            if (epTargetOffset == 0)
                return false;

            if (epCaptureIsLegal != Ternary.Unknown)
                return epCaptureIsLegal == Ternary.Yes;

            Square taker;       // Would a White Pawn or a Black Pawn be doing the capture?
            int dir;            // What direction would such a pawn move when not capturing?
            if (isWhiteTurn)
            {
                taker = Square.WP;
                dir = Direction.N;
            }
            else
            {
                taker = Square.BP;
                dir = Direction.S;
            }

            // There are up to two places a capturing pawn could be located:
            // 1. Diagonally and to the East of the en passant target.
            // 2. Diagonally and to the West of the en passant target.
            // Return true if either exists and capture is actually legal.
            int source = epTargetOffset + Direction.E - dir;
            if (square[source] == taker && IsLegalMove(source, epTargetOffset))
            {
                epCaptureIsLegal = Ternary.Yes;
                return true;
            }

            source = epTargetOffset + Direction.W - dir;
            if (square[source] == taker && IsLegalMove(source, epTargetOffset))
            {
                epCaptureIsLegal = Ternary.Yes;
                return true;
            }

            epCaptureIsLegal = Ternary.No;
            return false;
        }

        public GameHistory GetGameHistory()
        {
            var moveArray = new Move[unmoveStack.height];
            for (int i=0; i < unmoveStack.height; ++i)
                moveArray[i] = unmoveStack.array[i].move;

            string optionalFen = (initialFen == StandardSetup) ? null : initialFen;
            GameResult result = GetGameResult();
            return new GameHistory(optionalFen, moveArray, result);
        }

        public void LoadGameHistory(GameHistory history)
        {
            SetPosition(history.InitialState);
            foreach (Move move in history.MoveHistory)
                PushMove(move);
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
            if (0 != (castling & CastlingFlags.WhiteKingside))
                fen.Append('K');
            if (0 != (castling & CastlingFlags.WhiteQueenside))
                fen.Append('Q');
            if (0 != (castling & CastlingFlags.BlackKingside))
                fen.Append('k');
            if (0 != (castling & CastlingFlags.BlackQueenside))
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

        public static char Rank(int offset)
        {
            Algebraic(offset, out char file, out char rank);
            return rank;
        }

        public static char File(int offset)
        {
            Algebraic(offset, out char file, out char rank);
            return file;
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
                return TryGetOffset(algebraic[0], algebraic[1], out offset);
            offset = 0;
            return false;
        }

        private static bool TryGetOffset(char file, char rank, out int offset)
        {
            if (file >= 'a' && file <= 'h' && rank >= '1' && rank <= '8')
            {
                offset = (file - 'a') + 10*(rank - '1') + 21;
                return true;
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
            Clear(true);

            if (fen == null)
                fen = StandardSetup;

            // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            // https://ia902908.us.archive.org/26/items/pgn-standard-1994-03-12/PGN_standard_1994-03-12.txt

            string[] token = fen.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (token.Length != 6)
                throw new ArgumentException("FEN record must contain 6 space-delimited fields.");

            int total = 0;      // total number of squares filled (must end up 64)
            char file = 'a';
            char rank = '8';

            // token[0] = layout
            foreach (char c in token[0])
            {
                int count;
                Square piece;
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
                            case 'k':   piece = Square.BK;  bkofs = ofs;  break;
                            case 'P':   piece = Square.WP;  break;
                            case 'N':   piece = Square.WN;  break;
                            case 'B':   piece = Square.WB;  break;
                            case 'R':   piece = Square.WR;  break;
                            case 'Q':   piece = Square.WQ;  break;
                            case 'K':   piece = Square.WK;  wkofs = ofs;  break;
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

            int bk_count = inventory[(int)Square.BK];
            int wk_count = inventory[(int)Square.WK];
            if (bk_count != 1 || wk_count != 1)
                throw new ArgumentException(string.Format("FEN contains {0} white kings and {1} black kings.", wk_count, bk_count));

            // token[1] = turn to move
            switch (token[1])
            {
                case "w":   isWhiteTurn = true;     break;
                case "b":   isWhiteTurn = false;    break;
                default:
                    throw new ArgumentException("FEN side to move must be 'w' or 'b'.");
            }

            // token[2] = castling availability
            if (token[2] != "-")
            {
                foreach (char c in token[2])
                {
                    switch (c)
                    {
                        case 'K':   castling |= CastlingFlags.WhiteKingside;  break;
                        case 'Q':   castling |= CastlingFlags.BlackKingside;  break;
                        case 'k':   castling |= CastlingFlags.WhiteQueenside; break;
                        case 'q':   castling |= CastlingFlags.BlackQueenside; break;
                        default:
                            throw new ArgumentException("FEN castling availability is invalid.");
                    }
                }
            }

            // token[3] = en passant target
            if (token[3] != "-" && !TryGetOffset(token[3], out epTargetOffset))
                throw new ArgumentException("FEN invalid en passant target.");

            if (!int.TryParse(token[4], out halfMoveClock) || halfMoveClock < 0)
                throw new ArgumentException("FEN invalid halfmove clock.");

            if (!int.TryParse(token[5], out fullMoveNumber) || fullMoveNumber < 1)
                throw new ArgumentException("FEN invalid fullmove number.");

            initialFen = string.Join(" ", token);   // normalize the whitespace in the FEN string

            // One final sanity check: make sure the side not having the turn is not in check.
            if (!IsValidPosition())
                throw new ArgumentException("The non-moving player is in check.");
        }

        public string MoveNotation(Move move, MoveList legalMoves, MoveList scratch)
        {
            if (0 == (move.flags & MoveFlags.Valid))
                throw new Exception("Move does not have valid check/immobility flags.");

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

        public string PortableGameNotation(GameTags originalTags)
        {
            // Make a copy of the provided tags, so we can mutate them without side-effects to the caller.
            GameTags tags = (originalTags == null) ? (new GameTags()) : originalTags.Clone();

            // Mutate the tags so that the output will be correct, if this is a nonstandard position.
            tags.InitialState = initialFen;

            // Update the game result (win, loss, draw, in progress)...
            tags.Result = GetGameResult();

            string pgn = tags.ToString();
            GameHistory history = GetGameHistory();
            pgn += history.FormatMoveList(80);

            return pgn;
        }

        internal int RepCount()
        {
            int repCount = 0;
            if (halfMoveClock >= 2)
            {
                // Count how many times the current position has appeared in the past.
                // For efficiency, we never search backward in time beyond
                // the most recent capture or pawn move. Either kind of move
                // is irreversible. Such a move forms a barrier beyond which
                // a repeated position is impossible.
                int limitPly = Math.Max(0, unmoveStack.height - halfMoveClock);

                // Look back every other move in the move stack for duplicate hash values.
                HashValue currentHash = FastHash();
                for (int i = unmoveStack.height - 2; i >= limitPly; i -= 2)
                {
                    HashValue pastHash = unmoveStack.array[i].fastHash;
                    if (pastHash.a == currentHash.a && pastHash.b == currentHash.b)
                        if (++repCount == 2)
                            break;  // the game just ended in a draw by repetition; no need to search further
                }
            }
            return repCount;
        }

        public GameResult GetGameResult()
        {
            if (!PlayerCanMove())
            {
                if (IsPlayerInCheck())
                {
                    // checkmate
                    return isWhiteTurn ? GameResult.BlackWon : GameResult.WhiteWon;
                }

                return GameResult.Draw;     // stalemate
            }

#if false
            // FIXFIXFIX: some endgame tables require more than 50 moves to force mate.
            // Do not enforce the 50-move rule for now.
            // For example: the following position has a forced mate in 69 moves:
            // 3kq3/8/8/8/1BB5/1K6/8/8 b - - 1 1
            if (halfMoveClock > 100)
            {
                // Draw by the 50-move rule.
                return GameResult.Draw;
            }
#endif

            // It takes at least 4 full moves without captures or pawn movement
            // before it is possible to have a draw by repetition.
            if (halfMoveClock >= 8 && RepCount() == 2)
            {
                // Draw by threefold repetition.
                return GameResult.Draw;
            }

            if (IsDrawByInsufficientMaterial())
                return GameResult.Draw;

            return GameResult.InProgress;
        }

        public bool IsDrawByInsufficientMaterial()
        {
            // Look for insufficient mating material on both sides.
            // FIDE rule 9.6:
            // "The game is a draw when a position is reached from which
            //  a checkmate cannot occur by any possible series of legal moves."
            int possibleMaters =
                inventory[(int)Square.WQ] + inventory[(int)Square.BQ] +
                inventory[(int)Square.WR] + inventory[(int)Square.BR] +
                inventory[(int)Square.WP] + inventory[(int)Square.BP];

            if (possibleMaters == 0)
            {
                // Oddly, mate is possible (but not forcible) in K+B vs K+B.
                // But mate is not possible in K+B vs K.
                // So this isn't as obvious as one might think.

                int knightsAndBishops =
                    inventory[(int)Square.WB] + inventory[(int)Square.WN] +
                    inventory[(int)Square.BB] + inventory[(int)Square.BN];

                if (knightsAndBishops <= 1)
                {
                    // If either White or Black has BB, NB, or NN,
                    // White can possibly checkmate Black.
                    // The same goes for Black having bb, nb, or nn, against White.
                    // So we can't have a draw whenever either side has sum(bishop, knight) > 1.
                    // This leaves us to consider all possible cases where
                    // B+N <= 1 and b+n <= 1.
                    // The PossibleMates program confirms only the cases marked
                    // by X below represent situations where there do not exist
                    // any checkmates of Black by White.
                    //
                    //      wb  BNbn  checkmate?
                    //      00  0000  X
                    //      01  0001  X
                    //      01  0010  X
                    //      10  0100  X
                    //      10  1000  X
                    //      11  0101  8/8/8/8/8/1K6/2N5/kn6 b - - 0 1
                    //      11  0110  8/8/8/8/8/KN6/8/kb6 b - - 0 1
                    //      11  1001  8/8/8/8/8/1K6/1B6/kn6 b - - 0 1
                    //      11  1010  8/8/8/8/8/K7/1B6/kb6 b - - 0 1
                    //
                    // This boils down to a simple rule:
                    // A position with more than one bishop and/or knight
                    // of either color can lead to a checkmate by some legal series of moves.
                    // So we can only terminate a game as a draw when the total
                    // count of bishops and knights is 0 or 1.
                    return true;
                }
            }

            return false;
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

        public void GenMoves(MoveList movelist, MoveGen opt = MoveGen.All)
        {
            if (isWhiteTurn)
                GenMoves(movelist, opt, Square.White, Square.Black, Direction.N, 2, 7);
            else
                GenMoves(movelist, opt, Square.Black, Square.White, Direction.S, 7, 2);
        }

        private bool IsIllegalPosition()
        {
            // Return true if we have arrived at a position where a move just made
            // places that same side king in check.
            return isWhiteTurn ? IsAttackedBy(bkofs, Square.White) : IsAttackedBy(wkofs, Square.Black);
        }

        private static bool Predicate(out Ternary cache, bool value)
        {
            cache = value ? Ternary.Yes : Ternary.No;
            return value;
        }

        public bool IsPlayerInCheck()
        {
            if (playerInCheck != Ternary.Unknown)
                return playerInCheck == Ternary.Yes;

            return Predicate(out playerInCheck, UncachedPlayerInCheck());
        }

        public bool PlayerCanMove()
        {
            if (playerCanMove != Ternary.Unknown)
                return playerCanMove == Ternary.Yes;

            return Predicate(out playerCanMove, UncachedPlayerCanMove());
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
            unmove.epCaptureIsLegal = epCaptureIsLegal;
            unmove.halfMoveClock = halfMoveClock;
            unmove.playerInCheck = playerInCheck;
            unmove.playerCanMove = playerCanMove;
            unmove.castling = castling;
            unmove.pieceHash = pieceHash;
            unmove.fastHash = FastHash();       // avoids crazy recursive cases for en passant (doesn't matter for draw by repetition)
            unmoveStack.Push(unmove);

            // Capturing an unmoved rook destroys castling on that side.
            // If the rook has already moved away and back, the castling
            // flag for that side is false, and setting it to false again is harmless.
            switch (unmove.capture)
            {
                case Square.WR:
                    if (move.dest == 28)
                        castling &= ~CastlingFlags.WhiteKingside;
                    else if (move.dest == 21)
                        castling &= ~CastlingFlags.WhiteQueenside;
                    break;

                case Square.BR:
                    if (move.dest == 98)
                        castling &= ~CastlingFlags.BlackKingside;
                    else if (move.dest == 91)
                        castling &= ~CastlingFlags.BlackQueenside;
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
                    // White cannot castle in either direction after moving his King.
                    castling &= ~(CastlingFlags.WhiteKingside | CastlingFlags.WhiteQueenside);
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
                    // Black cannot castle in either direction after moving his King.
                    castling &= ~(CastlingFlags.BlackKingside | CastlingFlags.BlackQueenside);
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
                        castling &= ~CastlingFlags.WhiteKingside;
                    else if (move.source == 21)
                        castling &= ~CastlingFlags.WhiteQueenside;
                    break;

                case Square.BR:
                    // Moving a rook prevents castling on the same side.
                    if (move.source == 98)
                        castling &= ~CastlingFlags.BlackKingside;
                    else if (move.source == 91)
                        castling &= ~CastlingFlags.BlackQueenside;
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
                throw new Exception(string.Format("White King is not at {0} after {1} in {2}", Algebraic(wkofs), move, ForsythEdwardsNotation()));

            if (square[bkofs] != Square.BK)
                throw new Exception(string.Format("Black King is not at {0} after {1} in {2}", Algebraic(bkofs), move, ForsythEdwardsNotation()));
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

            castling = unmove.castling;
            epTargetOffset = unmove.epTargetOffset;
            epCaptureIsLegal = unmove.epCaptureIsLegal;
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

            if (pieceHash.a != unmove.pieceHash.a || pieceHash.b != unmove.pieceHash.b)
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
                PieceHashValues(piece, ofs, out ulong a, out ulong b);
                pieceHash.a -= a;
                pieceHash.b -= b;
                --inventory[(int)piece];
                switch (piece)
                {
                    case Square.WB:
                        --whiteBishopsOnColor[SquareColor(ofs)];
                        break;

                    case Square.BB:
                        --blackBishopsOnColor[SquareColor(ofs)];
                        break;
                }
            }
            return piece;
        }

        private void Drop(int ofs, Square piece)
        {
            if (square[ofs] != Square.Empty)
                throw new ArgumentException("Attempt to drop to non-empty offset " + ofs);

            square[ofs] = piece;

            if (piece != Square.Empty)
            {
                PieceHashValues(piece, ofs, out ulong a, out ulong b);
                pieceHash.a += a;
                pieceHash.b += b;
                ++inventory[(int)piece];
                switch (piece)
                {
                    case Square.WB:
                        ++whiteBishopsOnColor[SquareColor(ofs)];
                        break;

                    case Square.BB:
                        ++blackBishopsOnColor[SquareColor(ofs)];
                        break;
                }
            }
        }

        public static int SquareColor(int ofs)
        {
            // Returns 1 if 'ofs' corresponds to a dark colored square.
            // Returns 0 for a light colored square.
            return ((ofs % 10) + (ofs / 10)) & 1;
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

        private void GenMoves(MoveList movelist, MoveGen opt, Square friend, Square enemy, int pawndir, int homerank, int promrank)
        {
            epCaptureIsLegal = Ternary.No;      // will change to Yes if GenMoves_Pawn() finds any legal en passant captures.
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
                                GenMoves_Pawn(movelist, opt, ofs, friend, enemy, pawndir, homerank, promrank);
                                break;

                            case Square.Knight:
                                GenMove_Single(movelist, opt, ofs, Direction.NEE, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.NNE, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.NNW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.NWW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SWW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SSW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SSE, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SEE, friend);
                                break;

                            case Square.Bishop:
                                GenMoves_Ray(movelist, opt, ofs, Direction.NE, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.NW, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.SW, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.SE, friend);
                                break;

                            case Square.Rook:
                                GenMoves_Ray(movelist, opt, ofs, Direction.N, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.W, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.S, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.E, friend);
                                break;

                            case Square.Queen:
                                GenMoves_Ray(movelist, opt, ofs, Direction.NE, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.NW, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.SW, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.SE, friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.N,  friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.W,  friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.S,  friend);
                                GenMoves_Ray(movelist, opt, ofs, Direction.E,  friend);
                                break;

                            case Square.King:
                                GenMove_Single(movelist, opt, ofs, Direction.NE, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.NW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SW, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.SE, friend);
                                GenMove_Single(movelist, opt, ofs, Direction.N,  friend);
                                GenMove_Single(movelist, opt, ofs, Direction.S,  friend);
                                GenMove_Single(movelist, opt, ofs, Direction.E,  friend);
                                GenMove_Single(movelist, opt, ofs, Direction.W,  friend);
                                if (opt != MoveGen.Captures)
                                {
                                    // See if castling kingside (O-O) is legal.
                                    CastlingFlags flag = isWhiteTurn ? CastlingFlags.WhiteKingside : CastlingFlags.BlackKingside;
                                    if ((0 != (castling & flag)) && !IsPlayerInCheck() && !IsAttackedBy(ofs + Direction.E, enemy))
                                    {
                                        // Not allowed to castle unless both squares between king and rook are empty.
                                        int dest = ofs + 2*Direction.E;
                                        if ((square[ofs + Direction.E] | square[dest]) == Square.Empty)
                                            AddMove(movelist, opt, ofs, dest);
                                    }

                                    // See if castling queenside (O-O-O) is legal.
                                    flag = isWhiteTurn ? CastlingFlags.WhiteQueenside : CastlingFlags.BlackQueenside;
                                    if ((0 != (castling & flag)) && !IsPlayerInCheck() && !IsAttackedBy(ofs + Direction.W, enemy))
                                    {
                                        // Not allowed to castle unless all 3 squares between king and rook are empty.
                                        int dest = ofs + 2*Direction.W;
                                        if ((square[ofs + Direction.W] | square[dest] | square[ofs + 3*Direction.W]) == Square.Empty)
                                            AddMove(movelist, opt, ofs, dest);
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

        private bool AddMove(
            MoveList movelist,
            MoveGen opt,
            int source,
            int dest,
            char prom = '\0',
            bool knownLegal = false,
            bool knownCapture = false)
        {
            bool capture = (square[dest] != Square.Empty) || (prom != '\0') || knownCapture;

            // We can filter out non-captures when requested, without even trying the move.
            if ((opt == MoveGen.Captures) && !capture)
                return false;

            // Append 'move' to 'movelist', but only if making that move doesn't leave the mover in check.
            var move = new Move(source, dest, prom);
            PushMove(move);
            bool legal = knownLegal || !IsIllegalPosition();
            if (legal)
            {
                bool check = IsPlayerInCheck();
                if ((opt != MoveGen.ChecksAndCaptures) || capture || check)
                {
                    move.flags = MoveFlags.Valid;
                    if (check)
                        move.flags |= MoveFlags.Check;
                    if (capture)
                        move.flags |= MoveFlags.Capture;
                    if (!PlayerCanMove())
                        move.flags |= MoveFlags.Immobile;
                    movelist.Add(move);
                }
            }
            PopMove();
            return legal;
        }

        private bool IsLegalMove(int source, int dest, char prom = '\0')
        {
            PushMove(new Move(source, dest, prom));
            bool legal = !IsIllegalPosition();
            PopMove();
            return legal;
        }

        private bool CanMove_Single(int source, int dir, Square friend)
        {
            int dest = source + dir;
            if (0 == (square[dest] & (friend | Square.Offboard)))
                return IsLegalMove(source, dest);
            return false;
        }

        private bool GenMove_Single(MoveList movelist, MoveGen opt, int source, int dir, Square friend)
        {
            int dest = source + dir;
            if (0 == (square[dest] & (friend | Square.Offboard)))
                return AddMove(movelist, opt, source, dest);
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

        private void GenMoves_Ray(MoveList movelist, MoveGen opt, int source, int dir, Square friend)
        {
            int dest;
            for (dest = source + dir; square[dest] == Square.Empty; dest += dir)
                if (opt != MoveGen.Captures)
                    AddMove(movelist, opt, source, dest);

            if (0 == (square[dest] & (friend | Square.Offboard)))
                AddMove(movelist, opt, source, dest);
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

        private void GenMoves_Pawn(
            MoveList movelist, MoveGen opt, int source, Square friend, Square enemy, int pawndir, int homerank, int promrank)
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
                    if (AddMove(movelist, opt, source, dest, 'q', false))
                    {
                        AddMove(movelist, opt, source, dest, 'r', true);
                        AddMove(movelist, opt, source, dest, 'b', true);
                        AddMove(movelist, opt, source, dest, 'n', true);
                    }
                }
                else
                {
                    AddMove(movelist, opt, source, dest);
                    dest += pawndir;

                    // A pawn may move two squares forward on its first move, if both squares are empty.
                    if (rank == homerank && square[dest] == Square.Empty)
                        AddMove(movelist, opt, source, dest);
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
                    if (AddMove(movelist, opt, source, dest, 'q', false))
                    {
                        AddMove(movelist, opt, source, dest, 'r', true);
                        AddMove(movelist, opt, source, dest, 'b', true);
                        AddMove(movelist, opt, source, dest, 'n', true);
                    }
                }
                else
                {
                    if (AddMove(movelist, opt, source, dest, '\0', false, true))
                        if (dest == epTargetOffset)
                            epCaptureIsLegal = Ternary.Yes;
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
                    if (AddMove(movelist, opt, source, dest, 'q', false))
                    {
                        AddMove(movelist, opt, source, dest, 'r', true);
                        AddMove(movelist, opt, source, dest, 'b', true);
                        AddMove(movelist, opt, source, dest, 'n', true);
                    }
                }
                else
                {
                    if (AddMove(movelist, opt, source, dest, '\0', false, true))
                        if (dest == epTargetOffset)
                            epCaptureIsLegal = Ternary.Yes;
                }
            }
        }

#region Tablebase lookup support

        public const int MaxEndgamePieces = 5;

        public long GetEndgameConfigId(bool reverseSides)
        {
            if (castling != CastlingFlags.None)
                return -1;  // castling and endgame tables don't mix!

            // Use the inventory to compute which endgame configuration this is
            // and what index in that configuration table the position is.
            int npieces = 0;
            for (int i=0; i < inventory.Length; ++i)
                npieces += inventory[i];

            if (npieces > MaxEndgamePieces)
                return -1;   // too many pieces to fit the endgame table in memory

            // Calculate the decimal integer QqRrBbNnPp.
            // This identifies the lookup table to use.
            long id = 0;
            int w = (int)(reverseSides ? Square.Black : Square.White);
            int b = (int)(reverseSides ? Square.White : Square.Black);
            for (int p = (int)Square.Queen; p >= (int)Square.Pawn; --p)
                id = 100*id + 10*inventory[p|w] + inventory[p|b];

            return id;
        }

        private static int ReverseSideOffset(int ofs)
        {
            // Swap offsets from the point of view of White
            // with equivalents from the point of view of Black.
            // This is like rotating the board 180 degrees.
            int x = ofs % 10;
            int y = ofs / 10;
            return 10*(11 - y) + (9 - x);
        }

        private readonly Position PositionCache = new Position();
        private readonly Position PositionScratch = new Position();

        public void GetPosition(Position pos, bool reverseSides)
        {
            pos.Clear();

            if (reverseSides)
            {
                // Flip the board and toggle White and Black.
                pos.EpTargetOffset = (epTargetOffset == 0) ? 0 : ReverseSideOffset(epTargetOffset);

                for (int y = 21; y <= 91; y += 10)
                {
                    for (int x = 0; x < 8; ++x)
                    {
                        int ofs = x + y;
                        Square piece = square[ofs];
                        if (piece != Square.Empty)
                        {
                            Square oppositeSidePiece = piece ^ (Square.White | Square.Black);
                            int flipOffset = ReverseSideOffset(ofs);
                            pos.Append(oppositeSidePiece, flipOffset);
                        }
                    }
                }
            }
            else
            {
                // Reflect the board as-is into 'pos'.
                pos.EpTargetOffset = epTargetOffset;

                for (int y = 21; y <= 91; y += 10)
                {
                    for (int x = 0; x < 8; ++x)
                    {
                        int ofs = x + y;
                        Square piece = square[ofs];
                        if (piece != Square.Empty)
                            pos.Append(piece, ofs);
                    }
                }
            }

            // *** WARNING ***
            // For efficiency, we do not call pos.Sort() here,
            // because the only place that calls this function
            // proceeds to transform it, which causes the post-transformed
            // version to be sorted. The sort is necessary for eliminating
            // redundancy due to more than one piece of the same kind.
            // For example, [WP, WP, WP] allows 6 different ways to represent
            // the same position.
            // If any code in the future calls this function, it may need
            // to call pos.Sort() afterward.
        }

        public int GetEndgameTableIndex(bool reverseSides)
        {
            GetPosition(PositionCache, reverseSides);
            return PositionCache.GetEndgameTableIndex(PositionScratch);
        }

#endregion

#region Dangerous functions for brute-force endgame solvers, etc.

        public Square[] GetSquaresArray()
        {
            return square;
        }

        public int[] GetInventoryArray()
        {
            return inventory;
        }

        public void Clear(bool whiteToMove)
        {
            // Remove all pieces from the board and completely reset the inner state.
            // This creates an illegal position with no kings!

            for (int y = 21; y <= 91; y += 10)
                for (int x = 0; x < 8; ++x)
                    square[y+x] = Square.Empty;

            for (int i = 0; i < inventory.Length; ++i)
                inventory[i] = 0;

            unmoveStack.Reset();
            wkofs = bkofs = -1;     // will cause array bounds exception if used
            isWhiteTurn = whiteToMove;
            fullMoveNumber = 1;
            halfMoveClock = 0;
            castling = CastlingFlags.None;
            epTargetOffset = 0;
            epCaptureIsLegal = Ternary.Unknown;
            initialFen = null;
            playerInCheck = Ternary.Unknown;
            playerCanMove = Ternary.Unknown;
            pieceHash.a = pieceHash.b = 0;
            whiteBishopsOnColor[0] = whiteBishopsOnColor[1] = 0;
            blackBishopsOnColor[0] = blackBishopsOnColor[1] = 0;
        }

        public void RefreshAfterDangerousChanges()
        {
            // Update the piece inventory and hash values to reflect
            // direct changes to the contents of the board's squares.

            pieceHash.a = pieceHash.b = 0;
            whiteBishopsOnColor[0] = whiteBishopsOnColor[1] = 0;
            blackBishopsOnColor[0] = blackBishopsOnColor[1] = 0;

            for (int i=0; i < inventory.Length; ++i)
                inventory[i] = 0;

            for (int y = 21; y <= 91; y += 10)
            {
                for (int x = 0; x < 8; ++x)
                {
                    int ofs = x + y;
                    Square piece = square[ofs];
                    if (piece != Square.Empty)
                    {
                        PieceHashValues(piece, ofs, out ulong a, out ulong b);
                        pieceHash.a += a;
                        pieceHash.b += b;
                        ++inventory[(int)piece];
                        switch (piece)
                        {
                            case Square.WB:
                                ++whiteBishopsOnColor[SquareColor(ofs)];
                                break;

                            case Square.BB:
                                ++blackBishopsOnColor[SquareColor(ofs)];
                                break;
                        }
                    }
                }
            }
        }

        public void SetTurn(bool whiteToMove)
        {
            isWhiteTurn = whiteToMove;

            // Clear any cached information about the position,
            // forcing us to recalculate stuff.
            // This is important for the endgame tablebase generator,
            // so that it can poke things into the board before
            // calling the legal move generator.
            epCaptureIsLegal = Ternary.Unknown;
            playerInCheck = Ternary.Unknown;
            playerCanMove = Ternary.Unknown;
        }

        public bool BothSidesHavePawns()
        {
            return (inventory[(int)Square.WP] > 0) && (inventory[(int)Square.BP] > 0);
        }

        public int PawnCount()
        {
            return inventory[(int)Square.WP] + inventory[(int)Square.BP];
        }

        public void SetEpTarget(int ep)
        {
            epTargetOffset = ep;
        }

        public int GetEpTarget()
        {
            return epTargetOffset;
        }

        public int WhiteKingOffset => wkofs;
        public int BlackKingOffset => bkofs;

        public void PlaceWhiteKing(int ofs)
        {
            if (wkofs > 0 && wkofs != ofs && square[wkofs] == Square.WK)
                square[wkofs] = Square.Empty;

            square[ofs] = Square.WK;
            wkofs = ofs;
        }

        public void PlaceBlackKing(int ofs)
        {
            if (bkofs > 0 && bkofs != ofs && square[bkofs] == Square.BK)
                square[bkofs] = Square.Empty;

            square[ofs] = Square.BK;
            bkofs = ofs;
        }

        public bool IsValidPosition()
        {
            return (wkofs > 0) && (bkofs > 0) && !IsIllegalPosition();
        }

        public bool IsCheckmate()
        {
            return UncachedPlayerInCheck() && !UncachedPlayerCanMove();
        }

        public bool UncachedPlayerInCheck()
        {
            return isWhiteTurn
                ? IsAttackedBy(wkofs, Square.Black)
                : IsAttackedBy(bkofs, Square.White);
        }

        public bool UncachedPlayerCanMove()
        {
            return isWhiteTurn
                ? PlayerCanMove(Square.White, Square.Black, Direction.N, 2, 7)
                : PlayerCanMove(Square.Black, Square.White, Direction.S, 7, 2);
        }

#endregion
    }
}
