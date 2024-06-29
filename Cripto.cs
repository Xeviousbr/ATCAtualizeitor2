namespace ATCAtualizeitor
{
    public class Cripto
    {
        public static string Encrypt(string input)
        {
            string output = "";
            for (int i = 0; i < input.Length; i++)
            {
                int charCode = (int)input[i];
                int newCharCode = (charCode + i) % 256;
                output += (char)newCharCode;
            }
            return output;
        }

        public static string Decrypt(string input)
        {
            string output = "";
            for (int i = 0; i < input.Length; i++)
            {
                int charCode = (int)input[i];
                int newCharCode = (charCode - i) % 256;
                if (newCharCode < 0)
                {
                    newCharCode += 256;
                }
                output += (char)newCharCode;
            }
            return output;
        }

    }
}
