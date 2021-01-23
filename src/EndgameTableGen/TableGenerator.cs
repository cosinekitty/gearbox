#define DEBUG_PARENT_GENERATOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gearbox;

namespace EndgameTableGen
{
    internal delegate int PositionVisitorFunc(Table table, Board board, int tindex);

    internal class TableGenerator : TableWorker
    {
        internal const int UndefinedScore   = Table.MinScore;
        internal const int UnreachablePos   = Table.MinScore + 1;
        internal const int EnemyMatedScore  = +2000;
        internal const int FriendMatedScore = -2000;
        internal const int DrawScore = 0;

        private static readonly int[] EightfoldSymmetryTable = new int[]
        {
            21, 22, 23, 24,
                32, 33, 34,
                    43, 44,
                        54
        };
        internal static readonly int[] WholeBoardOffsetTable = MakeOffsetTable('a', 'h', '1', '8');
        private static readonly int[] LeftHalfOffsetTable = MakeOffsetTable('a', 'd', '1', '8');
        private static readonly int[] PawnOffsetTable = MakeOffsetTable('a', 'h', '2', '7');

        private readonly MoveList LegalMoveList = new MoveList();
        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<long, Table> finished = new();
        public long CurrentConfigId;
        private MemoryTable table;
        private MemoryTable bestScoreSoFar;
        private byte[] whiteUnresolvedChildren;
        private byte[] blackUnresolvedChildren;
        private List<int> parent_tindex_list = new();
        private List<int> local_child_tindex_list = new();
        private int prevTableIndex;     // detects generating table indexes out of order
        private int child_ply;
        private int max_child_ply;     // most distant forced mate that reaches into foreign tables

        public TableGenerator(int max_table_size)
        {
            if (max_table_size > 0)
            {
                // Pre-allocate the in-memory table image to the largest size we will need.
                // Start out with size=0, capacity=max_table_size.
                // We will adjust the effective table size as needed for each configuration.
                table = new MemoryTable(0, max_table_size);
                bestScoreSoFar = new MemoryTable(0, max_table_size);
                whiteUnresolvedChildren = new byte[max_table_size];
                blackUnresolvedChildren = new byte[max_table_size];
            }
        }

        public override void Dispose()
        {
            lock (finished)
            {
                foreach (Table table in finished.Values)
                    table.Dispose();

                finished.Clear();
            }
        }

        public void AddToFinishedTable(long config_id, Table table)
        {
            // This is needed only by the ParallelTableGenerator so that
            // multiple worker threads can share the tables they have completed.
            // There is no need to lock the table, because this function is only
            // called thread-safely from the same thread that calls GenerateTable.
            // However, reading from the table itself is not thread-safe in general
            // (DiskTable contains a file stream in which we have to seek).
            // So only if we don't know about this table already, make a read-only clone of it.

            if (!finished.ContainsKey(config_id))
                finished.Add(config_id, table.ReadOnlyClone());
        }

        private static int[] MakeOffsetTable(char file1, char file2, char rank1, char rank2)
        {
            var table = new List<int>();
            for (char rank = rank1; rank <= rank2; ++rank)
                for (char file = file1; file <= file2; ++file)
                    table.Add(Board.Offset(file, rank));
            return table.ToArray();
        }

        private static int KingDistance(int ofs1, int ofs2)
        {
            int dx = Math.Abs((ofs1 % 10) - (ofs2 % 10));
            int dy = Math.Abs((ofs1 / 10) - (ofs2 / 10));
            return Math.Max(dx, dy);
        }

        public override void Start()
        {
            chrono.Restart();
            Debug.Assert(ReverseSideConfigId(1234567851) == 2143658715);
        }

