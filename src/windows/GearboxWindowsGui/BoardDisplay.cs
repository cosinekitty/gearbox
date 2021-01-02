using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gearbox;

namespace GearboxWindowsGui
{
    internal class BoardDisplay
    {
        internal bool reverse;   // false = show from White's point of view, true = Black's
        internal readonly Board board = new Board();
        private readonly MoveList legalMoveList = new MoveList();
        private readonly Dictionary<Square, Image> imageTable = new();
        private SolidBrush lightSqaureBrush = new SolidBrush(Color.FromArgb(0xe8, 0xdd, 0xb9));
        private SolidBrush darkSquareBrush = new SolidBrush(Color.FromArgb(0xc9, 0xb0, 0x60));
        private SolidBrush pawnPromotionBackgroundBrush = new SolidBrush(Color.FromArgb(0xa3, 0xd1, 0xbc));
        private Pen pawnPromotionBorderPen = new Pen(Color.Red);
        internal int pixelsPerSquare;
        private Square pieceBeingDragged = Square.Empty;
        private int dragMouseX;
        private int dragMouseY;
        private int dragSourceOffset;
        private Pen emphasisPen = new Pen(Color.DarkOrange);
        private int[] emphasizedOffsetList = new int[0];
        private Dictionary<int, Square> promotionChoices = new();
        private int promotionSourceOffset;
        private int promotionDestOffset;

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
            string path = Path.Combine(MainForm.InstallFolder, "images", fn);
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
            int promMinX = int.MaxValue;
            int promMinY = int.MaxValue;
            int promMaxX = int.MinValue;
            int promMaxY = int.MinValue;

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

                    if (promotionChoices.TryGetValue(ofs, out Square promChoice))
                    {
                        // Render one of the pawn promotion choices for the user.
                        graphics.FillRectangle(pawnPromotionBackgroundBrush, rect);
                        graphics.DrawImage(imageTable[promChoice], rect);

                        // Find the outer screen boundaries of a rectangle enclosing the promotion choices.
                        promMinX = Math.Min(promMinX, rect.X);
                        promMaxX = Math.Max(promMaxX, rect.X + rect.Width);
                        promMinY = Math.Min(promMinY, rect.Y);
                        promMaxY = Math.Max(promMaxY, rect.Y + rect.Height);
                    }
                    else
                    {
                        var brush = (((x + y) & 1) == 0) ? darkSquareBrush : lightSqaureBrush;

                        // Draw the colored square itself.
                        graphics.FillRectangle(brush, rect);

                        if (emphasizedOffsetList.Contains(ofs))
                            graphics.DrawRectangle(emphasisPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

                        if (IsDraggingPiece() && (ofs == dragSourceOffset))
                        {
                            // Special case: if the user is currently dragging a piece,
                            // do not show it in its original square.
                            // Instead, paint it being dragged... we do that last,
                            // so it shows up on top of other pieces.
                        }
                        else if (ofs == promotionSourceOffset)
                        {
                            // Another special case: while waiting for the user to make
                            // a pawn promotion choice, do not show the pawn still in the source square.
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
            else
            {
                GameResult result = board.GetGameResult();
                switch (result)
                {
                    case GameResult.WhiteWon:
                        DrawResultText(graphics, "1–0");
                        break;

                    case GameResult.BlackWon:
                        DrawResultText(graphics, "0–1");
                        break;

                    case GameResult.Draw:
                        DrawResultText(graphics, "½–½");
                        break;

                    case GameResult.InProgress:
                    default:
                        break;      // do nothing
                }
            }

            if (WaitingForPromotionChoice())
            {
                // Draw a rectangular border around the promotion choices,
                // to help visually isolate them from the rest of the board.

                graphics.DrawRectangle(pawnPromotionBorderPen, promMinX, promMinY, promMaxX - promMinX, promMaxY - promMinY);
            }
        }

        private void DrawResultText(Graphics graphics, string text)
        {
            // Draw the text that represents the end of the game
            // centered on the chess board.

            using var font = new Font(FontFamily.GenericMonospace, 1.2f * pixelsPerSquare, FontStyle.Regular);
            using var bgBrush = new SolidBrush(Color.Yellow);
            using var fgBrush = new SolidBrush(Color.DarkRed);
            Size tsize = TextRenderer.MeasureText(text, font);
            float x = (4 * pixelsPerSquare) - (tsize.Width / 2);
            float y = (4 * pixelsPerSquare) - (tsize.Height / 2);
            float dx = -0.01f * pixelsPerSquare;
            float dy = -0.01f * pixelsPerSquare;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.DrawString(text, font, bgBrush, x + dx, y + dy);
            graphics.DrawString(text, font, fgBrush, x, y);
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

        public void StartDraggingPiece(int mouseX, int mouseY)
        {
            if (WaitingForPromotionChoice())
            {
                // Special case: we are waiting for the user to choose which piece to promote.
                // The trigger for selection is the mouse location when the button is released.
                // This function is called when the mouse is button is pressed (or screen touched).
                // Do nothing substantive.
                // FIXFIXFIX - highlight the square as it is dragged?
                return;
            }

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
                            return;
                        }
                    }
                }
            }
            return;
        }

