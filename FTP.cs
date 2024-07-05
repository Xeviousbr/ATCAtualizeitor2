using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace ATCAtualizeitor
{
    public class FTP
    {
        private string ftpIPServidor = "";
        private string ftpUsuarioID = "";
        private string ftpSenha = "";
        private string Erro = "";
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
            string info = reader.ReadToEnd();
            string[] lines = info.Split('|');
            string versaoTexto = lines[0];
            this.Mensagem = lines[1];
            this.ComandosSQL = new List<string>();
            if (lines.Length > 2)
            {
                for (int i = 2; i < lines.Length; i++)
                {
                    this.ComandosSQL.Add(lines[i]);
                }
            }
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
            //bool isDebugging = System.Diagnostics.Debugger.IsAttached;
            //if (isDebugging)
            //{
            //    return true;
            //}
            string Suri = "ftp://" + this.ftpIPServidor + @"/" + nmPasta + @"/" + nomeArquivoLocal;
            const int maxTentativas = 3;
            bool sucesso = false;
            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                FtpWebRequest requisicaoFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
                requisicaoFTP.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
                requisicaoFTP.KeepAlive = false;
                requisicaoFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                requisicaoFTP.UseBinary = true;
                try
                {
                    using (FtpWebResponse respDown = (FtpWebResponse)requisicaoFTP.GetResponse())
                    using (Stream responseStream = respDown.GetResponseStream())
                    using (FileStream fileStream = File.Create(nomeArquivoLocal))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long bytesReceived = 0;
                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            bytesReceived += bytesRead;
                            if (tamanhoTotalArquivo > 0)
                            {
                                int progresso = (int)((bytesReceived * 100) / tamanhoTotalArquivo);
                                worker.ReportProgress(progresso);
                            }
                        }
                    }
                    if (Path.GetExtension(nomeArquivoLocal).ToLower() == ".exe")
                    {
                        if (VerificarArquivoExecutavel(nomeArquivoLocal))
                        {
                            sucesso = true;
                            break;
                        }
                        else
                        {
                            File.Delete(nomeArquivoLocal);
                            if (tentativa == maxTentativas)
                            {
                                Log.Loga("Erro: O arquivo executável no FTP parece estar corrompido após 3 tentativas.");
                            }
                        }
                    }
                    else
                    {
                        sucesso = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Loga($"Erro na tentativa {tentativa}: {ex.Message}");
                    if (tentativa == maxTentativas)
                    {
                        Log.Loga("Falha após 3 tentativas de download.");
                    }
                }
            }
            return sucesso;
        }

        private bool VerificarArquivoExecutavel(string caminhoArquivo)
        {
            try
            {
                using (FileStream fs = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    if (br.ReadInt16() != 0x5A4D)
                    {
                        Log.Loga("O arquivo não é um executável válido (Assinatura MZ ausente).");
                        return false;
                    }
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = br.ReadInt32();
                    fs.Seek(peOffset, SeekOrigin.Begin);
                    if (br.ReadInt32() != 0x00004550)
                    {
                        Log.Loga("O arquivo não é um executável válido (Assinatura PE ausente).");
                        return false;
                    }
                    fs.Seek(peOffset + 22, SeekOrigin.Begin);
                    ushort characteristics = br.ReadUInt16();
                    bool is32Bit = (characteristics & 0x0100) != 0;
                    bool is64Bit = (characteristics & 0x0020) != 0;
                    if (!is32Bit && !is64Bit)
                    {
                        Log.Loga("O executável não é compatível com 32-bit nem 64-bit.");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Loga($"Erro ao verificar o executável: {ex.Message}");
                return false;
            }
        }
    }
}


