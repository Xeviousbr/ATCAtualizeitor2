using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Windows.Forms;

namespace RH
{
    public class VendedoresDAO : BaseDAO
    {        

        public int Id { get; set; }

        protected string _Nome;
        public string Nome
        {
            get { return _Nome; }
            set
            {
                _Nome = value;
            }
        }

        public string Loja { get; set; }

        public bool Atende { get; set; }

        public string Nro { get; set; }

        public string Usuario { get; set; }
        public string Senha { get; set; }
        public int Nivel { get; set; }
        

        public VendedoresDAO()
        {
            
        }
        public int getUsuarioNro(string Nro)
        {
            string query = "SELECT * FROM Vendedores WHERE Nro = '" + Nro + "'"; 
            DataTable ret = ExecutarConsultaVendedor(query);
            if (ret == null || ret.Rows.Count == 0)
            {
                return 0; 
            }
            else
            {
                this.Nome = Convert.ToString(ret.Rows[0]["Nome"]);
                return Convert.ToInt32(ret.Rows[0]["ID"]); 
            }
        }

        public override void Grava(object obj)
        {
            VendedoresDAO vendedor = (VendedoresDAO)obj;
            string query;
            List<OleDbParameter> parameters=null;
            int iAtende = vendedor.Atende ? 1 : 0;
            string sCript = "";
            if (vendedor.Senha.Length>0)
            {
                sCript = Cripto.Encrypt(vendedor.Senha);
            }
            if (vendedor.Adicao)
            {
                query = $"INSERT INTO Vendedores (Nome, Loja, Atende, Nro, Usuario, Senha, Nivel) " +
                    $"VALUES ('{vendedor.Nome}', '{vendedor.Loja}', {iAtende}, '{vendedor.Nro}','{vendedor.Usuario}' ,'{sCript}' ,{vendedor.Nivel} )";
            }
            else
            {                
                query = $"UPDATE Vendedores SET Nome = '{vendedor.Nome}', Loja = '{vendedor.Loja}', Atende = {iAtende}, Nro = '{vendedor.Nro}', Usuario = '{vendedor.Usuario}', Senha = '{sCript}', Nivel = {vendedor.Nivel} WHERE ID = {vendedor.Id} ";
            }
            try
            {
                DB.ExecutarComandoSQL(query, parameters);
            }
            catch (Exception ex)
            {
                string x = ex.ToString();
                MessageBox.Show(x, "Erro na operação do banco de dados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public DataTable ExecutarConsultaVendedor(string query)
        {
            glo.Loga("Dentro de ExecutarConsultaVendedor");
            if (glo.ODBC)
            {
                glo.Loga("ODBC");
                return ExecutarConsultaVendedorODBC(query);
            } else
            {
                glo.Loga("Não ODBC");
                return ExecutarConsultaVendedorADO(query);
            }
        }

        private DataTable ExecutarConsultaVendedorODBC(string query)
        {            
            using (OdbcConnection connection = new OdbcConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Columns.Add("ID", typeof(int));
                            dataTable.Columns.Add("Nome", typeof(string));
                            dataTable.Columns.Add("Loja", typeof(string));
                            dataTable.Columns.Add("Atende", typeof(string));
                            dataTable.Columns.Add("Nro", typeof(string));
                            dataTable.Columns.Add("Usuario", typeof(string));
                            dataTable.Columns.Add("Senha", typeof(string));
                            dataTable.Columns.Add("Nivel", typeof(int));

                            while (reader.Read())
                            {
                                DataRow row = dataTable.NewRow();
                                row["ID"] = reader.GetInt32(reader.GetOrdinal("ID"));
                                row["Nome"] = reader.GetString(reader.GetOrdinal("Nome"));
                                row["Loja"] = reader.GetString(reader.GetOrdinal("Loja"));
                                row["Atende"] = reader.GetString(reader.GetOrdinal("Atende"));
                                row["Nro"] = reader.GetString(reader.GetOrdinal("Nro"));
                                row["Usuario"] = reader.GetString(reader.GetOrdinal("Usuario"));
                                row["Senha"] = reader.GetString(reader.GetOrdinal("Senha"));
                                row["Nivel"] = reader.GetInt32(reader.GetOrdinal("Nivel"));
                                dataTable.Rows.Add(row);
                            }
                            return dataTable;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }
        }

        private DataTable ExecutarConsultaVendedorADO(string query)
        {
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            DataTable dataTable = new DataTable();
                            dataTable.Columns.Add("ID", typeof(int));
                            dataTable.Columns.Add("Nome", typeof(string));
                            dataTable.Columns.Add("Loja", typeof(string));
                            dataTable.Columns.Add("Atende", typeof(string));
                            dataTable.Columns.Add("Nro", typeof(string));
                            dataTable.Columns.Add("Usuario", typeof(string));
                            dataTable.Columns.Add("Senha", typeof(string));
                            dataTable.Columns.Add("Nivel", typeof(int));

                            while (reader.Read())
                            {
                                DataRow row = dataTable.NewRow();
                                row["ID"] = reader["ID"];
                                row["Nome"] = reader["Nome"];
                                row["Loja"] = reader["Loja"];
                                row["Atende"] = reader["Atende"];
                                row["Nro"] = reader["Nro"];
                                row["Usuario"] = reader["Usuario"];
                                row["Senha"] = reader["Senha"];
                                row["Nivel"] = reader["Nivel"];
                                dataTable.Rows.Add(row);
                            }
                            return dataTable;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }
        }

        public override IDataEntity GetEsse()
        {
            return (Vendedor)new Vendedor
            {
                Id = Id,
                Nome = Nome,
                Loja = Loja,
                Atende = Atende,
                Nro = Nro,

                Usuario = Usuario,
                Senha = Senha,
                Nivel = Nivel
            };
        }

        public override DataTable getDados()
        {
            string query = $"SELECT * FROM Vendedores";
            return ExecutarConsultaVendedor(query);
        }

        public override IDataEntity GetPeloID(string id)
        {
            string query = $"SELECT * FROM Vendedores Where ID = {id} ";
            return ExecutarConsultaVendedor2(query);
        }

        private Vendedor ExecutarConsultaVendedor2(string query)
        {            
            using (OleDbConnection connection = new OleDbConnection(glo.connectionString))
            {
                try
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Nome = reader["Nome"].ToString();
                                Id = Convert.ToInt32(reader["ID"]);
                                Loja = reader["Loja"].ToString();
                                object oAt = reader["Atende"];
                                if (oAt == DBNull.Value)
                                {
                                    Atende = false;
                                } else
                                {
                                    int iAt = Convert.ToInt32(oAt);
                                    Atende = !(iAt == 0);
                                }     
                                Nro = reader["Nro"].ToString();

                                Usuario = reader["Usuario"].ToString();
                                Senha = reader["Senha"].ToString();

                                object oNivel = reader["Nivel"];
                                if (oNivel == DBNull.Value)
                                {
                                    Nivel = 0;
                                } else
                                {
                                    Nivel = Convert.ToInt32(oNivel);
                                }                                    

                                return (Vendedor)GetEsse();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {                    
                    return null;
                }
            }
            return null;
        }

        public override IDataEntity Apagar(int direcao, IDataEntity entidade)
        {
            DB.ExecutarComandoSQL("DELETE FROM Vendedores WHERE ID = " + Id.ToString(), null);
            Vendedor proximocliente = direcao > -1 ? ParaFrente() as Vendedor : ParaTraz() as Vendedor;
            if (proximocliente == null || proximocliente.Id == 0)
            {
                proximocliente = direcao > -1 ? ParaTraz() as Vendedor : ParaFrente() as Vendedor;
            }
            return proximocliente ?? new Vendedor();
        }

        public override IDataEntity ParaFrente()
        {
            string query = $"SELECT TOP 1 * FROM Vendedores Where Nome > '{Nome}' ORDER BY Nome ";
            return ExecutarConsultaVendedor2(query);
        }

        public override IDataEntity ParaTraz()
        {
            string query = $"SELECT TOP 1 * FROM Vendedores Where Nome < '{Nome}' ORDER BY Nome Desc";
            return ExecutarConsultaVendedor2(query);
        }

        public override DataTable GetDadosOrdenados(string filtro="", string ordem ="")   
        {
            string query = $"SELECT * FROM Vendedores {filtro} Order By Nome {ordem}";
            return ExecutarConsultaVendedor(query);
        }

        public override string VeSeJaTem(object obj)
        {
            VendedoresDAO vendedor = (VendedoresDAO)obj;
            string wre = "";
            if (!vendedor.Adicao)
            {
                wre = " and ID <> " + vendedor.Id.ToString();
            }
            string queryNome = $"SELECT COUNT(*) FROM Vendedores WHERE Nome = '{vendedor.Nome}' " + wre;
            int countNome = DB.ExecutarConsultaCount(queryNome);
            if (countNome > 0)
            {
                return "Já existe um vendedor cadastrado com esse nome.";
            }
            return "";
        }

        public override void SetId(int iD)
        {
            this.Id = iD;
        }
        public override void SetNome(string nome)
        {
            this.Nome = nome;
        }

        public string getNome()
        {
            return this.Nome;
        }

        public override object GetUltimo()
        {
            string query = "SELECT TOP 1 * FROM Vendedores ORDER BY ID Desc";
            return ExecutarConsultaVendedor(query); 
        }
    }
}
