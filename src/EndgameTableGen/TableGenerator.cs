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
        private static readonly int[] WholeBoardOffsetTable = MakeOffsetTable('a', 'h', '1', '8');
        private static readonly int[] LeftHalfOffsetTable = MakeOffsetTable('a', 'd', '1', '8');
        private static readonly int[] PawnOffsetTable = MakeOffsetTable('a', 'h', '2', '7');

        private readonly MoveList LegalMoveList = new MoveList();
        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<long, Table> finished = new();
        public long CurrentConfigId;
        private MemoryTable table;
        private int prevTableIndex;     // detects generating table indexes out of order
        private int search_ply;
        private int max_search_ply;     // most distant forced mate that reaches into foreign tables

        public TableGenerator(int max_table_size)
        {
            if (max_table_size > 0)
            {
                // Pre-allocate the in-memory table image to the largest size we will need.
                // Start out with size=0, capacity=max_table_size.
                // We will adjust the effective table size as needed for each configuration.
                table = new MemoryTable(0, max_table_size);
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
                    table.Clear();
                }
                else
                {
                    Log("GenerateTable: Allocating new table with size {0}", size);
                    table = new MemoryTable(size, size);
                }

                max_search_ply = 0;
                table.SetAllScores(UnreachablePos);

                for (search_ply = 0; search_ply <= max_search_ply; ++search_ply)
                    ForEachPosition(table, config, VisitPosition);

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

                        if (timeSinceLastUpdate.Elapsed.TotalSeconds > 15.0)
                        {
                            Log("ForEachPosition[{0} : {1:00}/{2:00}]: wk={3}/{4}, bk={5}/{6}, sum={7}",
                                config_text,
                                search_ply,
                                max_search_ply,
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

        private int VisitPosition(Table table, Board board, int tindex)
        {
            // This function is called twice for each position:
            // once with White to move, the other with Black to move.

            // If we have already scored this position, bail out immediately!
            int score = GetScore(table, board.IsWhiteTurn, tindex);
            if (score > UnreachablePos)
                return 0;

            board.GenMoves(LegalMoveList);

            if (LegalMoveList.nmoves == 0)
            {
                // This is either stalemate or checkmate.
                // Set the score for this position accordingly.
                score = board.UncachedPlayerInCheck() ? FriendMatedScore : DrawScore;
                UpdateMaxSearchPly(score, board, tindex);
                return SetScore(table, board.IsWhiteTurn, tindex, score);
            }

            // Indicate that we did reach this as a valid position.
            SetScore(table, board.IsWhiteTurn, tindex, UndefinedScore);

            int best_score = UndefinedScore;

            for (int i = 0; i < LegalMoveList.nmoves; ++i)
            {
                Move move = LegalMoveList.array[i];
                board.PushMove(move);
                long after_config_id = board.GetEndgameConfigId(false);
                int after_score;
                if (after_config_id == CurrentConfigId)
                {
                    // The most common case: making a move stays inside the current endgame table.
                    int after_tindex = board.GetEndgameTableIndex(false);
                    after_score = GetScore(table, board.IsWhiteTurn, after_tindex);
                }
                else
                {
                    // This move transitions out of this endgame configuration and into another.
                    // We do not save such children, because the score of the after-position
                    // is already decided. We just remember the best of these "foreign" scores
                    // and submit it below to the child writer, to close out this batch of children.
                    Table after_table;
                    if (finished.TryGetValue(after_config_id, out after_table))
                    {
                        int after_tindex = board.GetEndgameTableIndex(false);
                        after_score = GetScore(after_table, board.IsWhiteTurn, after_tindex);
                    }
                    else
                    {
                        // We don't know about that endgame table.
                        // Try swapping White and Black pieces.
                        after_config_id = board.GetEndgameConfigId(true);
                        if (finished.TryGetValue(after_config_id, out after_table))
                        {
                            int after_tindex = board.GetEndgameTableIndex(true);
                            after_score = GetScore(after_table, board.IsBlackTurn, after_tindex);
                        }
                        else
                        {
                            // The move transitions to a configuration we did not calculate.
                            // We only do that when the configuration is always a draw due to insufficient material.
                            after_score = 0;
                        }
                    }
                }
                board.PopMove();

                if (after_score > UnreachablePos)
                {
                    score = AdjustScoreForPly(after_score);
                    if (score > best_score)
                        best_score = score;
                }
                else
                {
                    // On even plies, we look for forced losses for the side to move.
                    // A forced loss requires ALL children have known scores.
                    // If we find any unknown score, bail out immediately so we don't waste time.
                    if (0 == (search_ply & 1))
                        return 0;
                }
            }

            // Track how far into the future (number of plies) the best foreign score reaches.
            // This controls how many times we iterate in the loop around the ForEachPosition call.
            UpdateMaxSearchPly(best_score, board, tindex);

            if (best_score > UnreachablePos)
            {
                if (0 != (search_ply & 1))
                {
                    // On odd plies, we look for forced wins with forced wins for the side to move.
                    // A forced win occurs when at least one child node has the winning score
                    // corresponding to this odd ply value.
                    int winning_score = TableGenerator.EnemyMatedScore - search_ply;
                    if (best_score == winning_score)
                        return SetScore(table, board.IsWhiteTurn, tindex, best_score);
                }
                else
                {
                    // On even plies, we look for forced losses for the side to move.
                    // A forced loss occurs when ALL children have known scores, and the score
                    // is the exact score we expect for losing at the specified ply level.
                    // Getting here means we didn't bail out early in the move loop,
                    // which means we found all child scores.
                    int losing_score = TableGenerator.FriendMatedScore + search_ply;
                    if (best_score == losing_score)
                        return SetScore(table, board.IsWhiteTurn, tindex, best_score);
                }
            }

            return 0;
        }

        private void UpdateMaxSearchPly(int score, Board board, int tindex)
        {
            if (score != 0 && score >= FriendMatedScore && score <= EnemyMatedScore)
            {
                int plies = EnemyMatedScore - Math.Abs(score);
                if (max_search_ply < plies)
                {
                    max_search_ply = plies;
                    Log("Updated max search ply to {0} for tindex={1} : {2}", plies, tindex, board.ForsythEdwardsNotation());
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
    }
}
