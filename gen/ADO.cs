using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace RH
{
    public static class ADO
    {
        public static DataTable ExecutarConsulta(string query)
        {
            DataTable dataTable = new DataTable();
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, connection))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    glo.Loga(ex.Message);
                    // Console.WriteLine(ex.Message);
                }
            }
            return dataTable;
        }

        public static void ExecutarComandoSQL(string query, List<OleDbParameter> parameters = null)
        {
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                    }
                    connection.Open();
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        glo.Loga(ex.ToString());
                        throw;
                    }
                }
            }
        }

        public static int ExecutarConsultaCount(string query)
        {
            int count = 0;
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        count = (int)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    // Tratamento de exceções adequado
                    throw;
                }
            }
            return count;
        }


    }
}
