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
            // Refatorado em 11/08/24 Original 50 linhas, resultado 34 linhas
            string caminhoArquivo = "/public_html/public/entregas/versao.txt";
            FtpWebRequest request = CriarFtpRequest(caminhoArquivo);
            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string info = reader.ReadToEnd();
                    return ProcessarResposta(info);
                }
            }
            catch (WebException ex)
            {
                throw new Exception("Erro ao conectar ao servidor FTP: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler versão do FTP: " + ex.Message);
            }
        }

        private FtpWebRequest CriarFtpRequest(string caminhoArquivo)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + this.ftpIPServidor + caminhoArquivo));
            request.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UsePassive = true;
            return request;
        }

        private int ProcessarResposta(string info)
        {
            // info = "2.5.8;TESTE;Update Vendedores Set Nome = 'DENISS' Where ID = 1";
            string[] parts = info.Split(new[] { ';' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new Exception("Arquivo de versão vazio ou inválido.");

            string versaoTexto = parts[0].Trim();
            this.Mensagem = parts.Length > 1 ? parts[1].Trim() : "";
            ProcessarComandos(parts.Length > 2 ? parts[2] : null);
            return int.Parse(versaoTexto.Replace(".", ""));
        }

        private void ProcessarComandos(string comandosTexto)
        {
            this.ComandosSQL = new List<string>();
            if (!string.IsNullOrEmpty(comandosTexto))
            {
                string[] comandos = comandosTexto.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string comando in comandos)
                {
                    string comandoTrimmed = comando.Trim();
                    if (!string.IsNullOrEmpty(comandoTrimmed))
                    {
                        this.ComandosSQL.Add(comandoTrimmed);
                    }
                }
            }
        }

        public bool Upload(string _nomeArquivo, string Caminho)
        {
            string Cam = Caminho.Replace(@"\", @"/");
            string caminhoArquivo = Path.Combine(Caminho, _nomeArquivo);
            FileInfo _arquivoInfo = new FileInfo(caminhoArquivo);
            string Suri = "ftp://" + this.ftpIPServidor + @"/" + Cam + _arquivoInfo.Name;
            FtpWebRequest requisicaoFTP;
            requisicaoFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
            requisicaoFTP.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
            requisicaoFTP.KeepAlive = false;
            requisicaoFTP.Method = WebRequestMethods.Ftp.UploadFile;
            requisicaoFTP.UseBinary = true;
            requisicaoFTP.ContentLength = _arquivoInfo.Length;
            FileStream fs = _arquivoInfo.OpenRead();
            bool sair = false;
            bool bReturn = false;
            while (sair == false)
            {
                string ret = this.UploadEmSi(requisicaoFTP, fs);
                if (ret == "")
                {
                    bReturn = true;
                    sair = true;
                }
                else
                {
                    if (ret.IndexOf("553") > 0)
                    {
                        string sUrlD = "ftp://" + this.ftpIPServidor + Cam;
                        FtpWebRequest requestCD = (FtpWebRequest)FtpWebRequest.Create(new Uri(sUrlD));
                        requestCD.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
                        requestCD.KeepAlive = false;
                        requestCD.Method = WebRequestMethods.Ftp.MakeDirectory;
                        requestCD.Credentials = new NetworkCredential("user", "pass");
                        try
                        {
                            using (var resp = (FtpWebResponse)requestCD.GetResponse())
                            {
                                Console.WriteLine(resp.StatusCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            bReturn = false;
                            sair = true;
                        }
                    }
                    else
                    {
                        bReturn = false;
                        sair = true;
                    }
                }
            }
            return bReturn;
        }

        internal bool UploadFile(string tempFile, string v)
        {
            throw new NotImplementedException();
        }

        private string UploadEmSi(FtpWebRequest requisicaoFTP, FileStream fs)
        {
            try
            {
                // Stream  para o qual o arquivo a ser enviado será escrito
                Stream strm = requisicaoFTP.GetRequestStream();

                int buffLength = 2048;
                byte[] buff = new byte[buffLength];

                // Lê a partir do arquivo stream, 2k por vez
                int tamanhoConteudo = fs.Read(buff, 0, buffLength);

                // ate o conteudo do stream terminar
                while (tamanhoConteudo != 0)
                {
                    // Escreve o conteudo a partir do arquivo para o stream FTP 
                    strm.Write(buff, 0, tamanhoConteudo);
                    tamanhoConteudo = fs.Read(buff, 0, buffLength);
                }

                // Fecha o stream a requisição
                strm.Close();
                fs.Close();
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public List<string> getComandos()
        {
            return this.ComandosSQL;
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

        public List<string> DownloadFileList(string nmPasta)
        {
            List<string> fileList = new List<string>();
            string fileListPath = "arquivos.txt";
            string Suri = $"ftp://{this.ftpIPServidor}/{nmPasta}/{fileListPath}";

            FtpWebRequest requisicaoFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
            requisicaoFTP.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
            requisicaoFTP.KeepAlive = false;
            requisicaoFTP.Method = WebRequestMethods.Ftp.DownloadFile;
            requisicaoFTP.UseBinary = true;

            try
            {
                using (FtpWebResponse respDown = (FtpWebResponse)requisicaoFTP.GetResponse())
                {
                    using (Stream responseStream = respDown.GetResponseStream())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            using (StreamReader reader = new StreamReader(memoryStream))
                            {
                                string content = reader.ReadToEnd();
                                string[] files = content.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string file in files)
                                {
                                    fileList.Add(file.Trim());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Loga($"Erro ao baixar lista de arquivos: {ex.Message}");
            }

            return fileList;
        }

        public bool DownloadAllFiles(string nmPasta, List<string> fileList, BackgroundWorker worker)
        {
            long totalSize = GetTotalFileSize(nmPasta, fileList);
            long downloadedSize = 0;

            foreach (string fileName in fileList)
            {
                string Suri = $"ftp://{this.ftpIPServidor}/{nmPasta}/{fileName}";
                FtpWebRequest requisicaoFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
                requisicaoFTP.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
                requisicaoFTP.KeepAlive = true;
                requisicaoFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                requisicaoFTP.UseBinary = true;

                try
                {
                    using (FtpWebResponse respDown = (FtpWebResponse)requisicaoFTP.GetResponse())
                    using (Stream responseStream = respDown.GetResponseStream())
                    using (FileStream fileStream = File.Create(fileName))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            downloadedSize += bytesRead;
                            int progress = (int)((downloadedSize * 100) / totalSize);
                            worker.ReportProgress(progress);
                        }
                    }

                    if (Path.GetExtension(fileName).ToLower() == ".exe" && !VerificarArquivoExecutavel(fileName))
                    {
                        File.Delete(fileName);
                        Log.Loga($"Erro: O arquivo executável {fileName} parece estar corrompido.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Loga($"Erro ao baixar o arquivo {fileName}: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        private long GetTotalFileSize(string nmPasta, List<string> fileList)
        {
            long totalSize = 0;
            foreach (string fileName in fileList)
            {
                string Suri = $"ftp://{this.ftpIPServidor}/{nmPasta}/{fileName}";
                FtpWebRequest sizeRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri(Suri));
                sizeRequest.Credentials = new NetworkCredential(this.ftpUsuarioID, this.ftpSenha);
                sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;

                try
                {
                    using (FtpWebResponse response = (FtpWebResponse)sizeRequest.GetResponse())
                    {
                        totalSize += response.ContentLength;
                    }
                }
                catch (Exception ex)
                {
                    Log.Loga($"Erro ao obter o tamanho do arquivo {fileName}: {ex.Message}");
                }
            }
            return totalSize;
        }

    }
}