        public override Table GenerateTable(int[,] config)
        {
            string filename = ConfigFileName(config);
            int size = (int)TableSize(config);
            CurrentConfigId = GetConfigId(config, false);
            string configIdText = CurrentConfigId.ToString("D10");
            Log("CurrentConfigId = {0}", configIdText);
            long reverseConfigId = GetConfigId(config, true);
            Debug.Assert(ReverseSideConfigId(CurrentConfigId) == reverseConfigId);

            if (File.Exists(filename))
            {
                // We have already calculated this endgame table. Fall through to code below to recycle the file.
                Log("Recyling: {0}", filename);
            }
            else
            {
                Log("Generating size {0:n0} table: {1}", size, filename);

                // Try to recycle the previously used table's memory if possible.
                // Otherwise, allocate a new table.
                if (table != null && table.Capacity >= size)
                {
                    Log("GenerateTable: Resizing existing table with capacity {0} to {1}", table.Capacity, size);
                    table.Resize(size);
                    bestScoreSoFar.Resize(size);
                    Array.Clear(whiteUnresolvedChildren, 0, whiteUnresolvedChildren.Length);
                    Array.Clear(blackUnresolvedChildren, 0, blackUnresolvedChildren.Length);
                }
                else
                {
                    Log("GenerateTable: Allocating new table with size {0}", size);
                    table = new MemoryTable(size, size);
                    bestScoreSoFar = new MemoryTable(size, size);
                    whiteUnresolvedChildren = new byte[size];
                    blackUnresolvedChildren = new byte[size];
                }

                table.SetAllScores(UnreachablePos);
                bestScoreSoFar.SetAllScores(UndefinedScore);

                max_child_ply = 0;
                int progress = ForEachPosition(table, config, InitPosition);
                for (child_ply = 0; child_ply <= max_child_ply || progress > 0; ++child_ply)
                {
                    progress = ForEachPosition(table, config, VisitChildPosition);
                    progress += SweepParentPositions();
                }

                // Any lingering positions with undefined scores should be interpreted as draws.
                table.ReplaceScores(UndefinedScore, DrawScore);

                // Save the table to disk.
                table.Save(filename);
                Log("Saved: {0}", filename);
            }

            // Migrate the table from high-speed/high-memory to slower/low-memory.
            var diskTable = new DiskTable(size, filename);
            diskTable.OpenForRead();
            finished.Add(CurrentConfigId, diskTable);
            return diskTable;
        }

        public override void Finish()
        {
            chrono.Stop();
            Log("Finished after {0} = {1} seconds.", chrono.Elapsed, chrono.Elapsed.TotalSeconds);
        }

        public int ForEachPosition(Table table, int[,] config, PositionVisitorFunc func)
        {
            int sum = 0;

            prevTableIndex = 0;

            var board = new Board(false);       // start with a completely empty chess board

            int[] wkOffsetTable;
            int pawns = config[WHITE, P_INDEX] + config[BLACK, P_INDEX];
            if (pawns > 0)
            {
                // If there is at least one pawn on the board, we use left/right symmetry
                // for placing the White King on the board.
                // This restricts the range of White King digits to the range 0..31.
                wkOffsetTable = LeftHalfOffsetTable;
            }
            else
            {
                // When there are no pawns, we use 8-fold symmetry for placing the White king.
                wkOffsetTable = EightfoldSymmetryTable;
            }

            string config_text = ConfigString(config);
            for (int wkindex = 0; wkindex < wkOffsetTable.Length; ++wkindex)
            {
                int wkofs = wkOffsetTable[wkindex];
                board.PlaceWhiteKing(wkofs);

                // There can be redundancy when we are using eightfold symmetry (there are no pawns),
                // and the white king is on the diagonal a1..d4.
                // A mirror image across that diagonal can yield two different but equivalent positions.
                // We break the tie by working the position that leaves the first off-diagonal piece
                // below the diagonal. If *all* pieces are on the diagonal, there is no redundancy to resolve.
                int diag = Position.DiagonalHeight(Position.IndexFromOffset(wkofs));
                bool need_diag_filter = (pawns == 0) && (diag == 0);
                var timeSinceLastUpdate = Stopwatch.StartNew();

                for (int bkindex = 0; bkindex < WholeBoardOffsetTable.Length; ++bkindex)
                {
                    int bk_diag_height = Position.DiagonalHeight(bkindex);
                    if (need_diag_filter && (bk_diag_height > 0))
                        continue;   // eliminate redundant board positions when using 8-fold symmetry and white king is on the diagonal

                    int bkofs = WholeBoardOffsetTable[bkindex];

                    // Avoid all illegal positions caused by "touching" kings.
                    if (KingDistance(wkofs, bkofs) > 1)
                    {
                        board.PlaceBlackKing(bkofs);

                        sum += PositionSearch(
                            func, table, config, board,
                            64*wkindex + bkindex,
                            Q_INDEX,
                            config[WHITE,Q_INDEX],
                            config[BLACK,Q_INDEX],
                            need_diag_filter && (bk_diag_height == 0),
                            0);

                        if ((wkindex + 1 == wkOffsetTable.Length && bkindex + 1 == WholeBoardOffsetTable.Length) || (timeSinceLastUpdate.Elapsed.TotalSeconds > 15.0))
                        {
                            Log("ForEachPosition[{0} : {1:00}/{2:00}]: wk={3}/{4}, bk={5}/{6}, sum={7}",
                                config_text,
                                child_ply,
                                max_child_ply,
                                wkindex,
                                wkOffsetTable.Length,
                                bkindex,
                                WholeBoardOffsetTable.Length,
                                sum);

                            timeSinceLastUpdate.Restart();
                        }
                    }
                }
            }

            return sum;
        }

