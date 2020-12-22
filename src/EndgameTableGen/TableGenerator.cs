using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gearbox;

namespace EndgameTableGen
{
    internal delegate int PositionVisitorFunc(Table table, int[,] config, Board board, int tindex);

    internal class TableGenerator : TableWorker
    {
        private static readonly int[] OffsetTable = MakeOffsetTable('a', 'h', '1', '8');
        private static readonly int[] PawnOffsetTable = MakeOffsetTable('a', 'h', '2', '7');
        private static readonly int[] PawnSymmetryTable = MakeOffsetTable('a', 'd', '2', '7');
        private static readonly int[] KingSymmetryTable = new int[]
        {
            21, 22, 23, 24,
                32, 33, 34,
                    43, 44,
                        54
        };

        private readonly Stopwatch chrono = new Stopwatch();
        private readonly Dictionary<string, Table> finished = new Dictionary<string, Table>();

        private static int[] MakeOffsetTable(char file1, char file2, char rank1, char rank2)
        {
            var table = new List<int>();
            for (char file = file1; file <= file2; ++file)
                for (char rank = rank1; rank <= rank2; ++rank)
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
            if (File.Exists(filename))
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

                int sum = ForEachPosition(table, config, FindCheckmate);
                Log("Found {0} checkmates.", sum);

                // Save the table to disk.
                table.Save(filename);
                Log("Saved: {0}", filename);
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
            if (config[WHITE, P_INDEX] + config[BLACK, P_INDEX] > 0)
            {
                // If there is at least one pawn on the board, we will use left/right symmetry
                // for placing the "primary" pawn, deeper down in recursion.
                // Here, it means we put the White king at every possible offset.
                wkOffsetTable = OffsetTable;
            }
            else
            {
                // When there are no pawns, we use 8-fold symmetry for placing the White king.
                wkOffsetTable = KingSymmetryTable;
            }

            for (int wkindex = 0; wkindex < wkOffsetTable.Length; ++wkindex)
            {
                int wkofs = wkOffsetTable[wkindex];
                board.PlaceWhiteKing(wkofs);
                for (int bkindex = 0; bkindex < OffsetTable.Length; ++bkindex)
                {
                    int bkofs = OffsetTable[bkindex];
                    if (KingDistance(wkofs, bkofs) > 1)
                    {
                        board.PlaceBlackKing(bkofs);
                        sum += PositionSearch(func, table, config, board, 64*wkindex + bkindex, Q_INDEX, config[WHITE,Q_INDEX], config[BLACK,Q_INDEX]);
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
            int blackRemaining)
        {
            int sum = 0;
            Square[] square = board.GetSquaresArray();

            if (pieceIndex < P_INDEX)
            {
                // A White, non-pawn, non-king piece can be placed in any of the 64 squares, so long as that square is empty.
                if (whiteRemaining > 0)
                {
                    for (int wi = 0; wi < OffsetTable.Length; ++wi)
                    {
                        int wofs = OffsetTable[wi];
                        if (square[wofs] == Square.Empty)
                        {
                            square[wofs] = MakeSquare(WHITE, pieceIndex);
                            sum += PositionSearch(func, table, config, board, 64*tableIndex + wi, pieceIndex, whiteRemaining-1, blackRemaining);
                            square[wofs] = Square.Empty;
                        }
                    }
                }
                else if (blackRemaining > 0)
                {
                    // A Black, non-pawn, non-king piece can be placed in any of the 64 squares, so long as that square is empty.
                    for (int bi = 0; bi < OffsetTable.Length; ++bi)
                    {
                        int bofs = OffsetTable[bi];
                        if (square[bofs] == Square.Empty)
                        {
                            square[bofs] = MakeSquare(BLACK, pieceIndex);
                            sum += PositionSearch(func, table, config, board, 64*tableIndex + bi, pieceIndex, whiteRemaining, blackRemaining-1);
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
                        config[BLACK, pieceIndex + 1]);
                }
            }
            else // pieceIndex == P_INDEX
            {
                // If both White and Black have at least one pawn, en passant captures are possible.
                // In this case, the side that just moved could have moved a pawn two squares forward,
                // creating an en passant capture opportunity.
                // To keep table index calculations as simple as possible, always assume en passant captures are possible,
                // even when only one side has pawn(s).
                // The first pawn placed (a White pawn if White has any, otherwise the first Black pawn)
                // gets special symmetry placement: only the left side of the board (files a..d) are used.

                if (whiteRemaining + blackRemaining > 0)
                {
                    int wp = config[WHITE, P_INDEX];
                    int bp = config[BLACK, P_INDEX];
                    int ep_select;      // value to add to turn a pawn state into an en passant target
                    int multiplier;
                    int[] offsetTable;

                    if ((wp > 0 && wp == whiteRemaining) || (wp == 0 && bp > 0 && bp == blackRemaining))
                    {
                        // This is the primary pawn, so it gets put on the left side of the board only.
                        // There are 24 such squares, 4 of which offer en passant capture, for a total of 28 states.
                        multiplier = 28;
                        ep_select = 16;     // 8+16 = 24, the first en passant state code with symmetry
                        offsetTable = PawnSymmetryTable;
                    }
                    else
                    {
                        // This pawn can go in 48 different squares, 8 of which offer en passant capture.
                        multiplier = 56;
                        ep_select = 32;     // 16+32 = 48, the first en passant state code without symmetry
                        offsetTable = PawnOffsetTable;
                    }

                    if (whiteRemaining > 0)
                    {
                        // Try putting the next White pawn everywhere it can go.
                        for (int i=0; i < offsetTable.Length; ++i)
                        {
                            int ofs = offsetTable[i];
                            if (square[ofs] == Square.Empty)
                            {
                                square[ofs] = Square.WP;

                                sum += PositionSearch(
                                    func, table, config, board,
                                    (multiplier * tableIndex) + i,
                                    pieceIndex,
                                    whiteRemaining - 1,
                                    blackRemaining);

                                if (RankNumber(ofs) == 4)
                                {
                                    // A White pawn on the fourth rank could have just moved two squares to get there,
                                    // but only if the two squares behind it are empty!
                                    if (square[ofs + Direction.S] == Square.Empty && square[ofs + 2*Direction.S] == Square.Empty)
                                    {
                                        board.SetEpTarget(ofs + Direction.S);

                                        sum += PositionSearch(
                                            func, table, config, board,
                                            (multiplier * tableIndex) + (i + ep_select),
                                            pieceIndex,
                                            whiteRemaining - 1,
                                            blackRemaining);

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
                        for (int i=0; i < offsetTable.Length; ++i)
                        {
                            int ofs = offsetTable[i];
                            if (square[ofs] == Square.Empty)
                            {
                                square[ofs] = Square.BP;

                                sum += PositionSearch(
                                    func, table, config, board,
                                    (multiplier * tableIndex) + i,
                                    pieceIndex,
                                    whiteRemaining,
                                    blackRemaining - 1);

                                if (RankNumber(ofs) == 5)
                                {
                                    // A White pawn on the fifth rank could have just moved two squares to get there,
                                    // but only if the two squares behind it are empty!
                                    if (square[ofs + Direction.N] == Square.Empty && square[ofs + 2*Direction.N] == Square.Empty)
                                    {
                                        board.SetEpTarget(ofs + Direction.N);

                                        sum += PositionSearch(
                                            func, table, config, board,
                                            (multiplier * tableIndex) + (i + ep_select),
                                            pieceIndex,
                                            whiteRemaining,
                                            blackRemaining - 1);

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
                    // Visit the resulting position from both points of view: White's and Black's.

                    // What if it is White's turn to move?
                    board.SetTurn(true);
                    if (board.IsValidPosition())
                        sum += func(table, config, board, tableIndex);

                    // What if it is Black's turn to move?
                    board.SetTurn(false);
                    if (board.IsValidPosition())
                        sum += func(table, config, board, tableIndex);
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

        private int FindCheckmate(Table table, int[,] config, Board board, int tindex)
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
