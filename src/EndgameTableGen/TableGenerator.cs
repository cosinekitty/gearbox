using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gearbox;

namespace EndgameTableGen
{
    internal delegate int PositionVisitorFunc(Table table, Board board, int tindex);

    internal enum SweepStrategy
    {
        Simple,
        Graph,
    }

    internal class TableGenerator : TableWorker
    {
        private const int EnemyMatedScore  = +2000;
        private const int FriendMatedScore = -2000;

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
        private int PlyLevel;   // how many plies in the future are we finding mates for?
        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<long, Table> finished = new Dictionary<long, Table>();
        public bool EnableSelfCheck = true;
        public bool EnableTableGeneration = true;
        public SweepStrategy Strategy = SweepStrategy.Graph;
        private GraphPool gpool;
        public long WhiteConfigId;
        public long BlackConfigId;

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

        public override void GenerateTable(int[,] config)
        {
            Table table;
            string filename = ConfigFileName(config);
            int size = (int)TableSize(config);
            WhiteConfigId = GetConfigId(config, false);
            BlackConfigId = GetConfigId(config, true);
            Log("WhiteConfigId = {0}, BlackConfigId = {1}", WhiteConfigId.ToString("D10"), BlackConfigId.ToString("D10"));
            Debug.Assert(ReverseSideConfigId(WhiteConfigId) == BlackConfigId);

            if (EnableTableGeneration && File.Exists(filename))
            {
                // We have already calculated this endgame table. Load it from disk.
                table = Table.Load(filename, size);
                Log("Loaded: {0}", filename);
            }
            else
            {
                Log("Generating size {0:n0} table: {1}", size, filename);

                // Generate the table.
                table = new Table(size);
                PositionVisitorFunc sweepFunc;

                switch (Strategy)
                {
                    case SweepStrategy.Simple:
                        sweepFunc = FindForcedMates_Simple;
                        break;

                    case SweepStrategy.Graph:
                        gpool = new GraphPool(size);
                        sweepFunc = FindForcedMates_GraphMode;
                        break;

                    default:
                        throw new Exception(string.Format("Unsupported strategy: {0}", Strategy));
                }

                if (EnableSelfCheck)
                {
                    WhiteCount = BlackCount = 0;
                    int total = ForEachPosition(table, config, SelfTest);
                    double ratio = total / (2.0 * size);        // There are 2 scores per position (White and Black).
                    Log("SelfTest: {0:n0} White positions, {1:n0} Black positions, {2:n0} total; ratio = {3}.",
                        WhiteCount, BlackCount, total, ratio.ToString("F6"));
                    Debug.Assert(total == WhiteCount + BlackCount);
                    Debug.Assert(total < 2*size);
                    table.Clear();
                }

                if (EnableTableGeneration)
                {
                    long prev_sum = 1;
                    long sum = 1;
                    long total = 0;
                    // For the KP vs K table, we don't find immediate checkmates.
                    // So we can't stop as soon as sum == 0.
                    // In general, we allow 2 levels to go by without any progress before stopping,
                    // because we want both Black and White to get another turn.
                    for (PlyLevel = 0; sum + prev_sum > 0; ++PlyLevel)
                    {
                        prev_sum = sum;
                        total += sum = ForEachPosition(table, config, sweepFunc);

                        if (PlyLevel == 0 && gpool != null)
                            Log("Pool size = {0:n0} ; average moves/score = {1}", gpool.pool.Count, ((double)gpool.pool.Count / size).ToString("F2"));

                        // There are up to 2 scores per position (one for White, one for Black).
                        double ratio = (double)total / (2.0 * size);
                        Log("PlyLevel {0}: Added {1} scores for a total of {2}/{3} = {4}.", PlyLevel, sum, total, 2*size, ratio.ToString("F4"));

                        // It should never be possible to even reach table saturation,
                        // because the table index scheme accomodates putting more than one piece in the same square,
                        // which never happens in an actual position.
                        Debug.Assert(total < 2*size);
                    }

                    // Save the table to disk.
                    table.Save(filename);
                    Log("Saved: {0}", filename);
                }
            }

            // Store the finished table in memory.
            finished.Add(WhiteConfigId, table);
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
                    if (need_diag_filter && Position.DiagonalHeight(bkindex) > 0)
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
                            need_diag_filter,
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
                    for (int wi = startIndex; wi < WholeBoardOffsetTable.Length; ++wi)
                    {
                        int diag = Position.DiagonalHeight(wi);
                        if (need_diag_filter && (diag > 0))
                            continue;   // this is a redundant position, compared to a diagonal mirror image

                        int wofs = WholeBoardOffsetTable[wi];
                        if (square[wofs] == Square.Empty)
                        {
                            square[wofs] = MakeSquare(WHITE, pieceIndex);

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
                    for (int bi = startIndex; bi < WholeBoardOffsetTable.Length; ++bi)
                    {
                        int diag = Position.DiagonalHeight(bi);
                        if (need_diag_filter && (diag > 0))
                            continue;   // this is a redundant position, compared to a diagonal mirror image

                        int bofs = WholeBoardOffsetTable[bi];
                        if (square[bofs] == Square.Empty)
                        {
                            square[bofs] = MakeSquare(BLACK, pieceIndex);

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

        private int SelfTest(Table table, Board board, int tindex)
        {
            ++CallCount;

            // Verify we are calculating all the table indexes correctly.
            int check_tindex = board.GetEndgameTableIndex(false);
            if (check_tindex != tindex)
                throw new Exception(string.Format("#{0} Table index disagreement (check={1}, tindex={2}): {3}", CallCount, check_tindex, tindex, board.ForsythEdwardsNotation()));

            // Verify we are calculating config identifiers consistently.
            long check_id = board.GetEndgameConfigId(false);
            if (check_id != WhiteConfigId)
                throw new Exception(string.Format("#{0} Config identifier disagreement (check={1}, config={2}): {3}", CallCount, check_id, WhiteConfigId, board.ForsythEdwardsNotation()));

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

        private int FindForcedMates_Simple(Table table, Board board, int tindex)
        {
            // If we have already scored a position, don't try to work it again.
            if (0 != GetScore(table, board.IsWhiteTurn, tindex))
                return 0;

            if (PlyLevel == 0)
            {
                // Look for immediate checkmates only.
                if (board.IsCheckmate())
                {
                    SetScore(table, board.IsWhiteTurn, tindex, FriendMatedScore);
                    return 1;
                }
            }
            else
            {
                // Negamax search for moves that lead to forced mates in exactly PlyLevel plies.
                int bestscore = Score.NegInf;
                board.GenMoves(LegalMoveList);
                if (LegalMoveList.nmoves == 0)
                    return 0;   // ignore all checkmates and stalemates as this level of the search

                for (int i = 0; i < LegalMoveList.nmoves; ++i)
                {
                    Move move = LegalMoveList.array[i];
                    board.PushMove(move);

                    int next_tindex = board.GetEndgameTableIndex(false);
                    long w_next_id = board.GetEndgameConfigId(false);
                    long b_next_id = ReverseSideConfigId(w_next_id);
                    Debug.Assert(b_next_id == board.GetEndgameConfigId(true));

                    int score;
                    if (w_next_id == WhiteConfigId)
                    {
                        // We are still in the same endgame table (move is not a capture/promotion).
                        Debug.Assert(!move.IsCaptureOrPromotion());
                        score = GetScore(table, board.IsWhiteTurn, next_tindex);
                    }
                    else
                    {
                        // Capture or promotion has moved us to a different table.
                        Debug.Assert(move.IsCaptureOrPromotion());

                        // I don't think it's possible for us to toggle to the mirror image of the current configuration.
                        Debug.Assert(w_next_id != BlackConfigId);

                        Table next_table;
                        if (finished.TryGetValue(w_next_id, out next_table))
                        {
                            score = GetScore(next_table, board.IsWhiteTurn, next_tindex);
                        }
                        else if (finished.TryGetValue(b_next_id, out next_table))
                        {
                            // We flipped into a mirror image of a previously computed configuration.
                            int reverse_tindex = board.GetEndgameTableIndex(true);
                            score = GetScore(next_table, board.IsBlackTurn, reverse_tindex);
                        }
                        else if (board.IsDrawByInsufficientMaterial())
                        {
                            // We have wandered into a draw by insufficient material.
                            // We don't need endgame tables for those!
                            score = 0;
                        }
                        else
                        {
                            throw new Exception(string.Format("Don't know how to handle endgame position: {0}", board.ForsythEdwardsNotation()));
                        }
                    }

                    // Adjust for negamax and ply delay.
                    if (score > 0)
                        score = -(score - 1);
                    else if (score < 0)
                        score = -(score + 1);

                    if (score > bestscore)
                        bestscore = score;

                    board.PopMove();
                }

                Debug.Assert(bestscore != Score.NegInf);
                if (bestscore == FriendMatedScore + PlyLevel || bestscore == EnemyMatedScore - PlyLevel)
                {
                    SetScore(table, board.IsWhiteTurn, tindex, bestscore);

                    if (WhiteConfigId == BlackConfigId)
                    {
                        // This configuration has symmetrical White/Black material.
                        // Therefore we can score two positions at the same time!
                        int reverse_tindex = board.GetEndgameTableIndex(true);
                        SetScore(table, board.IsBlackTurn, reverse_tindex, bestscore);
                        return 2;
                    }

                    return 1;
                }
            }
            return 0;
        }

        private List<long> ConfigIdFromPackedId = new List<long>();
        private List<long> ReverseConfigIdFromPackedId = new List<long>();
        private Dictionary<long, int> PackedIdFromConfigId = new Dictionary<long, int>();

        int PackConfigId(long config_id)
        {
            long rev_id = ReverseSideConfigId(config_id);

            int packed_id;
            if (PackedIdFromConfigId.TryGetValue(config_id, out packed_id))
                return packed_id;

            packed_id = ConfigIdFromPackedId.Count;
            ConfigIdFromPackedId.Add(config_id);
            ReverseConfigIdFromPackedId.Add(rev_id);
            PackedIdFromConfigId.Add(config_id, packed_id);
            return packed_id;
        }

        long UnpackConfigId(int packed_id)
        {
            return ConfigIdFromPackedId[packed_id];
        }

        long UnpackReverseConfigId(int packed_id)
        {
            return ReverseConfigIdFromPackedId[packed_id];
        }

        private int FindForcedMates_GraphMode(Table table, Board board, int tindex)
        {
            // If we have already scored a position, don't try to work it again.
            if (0 != GetScore(table, board.IsWhiteTurn, tindex))
                return 0;

            bool wturn = board.IsWhiteTurn;
            bool bturn = !wturn;

            if (PlyLevel == 0)
            {
                // This is the first pass through the table.
                // This function is called twice for each position:
                // once with White to move, the other with Black to move.
                // Pre-generate the list of transitions to other (config_id, table_index)
                // pairs based on the legal moves available to the side with the turn.
                // These graph edges will be recycled for all the subsequent passes.
                board.GenMoves(LegalMoveList);
                if (LegalMoveList.nmoves == 0)
                {
                    // This is either stalemate or checkmate.
                    // If checkmate, store the score.
                    if (board.UncachedPlayerInCheck())
                        return SetScore(table, board.IsWhiteTurn, tindex, FriendMatedScore);

                    return 0;   // stalemate
                }

                gpool.StartNewList(tindex, wturn, LegalMoveList.nmoves);
                for (int i = 0; i < LegalMoveList.nmoves; ++i)
                {
                    Move move = LegalMoveList.array[i];
                    board.PushMove(move);
                    gpool.pool.Add(new GraphEdge
                    {
                        packed_config_id = PackConfigId(board.GetEndgameConfigId(false)),
                        next_tindex = board.GetEndgameTableIndex(false),
                        reverse_tindex = board.GetEndgameTableIndex(true),
                    });
                    board.PopMove();
                }
            }
            else
            {
                // Negamax search for moves that lead to forced mates in exactly PlyLevel plies.
                int bestscore = Score.NegInf;

                GraphEdgeList edgeList = gpool.GetList(tindex, wturn);
                if (edgeList.count == 0)
                    return 0;   // ignore all checkmates and stalemates as this level of the search

                for (int e = 0; e < edgeList.count; ++e)
                {
                    GraphEdge edge = gpool.pool[edgeList.front + e];
                    int next_tindex = edge.next_tindex;
                    long w_next_id = UnpackConfigId(edge.packed_config_id);
                    long b_next_id = UnpackReverseConfigId(edge.packed_config_id);

                    int score;
                    if (w_next_id == WhiteConfigId)
                    {
                        // We are still in the same endgame table (move is not a capture/promotion).
                        score = GetScore(table, bturn, next_tindex);
                    }
                    else
                    {
                        // Capture or promotion has moved us to a different table.

                        Table next_table;
                        if (finished.TryGetValue(w_next_id, out next_table))
                        {
                            score = GetScore(next_table, bturn, next_tindex);
                        }
                        else if (finished.TryGetValue(b_next_id, out next_table))
                        {
                            // We flipped into a mirror image of a previously computed configuration.
                            score = GetScore(next_table, wturn, edge.reverse_tindex);
                        }
                        else
                        {
                            // We have wandered into a draw by insufficient material.
                            // We don't need endgame tables for those!
                            score = 0;
                        }
                    }

                    // Adjust for negamax and ply delay.
                    if (score > 0)
                        score = -(score - 1);
                    else if (score < 0)
                        score = -(score + 1);

                    if (score > bestscore)
                        bestscore = score;
                }

                Debug.Assert(bestscore != Score.NegInf);
                if (bestscore == FriendMatedScore + PlyLevel || bestscore == EnemyMatedScore - PlyLevel)
                    return SetScore(table, wturn, tindex, bestscore);
            }
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

        internal static void DecodePosition(Board board, long config_id, int table_index, bool white_turn)
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
