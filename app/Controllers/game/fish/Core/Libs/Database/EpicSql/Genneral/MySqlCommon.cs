using BanCa.Sql;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySqlProcess.Genneral
{
    public class MySqlCommon
    {
        public static void ExecuteNonQuery(string query)
        {
            SqlLogger.ExecuteNonQuery(query);
            //try
            //{
            //    using (MySqlConnection con = MySqlConnect.Connection())
            //    {
            //        using (MySqlCommand cmd = new MySqlCommand(query, con))
            //        {
            //            try
            //            {
            //                cmd.CommandType = CommandType.Text;
            //                con.Open();
            //                cmd.ExecuteNonQuery();
            //            }
            //            catch (Exception ex)
            //            {
            //                var msg = ex.ToString();
            //                Logger.Error("Error in MySqlCommon.ExecuteNonQuery2 " + query + "\n" + msg);
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    var msg = ex.ToString();
            //    Logger.Error("Error in MySqlCommon.ExecuteNonQuery " + query + "\n" + msg);
            //}
        }

        public static async Task<string> ExecuteScalar(string query)
        {
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        try
                        {
                            cmd.CommandType = CommandType.Text;
                            await con.OpenAsync();
                            var _object = await cmd.ExecuteScalarAsync();
                            if (_object != null)
                            {
                                string result = _object.ToString();
                                return result;
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.ToString();
                            Logger.Error("Error in MySqlCommon.ExecuteScalar " + query + "\n" + msg);
                            return msg;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.ToString();
                Logger.Error("Error in MySqlCommon.ExecuteScalar " + query + "\n" + msg);
                return msg;
            }
        }

        public static string md5(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
