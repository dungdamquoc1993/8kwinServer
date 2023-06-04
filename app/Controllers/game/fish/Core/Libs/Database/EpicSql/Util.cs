using System.Text;

namespace MySqlProcess
{
    public class Util
    {
        public static string GenString(string input)
        {
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            Encoding utf8 = Encoding.UTF8;
            byte[] utfBytes = utf8.GetBytes(input);
            byte[] isoBytes = Encoding.Convert(utf8, iso, utfBytes);
            string str = iso.GetString(isoBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if ((str[i] >= '0' && str[i] <= '9')
                    || (str[i] >= 'A' && str[i] <= 'z'
                        || (str[i] == '.' || str[i] == '_')))
                {
                    sb.Append(str[i]);
                }
            }
            return sb.ToString();
        }
    }
}
