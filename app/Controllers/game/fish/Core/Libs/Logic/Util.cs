using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BanCa.Libs
{
    public class Util
    {
        private static readonly List<string> BlackChats = new List<string> {
"bác hồ",
"hồ chí minh",
"võ nguyên giáp",
"đảng",
"cộng sản",
"phú trọng",
"bí thư",
"chính trị",
"ủy viên",
"chủ tịch nước",
"quốc hội",
"thủ tướng",
"bộ trưởng",
"chính phủ",
"c50",
"công an",
"trung quốc",
"đa đảng",
"tổng thống",
"phản động",
"việt nam",
"đại tướng",
"cờ bạc",
"ma túy",
"thuốc phiện",
"heroin",
"lừa bịp",
"bán rẻ",
"đại lý",
"đổi thưởng",
"lừa đảo",
"đổi thẻ",
"tiền mặt",
"x2",
"x3",
"hút máu",
"bán cc",
"visa",
"atm",
"Bán",
"Nạp thẻ",
"GDTT",
"Giao dịch",
"zalo",
"091",
"092",
"093",
"094",
"095",
"096",
"097",
"098",
"012",
"088",
"mua"
};

        public static float Sin(float rad)
        {
            return (float)System.Math.Sin(rad);
        }

        public static float Cos(float rad)
        {
            return (float)System.Math.Cos(rad);
        }

        public static byte[] HmacSHA256(string data, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes(key)))
            {
                return hmac.ComputeHash(Encoding.ASCII.GetBytes(data));
            }
        }

        public static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static bool ChatFilter(string message)
        {
            message = message.ToLower();
            string message_convert = CharacterFilter(message);

            bool check = false;

            for (int i = 0; i < BlackChats.Count; i++)
            {
                if (message.Contains(BlackChats[i]) || message_convert.Contains(CharacterFilter(BlackChats[i])))
                {
                    check = true;
                    break;
                }
            }

            return check;
        }
        public static string CharacterFilter(string messsage)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = messsage.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
    }
}
