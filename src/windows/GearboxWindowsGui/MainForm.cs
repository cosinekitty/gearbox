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
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.panel_ChessBoard.Width != this.panel_ChessBoard.Height)
            {
                this.panel_ChessBoard.Width = this.panel_ChessBoard.Height;
            }
        }

        private void panel_ChessBoard_Paint(object sender, PaintEventArgs e)
        {
            int squarePixels = panel_ChessBoard.Width / 8;
            boardDisplay.Render(e.Graphics, squarePixels);
        }
    }
}
