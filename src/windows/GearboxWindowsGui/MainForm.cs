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
    public partial class MainForm : Form, ISearchInfoSink
    {
        internal static readonly string InstallFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private BoardDisplay boardDisplay = new();
        private GameTags gameTags = new GameTags();
        private string currentPgnFileName;
        private bool keepRunningThinker = true;
        private Thread thinkerThread;
        private AutoResetEvent signal = new AutoResetEvent(false);
        private const int HashTableSize = 50000000;
        private Thinker thinker = new Thinker(HashTableSize);
        private bool isComputerThinking;
        private System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private const int millisPerAnimationFrame = 15;
        private const double animationSquaresPerSecond = 15.0;   // how fast to animate the computer's pieces sliding across the board
        private int animationFrameCounter;
        private int animationTotalFrames;
        private Move animationMoveInProgress;
        private BestPath currentBestPath;

        private int TopMarginPixels()
        {
            return mainMenuStrip.Height + 25;
        }

        private void EnableDoubleBuffering(Control control)
        {
            // A hack I found to stop flickering on rendering: enable double-buffering on the panel.
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                control,
                new object[] { true }
            );
        }

        public MainForm()
        {
            InitializeComponent();
            EnableDoubleBuffering(panel_ChessBoard);
            EnableDoubleBuffering(panel_BestPath);
            animationTimer.Tick += OnAnimationTimerTick;
            animationTimer.Interval = millisPerAnimationFrame;
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

            panel_BestPath.Left = panel_ChessBoard.Right + 10;

            panel_FileLetters.Width = pixels;
            panel_FileLetters.Top = panel_ChessBoard.Bottom + 1;
            panel_FileLetters.Invalidate();

            panel_RankNumbers.Top = panel_ChessBoard.Top;
            panel_RankNumbers.Height = panel_ChessBoard.Height;
            panel_RankNumbers.Invalidate();
        }

        private void InitThinker()
        {
            string endgameTableDir =
                Environment.GetEnvironmentVariable("GEARBOX_TABLEBASE_DIR") ??
                Path.Combine(InstallFolder, "endgame");

            thinker.LoadEndgameTables(endgameTableDir);
            thinker.SetInfoSink(this);

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

                // It's best to force repainting the whole board, because
                // the piece will start out "snapped" to a random
                // distance from where it started, depending on the mouse coordinates.
                panel_ChessBoard.Invalidate();
            }
        }

        private void SetComputerIsThinking(bool thinking)
        {
            isComputerThinking = thinking;

            // If the computer is thinking, disable controls that don't make sense.
            // If the computer is not thinking, enable those controls.

            newToolStripMenuItem.Enabled = !thinking;
            openToolStripMenuItem.Enabled = !thinking;
            saveToolStripMenuItem.Enabled = !thinking;
            saveAsToolStripMenuItem.Enabled = !thinking;
        }

        private void OnTurnChanged()
        {
            if (!isComputerThinking)
            {
                // Never tell the computer to think when the game is over.
                GameResult result = boardDisplay.board.GetGameResult();
                if (result == GameResult.InProgress)
                {
                    bool computerShouldThink = boardDisplay.board.IsWhiteTurn ? toolStripMenuItemComputerWhite.Checked : toolStripMenuItemComputerBlack.Checked;
                    if (computerShouldThink)
                    {
                        // FIXFIXFIX - Cancel any partially selected human move.
                        SetComputerIsThinking(true);
                        signal.Set();
                    }
                }
            }
        }

        private void panel_ChessBoard_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isComputerThinking)
            {
                if (boardDisplay.DropPiece(e.X, e.Y))
                    OnTurnChanged();

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

        private void OnAnimationTimerTick(object sender, EventArgs evt)
        {
            if (animationFrameCounter < animationTotalFrames)
            {
                double fraction = (double)animationFrameCounter / (double)animationTotalFrames;
                Rectangle prev = boardDisplay.AnimationRectangle();
                boardDisplay.UpdateAnimation(animationMoveInProgress, fraction);
                Rectangle curr = boardDisplay.AnimationRectangle();
                panel_ChessBoard.Invalidate(prev);
                panel_ChessBoard.Invalidate(curr);
                ++animationFrameCounter;
            }
            else
            {
                animationTimer.Stop();
                boardDisplay.StopAnimatingMove();
                boardDisplay.MakeMove(animationMoveInProgress);
                panel_ChessBoard.Invalidate();
                SetComputerIsThinking(false);
                OnTurnChanged();
            }
        }

        private void OnSearchCompleted(Move move)
        {
            // Start animating the computer's move.
            // The move doesn't actually commit until the animation is complete.

            boardDisplay.StartAnimatingMove(move);

            // Calculate the number of frames needed for this animation.
            // We scale it so that the perceived speed of the sliding piece is always about the same.
            double dx = (move.dest % 10) - (move.source % 10);
            double dy = (move.dest / 10) - (move.source / 10);
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            double animationTime = distance / animationSquaresPerSecond;
            animationTotalFrames = (int)Math.Round((animationTime * 1000.0) / millisPerAnimationFrame);
            animationFrameCounter = 0;
            animationMoveInProgress = move;
            animationTimer.Start();
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
            boardDisplay.RefreshMoves();
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
                        panel_ChessBoard.Invalidate();
                        OnTurnChanged();
                    }
                }
            }
        }

        private void rotateBoardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            boardDisplay.RotateBoard();
            panel_ChessBoard.Invalidate();
            panel_RankNumbers.Invalidate();
            panel_FileLetters.Invalidate();
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
                thinker.SetSearchTime(3000);
                Move move = thinker.Search(board);
                if (!keepRunningThinker)
                    break;

                this.BeginInvoke(new Action<Move>(OnSearchCompleted), move);
            }
        }

        private void toolStripMenuItemComputerWhite_Click(object sender, EventArgs e)
        {
            // Toggle whether the computer should play White.
            toolStripMenuItemComputerWhite.Checked = !toolStripMenuItemComputerWhite.Checked;
            OnTurnChanged();
        }

        private void toolStripMenuItemComputerBlack_Click(object sender, EventArgs e)
        {
            // Toggle whether the computer should play Black.
            toolStripMenuItemComputerBlack.Checked = !toolStripMenuItemComputerBlack.Checked;
            OnTurnChanged();
        }

        public void OnBeginSearchMove(Board board, Move move, int limit)
        {
        }

        private void DisplayBestPath(BestPath path)
        {
            currentBestPath = path;
            panel_BestPath.Invalidate();
        }

        public void OnBestPath(Board board, BestPath path)
        {
            this.BeginInvoke(new Action<BestPath>(DisplayBestPath), path);
        }

        private void panel_BestPath_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            using var font = new Font(FontFamily.GenericMonospace, 10.0f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);
            const float PixelsPerRow = 20.0f;
            if (currentBestPath != null && currentBestPath.nodes != null)
            {
                string scoreText = Score.Format(currentBestPath.nodes[0].move.score);
                graphics.DrawString(scoreText, font, brush, 10.0f, 0.0f);
                for (int i = 0; i < currentBestPath.nodes.Length; ++i)
                {
                    BestPathNode node = currentBestPath.nodes[i];
                    graphics.DrawString(node.san, font, brush, 10.0f, (i + 1) * PixelsPerRow);
                }
            }
        }

        private void panel_FileLetters_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            // Draw the letters a..h (for White's view) or h..a (for Black's view).
            // Space them at the center of the 8 squares.
            string letters = boardDisplay.reverse ? "hgfedcba" : "abcdefgh";
            using var font = new Font(FontFamily.GenericMonospace, 10.0f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);
            for (int i = 0; i < 8; ++i)
            {
                float x = ((0.5f + i) * boardDisplay.pixelsPerSquare) - 6.0f;
                float y = 3.0f;
                graphics.DrawString(letters[i].ToString(), font, brush, x, y);
            }
        }

        private void panel_RankNumbers_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            // Draw the numerals 1..8 (for White's view) or 8..1 (for Black's view).
            // Space them at the center of the 8 squares.
            using var font = new Font(FontFamily.GenericMonospace, 10.0f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);
            for (int i = 0; i < 8; ++i)
            {
                float x = 8.0f;
                float y = ((0.5f + i) * boardDisplay.pixelsPerSquare) - 8.0f;
                string numeral = (boardDisplay.reverse ? (1 + i) : (8 - i)).ToString();
                graphics.DrawString(numeral, font, brush, x, y);
            }
        }
    }
}
