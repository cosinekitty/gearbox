using Gearbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GearboxWindowsGui
{
    public partial class MainForm : Form
    {
        private BoardDisplay boardDisplay = new();
        private GameTags gameTags = new GameTags();
        private string currentPgnFileName;
        private bool keepRunningThinker = true;
        private Thread thinkerThread;
        private AutoResetEvent signal = new AutoResetEvent(false);
        private const int HashTableSize = 50000000;
        private Thinker thinker = new Thinker(HashTableSize);
        private bool isComputerThinking;

        private int TopMarginPixels()
        {
            return mainMenuStrip.Height + 25;
        }

        public MainForm()
        {
            InitializeComponent();

            // A hack I found to stop flickering on rendering: enable double-buffering on the chess board panel.
            typeof(Panel).InvokeMember(
                "DoubleBuffered", 
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, 
                panel_ChessBoard, 
                new object[] { true }
            );

            ResizeChessBoard();
            InitThinker();
        }

        private void ResizeChessBoard()
        {
            // Figure out how large to make the chess board based on changes
            // to the size of the whole form window.
            // We never want to clip the chess board, and it must
            // always have a number of pixels that is divisible by 8
            // so all of the squares have an integer number of pixels.
            int pixels = (Math.Min(ClientRectangle.Width, ClientRectangle.Height) - TopMarginPixels()) & ~7;
            boardDisplay.SetPixelsPerSquare(pixels / 8);
            panel_ChessBoard.Width = pixels;
            panel_ChessBoard.Height = pixels;
            panel_ChessBoard.Invalidate();
        }

        private void InitThinker()
        {
            string endgameTableDir = Environment.GetEnvironmentVariable("GEARBOX_TABLEBASE_DIR");   // FIXFIXFIX: should find tables with executable
            if (endgameTableDir != null)
                thinker.LoadEndgameTables(endgameTableDir);

            thinkerThread = new Thread(ThinkerThreadFunc)
            {
                IsBackground = true,
                Name = "Gearbox Thinker",
            };
            thinkerThread.Start();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            ResizeChessBoard();
        }

        private void panel_ChessBoard_Paint(object sender, PaintEventArgs e)
        {
            boardDisplay.Render(e.Graphics, e.ClipRectangle);
        }

        private void panel_ChessBoard_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isComputerThinking)
            {
                // Did the user just click on a square that contains a piece
                // the current side can move?
                // If so, start animating its movement along with the mouse.
                boardDisplay.StartDraggingPiece(e.X, e.Y);
                panel_ChessBoard.Invalidate();
            }
        }

        private void panel_ChessBoard_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isComputerThinking)
            {
                if (boardDisplay.DropPiece(e.X, e.Y))
                {
                    isComputerThinking = true;
                    signal.Set();
                }
                panel_ChessBoard.Invalidate();
            }
        }

        private void panel_ChessBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isComputerThinking)
            {
                if (boardDisplay.IsDraggingPiece())
                {
                    // Keep animating the piece being moved.
                    Rectangle prev = boardDisplay.AnimationRectangle();
                    boardDisplay.UpdateDraggedPieceLocation(e.X, e.Y);
                    Rectangle curr = boardDisplay.AnimationRectangle();

                    panel_ChessBoard.Invalidate(prev);
                    panel_ChessBoard.Invalidate(curr);
                }
            }
        }

        private void OnSearchCompleted(Move move)
        {
            boardDisplay.MakeMove(move);
            panel_ChessBoard.Invalidate();
            isComputerThinking = false;
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            keepRunningThinker = false;
            thinker.AbortSearch();
            signal.Set();
            thinkerThread.Join();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Save()
        {
            string pgn = boardDisplay.board.PortableGameNotation(gameTags);
            using (StreamWriter output = File.CreateText(currentPgnFileName))
                output.WriteLine(pgn);
        }

        private void SaveAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Chess Game|*.pgn";
                dialog.Title = "Save the chess game in Portable Game Notation (PGN) format";
                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.FileName))
                {
                    currentPgnFileName = dialog.FileName;
                    Save();
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPgnFileName))
                SaveAs();
            else
                Save();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPgnFileName = null;
            boardDisplay.board.SetPosition(Board.StandardSetup);
            panel_ChessBoard.Invalidate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Chess Game|*.pgn";
                dialog.Title = "Load a chess game from a Portable Game Notation (PGN) file";
                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.FileName))
                {
                    // Test the correctness of the PNG file.
                    // If it contains more than one game, just load the first game in it.
                    Game firstGame = null;
                    try
                    {
                        using (StreamReader infile = File.OpenText(dialog.FileName))
                        {
                            firstGame = Game.FromStream(infile).FirstOrDefault();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error loading PGN file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (firstGame != null)
                    {
                        boardDisplay.board.LoadGame(firstGame);
                        gameTags = firstGame.Tags;
                        boardDisplay.RefreshMoves();
                        currentPgnFileName = dialog.FileName;
                    }
                }
            }
        }

        private void rotateBoardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            boardDisplay.RotateBoard();
            panel_ChessBoard.Invalidate();
        }

        private void ThinkerThreadFunc()
        {
            var board = new Board();
            while (signal.WaitOne() && keepRunningThinker)
            {
                // Clone the game state from the display board.
                // Then use the local clone to do the analysis.
                GameHistory history = boardDisplay.board.GetGameHistory();
                board.LoadGameHistory(history);
                thinker.SetSearchTime(5000);
                Move move = thinker.Search(board);
                this.BeginInvoke(new Action<Move>(OnSearchCompleted), move);
            }
        }
    }
}
