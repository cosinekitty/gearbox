using Gearbox;
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
    public partial class FenEditForm : Form
    {
        private Board scratchBoard = new Board(false);

        public FenEditForm()
        {
            InitializeComponent();
            textBox_fen.KeyPress += new KeyPressEventHandler(OnKeyPress);
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
                TrySubmit();
            }
        }

        public void SetFen(string fen)
        {
            textBox_fen.Text = fen;
        }

        public string GetFen()
        {
            return textBox_fen.Text;
        }

        private void button_FenOK_Click(object sender, EventArgs e)
        {
            TrySubmit();
        }

        private void TrySubmit()
        {
            // Do not allow submitting the FEN string unless it is valid.
            string fen = textBox_fen.Text;
            string message = "";
            try
            {
                scratchBoard.SetPosition(fen);
            }
            catch (ArgumentException ex)
            {
                message = ex.Message;
            }
            label_FenErrorMessage.Text = message;
            if (message == "")
                this.DialogResult = DialogResult.OK;
            else
                textBox_fen.Focus();
        }

        private void button_FenCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void FenEditForm_Load(object sender, EventArgs e)
        {

        }
    }
}
