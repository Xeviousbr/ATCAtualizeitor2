using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace TeleBonifacio
{
    public class FTP
    {
        private int _tamanhoConteudo = 0;
        private int Tot = 0;
        private string ftpIPServidor = "";
        private string ftpUsuarioID = "";
        private string ftpSenha = "";
        private string Erro = "";
        private ProgressBar ProgressBar1= null;
        public long bytesReceived = 0;
        private string Mensagem = "";
        private List<string> ComandosSQL { get; set; }

        public FTP(string ftpIPServidor, string ftpUsuarioID, string ftpSenha)
        {
            this.ftpIPServidor = ftpIPServidor;
            this.ftpUsuarioID = ftpUsuarioID;
            this.ftpSenha = ftpSenha;
        }

        public FTP()
        {

        }

        public int LerVersaoDoFtp()
        {
            string caminhoArquivo = "/public_html/public/entregas/versao.txt";
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + this.ftpIPServidor + caminhoArquivo));
            request.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UsePassive = true;
            FtpWebResponse response;
            try
            {
                response = (FtpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                throw new Exception("Erro ao conectar ao servidor FTP: " + ex.Message);
            }
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            //string info = reader.ReadToEnd();
            //string[] lines = info.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            //string versaoTexto = lines[0];
            //this.Mensagem = lines[1];
            //this.ComandosSQL = new List<string>();
            //// Verifica se há um comando SQL após a mensagem
            //if (lines[1].Contains("|"))
            //{
            //    // Separa a mensagem da linha contendo o comando SQL
            //    string[] parts = lines[1].Split(new char[] { '|' }, 2);

            //    this.Mensagem = parts[0]; // A primeira parte antes do '|'
            //    string comandoSQL = parts[1].Trim(); // A segunda parte após o '|'

            //    // Adiciona o comando SQL à lista
            //    this.ComandosSQL.Add(comandoSQL);
            //}
            //else
            //{
            //    // Se não houver comando SQL, apenas adiciona a mensagem à lista
            //    this.ComandosSQL.Add(lines[1]);
            //}

            //// Continua com a lógica original para adicionar outros comandos SQL presentes no arquivo
            //for (int i = 2; i < lines.Length; i++)
            //{
            //    string line = lines[i].Trim();
            //    if (!string.IsNullOrEmpty(line) && !line.Contains("|")) // Ignora linhas que contêm '|'
            //    {
            //        string comandoSQL = line;
            //        while (!comandoSQL.EndsWith(";") && i + 1 < lines.Length)
            //        {
            //            i++;
            //            comandoSQL += " " + lines[i].Trim();
            //        }
            //        ComandosSQL.Add(comandoSQL);
            //    }
            //}

            string info = reader.ReadToEnd();
            string[] lines = info.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string versaoTexto = lines[0];
            // this.Mensagem = lines[1];
            this.ComandosSQL = new List<string>();
            if (lines[1].Contains("|"))
            {
                string[] parts = lines[1].Split(new char[] { '|' }, 2);
                this.Mensagem = parts[0]; 
                string comandoSQL = parts[1].Trim(); 
                this.ComandosSQL.Add(comandoSQL);
            }
            else
            {
                this.Mensagem = lines[1];
                // this.ComandosSQL.Add(lines[1]);
            }
            //for (int i = 2; i < lines.Length; i++)
            //{
            //    string line = lines[i].Trim();
            //    if (!string.IsNullOrEmpty(line))
            //    {
            //        string comandoSQL = line;
            //        while (!comandoSQL.EndsWith(";") && i + 1 < lines.Length)
            //        {
            //            i++;
            //            comandoSQL += " " + lines[i].Trim();
            //        }
            //        ComandosSQL.Add(comandoSQL);
            //    }
            //}
            reader.Close();
            responseStream.Close();
            response.Close();
            int versaoNumero = int.Parse(versaoTexto.Replace(".", ""));
            return versaoNumero;
        }

        public string retMensagem()
        {
            return this.Mensagem;
        }

        public List<string> getComandos()
        {
            return this.ComandosSQL;
        }

        public string getErro()
        {
            return this.Erro;
        }

        public bool Download(string nmPasta, string nomeArquivoLocal, BackgroundWorker worker, long tamanhoTotalArquivo)
        {
            bool isDebugging = System.Diagnostics.Debugger.IsAttached;
            if (isDebugging)
            {
                return true;
            }
            string Suri = "ftp://" + this.ftpIPServidor + @"/" + nmPasta + @"/" + nomeArquivoLocal;
            FtpWebRequest requisicaoFTP;
            requisicaoFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
            requisicaoFTP.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
            requisicaoFTP.KeepAlive = false;
            requisicaoFTP.Method = WebRequestMethods.Ftp.DownloadFile;
            requisicaoFTP.UseBinary = true;
            bool ret = false;
            try
            {
                FtpWebResponse respDown = (FtpWebResponse)requisicaoFTP.GetResponse();
                Stream responseStream = respDown.GetResponseStream();
                using (FileStream fileStream = File.Create(nomeArquivoLocal))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;                    
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        bytesReceived += bytesRead;
                        if (tamanhoTotalArquivo>0)
                        {                            
                            int progresso = (int)((bytesReceived * 100) / tamanhoTotalArquivo);
                            worker.ReportProgress(progresso);
                        }
                    }
                }
                respDown.Close();
                ret = true;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

    }
}


