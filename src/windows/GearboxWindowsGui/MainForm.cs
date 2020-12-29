﻿using Gearbox;
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
                boardDisplay.UpdateDraggedPieceLocation(e.X, e.Y);
                panel_ChessBoard.Invalidate();
            }
        }
    }
}