        private static int RankNumber(int offset)
        {
            return (offset / 10) - 1;
        }

        private int PositionSearch(
            PositionVisitorFunc func,
            Table table,
            int[,] config,
            Board board,
            int tableIndex,
            int pieceIndex,
            int whiteRemaining,
            int blackRemaining,
            bool need_diag_filter,
            int startIndex)
        {
            int sum = 0;
            Square[] square = board.GetSquaresArray();

            if (pieceIndex < P_INDEX)
            {
                // A White, non-pawn, non-king piece can be placed in any of the 64 squares, so long as that square is empty.
                if (whiteRemaining > 0)
                {
                    Square piece = MakeSquare(WHITE, pieceIndex);
                    for (int wi = startIndex; wi < WholeBoardOffsetTable.Length; ++wi)
                    {
                        int diag = Position.DiagonalHeight(wi);
                        if (need_diag_filter && (diag > 0))
                            continue;   // this is a redundant position, compared to a diagonal mirror image

                        int wofs = WholeBoardOffsetTable[wi];
                        if (square[wofs] == Square.Empty)
                        {
                            square[wofs] = piece;

                            sum += PositionSearch(
                                func, table, config, board,
                                (64 * tableIndex) + wi,
                                pieceIndex,
                                whiteRemaining-1,
                                blackRemaining,
                                need_diag_filter && (diag == 0),
                                (whiteRemaining > 1) ? (wi + 1) : 0);

                            square[wofs] = Square.Empty;
                        }
                    }
                }
                else if (blackRemaining > 0)
                {
                    // A Black, non-pawn, non-king piece can be placed in any of the 64 squares, so long as that square is empty.
                    Square piece = MakeSquare(BLACK, pieceIndex);
                    for (int bi = startIndex; bi < WholeBoardOffsetTable.Length; ++bi)
                    {
                        int diag = Position.DiagonalHeight(bi);
                        if (need_diag_filter && (diag > 0))
                            continue;   // this is a redundant position, compared to a diagonal mirror image

                        int bofs = WholeBoardOffsetTable[bi];
                        if (square[bofs] == Square.Empty)
                        {
                            square[bofs] = piece;

                            sum += PositionSearch(
                                func, table, config, board,
                                (64 * tableIndex) + bi,
                                pieceIndex,
                                whiteRemaining,
                                blackRemaining-1,
                                need_diag_filter && (diag == 0),
                                (blackRemaining > 1) ? (bi + 1) : 0);

                            square[bofs] = Square.Empty;
                        }
                    }
                }
                else
                {
                    // We have exhausted all the White and Black pieces at this piece index.
                    // Go to the next piece index.
                    sum += PositionSearch(
                        func, table, config, board,
                        tableIndex,
                        pieceIndex + 1,
                        config[WHITE, pieceIndex + 1],
                        config[BLACK, pieceIndex + 1],
                        need_diag_filter,
                        0);
                }
            }
            else if (whiteRemaining + blackRemaining > 0)   // pieceIndex == P_INDEX
            {
                // If both White and Black have at least one pawn, en passant captures are possible.
                // In this case, the side that just moved could have moved a pawn two squares forward,
                // creating an en passant capture opportunity.
                // Then pawns can be in one of 48 + 8 = 56 different states.
                // Otherwise, pawns can only be in one of 48 states.
                int wp = config[WHITE, P_INDEX];
                int bp = config[BLACK, P_INDEX];
                bool isEnPassantPossible = (wp > 0 && bp > 0);
                int pawnFactor = isEnPassantPossible ? 56 : 48;

                if (whiteRemaining > 0)
                {
                    // Try putting the next White pawn everywhere it can go.
                    // We need to generate table indexes in strictly increasing order,
                    // and because of the way en passant is encoded, we must generate
                    // all non-en-passant placements before all en-passant placements.
                    for (int i = startIndex; i < PawnOffsetTable.Length; ++i)
                    {
                        int ofs = PawnOffsetTable[i];
                        if (square[ofs] == Square.Empty)
                        {
                            // Tricky: board.GetEpTarget() will always return 0 when isEnPassantPossible is false.
                            // That's because we will never call board.SetEpTarget().
                            int ep = board.GetEpTarget();
                            if (ofs == ep || ofs == ep + Direction.N || ofs == ep + Direction.S)
                            {
                                // We can't put anything in the empty space behind a pawn that
                                // has just moved two squares. The square behind that pawn
                                // is called the "en passant target", where an enemy pawn could land
                                // after an en passant capture.
                                // Tricky: the above logic works whether the target pawn is white or black.
                                // One of (ep+N), (ep+S) will be the empty square the pawn just double-moved from,
                                // and the other will be where the pawn landed.
                                continue;
                            }

                            square[ofs] = Square.WP;

                            sum += PositionSearch(
                                func, table, config, board,
                                (pawnFactor * tableIndex) + i,
                                pieceIndex,
                                whiteRemaining - 1,
                                blackRemaining,
                                false,     // we don't need diag filter when there are pawns
                                (whiteRemaining > 1) ? (i + 1) : 0);

                            square[ofs] = Square.Empty;
                        }
                    }

                    // Check for en passant placements of this white pawn, but only if
                    // there are black pawns on the board too (isEnPassantPossible),
                    // and we haven't already put another pawn in the en passant state.
                    // "There can be only one!" -- Highlander.
                    if (isEnPassantPossible && (0 == board.GetEpTarget()))
                    {
                        for (int i = 16; i < 24; ++i)
                        {
                            int ofs = PawnOffsetTable[i];

                            // A white pawn on the fourth rank could have just moved two squares to get there,
                            // but only if the two squares behind it are empty!
                            if (square[ofs] == Square.Empty &&
                                square[ofs + Direction.S] == Square.Empty &&
                                square[ofs + 2*Direction.S] == Square.Empty)
                            {
                                square[ofs] = Square.WP;
                                board.SetEpTarget(ofs + Direction.S);

                                sum += PositionSearch(
                                    func, table, config, board,
                                    (pawnFactor * tableIndex) + (i + 32),   // encode white pawn as if on rank 8 instead of rank 4
                                    pieceIndex,
                                    whiteRemaining - 1,
                                    blackRemaining,
                                    false,     // we don't need diag filter when there are pawns
                                    (whiteRemaining > 1) ? (i + 1) : 0);

                                board.SetEpTarget(0);
                                square[ofs] = Square.Empty;
                            }
                        }
                    }
                }
                else // blackRemaining > 0
                {
                    // Try putting the next Black pawn everywhere it can go.
                    // We need to generate table indexes in strictly increasing order,
                    // and because of the way en passant is encoded, we must generate
                    // all non-en-passant placements before all en-passant placements.
                    for (int i=0; i < PawnOffsetTable.Length; ++i)
                    {
                        int ofs = PawnOffsetTable[i];
                        if (square[ofs] == Square.Empty)
                        {
                            // Tricky: board.GetEpTarget() will always return 0 when isEnPassantPossible is false.
                            // That's because we will never call board.SetEpTarget().
                            int ep = board.GetEpTarget();
                            if (ofs == ep || ofs == ep + Direction.N || ofs == ep + Direction.S)
                            {
                                // We can't put anything in the empty space behind a pawn that
                                // has just moved two squares. The square behind that pawn
                                // is called the "en passant target", where an enemy pawn could land
                                // after an en passant capture.
                                // Tricky: the above logic works whether the target pawn is white or black.
                                // One of (ep+N), (ep+S) will be the empty square the pawn just double-moved from,
                                // and the other will be where the pawn landed.
                                continue;
                            }

                            square[ofs] = Square.BP;

                            sum += PositionSearch(
                                func, table, config, board,
                                (pawnFactor * tableIndex) + i,
                                pieceIndex,
                                whiteRemaining,
                                blackRemaining - 1,
                                false,     // we don't need diag filter when there are pawns
                                (blackRemaining > 1) ? (i + 1) : 0);

                            square[ofs] = Square.Empty;
                        }
                    }

                    // Check for en passant placements of this black pawn, but only if
                    // there are white pawns on the board too (isEnPassantPossible),
                    // and we haven't already put another pawn in the en passant state.
                    // "There can be only one!" -- Highlander.
                    if (isEnPassantPossible && (0 == board.GetEpTarget()))
                    {
                        for (int i = 24; i < 32; ++i)
                        {
                            int ofs = PawnOffsetTable[i];

                            // A Black pawn on the fifth rank could have just moved two squares to get there,
                            // but only if the two squares behind it are empty!
                            if (square[ofs] == Square.Empty &&
                                square[ofs + Direction.N] == Square.Empty &&
                                square[ofs + 2*Direction.N] == Square.Empty)
                            {
                                square[ofs] = Square.BP;
                                board.SetEpTarget(ofs + Direction.N);

                                sum += PositionSearch(
                                    func, table, config, board,
                                    (pawnFactor * tableIndex) + (i + 24),   // encode black pawn as if on rank 8 instead of rank 5
                                    pieceIndex,
                                    whiteRemaining,
                                    blackRemaining - 1,
                                    false,     // we don't need diag filter when there are pawns
                                    (blackRemaining > 1) ? (i + 1) : 0);

                                board.SetEpTarget(0);
                                square[ofs] = Square.Empty;
                            }
                        }
                    }
                }
            }
            else
            {
                // We have placed all the pieces on the board!
                // Make sure we are generating table indexes in strictly increasing order.
                if (tableIndex <= prevTableIndex)
                    throw new Exception($"Generated table indexes out of order: prev={prevTableIndex}, curr={tableIndex}");

                prevTableIndex = tableIndex;

                // Update board inventory after having poked and prodded the board.
                board.RefreshAfterDangerousChanges();

                // Visit the resulting position from both points of view: White's and Black's.

                // En passant states limit whether White or Black could possibly have the turn.
                switch (board.GetEpTarget() / 10)
                {
                    case 0:     // no en passant: either side could have the turn.
                        // What if it is White's turn to move?
                        board.SetTurn(true);
                        if (board.IsValidPosition())
                            sum += func(table, board, tableIndex);

                        // What if it is Black's turn to move?
                        board.SetTurn(false);
                        if (board.IsValidPosition())
                            sum += func(table, board, tableIndex);
                        break;

                    case 4:     // A white pawn is in the en passant state. Therefore, it can only be Black's turn.
                        board.SetTurn(false);
                        if (board.IsValidPosition())
                            sum += func(table, board, tableIndex);
                        break;

                    case 7:     // A black pawn is in the en passant state. Therefore, it can only be White's turn.
                        board.SetTurn(true);
                        if (board.IsValidPosition())
                            sum += func(table, board, tableIndex);
                        break;

                    default:
                        throw new Exception($"Invalid en passant target {board.GetEpTarget()} in position: {board.ForsythEdwardsNotation()}");
                }
            }

            return sum;
        }

