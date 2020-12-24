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

        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<string, Table> finished = new Dictionary<string, Table>();
        public bool EnableSelfCheck = true;
        public bool EnableTableGeneration = true;

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
        }

        public override void GenerateTable(int[,] config)
        {
            Table table;

            string filename = ConfigFileName(config);
            int size = (int)TableSize(config);
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

                if (EnableSelfCheck)
                {
                    WhiteCount = BlackCount = 0;
                    ForEachPosition(table, config, SelfTest);
                    Log("SelfTest: {0:n0} White positions, {1:n0} Black positions.", WhiteCount, BlackCount);
                    table.Clear();
                }

                if (EnableTableGeneration)
                {
                    int sum = ForEachPosition(table, config, FindCheckmate);
                    Log("Found {0} checkmates.", sum);

                    // Save the table to disk.
                    table.Save(filename);
                    Log("Saved: {0}", filename);
                }
            }

            // Store the finished table in memory.
            string symbol = ConfigSymbol(config);
            finished.Add(symbol, table);
        }

        public override void Finish()
        {
            chrono.Stop();
            Log("Finished after {0} = {1} seconds.", chrono.Elapsed, chrono.Elapsed.TotalSeconds);
        }

        private int ForEachPosition(Table table, int[,] config, PositionVisitorFunc func)
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
                int diag = Board.DiagonalHeight(Board.IndexFromOffset(wkofs));
                bool need_diag_filter = (pawns == 0) && (diag == 0);

                for (int bkindex = 0; bkindex < WholeBoardOffsetTable.Length; ++bkindex)
                {
                    if (need_diag_filter && Board.DiagonalHeight(bkindex) > 0)
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
                        int diag = Board.DiagonalHeight(wi);
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
                        int diag = Board.DiagonalHeight(bi);
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
            else // pieceIndex == P_INDEX
            {
                // If both White and Black have at least one pawn, en passant captures are possible.
                // In this case, the side that just moved could have moved a pawn two squares forward,
                // creating an en passant capture opportunity.
                // To keep table index calculations as simple as possible, always assume en passant captures are possible,
                // even when only one side has pawn(s).

                if (whiteRemaining > 0)
                {
                    // Try putting the next White pawn everywhere it can go.
                    for (int i = startIndex; i < PawnOffsetTable.Length; ++i)
                    {
                        int ofs = PawnOffsetTable[i];
                        if (square[ofs] == Square.Empty)
                        {
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
                                (56 * tableIndex) + i,
                                pieceIndex,
                                whiteRemaining - 1,
                                blackRemaining,
                                false,     // we don't need diag filter when there are pawns
                                (whiteRemaining > 1) ? (i + 1) : 0);

                            if (RankNumber(ofs) == 4 && ep == 0)
                            {
                                // A White pawn on the fourth rank could have just moved two squares to get there,
                                // but only if the two squares behind it are empty!
                                if (square[ofs + Direction.S] == Square.Empty && square[ofs + 2*Direction.S] == Square.Empty)
                                {
                                    board.SetEpTarget(ofs + Direction.S);

                                    sum += PositionSearch(
                                        func, table, config, board,
                                        (56 * tableIndex) + (i + 32),
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
                else if (blackRemaining > 0)
                {
                    // Try putting the next Black pawn everywhere it can go.
                    for (int i=0; i < PawnOffsetTable.Length; ++i)
                    {
                        int ofs = PawnOffsetTable[i];
                        if (square[ofs] == Square.Empty)
                        {
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
                                (56 * tableIndex) + i,
                                pieceIndex,
                                whiteRemaining,
                                blackRemaining - 1,
                                false,     // we don't need diag filter when there are pawns
                                (blackRemaining > 1) ? (i + 1) : 0);

                            if (RankNumber(ofs) == 5 && board.GetEpTarget() == 0)
                            {
                                // A White pawn on the fifth rank could have just moved two squares to get there,
                                // but only if the two squares behind it are empty!
                                if (square[ofs + Direction.N] == Square.Empty && square[ofs + 2*Direction.N] == Square.Empty)
                                {
                                    board.SetEpTarget(ofs + Direction.N);

                                    sum += PositionSearch(
                                        func, table, config, board,
                                        (56 * tableIndex) + (i + 24),   // note we use a different adjustment than for White Pawns, because Black Pawns on on a different rank
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
            int check = board.GetEndgameTableIndex();
            if (check != tindex)
                throw new Exception(string.Format("#{0} Table index disagreement (check={1}, tindex={2}): {3}", CallCount, check, tindex, board.ForsythEdwardsNotation()));

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

        private int FindCheckmate(Table table, Board board, int tindex)
        {
            if (board.IsCheckmate())
            {
                if (board.IsWhiteTurn)
                    table.SetWhiteScore(tindex, -1000);
                else
                    table.SetBlackScore(tindex, -1000);
                return 1;
            }
            return 0;
        }
    }
}
