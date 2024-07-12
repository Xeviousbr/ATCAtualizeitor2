using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace RH
{
    public partial class Lancamento : Form
    {
        private LancamentoStatus lancamentoStatus;
        private DateTime currentTime;

        #region Inicialização

        public Lancamento()
        {
            InitializeComponent();
        }

        private void Lancamento_Load(object sender, EventArgs e)
        {
            lbNome.Text = glo.NomeUser;
            INI OIni = new INI();
            DateTime FimManha = OIni.ReadTime("Turnos", "ManFim", new DateTime(1, 1, 1, 12, 0, 0));
            DateTime FimDia = OIni.ReadTime("Turnos", "TarFim", new DateTime(1, 1, 1, 18, 0, 0));
            DateTime fimCafeManha = FimManha.AddMinutes(-15); // 11:45
            DateTime fimCafeTarde = FimDia.AddMinutes(-15); // 17:45

            currentTime = DateTime.Now;
            // currentTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 25, 0);            

            textBox1.Text = currentTime.ToString("HH:mm");
            VerificarStatusLancamento(FimManha, FimDia, fimCafeManha, fimCafeTarde);
        }

        #endregion

        #region Verificação do Status      

        private void checkLateCafeHours(DateTime currentTime, DateTime fimCafeManha, DateTime fimManha, DateTime fimCafeTarde, DateTime fimDia)
        {
            if (currentTime.TimeOfDay >= fimCafeManha.TimeOfDay && currentTime.TimeOfDay < fimManha.TimeOfDay)
            {
                lbInfo.Text = $"Saída pela manhã";
                lancamentoStatus = LancamentoStatus.SaidaManha;
            }
            if (currentTime.TimeOfDay >= fimCafeTarde.TimeOfDay && currentTime.TimeOfDay < fimDia.TimeOfDay)
            {
                lbInfo.Text = $"Saída do expediente";
                lancamentoStatus = LancamentoStatus.SaidaTarde;
            }
        }

        private void VerificarStatusLancamento(DateTime fimManha, DateTime fimDia, DateTime fimCafeManha, DateTime fimCafeTarde)
        {
            var lancamentoInfo = ObterLancamentoInfo();
            lancamentoStatus = LancamentoStatus.Vazio;
            if (lancamentoInfo == null)
            {
                handleNewEntry(currentTime, fimManha);
            }
            else
            {
                // Priorizando entrada para o café da manhã
                if (!handleMorningAndCoffeeChecks(lancamentoInfo, currentTime, fimCafeManha, fimManha))
                {
                    // Se handleMorningAndCoffeeChecks retorna false, então não encontrou condição válida, continua para a tarde
                    handleAfternoonAndEveningChecks(lancamentoInfo, currentTime, fimManha, fimDia);
                }
            }
            if (lancamentoStatus == LancamentoStatus.Vazio)
            {
                checkLateCafeHours(currentTime, fimCafeManha, fimManha, fimCafeTarde, fimDia);
            }            
        }

        private void handleAfternoonAndEveningChecks(LancamentoInfo info, DateTime currentTime, DateTime fimManha, DateTime fimDia)
        {
            // Verifica se é necessário marcar a saída da manhã.
            if (info.TxInMan.HasValue && !info.TxFmMan.HasValue)
            {
                lbInfo.Text = $"Saída pela manhã";
                lancamentoStatus = LancamentoStatus.SaidaManha;
            }
            // Verifica se é hora de entrar à tarde (após a saída da manhã).
            else if (info.TxFmMan.HasValue && !info.TxInTrd.HasValue && currentTime.TimeOfDay >= fimManha.TimeOfDay)
            {
                lbInfo.Text = $"Entrada da tarde";
                lancamentoStatus = LancamentoStatus.EntradaTarde;
            }
            // Verifica se é hora de entrada no café da tarde.
            else if (info.TxInTrd.HasValue && !info.TxInCafeTrd.HasValue)
            {
                lbInfo.Text = $"Entrada para o café da tarde";
                lancamentoStatus = LancamentoStatus.EntradaCafeTarde;
            }
            // Verifica se é hora de saída do café da tarde.
            else if (info.TxInCafeTrd.HasValue && !info.TxFmCafeTrd.HasValue)
            {
                lbInfo.Text = $"Saída do café da tarde";
                lancamentoStatus = LancamentoStatus.SaidaCafeTarde;
            }
            // Verifica se é hora de saída do expediente.
            else if (info.TxInTrd.HasValue && !info.TxFnTrd.HasValue)
            {
                lbInfo.Text = $"Saída do expediente";
                lancamentoStatus = LancamentoStatus.SaidaTarde;
            }
            // Se todas as marcações do dia já foram feitas.
            else if (info.TxFnTrd.HasValue)
            {
                lbInfo.Text = $"Todos os lançamentos de hoje completos!";
                lancamentoStatus = LancamentoStatus.Completo;
                button1.Text = "Fechar";
            }
            else
            {
                lbInfo.Text = $"Verifique as condições de horário";
            }
        }

        private bool handleMorningAndCoffeeChecks(LancamentoInfo info, DateTime currentTime, DateTime fimCafeManha, DateTime fimManha)
        {
            if (info.TxInMan.HasValue && !info.TxInCafeMan.HasValue && currentTime.TimeOfDay < fimCafeManha.TimeOfDay)
            {
                lbInfo.Text = $"Entrada para o café da manhã";
                lancamentoStatus = LancamentoStatus.EntradaCafeManha;
                return true; // Retorna true indicando que encontrou condição válida
            }
            else if (info.TxInCafeMan.HasValue && !info.TxFmCafeMan.HasValue)
            {
                lbInfo.Text = $"Saída do café da manhã";
                lancamentoStatus = LancamentoStatus.SaidaCafeManha;
                return true;
            }
            return false; // Nenhuma condição válida encontrada
        }

        private void handleNewEntry(DateTime currentTime, DateTime fimManha)
        {
            // Verifica se a hora atual já ultrapassou o fim da manhã, indicando que é tarde
            if (currentTime.TimeOfDay >= fimManha.TimeOfDay)
            {
                lbInfo.Text = $"Início da tarde";
                lancamentoStatus = LancamentoStatus.IniciarTarde;
            }
            else
            {
                lbInfo.Text = $"Início do Expediente";
                lancamentoStatus = LancamentoStatus.IniciarExpediente;
            }
        }

        //private void handleNewEntry(DateTime currentTime, DateTime fimManha)
        //{
        //    if (currentTime.TimeOfDay >= fimManha.TimeOfDay)
        //    {
        //        lbInfo.Text = $"Início da tarde";
        //        lancamentoStatus = LancamentoStatus.IniciarTarde;
        //    }
        //    else
        //    {
        //        lbInfo.Text = $"Início do Expediente";
        //        lancamentoStatus = LancamentoStatus.IniciarExpediente;
        //    }
        //}

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

        #endregion

        #region Finalização        

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text != "Fechar")
            {
                string sql;
                string horaLanc = currentTime.ToString("yyyy-MM-dd HH:mm:ss");
                string campo = "";
                string valor = $"'{horaLanc}'";

                switch (lancamentoStatus)
                {
                    case LancamentoStatus.SaidaManha:
                        campo = "txfmman";
                        break;
                    case LancamentoStatus.EntradaTarde:
                        campo = "txintrd";
                        break;
                    case LancamentoStatus.SaidaTarde:
                        campo = "txfntrd";
                        break;
                    case LancamentoStatus.EntradaCafeManha:
                        campo = "txInCafeMan";
                        break;
                    case LancamentoStatus.SaidaCafeManha:
                        campo = "txFmCafeMan";
                        break;
                    case LancamentoStatus.EntradaCafeTarde:
                        campo = "txInCafeTrd";
                        break;
                    case LancamentoStatus.SaidaCafeTarde:
                        campo = "txFmCafeTrd";
                        break;
                    case LancamentoStatus.IniciarExpediente:
                        string UID = glo.GenerateUID();
                        sql = $@"INSERT INTO horarios (idfunc, txinman, data, uid) VALUES ({glo.iUsuario}, {valor}, Date(), '{UID}')";
                        DB.ExecutarComandoSQL(sql);
                        this.Close();
                        return;
                    case LancamentoStatus.Completo:
                        MessageBox.Show("Todos os lançamentos de hoje estão completos.");
                        this.Close();
                        return;
                    default:
                        return; // Se o status não é reconhecido, não faz nada.
                }

                // Se o campo foi definido, monta e executa o SQL para update
                if (!string.IsNullOrEmpty(campo))
                {
                    sql = $@"UPDATE horarios SET {campo} = {valor} WHERE idfunc = {glo.iUsuario} AND data = Date()";
                    DB.ExecutarComandoSQL(sql);
                }
            }
            this.Close();
        }

        private void Lancamento_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        #endregion
    }
}

#region Classes

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

public enum LancamentoStatus
{
    Vazio,
    SaidaManha,
    EntradaTarde,
    SaidaTarde,
    EntradaCafeManha,
    SaidaCafeManha,
    CafeTarde,
    SaidaCafeTarde,
    IniciarExpediente,
    IniciarTarde,
    Completo,
    EntradaCafeTarde
}

#endregion