        private static Square MakeSquare(int side, int piece)
        {
            Square s;

            switch (side)
            {
                case WHITE:
                    s = Square.White;
                    break;

                case BLACK:
                    s = Square.Black;
                    break;

                default:
                    throw new ArgumentException("Invalid side");
            }

            switch (piece)
            {
                case Q_INDEX:
                    s |= Square.Queen;
                    break;

                case R_INDEX:
                    s |= Square.Rook;
                    break;

                case B_INDEX:
                    s |= Square.Bishop;
                    break;

                case N_INDEX:
                    s |= Square.Knight;
                    break;

                case P_INDEX:
                    s |= Square.Pawn;
                    break;

                default:
                    throw new ArgumentException("Invalid piece");
            }

            return s;
        }

        private int GetScore(Table table, bool isWhiteTurn, int tindex)
        {
            return isWhiteTurn ? table.GetWhiteScore(tindex) : table.GetBlackScore(tindex);
        }

        private int SetScore(Table table, bool isWhiteTurn, int tindex, int score)
        {
            if (isWhiteTurn)
                table.SetWhiteScore(tindex, score);
            else
                table.SetBlackScore(tindex, score);
            return 1;   // assist tallying the number of scores set
        }

        private void UpdateMaxSearchPly(int score, Board board, int tindex)
        {
            if (score != 0 && score >= FriendMatedScore && score <= EnemyMatedScore)
            {
                int plies = EnemyMatedScore - Math.Abs(score);
                if (max_child_ply < plies)
                {
                    max_child_ply = plies;
                    Log("Updated max_child_ply={0} for tindex={1} : {2}", plies, tindex, board.ForsythEdwardsNotation());
                }
            }
        }

