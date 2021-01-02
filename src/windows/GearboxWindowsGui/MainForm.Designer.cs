
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panel_ChessBoard = new System.Windows.Forms.Panel();
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rotateBoardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemComputerWhite = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemComputerBlack = new System.Windows.Forms.ToolStripMenuItem();
            this.panel_BestPath = new System.Windows.Forms.Panel();
            this.panel_RankNumbers = new System.Windows.Forms.Panel();
            this.panel_FileLetters = new System.Windows.Forms.Panel();
            this.mainMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel_ChessBoard
            // 
            this.panel_ChessBoard.Location = new System.Drawing.Point(33, 31);
            this.panel_ChessBoard.Name = "panel_ChessBoard";
            this.panel_ChessBoard.Size = new System.Drawing.Size(630, 636);
            this.panel_ChessBoard.TabIndex = 0;
            this.panel_ChessBoard.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_ChessBoard_Paint);
            this.panel_ChessBoard.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_ChessBoard_MouseDown);
            this.panel_ChessBoard.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel_ChessBoard_MouseMove);
            this.panel_ChessBoard.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_ChessBoard_MouseUp);
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.gameToolStripMenuItem});
            this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.Size = new System.Drawing.Size(843, 28);
            this.mainMenuStrip.TabIndex = 1;
            this.mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.toolStripSeparator,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripMenuItem.Image")));
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(178, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            this.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.saveAsToolStripMenuItem.Text = "Save &As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(178, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(49, 24);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rotateBoardToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(55, 24);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // rotateBoardToolStripMenuItem
            // 
            this.rotateBoardToolStripMenuItem.Name = "rotateBoardToolStripMenuItem";
            this.rotateBoardToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.rotateBoardToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.rotateBoardToolStripMenuItem.Text = "&Rotate board";
            this.rotateBoardToolStripMenuItem.Click += new System.EventHandler(this.rotateBoardToolStripMenuItem_Click);
            // 
            // gameToolStripMenuItem
            // 
            this.gameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemComputerWhite,
            this.toolStripMenuItemComputerBlack});
            this.gameToolStripMenuItem.Name = "gameToolStripMenuItem";
            this.gameToolStripMenuItem.Size = new System.Drawing.Size(62, 24);
            this.gameToolStripMenuItem.Text = "&Game";
            // 
            // toolStripMenuItemComputerWhite
            // 
            this.toolStripMenuItemComputerWhite.Name = "toolStripMenuItemComputerWhite";
            this.toolStripMenuItemComputerWhite.Size = new System.Drawing.Size(239, 26);
            this.toolStripMenuItemComputerWhite.Text = "Computer plays &White";
            this.toolStripMenuItemComputerWhite.Click += new System.EventHandler(this.toolStripMenuItemComputerWhite_Click);
            // 
            // toolStripMenuItemComputerBlack
            // 
            this.toolStripMenuItemComputerBlack.Name = "toolStripMenuItemComputerBlack";
            this.toolStripMenuItemComputerBlack.Size = new System.Drawing.Size(239, 26);
            this.toolStripMenuItemComputerBlack.Text = "Computer plays &Black";
            this.toolStripMenuItemComputerBlack.Click += new System.EventHandler(this.toolStripMenuItemComputerBlack_Click);
            // 
            // panel_BestPath
            // 
            this.panel_BestPath.Location = new System.Drawing.Point(669, 31);
            this.panel_BestPath.Name = "panel_BestPath";
            this.panel_BestPath.Size = new System.Drawing.Size(145, 636);
            this.panel_BestPath.TabIndex = 2;
            this.panel_BestPath.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_BestPath_Paint);
            // 
            // panel_RankNumbers
            // 
            this.panel_RankNumbers.Location = new System.Drawing.Point(0, 32);
            this.panel_RankNumbers.Name = "panel_RankNumbers";
            this.panel_RankNumbers.Size = new System.Drawing.Size(27, 635);
            this.panel_RankNumbers.TabIndex = 3;
            this.panel_RankNumbers.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_RankNumbers_Paint);
            // 
            // panel_FileLetters
            // 
            this.panel_FileLetters.Location = new System.Drawing.Point(33, 673);
            this.panel_FileLetters.Name = "panel_FileLetters";
            this.panel_FileLetters.Size = new System.Drawing.Size(630, 31);
            this.panel_FileLetters.TabIndex = 4;
            this.panel_FileLetters.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_FileLetters_Paint);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(843, 716);
            this.Controls.Add(this.panel_FileLetters);
            this.Controls.Add(this.panel_RankNumbers);
            this.Controls.Add(this.panel_BestPath);
            this.Controls.Add(this.panel_ChessBoard);
            this.Controls.Add(this.mainMenuStrip);
            this.MainMenuStrip = this.mainMenuStrip;
            this.MinimumSize = new System.Drawing.Size(820, 730);
            this.Name = "MainForm";
            this.Text = "Gearbox Chess Engine";
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel_ChessBoard;
        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rotateBoardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemComputerWhite;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemComputerBlack;
        private System.Windows.Forms.Panel panel_BestPath;
        private System.Windows.Forms.Panel panel_RankNumbers;
        private System.Windows.Forms.Panel panel_FileLetters;
    }
}

