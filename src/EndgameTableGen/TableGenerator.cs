using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gearbox;

namespace EndgameTableGen
{
    internal delegate int PositionVisitorFunc(Table table, Board board, int tindex);

    internal class TableGenerator : TableWorker
    {
        internal const int UndefinedScore   = Table.MinScore;
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
        public bool EnableTableGeneration = true;
        public long CurrentConfigId;
        private MemoryTable table;
        private EdgeWriter whiteEdgeWriter;     // table of edges where White has the turn after the move is made
        private EdgeWriter blackEdgeWriter;     // table of edges where Black has the turn after the move is made
        private byte[] whiteUnresolvedChildCount;
        private byte[] blackUnresolvedChildCount;
        private List<int> whiteCurrResolvedIndexList = new();
        private List<int> blackCurrResolvedIndexList = new();
        private List<int> whiteNextResolvedIndexList = new();
        private List<int> blackNextResolvedIndexList = new();

        public TableGenerator(int max_table_size)
        {
            if (max_table_size > 0)
            {
                // Pre-allocate the in-memory table image to the largest size we will need.
                // Start out with size=0, capacity=max_table_size.
                // We will adjust the effective table size as needed for each configuration.
                table = new MemoryTable(0, max_table_size);

                // When backpropagating scores, we need to know how many
                // child nodes emanate from a given parent node.
                // When the child count reaches zero, we know the parent's score is final.
                // Before then, the parent's score is just a lower bound.
                whiteUnresolvedChildCount = new byte[max_table_size];
                blackUnresolvedChildCount = new byte[max_table_size];
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
            Log("CurrentConfigId = {0}", CurrentConfigId.ToString("D10"));
            long reverseConfigId = GetConfigId(config, true);
            Debug.Assert(ReverseSideConfigId(CurrentConfigId) == reverseConfigId);

            if (EnableTableGeneration && File.Exists(filename))
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
                }
                else
                {
                    Log("GenerateTable: Allocating new table with size {0}", size);
                    table = new MemoryTable(size, size);
                }

                WhiteCount = BlackCount = 0;
                int total = ForEachPosition(table, config, InitialPass);
                double ratio = total / (2.0 * size);        // There are 2 scores per position (White and Black).
                Log("InitialPass: {0:n0} White positions, {1:n0} Black positions, {2:n0} total; ratio = {3}.",
                    WhiteCount, BlackCount, total, ratio.ToString("F6"));
                Debug.Assert(total == WhiteCount + BlackCount);
                Debug.Assert(total < 2*size);

                if (EnableTableGeneration)
                {
                    // We start out with every score in the table set to UndefinedScore,
                    // which is smaller than any valid score.
                    // As valid scores are backpropagated, scores will grow upward from there.
                    // At the very end, we need to search for all UndefinedScore values and
                    // set them back to 0 (draw).
                    table.SetAllScores(UndefinedScore);

                    // Reset all unresolved child counts to zero.
                    // They get incremented by WriteAllEdges as legal moves are generated.
                    // They get decremented as the backpropagator feeds scores backwards through the graph.
                    Array.Clear(whiteUnresolvedChildCount, 0, whiteUnresolvedChildCount.Length);
                    Array.Clear(blackUnresolvedChildCount, 0, blackUnresolvedChildCount.Length);

                    // Empty out the lists of table indexes that are known to be freshly resolved.
                    // These lists are primed in WriteAllEdges, refluxed in Backpropagate.
                    whiteCurrResolvedIndexList.Clear();
                    blackCurrResolvedIndexList.Clear();
                    whiteNextResolvedIndexList.Clear();
                    blackNextResolvedIndexList.Clear();

                    string workdir = ConfigWorkDirectory(CurrentConfigId);
                    if (Directory.Exists(workdir))
                        throw new Exception($"Work directory already existed: {workdir}");

                    Directory.CreateDirectory(workdir);

                    string whiteEdgeFileName = Path.Combine(workdir, "w.edge");
                    string blackEdgeFileName = Path.Combine(workdir, "b.edge");
                    using (blackEdgeWriter = new EdgeWriter(blackEdgeFileName))
                        using (whiteEdgeWriter = new EdgeWriter(whiteEdgeFileName))
                            ForEachPosition(table, config, WriteAllEdges);

                    string whiteIndexFileName = Path.Combine(workdir, "w.index");
                    string blackIndexFileName = Path.Combine(workdir, "b.index");
                    SortEdges(whiteEdgeFileName, whiteIndexFileName, size);
                    SortEdges(blackEdgeFileName, blackIndexFileName, size);
                    Backpropagate(table, whiteEdgeFileName, whiteIndexFileName, blackEdgeFileName, blackIndexFileName);
                    Directory.Delete(workdir, true);

                    // Any lingering positions with undefined scores should be interpreted as draws.
                    table.ReplaceScores(UndefinedScore, DrawScore);

                    // Save the table to disk.
                    table.Save(filename);
                    Log("Saved: {0}", filename);
                }
            }