        internal static int AdjustScoreForPly(int score)
        {
            if (score == UndefinedScore)
                throw new ArgumentException("Attempt to adjust an undefined score.");

            if (score == UnreachablePos)
                throw new ArgumentException("Attempt to adjust score for an unreachable position.");

            // Adjust for negamax and ply delay.
            if (score > 0)
                return -(score - 1);

            if (score < 0)
                return -(score + 1);

            return 0;
        }

        internal static long ReverseSideConfigId(long id)
        {
            if (id < 0 || id > 9999999999)
                throw new ArgumentException(string.Format("Invalid config id: {0}", id));

            // Swap the even/odd decimal digits to swap the White/Black material.
            long rev = 0;
            long pow = 1;
            for (int i=0; i < 5; ++i)
            {
                long shift = id / pow;
                long b = shift % 10;
                long w = (shift / 10) % 10;
                rev += pow * (10*b + w);
                pow *= 100;
            }
            return rev;
        }

        private static void ValidatePlacement(Square[] square, int ofs, Square piece)
        {
            if (ofs < 0 || ofs >= 120 || square[ofs] == Square.Offboard)
                throw new ArgumentException(string.Format("Offset is outside the board: {0}", ofs));

            if (square[ofs] != Square.Empty)
                throw new ArgumentException(string.Format("Attempt to place {0} at {1}, which already contains {2}", piece, Board.Algebraic(ofs), square[ofs]));
        }

