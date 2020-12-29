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
        private readonly MoveList legalMoveList = new MoveList();
        private readonly Dictionary<Square, Image> imageTable = new();
        private string imageFolder = @"c:\don\github\gearbox\src\windows\GearboxWindowsGui\images"; // FIXFIXFIX
        private SolidBrush lightSqaureBrush = new SolidBrush(Color.FromArgb(0xe1, 0xce, 0xaa));
        private SolidBrush darkSquareBrush = new SolidBrush(Color.FromArgb(0x9c, 0xa2, 0x66));
        private int pixelsPerSquare;
        private Square pieceBeingDragged = Square.Empty;
        private int dragMouseX;
        private int dragMouseY;
        private int dragSourceOffset;

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

            // Always keep the legal move list up to date.
            board.GenMoves(legalMoveList);
        }

        private Image LoadImage(string fn)
        {
            string path = Path.Combine(imageFolder, fn);
            return Image.FromFile(path);
        }

        public void RotateBoard(bool reverse)
        {
            this.reverse = reverse;
        }

        public void Render(Graphics graphics, int squarePixels)
        {
            this.pixelsPerSquare = squarePixels;
            for (int x=0; x < 8; ++x)
            {
                int sx = reverse ? (7 - x) : x;
                int rx = squarePixels * x;
                for (int y=0; y < 8; ++y)
                {
                    int sy = reverse ? (7 - y) : y;
                    int ry = (7 - y) * squarePixels;
                    int ofs = 21 + (10 * sy) + sx;

                    var rect = new Rectangle(rx, ry, squarePixels, squarePixels);
                    var brush = (((x + y) & 1) == 0) ? darkSquareBrush : lightSqaureBrush;

                    // Draw the colored square itself.
                    graphics.FillRectangle(brush, rect);

                    if (IsDraggingPiece() && (ofs == dragSourceOffset))
                    {
                        // Special case: if the user is currently dragging a piece,
                        // do not show it in its original square.
                        // Instead, paint it being dragged... we do that last,
                        // so it shows up on top of other pieces.
                    }
                    else
                    {
                        // If there is a piece on the square, superimpose its icon on the square.
                        Square piece = board.GetSquareContents(ofs);
                        if (imageTable.TryGetValue(piece, out Image image))
                        {
                            graphics.DrawImage(image, rect);
                        }
                    }
                }
            }

            if (IsDraggingPiece())
            {
                // Draw any dragged piece on top of everything else.
                if (imageTable.TryGetValue(pieceBeingDragged, out Image image))
                {
                    int px = dragMouseX - (squarePixels / 2);
                    int py = dragMouseY - (squarePixels / 2);
                    var rect = new Rectangle(px, py, squarePixels, squarePixels);
                    graphics.DrawImage(image, rect);
                }
            }
        }

        internal void UpdateDraggedPieceLocation(int mouseX, int mouseY)
        {
            dragMouseX = mouseX;
            dragMouseY = mouseY;
        }

        public int BoardOffset(int mouseX, int mouseY)
        {
            int sx = mouseX / pixelsPerSquare;
            int sy = mouseY / pixelsPerSquare;
            if (sx < 0 || sx > 7 || sy < 0 || sy > 7)
                return 0;   // invalid square

            // Tricky: mouse x-coordinates increase to the right,
            // but mouse y-coordinate increase downward.
            // If we have also rotated the board, we double-reverse sy,
            // meaning we leave sy alone.
            if (reverse)
                sx = 7 - sx;
            else
                sy = 7 - sy;

            return 21 + (10 * sy) + sx;
        }

        public bool IsDraggingPiece()
        {
            return pieceBeingDragged != Square.Empty;
        }

        public bool StartDraggingPiece(int mouseX, int mouseY)
        {
            if (!IsDraggingPiece())
            {
                int ofs = BoardOffset(mouseX, mouseY);
                if (ofs != 0)
                {
                    for (int i = 0; i < legalMoveList.nmoves; ++i)
                    {
                        if (legalMoveList.array[i].source == ofs)
                        {
                            dragSourceOffset = ofs;
                            pieceBeingDragged = board.GetSquareContents(ofs);
                            if (0 == (pieceBeingDragged & (Square.White | Square.Black)))
                                throw new Exception("Just tried to start dragging an invalid piece!");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool DropPiece(int mouseX, int mouseY)
        {
            if (IsDraggingPiece())
            {
                int dragDestOffset = BoardOffset(mouseX, mouseY);
                if (dragDestOffset != 0)
                {
                    for (int i = 0; i < legalMoveList.nmoves; ++i)
                    {
                        Move move = legalMoveList.array[i];
                        if (move.source == dragSourceOffset && move.dest == dragDestOffset)
                        {
                            // FIXFIXFIX: handle pawn promotion: user must choose promotion piece.
                            // Commit the move and stop animating.
                            board.PushMove(move);
                            CancelDrag();

                            // Update the list of legal moves.
                            board.GenMoves(legalMoveList);

                            GameResult result = board.GetGameResult();
                            if (result != GameResult.InProgress)
                            {
                                // FIXFIXFIX: handle end of game here.
                            }
                            return true;
                        }
                    }
                }
            }

            CancelDrag();
            return false;
        }

        private void CancelDrag()
        {
            dragSourceOffset = 0;
            pieceBeingDragged = Square.Empty;
        }
    }
}
