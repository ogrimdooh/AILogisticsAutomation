using System;
using System.Text;

namespace AILogisticsAutomation
{
    public static class Base64Utils
    {

        //convert string para base64
        static public string EncodeToBase64(string texto)
        {
            try
            {
                byte[] textoAsBytes = Encoding.ASCII.GetBytes(texto);
                string resultado = System.Convert.ToBase64String(textoAsBytes);
                return resultado;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //convert string para base64
        static public bool TryEncodeToBase64(string data, out string decodeData)
        {
            decodeData = null;
            try
            {
                decodeData = EncodeToBase64(data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //converte de base64 para texto
        static public string DecodeFrom64(string dados)
        {
            try
            {
                byte[] dadosAsBytes = System.Convert.FromBase64String(dados);
                string resultado = System.Text.ASCIIEncoding.ASCII.GetString(dadosAsBytes);
                return resultado;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //converte de base64 para texto
        static public bool TryDecodeFrom64(string data, out string decodeData)
        {
            decodeData = null;
            try
            {
                decodeData = DecodeFrom64(data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }

}