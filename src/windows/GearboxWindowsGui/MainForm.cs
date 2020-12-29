using Gearbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GearboxWindowsGui
{
    public partial class MainForm : Form
    {
        private BoardDisplay boardDisplay = new();
        private readonly int topMarginPixels;

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

            topMarginPixels = menuStrip1.Height + 25;
            ResizeChessBoard();
        }

        private void ResizeChessBoard()
        {
            // Figure out how large to make the chess board based on changes
            // to the size of the whole form window.
            // We never want to clip the chess board, and it must
            // always have a number of pixels that is divisible by 8
            // so all of the squares have an integer number of pixels.
            int pixels = (Math.Min(ClientRectangle.Width, ClientRectangle.Height) - topMarginPixels) & ~7;
            boardDisplay.SetPixelsPerSquare(pixels / 8);
            panel_ChessBoard.Width = pixels;
            panel_ChessBoard.Height = pixels;
            panel_ChessBoard.Invalidate();
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
            // Did the user just click on a square that contains a piece
            // the current side can move?
            // If so, start animating its movement along with the mouse.
            // FIXFIXFIX: check that it is the human's turn to move.
            boardDisplay.StartDraggingPiece(e.X, e.Y);
            panel_ChessBoard.Invalidate();
        }

        private void panel_ChessBoard_MouseUp(object sender, MouseEventArgs e)
        {
            boardDisplay.DropPiece(e.X, e.Y);
            panel_ChessBoard.Invalidate();
        }

        private void panel_ChessBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (boardDisplay.IsDraggingPiece())
            {
                // Keep animating the piece being moved.
                Rectangle prev = boardDisplay.AnimationRectangle();
                boardDisplay.UpdateDraggedPieceLocation(e.X, e.Y);
                Rectangle curr = boardDisplay.AnimationRectangle();

                // Limit the area of the invalidation to a rectangle that contains
                // pixels near where we have been animating the dragged piece.
                // This is necessary to keep the frame rate reasonable.
                Rectangle merge = MergeRectangles(prev, curr);
                panel_ChessBoard.Invalidate(merge);
            }
        }

        private Rectangle MergeRectangles(Rectangle a, Rectangle b)
        {
            int x1 = Math.Min(a.X, b.X);
            int y1 = Math.Min(a.Y, b.Y);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            var rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            return rect;
        }
    }
}
