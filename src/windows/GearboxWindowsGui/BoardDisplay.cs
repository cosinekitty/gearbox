using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gearbox;

namespace GearboxWindowsGui
{
    internal class BoardDisplay
    {
        private bool reverse;   // false = show from White's point of view, true = Black's
        private readonly Board board = new Board();
        private readonly Dictionary<Square, Image> imageTable = new();
        private string imageFolder = @"c:\don\github\gearbox\src\windows\GearboxWindowsGui\images"; // FIXFIXFIX
        private SolidBrush lightSqaureBrush = new SolidBrush(Color.FromArgb(0xe1, 0xce, 0xaa));
        private SolidBrush darkSquareBrush = new SolidBrush(Color.FromArgb(0x9c, 0xa2, 0x66));

        public BoardDisplay()
        {
            // Load PNG images for the pieces.
            imageTable[Square.WK] = LoadImage("wk.png");
            imageTable[Square.WQ] = LoadImage("wq.png");
            imageTable[Square.WR] = LoadImage("wr.png");
            imageTable[Square.WB] = LoadImage("wb.png");
            imageTable[Square.WN] = LoadImage("wn.png");
            imageTable[Square.WP] = LoadImage("wp.png");

            imageTable[Square.BK] = LoadImage("bk.png");
            imageTable[Square.BQ] = LoadImage("bq.png");
            imageTable[Square.BR] = LoadImage("br.png");
            imageTable[Square.BB] = LoadImage("bb.png");
            imageTable[Square.BN] = LoadImage("bn.png");
            imageTable[Square.BP] = LoadImage("bp.png");
        }

        private Image LoadImage(string fn)
        {
            string path = Path.Combine(imageFolder, fn);
            return Image.FromFile(path);
        }

        void SetReverse(bool reverse)
        {
            this.reverse = reverse;
            // FIXFIXFIX: repaint the display to reflect the change
        }

        public void Render(Graphics graphics, int squarePixels)
        {
            Square[] squares = board.GetSquaresArray();
            for (int x=0; x < 8; ++x)
            {
                int sx = reverse ? (7 - x) : x;
                int rx = squarePixels * x;
                for (int y=0; y < 8; ++y)
                {
                    int sy = reverse ? (7 - y) : y;
                    int ry = (7 - y) * squarePixels;
                    var rect = new Rectangle(rx, ry, squarePixels, squarePixels);
                    var brush = (((x + y) & 1) == 0) ? darkSquareBrush : lightSqaureBrush;

                    // Draw the colored square itself.
                    graphics.FillRectangle(brush, rect);

                    // If there is a piece on the square, superimpose its icon on the square.
                    int ofs = 21 + (10 * sy) + sx;
                    Square piece = squares[ofs];
                    if (imageTable.TryGetValue(piece, out Image image))
                    {
                        graphics.DrawImage(image, rect);
                    }
                }
            }
        }
    }
}
