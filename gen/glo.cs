// #define ODBC

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Data.Odbc;
using RH;

namespace RH
{
    public static class glo
    {
        private static string caminhoBase;

        public static int Nivel = 0;
        // 0 Balconista, ve só as faltas  
        // 1 Caixa
        // 2 Adm

        public static int iUsuario = 0;
        public static string NomeUser = "";
        public static bool ODBC = false;

        public static string CaminhoBase
        {
            get
            {
                if (string.IsNullOrEmpty(caminhoBase))
                {
                    INI MeuIni = new INI();
                    caminhoBase = MeuIni.ReadString("Config", "Base", "");
                }
                return caminhoBase;
            }
            set
            {
                caminhoBase = value;
                INI MeuIni = new INI();
                MeuIni.WriteString("Config", "Base", value);
            }
        }

        public static string connectionString
        {
            get
            {
                if (ODBC)
                {
                    glo.Loga("DSN=MbCarros;");
                    return "connectionString = 'DSN=MbCarros;' ";
                    // return "Driver={Microsoft Access Driver (*.mdb)};DBQ=\\SERVIDOR\\MbCarros\\MbCarros.mdb;";
                } else
                {
                    return @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + CaminhoBase + ";";
                }
            }
        }

        #region FormatacaoTela

        public static string fa(string str)
        {
            return "'" + str + "'";
        }


        #endregion

        public static void Loga(string message)
        {
            string logFilePath = @"C:\Entregas\Entregas.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                string Texto = $"{DateTime.Now}: {message}";
                writer.WriteLine(Texto);
                Console.WriteLine(Texto);
            }
        }

        #region DB

        public static DataTable getDados(string query)
        {
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return null;
        }

        public static string GenerateUID()
        {
            string dateTimePart = DateTime.Now.ToString("ddMMyyyyHHmmss");
            int QtdCarac = 20 - dateTimePart.Length;
            string randomChars = RandomString(QtdCarac);
            return dateTimePart + randomChars;
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}