
namespace GearboxWindowsGui
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel_ChessBoard = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panel_ChessBoard
            // 
            this.panel_ChessBoard.Location = new System.Drawing.Point(13, 13);
            this.panel_ChessBoard.Name = "panel_ChessBoard";
            this.panel_ChessBoard.Size = new System.Drawing.Size(626, 654);
            this.panel_ChessBoard.TabIndex = 0;
            this.panel_ChessBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_ChessBoard_Paint);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 685);
            this.Controls.Add(this.panel_ChessBoard);
            this.MinimumSize = new System.Drawing.Size(700, 700);
            this.Name = "MainForm";
            this.Text = "Gearbox Chess Engine";
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel_ChessBoard;
    }
}