        private static void Place(Square[] square, int ofs, Square piece)
        {
            ValidatePlacement(square, ofs, piece);
            square[ofs] = piece;
        }

        public static void DecodePosition(Board board, long config_id, int table_index, bool white_turn)
        {
            board.Clear(white_turn);
            Square[] square = board.GetSquaresArray();

            int[,] config = DecodeConfig(config_id);
            int wp = config[WHITE, P_INDEX];
            int bp = config[BLACK, P_INDEX];
            bool ep = (wp > 0 && bp > 0);
            int pawn_multiplier = ep ? 56 : 48;

            int t = table_index;
            int ofs, index;
            int ep_count = 0;

            // Decode black pawns.
            for (int k=0; k < bp; ++k)
            {
                index = t % pawn_multiplier;
                t /= pawn_multiplier;
                if (index >= 48)
                {
                    // This black pawn is in the en passant target state.
                    ++ep_count;
                    index -= 24;
                    ofs = Position.OffsetFromIndex(index + 8);
                    board.SetEpTarget(ofs + Direction.N);
                }
                else
                {
                    ofs = Position.OffsetFromIndex(index + 8);
                }
                Place(square, ofs, Square.BP);
            }

            // Decode white pawns.
            for (int k=0; k < wp; ++k)
            {
                index = t % pawn_multiplier;
                t /= pawn_multiplier;
                if (index >= 48)
                {
                    // This white pawn is in the en passant target state.
                    ++ep_count;
                    index -= 32;
                    ofs = Position.OffsetFromIndex(index + 8);
                    board.SetEpTarget(ofs + Direction.S);
                }
                else
                {
                    ofs = Position.OffsetFromIndex(index + 8);
                }
                Place(square, ofs, Square.WP);
            }

            if (ep_count > 1)
                throw new ArgumentException(string.Format("Position has {0} pawns in the en passant target state.", ep_count));

            // Decode all pieces between king and pawn.
            for (int mover = N_INDEX; mover >= Q_INDEX; --mover)
            {
                for (int side = BLACK; side >= WHITE; --side)
                {
                    Square piece = MakeSquare(side, mover);
                    for (int k=0; k < config[side, mover]; ++k)
                    {
                        index = t % 64;
                        t /= 64;
                        ofs = Position.OffsetFromIndex(index);
                        Place(square, ofs, piece);
                    }
                }
            }

            // Decode the black king.
            index = t % 64;
            t /= 64;
            ofs = Position.OffsetFromIndex(index);
            ValidatePlacement(square, ofs, Square.BK);
            board.PlaceBlackKing(ofs);

            // Decode the white king, following the symmetry rules.
            if (wp + bp == 0)
                ofs = EightfoldSymmetryTable[t];
            else
                ofs = LeftHalfOffsetTable[t];
            ValidatePlacement(square, ofs, Square.WK);
            board.PlaceWhiteKing(ofs);

            board.RefreshAfterDangerousChanges();
        }