        public void StopAnimatingMove()
        {
            CancelDrag();
        }

        private bool WaitingForPromotionChoice()
        {
            return promotionSourceOffset > 0;
        }

        private void EnterPawnPromotionChoiceState(int source, int dest)
        {
            // Overlay pawn promotion choices vertically away
            // from the pawn promotion location.
            // Put a queen on the promotion square, a rook one square
            // beyond (above or below, depending on player side and board rotation),
            // a bishop one square beyond that, and a knight one square beyond that.
            if (board.IsWhiteTurn)
            {
                promotionChoices[dest] = Square.WQ;
                promotionChoices[dest + Direction.S] = Square.WR;
                promotionChoices[dest + (2 * Direction.S)] = Square.WB;
                promotionChoices[dest + (3 * Direction.S)] = Square.WN;
            }
            else
            {
                promotionChoices[dest] = Square.BQ;
                promotionChoices[dest + Direction.N] = Square.BR;
                promotionChoices[dest + (2 * Direction.N)] = Square.BB;
                promotionChoices[dest + (3 * Direction.N)] = Square.BN;
            }

            promotionSourceOffset = source;
            promotionDestOffset = dest;
        }

        private void ExitPawnPromotionChoiceState()
        {
            promotionSourceOffset = 0;
            promotionDestOffset = 0;
            promotionChoices.Clear();
        }

        public bool DropPiece(int mouseX, int mouseY)
        {
            if (WaitingForPromotionChoice())
            {
                int clickOffset = BoardOffset(mouseX, mouseY);
                if (promotionChoices.TryGetValue(clickOffset, out Square promPiece))
                {
                    // Now we can finally complete the panw promotion move.
                    // We know the source, the destination, and the promotion piece.

                    char promChar;
                    switch (promPiece & Square.PieceMask)
                    {
                        case Square.Queen:  promChar = 'q'; break;
                        case Square.Rook:   promChar = 'r'; break;
                        case Square.Bishop: promChar = 'b'; break;
                        case Square.Knight: promChar = 'n'; break;
                        default:
                            throw new Exception("Internal error: invalid promotion piece = " + promPiece);
                    }

                    for (int i=0; i < legalMoveList.nmoves; ++i)
                    {
                        Move move = legalMoveList.array[i];
                        if (move.source == promotionSourceOffset && move.dest == promotionDestOffset && move.prom == promChar)
                        {
                            MakeMove(move);
                            ExitPawnPromotionChoiceState();

                            // Return true to signal changing the side to move.
                            return true;
                        }
                    }

                }
                return false;
            }

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
                            CancelDrag();
                            if (move.IsPromotion())
                            {
                                // Go into a special UI state that waits for the user
                                // to choose the promotion piece (Queen, Rook, Bishop, Knight).
                                EnterPawnPromotionChoiceState(move.source, move.dest);
                                return false;
                            }
                            MakeMove(move);
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
                // Highlight the source and destination squares.
                emphasizedOffsetList = new int[] { move.source, move.dest };

                // Make the move on the board.
                board.PushMove(move);

                // Update the list of currently legal moves.
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