            if (EnableTableGeneration)
            {
                // Migrate the table from high-speed/high-memory to slower/low-memory.
                var diskTable = new DiskTable(size, filename);
                diskTable.OpenForRead();
                finished.Add(CurrentConfigId, diskTable);
                return diskTable;
            }

            return null;
        }

        private void Backpropagate(
            Table table,
            string whiteEdgeFileName,
            string whiteIndexFileName,
            string blackEdgeFileName,
            string blackIndexFileName)
        {
            using var whiteIndexer = new EdgeIndexer(whiteIndexFileName, whiteEdgeFileName);
            using var blackIndexer = new EdgeIndexer(blackIndexFileName, blackEdgeFileName);
            var before_tindex_list = new List<int>();

            for (int ply = 0; whiteCurrResolvedIndexList.Count + blackCurrResolvedIndexList.Count > 0; ++ply)
            {
                int targetScore = EnemyMatedScore - (ply+1);
                Log("Backprop[{0}]: white list = {1}, black list = {2}.", ply, whiteCurrResolvedIndexList.Count, blackCurrResolvedIndexList.Count);

                blackNextResolvedIndexList.Clear();
                foreach (int after_tindex in whiteCurrResolvedIndexList)
                {
                    int score = table.GetWhiteScore(after_tindex);
                    int adjusted_score = AdjustScoreForPly(score);
                    whiteIndexer.GetBeforeTableIndexes(before_tindex_list, after_tindex);
                    foreach (int before_tindex in before_tindex_list)
                    {
                        if (blackUnresolvedChildCount[before_tindex] > 0)
                        {
                            BumpBlackScore(table, before_tindex, adjusted_score);
                            int remaining = --blackUnresolvedChildCount[before_tindex];
                            if (remaining == 0 || adjusted_score == targetScore)
                            {
                                blackUnresolvedChildCount[before_tindex] = 0;
                                blackNextResolvedIndexList.Add(before_tindex);
                            }
                        }
                    }
                }

                whiteNextResolvedIndexList.Clear();
                foreach (int after_tindex in blackCurrResolvedIndexList)
                {
                    int score = table.GetBlackScore(after_tindex);
                    int adjusted_score = AdjustScoreForPly(score);
                    blackIndexer.GetBeforeTableIndexes(before_tindex_list, after_tindex);
                    foreach (int before_tindex in before_tindex_list)
                    {
                        if (whiteUnresolvedChildCount[before_tindex] > 0)
                        {
                            BumpWhiteScore(table, before_tindex, adjusted_score);
                            int remaining = --whiteUnresolvedChildCount[before_tindex];
                            if (remaining == 0 || adjusted_score == targetScore)
                            {
                                whiteUnresolvedChildCount[before_tindex] = 0;
                                whiteNextResolvedIndexList.Add(before_tindex);
                            }
                        }
                    }
                }

                var swap = whiteCurrResolvedIndexList;
                whiteCurrResolvedIndexList = whiteNextResolvedIndexList;
                whiteNextResolvedIndexList = swap;

                swap = blackCurrResolvedIndexList;
                blackCurrResolvedIndexList = blackNextResolvedIndexList;
                blackNextResolvedIndexList = swap;
            }

            Log("Backprop finished.");
        }

        private static string ConfigWorkDirectory(long config_id)
        {
            return Path.Combine(OutputDirectory(), "work_" + config_id.ToString("D10"));
        }

        private void SortEdges(string edgeFileName, string outIndexFileName, int table_size)
        {
            // Sort the edges in ascending order of after_table_index.

            const int SortRadix = 16;
            string sort_dir = ConfigWorkDirectory(CurrentConfigId);

            using (var sorter = new EdgeFileSorter(sort_dir, SortRadix, table_size))
                sorter.Sort(edgeFileName, outIndexFileName);

            Log("Sorted: {0}", edgeFileName);
        }

        public override void Finish()
        {
            chrono.Stop();
            Log("Finished after {0} = {1} seconds.", chrono.Elapsed, chrono.Elapsed.TotalSeconds);
        }

