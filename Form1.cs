using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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
                        Log.Loga(this.ERRO);
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
                        Log.Loga(this.ERRO);
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
            Log.Loga("Acionando programa em "+ arquivoDestino);
            Process.Start(arquivoDestino);
            Log.Loga("Fechando atualizador");
            Environment.Exit(0);
        }
        
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Refatorado em 11/08/24 Original 93 linhas, resultado 65 linhas
            cINI = new INI();
            string retornar = cINI.ReadString("Atualizador", "Retornar", "");
            string pastaDoEntregas = cINI.ReadString("Atualizacao", "Programa", "");
            string pastaBackup = Path.Combine(pastaDoEntregas, "Bak");
            Log.Loga("Retornar = " + retornar);

            if (retornar == "1")
            {
                ProcessarRetorno(cINI, pastaDoEntregas, e);
                return;
            }

            string url = cINI.ReadString("FTP", "URL", "");
            string user = Cripto.Decrypt(cINI.ReadString("FTP", "user", ""));
            string senha = Cripto.Decrypt(cINI.ReadString("FTP", "pass", ""));
            cFPT = new FTP(url, user, senha);
            string pasta = @"/public_html/public/entregas/";

            List<string> filesToDownload = cFPT.DownloadFileList(pasta);
            if (filesToDownload.Count > 0)
            {
                PrepararBackup(filesToDownload, pastaBackup);
                if (cFPT.DownloadAllFiles(pasta, filesToDownload, worker))
                {
                    ProcessarAtualizacao(filesToDownload, cINI, pastaDoEntregas, e);
                }
                else
                {
                    e.Result = false;
                }
            }
            else
            {
                Log.Loga("Nenhum arquivo para baixar.");
                e.Result = false;
            }
        }

        private void ProcessarRetorno(INI cINI, string pastaDoEntregas, DoWorkEventArgs e)
        {
            string arquivoOrigem = @"C:\Entregas\Bak\TeleBonifacio.exe";
            Log.Loga("arquivoOrigem = " + arquivoOrigem);
            File.Copy(arquivoOrigem, Path.Combine(pastaDoEntregas, "TeleBonifacio.exe"), true);
            cINI.WriteString("Atualizador", "Retornar", "0");
            System.Threading.Thread.Sleep(1000);
            e.Result = true;
        }

        private void PrepararBackup(List<string> filesToDownload, string pastaBackup)
        {
            string pastaAtual = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (!Directory.Exists(pastaBackup))
            {
                Directory.CreateDirectory(pastaBackup);
            }

            foreach (string fileName in filesToDownload)
            {
                string arquivoLocal = Path.Combine(pastaAtual, fileName);
                string arquivoBackup = Path.Combine(pastaBackup, fileName);

                if (File.Exists(arquivoLocal))
                {
                    File.Copy(arquivoLocal, arquivoBackup, true);
                    Log.Loga($"Backup criado: {arquivoBackup}");
                }
            }
        }

        private void ProcessarAtualizacao(List<string> filesToDownload, INI cINI, string pastaDoEntregas, DoWorkEventArgs e)
        {
            string pastaAtual = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string pastaDestino = cINI.ReadString("Config", "Programa", "");
            int versaoFtp = cFPT.LerVersaoDoFtp();
            string versaoNovaStr = $"{versaoFtp / 100}.{(versaoFtp / 10) % 10}.{versaoFtp % 10}";
            Log.Loga("Atualizando para " + versaoNovaStr);

            AtualizarVersaoAnterior(cINI);
            ExecutarComandosSQL(cINI);

            cINI.WriteString("Config", "VersaoAtual", versaoNovaStr);
            System.Threading.Thread.Sleep(100);
            CopiarArquivosAtualizados(filesToDownload, pastaAtual, pastaDestino);

            System.Threading.Thread.Sleep(100);
            Log.Loga("Executar programa em " + pastaDestino);
            this.Invoke(new MethodInvoker(delegate { this.WindowState = FormWindowState.Minimized; }));
            System.Threading.Thread.Sleep(1000);
            e.Result = true;
        }

        private void AtualizarVersaoAnterior(INI cINI)
        {
            string versaoAnterior = cINI.ReadString("Config", "VersaoAtual", "");
            if (!string.IsNullOrEmpty(versaoAnterior))
            {
                Log.Loga("VersaoAnterior = " + versaoAnterior);
                cINI.WriteString("Atualizador", "VersaoAnterior", versaoAnterior);
            }
        }

        private void ExecutarComandosSQL(INI cINI)
        {
            List<string> comandosSQL = cFPT.getComandos();
            Log.Loga("Quantidade de comandos SQL: " + comandosSQL.Count);
            if (comandosSQL.Count > 0)
            {
                string caminhoBase = cINI.ReadString("Config", "Base", "");
                connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + caminhoBase + ";";
                foreach (string comando in comandosSQL)
                {
                    Log.Loga(comando);
                    ExecutarComandoSQL(comando);
                    Log.Loga("Comando Executado");
                }
            }
        }

        private void CopiarArquivosAtualizados(List<string> filesToDownload, string pastaAtual, string pastaDestino)
        {
            foreach (string fileName in filesToDownload)
            {
                string arquivoLocal = Path.Combine(pastaAtual, fileName);
                string arquivoDestino = Path.Combine(pastaDestino, fileName);
                File.Copy(arquivoLocal, arquivoDestino, true);
                Log.Loga($"Arquivo atualizado: {arquivoDestino}");
            }
        }        

    }
}
