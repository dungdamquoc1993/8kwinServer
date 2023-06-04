using Entites;
using Entites.General;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace MySqlProcess.Genneral
{
    public class MySqlAnnounce
    {
        public static async Task<Anns> GetAnnounces(long user_id, byte cate)
        {
            Anns res = new Anns() { error = 0, message = "" };
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_GetAnnounces", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_user_id", user_id);
                        cmd.Parameters.AddWithValue("@_cate", cate);
                        res.message = user_id + ":" + cate;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)(await cmd.ExecuteReaderAsync()))
                        {
                            while (await reader.ReadAsync())
                            {
                                Announce ann = new Announce();
                                ann.AnnouneId = int.Parse(reader["id"].ToString());
                                ann.Title = reader["title"].ToString();
                                ann.Content = reader["content"].ToString();
                                ann.Type = short.Parse(reader["type"].ToString());
                                ann.StartTime = DateTime.Parse(reader["time_start"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                ann.EndTime = DateTime.Parse(reader["time_end"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                res.Announces.Add(ann);
                            }
                            reader.Close();
                        }

                        con.Close();
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                res.error = 1;
                res.message = ex.ToString();
                return res;
            }
        }
    }
}
