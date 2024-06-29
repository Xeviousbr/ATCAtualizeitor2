using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TeleBonifacio;
//using TeleBonifacio.gen;

namespace ATCAtualizeitor
{
    public partial class Form1 : Form
    {
        private FTP cFPT;
        private BackgroundWorker worker;
        private string arquivoDestino = @"C:\Entregas\TeleBonifacio.exe";
        private INI cINI;
        private string connectionString = "";
        private int erros = 0;
        private string ERRO = "";

        private void ExecutarComandoSQL(string query)
        {
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                    }
                    catch (Exception Ex)
                    {
                        this.ERRO = Ex.ToString();
                        Loga(this.ERRO);
                        throw;
                    }
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception Ex)
                    {
                        this.erros++;
                        this.ERRO = Ex.ToString();
                        Loga(this.ERRO);
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int value = e.ProgressPercentage;
            if (value >= progressBar1.Minimum && value <= progressBar1.Maximum)
            {
                progressBar1.Value = value;
                this.Text = "Atualizador " + value.ToString() + " %";
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {            
            if (erros>0)
            {
                string mensagem = "";
                if (erros==1)
                {
                    mensagem = ERRO;
                } else
                {
                    mensagem = "Olhe o log para er os erros";
                }
                MessageBox.Show(mensagem, "Houveram erros de SQL", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            Loga("Acionando programa em "+ arquivoDestino);
            Process.Start(arquivoDestino);
            Loga("Fechando atualizador");
            Environment.Exit(0);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            cINI = new INI();
            string Retornar = cINI.ReadString("Atualizador", "Retornar", "");
            string PastaDoEntregas = cINI.ReadString("Atualizacao", "Programa", "");
            string pastaBackup = Path.Combine(PastaDoEntregas, "Bak");
            Loga("Retornar = " + Retornar);
            if (Retornar=="1") 
            {
                string arquivoOrigem = @"C:\Entregas\Bak\TeleBonifacio.exe";
                Loga("arquivoOrigem = " + arquivoOrigem);
                File.Copy(arquivoOrigem, arquivoDestino, true);
                cINI.WriteString("Atualizador", "Retornar", "0");
                System.Threading.Thread.Sleep(1000);
                e.Result = true;
            } else
            {
                string URL = cINI.ReadString("FTP", "URL", "");
                string user = Cripto.Decrypt(cINI.ReadString("FTP", "user", ""));
                string senha = Cripto.Decrypt(cINI.ReadString("FTP", "pass", ""));
                cFPT = new FTP(URL, user, senha);
                string nmPrograma = "TeleBonifacio.exe";
                string Pasta = @"/public_html/public/entregas/";
                string tamanho = cINI.ReadString("Config", "tamanho", "");
                long tamanhoTotalArquivo = tamanho.Length > 0 ? long.Parse(tamanho) : 0;
                string pastaAtual = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string arquivoLocal = Path.Combine(pastaAtual, nmPrograma);
                string arquivoBackup = arquivoLocal.Replace("Atualizacao", "Bak");
                File.Copy(arquivoDestino, arquivoBackup, true);
                if (cFPT.Download(Pasta, nmPrograma, worker, tamanhoTotalArquivo))
                {
                    long bytesReceived = cFPT.bytesReceived;
                    cINI.WriteString("Config", "tamanho", bytesReceived.ToString());
                    string pastaPrograma = Path.Combine(pastaAtual, "..");
                    Loga("pastaAtual : " + pastaAtual);
                    Loga("pastaPrograma : " + pastaPrograma);
                    Loga("pastaBackup : " + pastaBackup);
                    if (!Directory.Exists(pastaBackup))
                    {
                        Directory.CreateDirectory(pastaBackup);
                    }
                    string PastaDestino = cINI.ReadString("Config", "Programa", "");
                    Loga("arquivoLocal : " + arquivoLocal);
                    Loga("arquivoDestino : " + arquivoDestino);
                    Loga("arquivoBackup : " + arquivoBackup);
                    int versaoFtp = cFPT.LerVersaoDoFtp();
                    string versaoNovaStr = $"{versaoFtp / 100}.{(versaoFtp / 10) % 10}.{versaoFtp % 10}";
                    Loga("Atualizando para " + versaoNovaStr);
                    string VersaoAnterior = cINI.ReadString("Config", "VersaoAtual", "");
                    if (VersaoAnterior.Length > 0)
                    {
                        Loga("VersaoAnterior = "+ versaoNovaStr);
                        cINI.WriteString("Atualizador", "VersaoAnterior", versaoNovaStr);
                    }
                    List<string> ComandosSQL = cFPT.getComandos();
                    Loga("Quantidade de comandos SQL" + ComandosSQL.Count.ToString());
                    if (ComandosSQL.Count > 0)
                    {
                        string CaminhoBase = cINI.ReadString("Config", "Base", "");
                        connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + CaminhoBase + ";";
                        for (int i = 0; i < ComandosSQL.Count; i++)
                        {
                            Loga(ComandosSQL[i]);
                            ExecutarComandoSQL(ComandosSQL[i]);
                            Loga("Comando Executado");
                        }
                    }
                    cINI.WriteString("Config", "VersaoAtual", versaoNovaStr);
                    System.Threading.Thread.Sleep(100);
                    File.Copy(arquivoLocal, arquivoDestino, true);
                    System.Threading.Thread.Sleep(100);
                    Loga("Executar programa em " + arquivoDestino);
                    this.Invoke(new MethodInvoker(delegate { this.WindowState = FormWindowState.Minimized; }));
                    System.Threading.Thread.Sleep(1000);
                    e.Result = true;
                }
                else
                {
                    e.Result = false;
                }
            }
        }

        private void Loga(string message)
        {
            string logFilePath = @"C:\Entregas\Atualizador.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

    }
}
