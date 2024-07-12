using System;

namespace RH
{
    public class Vendedor : IDataEntity
    {
        public int Id { get; set; } 

        public string Nome { get; set; } 

        public string Loja { get; set; }

        public bool Adicao { get; set; }
        
        public bool Atende { get; set; }

        public string Nro { get; set; }

        public string Usuario { get; set; }
        public string Senha { get; internal set; }
        public int Nivel { get; set; }


    }
}
