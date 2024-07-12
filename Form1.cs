using System;
using System.Data;
using System.Windows.Forms;

namespace RH
{
    public partial class Form1 : Form
    {
        private INI cINI;
        private string connectionString = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            Busca();
        }

        private void Busca()
        {
            VendedoresDAO dVend = new VendedoresDAO();
            glo.iUsuario = dVend.getUsuarioNro(txNro.Text);
            if (glo.iUsuario > 0)
            {
                glo.NomeUser = dVend.getNome();
                Lancamento fLanc = new Lancamento();
                fLanc.Show();
                this.Visible = false;
            }
            else
            {
                MessageBox.Show("Não foi identificado");
            }
        }

        private void txNro_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {                
                e.SuppressKeyPress = true;
                Busca();
            }
        }

    }
}
