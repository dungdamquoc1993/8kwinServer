using Entites;
using Entites.Cms;
using Entites.Payment;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace MySqlProcess.Genneral
{
    public class MySqlPayment
    {
        public static async Task<bool> CancelCashOut(long transId, long userId, long price)
        {
            string sql = string.Format("UPDATE cashouts SET `status`=2 WHERE `trans_id`={0} AND `user_id`={1} AND `status`=0 AND `price`={2} LIMIT 1", transId, userId, price);
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        return (await cmd.ExecuteNonQueryAsync()) != 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error CancelCashOut: " + ex.ToString());
                return false;
            }
        }

        public static async Task<CashoutHistories> GetCashOutHistories(string username, int versionCode)
        {
            int status = -1;
            int trans_id = 0;

            CashoutHistories res = new CashoutHistories() { error = 0, message = "" };
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_CashoutHistories", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_username", username);
                        cmd.Parameters.AddWithValue("@_status", status);
                        cmd.Parameters.AddWithValue("@_trans_id", trans_id);
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                CashoutHistory item = new CashoutHistory();
                                string itemId = reader["item_id"].ToString();
                                string telco = reader["telco"].ToString();
                                item.TransId = long.Parse(reader["trans_id"].ToString());
                                item.NumberCard = reader["number_card"].ToString();
                                if (telco == "Code")
                                {
                                    item.Seri = "code";
                                }
                                else
                                {
                                    //Logger.Info("Request code " + versionCode);
                                    if (versionCode < 2)
                                    {
                                        item.Seri = reader["seri"].ToString() + " MT:" + item.NumberCard;
                                    }
                                    else
                                    {
                                        item.Seri = reader["seri"].ToString();
                                    }
                                }

                                item.Price = long.Parse(reader["price"].ToString());
                                item.ItemId = string.IsNullOrEmpty(itemId) ? string.Format("{0} {1}", telco.ToUpper(), item.Price) : itemId;
                                int _status = int.Parse(reader["status"].ToString());
                                //switch (_status)
                                //{
                                //    case 0: item.Status = "Chờ duyệt"; break;
                                //    case 1: item.Status = "Đã duyệt"; break;
                                //    case 2: item.Status = "Hủy"; break;
                                //    case 3: item.Status = "Huy (khong hoan)"; break;
                                //    default: item.Status = "Chờ duyệt"; break;
                                //}
                                if (_status != 0 && _status != 1 && _status != 2) // default = cancel
                                    _status = 2;
                                item.Status = _status;
                                item.TimeCashout = DateTime.Parse(reader["time_cashout"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                res.Histories.Add(item);
                            }
                        }
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

        public static async Task<Tuple<string, long, byte>> GiftCode(string gift_code, long user_receiver, string phone_number)
        {
            long amount;
            byte error;
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_GIFT_CODE", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_gift_code", gift_code);
                        cmd.Parameters.AddWithValue("@_user_receiver", user_receiver);
                        cmd.Parameters.AddWithValue("@_phone_number", phone_number);
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            error = byte.Parse(reader["error"].ToString());
                            amount = long.Parse(reader["amount"].ToString());
                        }
                        return new Tuple<string, long, byte>("OK", amount, error);
                    }
                }
            }
            catch (Exception ex)
            {
                error = 99;
                amount = 0;
                return new Tuple<string, long, byte>(ex.ToString(), amount, error);
            }
        }

        public static async Task<SimpleJSON.JSONArray> GiftCodeHistory(long userId, int limit = 30)
        {
            var res = new SimpleJSON.JSONArray();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = string.Format("SELECT `gift_code`,`cash`,`use_time` FROM `gift_codes` WHERE `receiver`={0} ORDER BY `use_time` DESC LIMIT {1};", userId, limit);
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var data = new SimpleJSON.JSONObject();

                                data["gift_code"] = reader["gift_code"].ToString();
                                data["cash"] = reader.GetInt64("cash");
                                data["use_time"] = reader["use_time"].ToString();

                                res.Add(data);
                            }

                        }
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting gift code history: " + ex.ToString());
                return res;
            }
        }
    }
}