        private int InitPosition(Table table, Board board, int parent_tindex)
        {
            // This function is called twice for each position:
            // once with White to move, the other with Black to move.
            // This function interprets each position as a parent position,
            // from which emanate zero or more legal moves that lead to child position(s).
            bool parent_is_white = board.IsWhiteTurn;
            bool child_is_white = !parent_is_white;

            // Initialize checkmates, stalemates, and foreign "best-so-far" table scores.
            // InitPoistion is the only time we have to generate legal moves.
            // After this, VisitPosition will directly compute parent table indexes that lead to this child position.
            board.GenMoves(LegalMoveList);

            if (LegalMoveList.nmoves == 0)
            {
                // This is either stalemate or checkmate.
                // Set the score for this position accordingly.
                int score = board.UncachedPlayerInCheck() ? FriendMatedScore : DrawScore;
                return SetScore(table, parent_is_white, parent_tindex, score);
            }

            local_child_tindex_list.Clear();
            int best_parent_score = UndefinedScore;
            for (int i = 0; i < LegalMoveList.nmoves; ++i)
            {
                Move move = LegalMoveList.array[i];
                board.PushMove(move);
                long child_config_id = board.GetEndgameConfigId(false);
                if (child_config_id == CurrentConfigId)
                {
                    // We want to count up how many unresolved children there are for each parent position.
                    // Tricky: two different legal moves can lead to the same child table index,
                    // due to symmetries. We must avoid counting a (parent_tindex, child_tindex) pair more than once.
                    int child_tindex = board.GetEndgameTableIndex(false);
                    if (!local_child_tindex_list.Contains(child_tindex))
                    {
                        local_child_tindex_list.Add(child_tindex);
                        if (local_child_tindex_list.Count > (int)byte.MaxValue)
                            throw new Exception($"Overflowed local_child_tindex_list at parent_tindex = {parent_tindex} in board position {board.ForsythEdwardsNotation()}");

#if DEBUG_PARENT_GENERATOR
                        ParentPositionGenerator.GenParentList(parent_tindex_list, board);
                        if (!parent_tindex_list.Contains(parent_tindex))
                            throw new Exception($"parent list [{string.Join(", ", parent_tindex_list)}] does not contain {parent_tindex} for position {board.ForsythEdwardsNotation()}");
#endif
                    }
                }
                else
                {
                    // This move transitions out of this endgame configuration and into another.
                    // Find the best parent score for any foreign child score.
                    int child_score;
                    Table child_table;
                    if (finished.TryGetValue(child_config_id, out child_table))
                    {
                        int child_tindex = board.GetEndgameTableIndex(false);
                        child_score = GetScore(child_table, child_is_white, child_tindex);
                    }
                    else
                    {
                        // We don't know about that endgame table.
                        // Try swapping White and Black pieces.
                        child_config_id = board.GetEndgameConfigId(true);
                        if (finished.TryGetValue(child_config_id, out child_table))
                        {
                            int child_tindex = board.GetEndgameTableIndex(true);
                            child_score = GetScore(child_table, !child_is_white, child_tindex);
                        }
                        else
                        {
                            // The move transitions to a configuration we did not calculate.
                            // We only do that when the configuration is always a draw due to insufficient material.
                            child_score = 0;
                        }
                    }
                    int parent_score = AdjustScoreForPly(child_score);
                    if (parent_score > best_parent_score)
                        best_parent_score = parent_score;
                }
                board.PopMove();
            }

            // Track how far into the future (number of plies) the best foreign score reaches.
            // This affects how many times we iterate in the loop around the ForEachPosition call.
            UpdateMaxSearchPly(best_parent_score, board, parent_tindex);

            // Remember how many child nodes we have not yet resolved for this parent node.
            // To "resolve" means to put a finalized score into table[parent_index].
            byte local_children = (byte)local_child_tindex_list.Count;
            if (parent_is_white)
                whiteUnresolvedChildren[parent_tindex] = local_children;
            else
                blackUnresolvedChildren[parent_tindex] = local_children;

            // Indicate that this position is reachable, but we don't yet know its eventual score.
            SetScore(table, parent_is_white, parent_tindex, UndefinedScore);

            // Remember the score of the best child node we have seen so far.
            return SetScore(bestScoreSoFar, parent_is_white, parent_tindex, best_parent_score);
        }

