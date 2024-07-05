using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATCAtualizeitor
{
    public static class Log
    {
        public static void Loga(string message)
        {
            string logFilePath = @"C:\Entregas\Atualizador.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

    }
}
