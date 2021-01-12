
namespace GearboxWindowsGui
{
    partial class FenEditForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_fen = new System.Windows.Forms.TextBox();
            this.button_FenOK = new System.Windows.Forms.Button();
            this.button_FenCancel = new System.Windows.Forms.Button();
            this.label_FenErrorMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBox_fen
            // 
            this.textBox_fen.Location = new System.Drawing.Point(12, 12);
            this.textBox_fen.Name = "textBox_fen";
            this.textBox_fen.Size = new System.Drawing.Size(502, 27);
            this.textBox_fen.TabIndex = 0;
            // 
            // button_FenOK
            // 
            this.button_FenOK.Location = new System.Drawing.Point(13, 119);
            this.button_FenOK.Name = "button_FenOK";
            this.button_FenOK.Size = new System.Drawing.Size(94, 29);
            this.button_FenOK.TabIndex = 1;
            this.button_FenOK.Text = "OK";
            this.button_FenOK.UseVisualStyleBackColor = true;
            this.button_FenOK.Click += new System.EventHandler(this.button_FenOK_Click);
            // 
            // button_FenCancel
            // 
            this.button_FenCancel.Location = new System.Drawing.Point(420, 119);
            this.button_FenCancel.Name = "button_FenCancel";
            this.button_FenCancel.Size = new System.Drawing.Size(94, 29);
            this.button_FenCancel.TabIndex = 2;
            this.button_FenCancel.Text = "Cancel";
            this.button_FenCancel.UseVisualStyleBackColor = true;
            this.button_FenCancel.Click += new System.EventHandler(this.button_FenCancel_Click);
            // 
            // label_FenErrorMessage
            // 
            this.label_FenErrorMessage.Location = new System.Drawing.Point(12, 42);
            this.label_FenErrorMessage.Name = "label_FenErrorMessage";
            this.label_FenErrorMessage.Size = new System.Drawing.Size(501, 35);
            this.label_FenErrorMessage.TabIndex = 3;
            // 
            // FenEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(526, 160);
            this.Controls.Add(this.label_FenErrorMessage);
            this.Controls.Add(this.button_FenCancel);
            this.Controls.Add(this.button_FenOK);
            this.Controls.Add(this.textBox_fen);
            this.Name = "FenEditForm";
            this.Text = "Edit Forsyth Edwards Notation (FEN)";
            this.Load += new System.EventHandler(this.FenEditForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_fen;
        private System.Windows.Forms.Button button_FenOK;
        private System.Windows.Forms.Button button_FenCancel;
        private System.Windows.Forms.Label label_FenErrorMessage;
    }
}