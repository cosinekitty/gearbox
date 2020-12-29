using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GearboxWindowsGui
{
    public partial class MainForm : Form
    {
        private BoardDisplay boardDisplay = new BoardDisplay();

        public MainForm()
        {
            InitializeComponent();
            ResizeChessBoard();
        }

        private void ResizeChessBoard()
        {
            // Figure out how large to make the chess board based on changes
            // to the size of the whole form window.
            // We never want to clip the chess board, and it must
            // always have a number of pixels that is divisible by 8
            // so all of the squares have an integer number of pixels.
            int pixels = (Math.Min(ClientRectangle.Width, ClientRectangle.Height) - 20) & ~7;
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
            int squarePixels = panel_ChessBoard.Width / 8;
            boardDisplay.Render(e.Graphics, squarePixels);
        }
    }
}