        private int VisitChildPosition(Table table, Board board, int child_tindex)
        {
            // This function is called twice for each position:
            // once with White to move, the other with Black to move.
            // This function interprets each position as a child position,
            // into which zero or more parent positions can reach via legal moves.
            bool child_is_white = board.IsWhiteTurn;
            bool parent_is_white = !child_is_white;

            int child_score = GetScore(table, child_is_white, child_tindex);
            int progress = 0;
            int parent_ply = child_ply + 1;
            byte[] unresolvedChildren = parent_is_white ? whiteUnresolvedChildren : blackUnresolvedChildren;

            int child_target_score;
            int parent_target_score;
            if (0 != (parent_ply & 1))
            {
                // On odd parent plies, we are looking for winning parent positions.
                parent_target_score = EnemyMatedScore - parent_ply;
                child_target_score = FriendMatedScore + child_ply;
            }
            else
            {
                // On even parent plies, are looking for losing parent positions.
                parent_target_score = FriendMatedScore + parent_ply;
                child_target_score = EnemyMatedScore - child_ply;
            }

            if (child_score == child_target_score)
            {
                ParentPositionGenerator.GenParentList(parent_tindex_list, board);
                foreach (int parent_tindex in parent_tindex_list)
                {
                    if (unresolvedChildren[parent_tindex] == 0)
                        throw new Exception($"Expected unresolved child {child_tindex} for parent {parent_tindex}.");

                    if (GetScore(bestScoreSoFar, parent_is_white, parent_tindex) < parent_target_score)
                        progress += SetScore(bestScoreSoFar, parent_is_white, parent_tindex, parent_target_score);

                    --unresolvedChildren[parent_tindex];
                }
            }

            return progress;
        }

        private int SweepParentPositions()
        {
            int progress = 0;
            int parent_ply = child_ply + 1;
            int table_size = table.Size;

            if (0 != (parent_ply & 1))
            {
                // Promote winning best-scores to actual scores regardless of remaining children,
                // if best-so-far reaches the threshold, and we haven't already finalized the parent score.
                int parent_winning_score = TableGenerator.EnemyMatedScore - parent_ply;
                for (int parent_tindex = 0; parent_tindex < table_size; ++parent_tindex)
                {
                    if (table.GetWhiteScore(parent_tindex) == TableGenerator.UndefinedScore &&
                        bestScoreSoFar.GetWhiteScore(parent_tindex) == parent_winning_score)
                    {
                        table.SetWhiteScore(parent_tindex, parent_winning_score);
                        ++progress;
                    }

                    if (table.GetBlackScore(parent_tindex) == TableGenerator.UndefinedScore &&
                        bestScoreSoFar.GetBlackScore(parent_tindex) == parent_winning_score)
                    {
                        table.SetBlackScore(parent_tindex, parent_winning_score);
                        ++progress;
                    }
                }
            }
            else
            {
                // Promote losing best-scores to actual scores when all children are resolved.
                for (int parent_tindex = 0; parent_tindex < table_size; ++parent_tindex)
                {
                    if (whiteUnresolvedChildren[parent_tindex] == 0 &&
                        table.GetWhiteScore(parent_tindex) == TableGenerator.UndefinedScore)
                    {
                        int final_score = bestScoreSoFar.GetWhiteScore(parent_tindex);
                        if (final_score <= TableGenerator.UnreachablePos)
                            throw new Exception($"Invalid bestSoFar score {final_score} at White parent {parent_tindex}");
                        table.SetWhiteScore(parent_tindex, final_score);
                        ++progress;
                    }

                    if (blackUnresolvedChildren[parent_tindex] == 0 &&
                        table.GetBlackScore(parent_tindex) == TableGenerator.UndefinedScore)
                    {
                        int final_score = bestScoreSoFar.GetBlackScore(parent_tindex);
                        if (final_score <= TableGenerator.UnreachablePos)
                            throw new Exception($"Invalid bestSoFar score {final_score} at Black parent {parent_tindex}");
                        table.SetBlackScore(parent_tindex, final_score);
                        ++progress;
                    }
                }
            }

            Log("SweepParentPositions: finalized {0} parent scores.", progress);
            return progress;
        }
    }
}
