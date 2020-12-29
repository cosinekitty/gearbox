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
            int pixels = (this.ClientRectangle.Height - 20) & ~7;    // must be divisible by 8
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
