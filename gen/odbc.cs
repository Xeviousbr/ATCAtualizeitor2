using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.Data.OleDb;

namespace RH
{
    public static class odbc
    {
        public static DataTable ExecutarConsulta(string sQL)
        {
            using (OdbcConnection connection = new OdbcConnection(glo.connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(sQL, connection))
                {
                    using (OdbcDataAdapter adapter = new OdbcDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }

        public static void ExecutarComandoSQL(string query, List<OleDbParameter> parameters)
        {
            using (OdbcConnection connection = new OdbcConnection(glo.connectionString))
            {
                using (OdbcCommand command = new OdbcCommand(query, connection))
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
            using (OdbcConnection connection = new OdbcConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return count;
        }
    }
}