        public int ForEachPosition(Table table, int[,] config, PositionVisitorFunc func)
        {
            int sum = 0;

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

                            if (isEnPassantPossible && RankNumber(ofs) == 4 && ep == 0)
                            {
                                // A White pawn on the fourth rank could have just moved two squares to get there,
                                // but only if the two squares behind it are empty!
                                if (square[ofs + Direction.S] == Square.Empty && square[ofs + 2*Direction.S] == Square.Empty)
                                {
                                    board.SetEpTarget(ofs + Direction.S);

                                    sum += PositionSearch(
                                        func, table, config, board,
                                        (pawnFactor * tableIndex) + (i + 32),
                                        pieceIndex,
                                        whiteRemaining - 1,
                                        blackRemaining,
                                        false,     // we don't need diag filter when there are pawns
                                        (whiteRemaining > 1) ? (i + 1) : 0);

                                    board.SetEpTarget(0);
                                }
                            }

                            square[ofs] = Square.Empty;
                        }
                    }
                }
                else // blackRemaining > 0
                {
                    // Try putting the next Black pawn everywhere it can go.
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

                            if (isEnPassantPossible && RankNumber(ofs) == 5 && ep == 0)
                            {
                                // A White pawn on the fifth rank could have just moved two squares to get there,
                                // but only if the two squares behind it are empty!
                                if (square[ofs + Direction.N] == Square.Empty && square[ofs + 2*Direction.N] == Square.Empty)
                                {
                                    board.SetEpTarget(ofs + Direction.N);

                                    sum += PositionSearch(
                                        func, table, config, board,
                                        (pawnFactor * tableIndex) + (i + 24),   // note we use a different adjustment than for White Pawns, because Black Pawns on on a different rank
                                        pieceIndex,
                                        whiteRemaining,
                                        blackRemaining - 1,
                                        false,     // we don't need diag filter when there are pawns
                                        (blackRemaining > 1) ? (i + 1) : 0);

                                    board.SetEpTarget(0);
                                }
                            }

                            square[ofs] = Square.Empty;
                        }
                    }
                }
            }
            else
            {
                // We have placed all the pieces on the board!
                board.RefreshAfterDangerousChanges();

                // Visit the resulting position from both points of view: White's and Black's.

                // What if it is White's turn to move?
                board.SetTurn(true);
                if (board.IsValidPosition())
                    sum += func(table, board, tableIndex);

                // What if it is Black's turn to move?
                board.SetTurn(false);
                if (board.IsValidPosition())
                    sum += func(table, board, tableIndex);
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

        private long CallCount;
        private long WhiteCount;
        private long BlackCount;

        private int InitialPass(Table table, Board board, int tindex)
        {
            ++CallCount;

            // Verify we are calculating all the table indexes correctly.
            int check_tindex = board.GetEndgameTableIndex(false);
            if (check_tindex != tindex)
                throw new Exception(string.Format("#{0} Table index disagreement (check={1}, tindex={2}): {3}", CallCount, check_tindex, tindex, board.ForsythEdwardsNotation()));

            // Verify we are calculating config identifiers consistently.
            long check_id = board.GetEndgameConfigId(false);
            if (check_id != CurrentConfigId)
                throw new Exception(string.Format("#{0} Config identifier disagreement (check={1}, config={2}): {3}", CallCount, check_id, CurrentConfigId, board.ForsythEdwardsNotation()));

            if (board.IsWhiteTurn)
            {
                ++WhiteCount;
                if (table.GetWhiteScore(tindex) != 0)
                    throw new Exception(string.Format("Duplicate White position {0}: {1}", CallCount, board.ForsythEdwardsNotation()));
                table.SetWhiteScore(tindex, -57);
                if (table.GetWhiteScore(tindex) != -57)
                    throw new Exception("Could not read back White score at tindex=" + tindex);
            }
            else
            {
                ++BlackCount;
                if (table.GetBlackScore(tindex) != 0)
                    throw new Exception(string.Format("Duplicate Black position {0}: {1}", CallCount, board.ForsythEdwardsNotation()));
                table.SetBlackScore(tindex, -987);
                if (table.GetBlackScore(tindex) != -987)
                    throw new Exception("Could not read back Black score at tindex=" + tindex);
            }

            return 1;
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

        private int BumpWhiteScore(Table table, int tindex, int score)
        {
            int bestscore = table.GetWhiteScore(tindex);
            if (score > bestscore)
            {
                table.SetWhiteScore(tindex, score);
                return 1;   // updated 1 score
            }
            return 0;
        }

        private int BumpBlackScore(Table table, int tindex, int score)
        {
            int bestscore = table.GetBlackScore(tindex);
            if (score > bestscore)
            {
                table.SetBlackScore(tindex, score);
                return 1;   // updated 1 score
            }
            return 0;
        }

        private int BumpScore(Table table, bool isWhiteTurn, int tindex, int score)
        {
            // Incremental negamax assistance:
            // If the provided score is better than the best-so-far score stored in the table,
            // update the score in the table with the provided score.
            int bestscore;
            if (isWhiteTurn)
            {
                bestscore = table.GetWhiteScore(tindex);
                if (score > bestscore)
                {
                    table.SetWhiteScore(tindex, score);
                    return 1;   // updated 1 score
                }
            }
            else
            {
                bestscore = table.GetBlackScore(tindex);
                if (score > bestscore)
                {
                    table.SetBlackScore(tindex, score);
                    return 1;   // updated 1 score
                }
            }
            return 0;   // nothing changed
        }

        private void SetUnresolvedChildCount(bool white_turn, int tindex, int childCount)
        {
            if (childCount < 0 || childCount > 255)
                throw new ArgumentException($"Invalid child count = {childCount}");

            if (white_turn)
                whiteUnresolvedChildCount[tindex] = (byte)childCount;
            else
                blackUnresolvedChildCount[tindex] = (byte)childCount;
        }

        private int WriteAllEdges(Table table, Board board, int tindex)
        {
            // This function is called twice for each position:
            // once with White to move, the other with Black to move.
            // Pre-generate the list of transitions to other (config_id, table_index)
            // pairs based on the legal moves available to the side with the turn.
            // These graph edges will be recycled for all the subsequent passes.
            board.GenMoves(LegalMoveList);

            if (LegalMoveList.nmoves == 0)
            {
                // This is either stalemate or checkmate.

                // Put this into the list of after-indexes to be worked by the backpropagator.
                if (board.IsWhiteTurn)
                    whiteCurrResolvedIndexList.Add(tindex);
                else
                    blackCurrResolvedIndexList.Add(tindex);

                // Set the score for this position accordingly.
                int score = board.UncachedPlayerInCheck() ? FriendMatedScore : DrawScore;
                return SetScore(table, board.IsWhiteTurn, tindex, score);
            }

            var edge = new GraphEdge();
            edge.before_tindex = tindex;

            int best_foreign_score = UndefinedScore;
            int foreignEdgeCount = 0;

            // Write to blackEdgeWriter if Black has the turn AFTER making the move,
            // otherwise write to whiteEdgeWriter.
            EdgeWriter edgeWriter = board.IsWhiteTurn ? blackEdgeWriter : whiteEdgeWriter;

            for (int i = 0; i < LegalMoveList.nmoves; ++i)
            {
                Move move = LegalMoveList.array[i];
                board.PushMove(move);
                long after_config_id = board.GetEndgameConfigId(false);
                if (after_config_id == CurrentConfigId)
                {
                    // The most common case: making a move stays inside the current endgame table.
                    edge.after_tindex = board.GetEndgameTableIndex(false);
                    edgeWriter.WriteEdge(edge);
                }
                else
                {
                    // This move transitions out of this endgame configuration and into another.
                    // We do not save such edges, because the score of the after-position
                    // is already decided. We just remember the best of these "foreign" scores
                    // and bump the table position as needed below.
                    ++foreignEdgeCount;
                    int after_score;
                    int after_tindex;
                    Table after_table;
                    if (finished.TryGetValue(after_config_id, out after_table))
                    {
                        after_tindex = board.GetEndgameTableIndex(false);
                        after_score = GetScore(after_table, board.IsWhiteTurn, after_tindex);
                    }
                    else
                    {
                        // We don't know about that endgame table.
                        // Try swapping White and Black pieces.
                        after_config_id = board.GetEndgameConfigId(true);
                        if (finished.TryGetValue(after_config_id, out after_table))
                        {
                            after_tindex = board.GetEndgameTableIndex(true);
                            after_score = GetScore(after_table, board.IsWhiteTurn, after_tindex);
                        }
                        else
                        {
                            // The move transitions to a configuration we did not calculate.
                            // We only do that when the configuration is always a draw due to insufficient material.
                            after_score = 0;
                        }
                    }

                    // Find the "before score" of this move into a foreign table,
                    // adjusting for negamax and ply delay.
                    int foreign_score = AdjustScoreForPly(after_score);
                    if (foreign_score > best_foreign_score)
                        best_foreign_score = foreign_score;
                }
                board.PopMove();
            }

            int unresolved = LegalMoveList.nmoves - foreignEdgeCount;
            if (unresolved == 0)
            {
                // Put this into the list of after-indexes to be worked by the backpropagator.
                if (board.IsWhiteTurn)
                    whiteCurrResolvedIndexList.Add(tindex);
                else
                    blackCurrResolvedIndexList.Add(tindex);
            }
            else
            {
                SetUnresolvedChildCount(board.IsWhiteTurn, tindex, unresolved);
            }

            if (foreignEdgeCount > 0)
                return BumpScore(table, board.IsWhiteTurn, tindex, best_foreign_score);

            return 0;
        }

        private static int AdjustScoreForPly(int score)
        {
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
