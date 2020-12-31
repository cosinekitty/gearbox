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
        internal readonly Board board = new Board();
        private readonly MoveList legalMoveList = new MoveList();
        private readonly Dictionary<Square, Image> imageTable = new();
        private string imageFolder = @"c:\don\github\gearbox\src\windows\GearboxWindowsGui\images"; // FIXFIXFIX
        private SolidBrush lightSqaureBrush = new SolidBrush(Color.FromArgb(0xe8, 0xdd, 0xb9));
        private SolidBrush darkSquareBrush = new SolidBrush(Color.FromArgb(0xc9, 0xb0, 0x60));
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

        public void RotateBoard()
        {
            reverse = !reverse;
        }

        public void SetPixelsPerSquare(int squarePixels)
        {
            pixelsPerSquare = squarePixels;
        }

        internal void UpdateDraggedPieceLocation(int mouseX, int mouseY)
        {
            dragMouseX = mouseX;
            dragMouseY = mouseY;
        }

        private int SquareCenterScreenX(int ofs)
        {
            int x = (ofs % 10) - 1;
            return (pixelsPerSquare / 2) + pixelsPerSquare * (reverse ? (7 - x) : x);
        }

        private int SquareCenterScreenY(int ofs)
        {
            int y = (ofs / 10) - 2;
            return (pixelsPerSquare / 2) + pixelsPerSquare * (reverse ? y : (7 - y));
        }

        internal void UpdateAnimation(Move move, double fraction)
        {
            int x1 = SquareCenterScreenX(move.source);
            int y1 = SquareCenterScreenY(move.source);
            int x2 = SquareCenterScreenX(move.dest);
            int y2 = SquareCenterScreenY(move.dest);
            dragMouseX = (int)Math.Round(x1 + fraction * (x2 - x1));
            dragMouseY = (int)Math.Round(y1 + fraction * (y2 - y1));
        }

        internal Rectangle AnimationRectangle()
        {
            int px = dragMouseX - (pixelsPerSquare / 2);
            int py = dragMouseY - (pixelsPerSquare / 2);
            var rect = new Rectangle(px, py, pixelsPerSquare, pixelsPerSquare);
            return rect;
        }

        public void Render(Graphics graphics, Rectangle clipRectangle)
        {
            for (int x=0; x < 8; ++x)
            {
                int sx = reverse ? (7 - x) : x;
                int rx = pixelsPerSquare * x;
                for (int y=0; y < 8; ++y)
                {
                    int sy = reverse ? (7 - y) : y;
                    int ry = (7 - y) * pixelsPerSquare;
                    int ofs = 21 + (10 * sy) + sx;

                    var rect = new Rectangle(rx, ry, pixelsPerSquare, pixelsPerSquare);

                    // For the sake of performance, it is important to only draw the parts
                    // of the chess board that overlap with the invalidated rectangle.
                    if (!RectanglesOverlap(rect, clipRectangle))
                        continue;

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
                            graphics.DrawImage(image, rect);
                    }
                }
            }

            if (IsDraggingPiece())
            {
                // Draw any dragged piece on top of everything else.
                if (imageTable.TryGetValue(pieceBeingDragged, out Image image))
                {
                    Rectangle rect = AnimationRectangle();
                    if (RectanglesOverlap(rect, clipRectangle))
                        graphics.DrawImage(image, rect);
                }
            }
        }

        private bool RectanglesOverlap(Rectangle a, Rectangle b)
        {
            // If two rectangles overlap, one of the rectangle's corners must be inside the other rectangle.
            return
                PointInsideRectangle(a.X, a.Y, b) ||
                PointInsideRectangle(a.X + a.Width - 1, a.Y, b) ||
                PointInsideRectangle(a.X + a.Width - 1, a.Y + a.Height - 1, b) ||
                PointInsideRectangle(a.X, a.Y + a.Height - 1, b) ||
                PointInsideRectangle(b.X, b.Y, a) ||
                PointInsideRectangle(b.X + b.Width - 1, b.Y, a) ||
                PointInsideRectangle(b.X + b.Width - 1, b.Y + b.Height - 1, a) ||
                PointInsideRectangle(b.X, b.Y + b.Height - 1, a);
        }

        private bool PointInsideRectangle(int x, int y, Rectangle r)
        {
            return (x >= r.X) && (x < r.X + r.Width) && (y >= r.Y) && (y < r.Y + r.Height);
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

        public void StartAnimatingMove(Move move)
        {
            if (!legalMoveList.Contains(move))
                throw new Exception("Attempt to animate an illegal move.");

            pieceBeingDragged = board.GetSquareContents(move.source);
            if (0 == (pieceBeingDragged & (Square.White | Square.Black)))
                throw new Exception("Just tried to start animating an invalid piece!");

            dragSourceOffset = move.source;
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

        public void StopAnimatingMove()
        {
            CancelDrag();
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

        internal void RefreshMoves()
        {
            board.GenMoves(legalMoveList);
        }

        internal void MakeMove(Move move)
        {
            if (legalMoveList.Contains(move))
            {
                board.PushMove(move);
                board.GenMoves(legalMoveList);
            }
            else
                throw new Exception("Illegal move received: " + move);
        }

        private void CancelDrag()
        {
            dragSourceOffset = 0;
            pieceBeingDragged = Square.Empty;
        }
    }
}
