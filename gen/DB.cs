using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace RH
{
    public static class DB
    {
        public static DataTable ExecutarConsulta(string sQL)
        {
            DataTable dados;
            if (glo.ODBC)
            {
                dados = odbc.ExecutarConsulta(sQL);
            } else
            {
                dados = ADO.ExecutarConsulta(sQL);
            }
            return dados;
        }

        public static void ExecutarComandoSQL(string query, List<OleDbParameter> parameters = null)
        {
            if (glo.ODBC)
            {
                odbc.ExecutarComandoSQL(query, parameters);
            } else
            {
                ADO.ExecutarComandoSQL(query, parameters);
            }
        }

        public static int ExecutarConsultaCount(string query)
        {
            if (glo.ODBC)
            {
                return odbc.ExecutarConsultaCount(query);
            }
            else
            {
                return ADO.ExecutarConsultaCount(query);
            }
        }

    }
}
