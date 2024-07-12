using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace RH
{
    public partial class Lancamento : Form
    {
        private string lancamentoStatus = "";


        public Lancamento()
        {
            InitializeComponent();
        }

        private void Lancamento_Load(object sender, EventArgs e)
        {
            lbNome.Text = glo.NomeUser;

            INI OIni = new INI();
            DateTime FimMaha =
            DateTime FimDia =
            
            DateTime currentTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 1, 0);
            // DateTime currentTime = DateTime.Now;            

            currentTime.ToString("HH:mm");
            VerificarStatusLancamento(currentTime);
        }

        private LancamentoInfo ObterLancamentoInfo()
        {
            LancamentoInfo lancamentoInfo = null;
            string query = $@"SELECT TOP 1 * 
                      FROM horarios 
                      WHERE idfunc = {glo.iUsuario} AND data = Date()
                      ORDER BY id DESC";
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                OleDbCommand command = new OleDbCommand(query, connection);
                try
                {
                    connection.Open();
                    OleDbDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        lancamentoInfo = new LancamentoInfo
                        {
                            TxInMan = reader["txinman"] != DBNull.Value ? (DateTime?)reader["txinman"] : null,
                            TxFmMan = reader["txfmman"] != DBNull.Value ? (DateTime?)reader["txfmman"] : null,
                            TxInTrd = reader["txintrd"] != DBNull.Value ? (DateTime?)reader["txintrd"] : null,
                            TxFnTrd = reader["txfntrd"] != DBNull.Value ? (DateTime?)reader["txfntrd"] : null,
                            TxInCafeMan = reader["txInCafeMan"] != DBNull.Value ? (DateTime?)reader["txInCafeMan"] : null,
                            TxFmCafeMan = reader["txFmCafeMan"] != DBNull.Value ? (DateTime?)reader["txFmCafeMan"] : null,
                            TxInCafeTrd = reader["txInCafeTrd"] != DBNull.Value ? (DateTime?)reader["txInCafeTrd"] : null,
                            TxFmCafeTrd = reader["txFmCafeTrd"] != DBNull.Value ? (DateTime?)reader["txFmCafeTrd"] : null
                        };
                        ImprimirLancamentoInfo(lancamentoInfo);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao acessar banco de dados: {ex.Message}");
                }
            }
            return lancamentoInfo;
        }

        private void ImprimirLancamentoInfo(LancamentoInfo lancamentoInfo)
        {
            if (lancamentoInfo != null)
            {
                Console.WriteLine($"TxInMan: {lancamentoInfo.TxInMan}, TxFmMan: {lancamentoInfo.TxFmMan}, TxInTrd: {lancamentoInfo.TxInTrd}, TxFnTrd: {lancamentoInfo.TxFnTrd}");
                Console.WriteLine($"TxInCafeMan: {lancamentoInfo.TxInCafeMan}, TxFmCafeMan: {lancamentoInfo.TxFmCafeMan}, TxInCafeTrd: {lancamentoInfo.TxInCafeTrd}, TxFmCafeTrd: {lancamentoInfo.TxFmCafeTrd}");
            }
            else
            {
                Console.WriteLine("Nenhum registro encontrado.");
            }
        }

        private void VerificarStatusLancamento(DateTime currentTime)
        {
            var lancamentoInfo = ObterLancamentoInfo();

            if (lancamentoInfo == null)
            {
                if (currentTime.Hour >= 12)
                {
                    lbInfo.Text = $"Início da tarde - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Iniciar Tarde";
                }
                else
                {
                    lbInfo.Text = $"Início do Expediente - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Iniciar Expediente";
                }
            }
            else
            {
                if (!lancamentoInfo.TxInMan.HasValue)
                {
                    lbInfo.Text = $"Início do Expediente - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Iniciar Expediente";
                }
                else if (lancamentoInfo.TxInMan.HasValue && !lancamentoInfo.TxInCafeMan.HasValue)
                {
                    if (currentTime.TimeOfDay >= new TimeSpan(9, 0, 0) && currentTime.TimeOfDay < new TimeSpan(11, 45, 0))
                    {
                        lbInfo.Text = $"Entrada para o café da manhã - Hora atual: {currentTime:HH:mm}";
                        lancamentoStatus = "Entrada Café Manhã";
                    }
                }
                else if (lancamentoInfo.TxInCafeMan.HasValue && !lancamentoInfo.TxFmCafeMan.HasValue)
                {
                    if (currentTime.TimeOfDay >= new TimeSpan(11, 30, 0) && currentTime.TimeOfDay < new TimeSpan(12, 0, 0))
                    {
                        lbInfo.Text = $"Saída do café da manhã - Hora atual: {currentTime:HH:mm}";
                        lancamentoStatus = "Saída Café Manhã";
                    }
                }
                else if (lancamentoInfo.TxInMan.HasValue && !lancamentoInfo.TxFmMan.HasValue)
                {
                    lbInfo.Text = $"Saída pela manhã - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Saída Manhã";
                    if (currentTime.Hour >= 12)
                    {
                        lbInfo.Text += " - Aguarde para registrar a entrada da tarde";
                        button1.Enabled = false;
                    }
                }
                else if (lancamentoInfo.TxFmMan.HasValue && !lancamentoInfo.TxInTrd.HasValue && currentTime.Hour >= 12)
                {
                    lbInfo.Text = $"Entrada da tarde - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Entrada Tarde";
                }
                else if (lancamentoInfo.TxInTrd.HasValue && !lancamentoInfo.TxFnTrd.HasValue)
                {
                    lbInfo.Text = $"Saída do expediente - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Saída Tarde";
                }
                else
                {
                    lbInfo.Text = $"Todos os lançamentos de hoje completos! - Hora atual: {currentTime:HH:mm}";
                    lancamentoStatus = "Completo";
                }
            }

            // Verifica se é muito tarde para o café da manhã
            if (currentTime.TimeOfDay >= new TimeSpan(11, 45, 0) && currentTime.TimeOfDay < new TimeSpan(12, 0, 0))
            {
                button1.Enabled = false;
                lbInfo.Text = $"Horário de café da manhã encerrado - Hora atual: {currentTime:HH:mm}";
            }

            // Verifica se é muito tarde para o café da tarde
            if (currentTime.TimeOfDay >= new TimeSpan(17, 45, 0) && currentTime.TimeOfDay < new TimeSpan(18, 0, 0))
            {
                button1.Enabled = false;
                lbInfo.Text = $"Horário de café da tarde encerrado - Hora atual: {currentTime:HH:mm}";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sql = "";
            switch (lancamentoStatus)
            {
                case "Saída Manhã":
                    sql = $@"UPDATE horarios SET txfmman = Now() WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    break;
                case "Entrada Tarde":
                    sql = $@"UPDATE horarios SET txintrd = Now() WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    break;
                case "Saída Tarde":
                    sql = $@"UPDATE horarios SET txfntrd = Now() WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    break;
                case "Iniciar Expediente":
                    string UID = glo.GenerateUID();
                    sql = $@"INSERT INTO horarios (idfunc, txinman, data, uid) VALUES ({glo.iUsuario}, Now(), Date(), '{UID}')";
                    break;
                // Adiciona lógica para o café aqui, por exemplo:
                case "Café Manhã":
                    sql = $@"UPDATE horarios SET txInCafeMan = Now() WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    break;
                case "Café Tarde":
                    sql = $@"UPDATE horarios SET txInCafeTrd = Now() WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    break;
                case "Completo":
                    MessageBox.Show("Todos os lançamentos de hoje estão completos.");
                    break;
            }
            if (!string.IsNullOrEmpty(sql))
            {
                DB.ExecutarComandoSQL(sql);
            }
            this.Close();
        }

        private void Lancamento_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}

public class LancamentoInfo
{
    public DateTime? TxInMan { get; set; }
    public DateTime? TxFmMan { get; set; }
    public DateTime? TxInTrd { get; set; }
    public DateTime? TxFnTrd { get; set; }
    public DateTime? TxInCafeMan { get; set; }
    public DateTime? TxFmCafeMan { get; set; }
    public DateTime? TxInCafeTrd { get; set; }
    public DateTime? TxFmCafeTrd { get; set; }
}
