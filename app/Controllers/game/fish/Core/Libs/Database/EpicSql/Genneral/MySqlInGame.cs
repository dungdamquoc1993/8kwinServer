using MySql.Data.MySqlClient;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MySqlProcess.InGame
{
    public class Slot5History
    {
        public long GameSession { get; set; }
        public long Blind { get; set; }
        public byte[][] Slot { get; set; }
        public byte[] WinLines { get; set; }
        public JSONArray RequestLines { get; set; }
        public long WinCash { get; set; }
        public long TotalBet { get; set; }
        public string CreateTime { get; set; }
    }

    public class Slot3SubHistory
    {
        public long GameSession { get; set; }
        public long Blind { get; set; }
        public JSONNode Slot { get; set; }
        public JSONNode WinLines { get; set; }
        public long WinCash { get; set; }
        public string CreateTime { get; set; }
    }

    public class Slot5Histories
    {
        public int ErrorCode;
        public string ErrorMsg;
        public List<Slot5History> Histories;
        public Slot5Histories()
        {
            Histories = new List<Slot5History>();
        }
    }
    public class Slot3SubHistories
    {
        public int ErrorCode;
        public string ErrorMsg;
        public List<Slot3SubHistory> Histories;
        public Slot3SubHistories()
        {
            Histories = new List<Slot3SubHistory>();
        }
    }

    public class MySqlInGame
    {
        private static char[] slotSeperators = new char[] { '|' };
        public static async Task<Slot5Histories> GetSlot5Histories(long user_id, string gamename)
        {
            Slot5Histories res = new Slot5Histories() { ErrorCode = 0, ErrorMsg = "" };
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    string sql = string.Format("SELECT trans_id,blind,win_lines,slot,total_bet,win_cash,create_time,request_lines FROM slot5_histories WHERE user_id = {0} AND gamename = '{1}' order by trans_id desc LIMIT 50;", user_id, gamename);
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Slot5History his = new Slot5History();
                                his.GameSession = long.Parse(reader["trans_id"].ToString());
                                his.Blind = long.Parse(reader["blind"].ToString());
                                string lines = reader["win_lines"].ToString();
                                his.WinLines = string.IsNullOrEmpty(lines) ? new byte[0] : lines.Split(slotSeperators, StringSplitOptions.RemoveEmptyEntries).Select(l => Convert.ToByte(l)).ToArray();

                                string slot = reader["slot"].ToString();
                                string[] arr_slot = slot.Split('|');
                                his.Slot = new byte[arr_slot.Count()][];
                                for (int i = 0; i < arr_slot.Count(); i++)
                                {
                                    his.Slot[i] = arr_slot[i].Split('-').Select(l => Convert.ToByte(l)).ToArray();
                                }
                                his.TotalBet = long.Parse(reader["total_bet"].ToString());
                                his.WinCash = long.Parse(reader["win_cash"].ToString());
                                var request_lines = reader["request_lines"].ToString();
                                if (string.IsNullOrEmpty(request_lines))
                                {
                                    his.RequestLines = new JSONArray();
                                }
                                else
                                {
                                    his.RequestLines = JSON.Parse(request_lines).AsArray;
                                }
                                his.CreateTime = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                res.Histories.Add(his);
                            }
                        }

                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                res.ErrorCode = 1;
                Logger.Error("Error in GetSlot5Histories: " + ex.ToString());
                return res;
            }
        }

        public static async Task<Slot3SubHistories> GetSlot3SubHistories(long user_id, string gamename)
        {
            Slot3SubHistories res = new Slot3SubHistories() { ErrorCode = 0, ErrorMsg = "" };
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    string sql = string.Format("SELECT trans_id,blind,win_lines,slot,win_cash,create_time FROM slot3_histories WHERE user_id={0} AND gamename='{1}' order by trans_id desc LIMIT 50;", user_id, gamename);
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Slot3SubHistory his = new Slot3SubHistory();
                                his.GameSession = long.Parse(reader["trans_id"].ToString());
                                his.Blind = long.Parse(reader["blind"].ToString());
                                string lines = reader["win_lines"].ToString();
                                his.WinLines = JSON.Parse(lines);

                                string slot = reader["slot"].ToString();
                                his.Slot = JSON.Parse(slot);
                                his.WinCash = long.Parse(reader["win_cash"].ToString());
                                his.CreateTime = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                res.Histories.Add(his);
                            }
                        }

                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                res.ErrorCode = 1;
                Logger.Error("Error in GetSlot3SubHistories: " + ex.ToString());
                return res;
            }
        }

        public static async Task<List<byte[]>> GetBigSmallAnalysis()
        {
            List<byte[]> list = new List<byte[]>();
            try
            {
                string sql = "SELECT dices FROM big_small_histoies order by trans_id desc LIMIT 100;";
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string dices = reader["dices"].ToString();
                                var arr = dices.Split('|').Select(d => byte.Parse(d)).ToArray();
                                list.Add(arr);
                            }
                        }
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetBigSmallAnalysis: " + ex.ToString());
                return list;
            }

        }

        public static async Task<BigSmallHistories> GetBigSmallHistories(long userId, int limit = 50)
        {
            BigSmallHistories res = new BigSmallHistories() { ErrorCode = 0 };
            var sql = string.Format(@"SELECT `big_small_transactions`.`game_session`,
`big_small_transactions`.`big_bet`,
`big_small_transactions`.`big_refund`,
`big_small_transactions`.`small_bet`,
`big_small_transactions`.`small_refund`,
`big_small_transactions`.`create_time`,
`big_small_histoies`.`dices`,
`big_small_transactions`.`win_cash` 
FROM `big_small_transactions` INNER JOIN `big_small_histoies` ON `big_small_transactions`.`game_session`=`big_small_histoies`.`trans_id`AND `big_small_transactions`.`user_id`={0} 
ORDER BY `big_small_transactions`.`game_session` DESC LIMIT {1}", userId, limit);
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                BigSmallHistory his = new BigSmallHistory();
                                his.GameSession = long.Parse(reader["game_session"].ToString());
                                his.TotalBigBet = long.Parse(reader["big_bet"].ToString());
                                his.TotalBigRefund = long.Parse(reader["big_refund"].ToString());
                                his.TotalSmallBet = long.Parse(reader["small_bet"].ToString());
                                his.TotalSmallRefund = long.Parse(reader["small_refund"].ToString());
                                string dices = reader["dices"].ToString();
                                his.Dices = dices == "" ? new byte[0] : dices.Split('|').Select(l => Convert.ToByte(l)).ToArray();
                                his.WinCash = long.Parse(reader["win_cash"].ToString());
                                his.CreateTime = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                res.Histories.Add(his);
                            }
                        }

                        return res;

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetBigSmallHistories: " + ex.ToString());
                res.ErrorCode = 1;
                return res;
            }
        }

        public static async Task<TopGames> GetTopGameSlot5(string gamename)
        {
            TopGames res = new TopGames();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = $"SELECT trans_id, blind, nickname, win_cash, create_time, description FROM slot5_glory WHERE gamename='{gamename}' ORDER BY trans_id DESC LIMIT 50";
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                TopGame his = new TopGame();
                                his.GameSession = long.Parse(reader["trans_id"].ToString());
                                his.Blind = long.Parse(reader["blind"].ToString());
                                his.Nickname = reader["nickname"].ToString();
                                his.WinCash = long.Parse(reader["win_cash"].ToString());
                                his.CreateTime = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                his.Description = reader["description"].ToString();
                                res.Tops.Add(his);
                            }
                        }

                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Get top mini game fail " + ex.ToString());
                return res;
            }
        }

        public static async Task<JSONArray> GetTopGameMinipoker()
        {
            var arr = new JSONArray();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = $"SELECT id, blind, nickname, win_cash, create_time, win_type FROM minipoker_glory ORDER BY id DESC LIMIT 50";
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var his = new JSONObject();
                                his["id"] = long.Parse(reader["id"].ToString());
                                his["blind"] = long.Parse(reader["blind"].ToString());
                                his["nickname"] = reader["nickname"].ToString();
                                his["winCash"] = long.Parse(reader["win_cash"].ToString());
                                his["createTime"] = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                his["winType"] = reader["win_type"].ToString();
                                arr.Add(his);
                            }
                        }

                        return arr;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Get top mini game fail " + ex.ToString());
                return arr;
            }
        }

        public static async Task<TopGames> GetTopGameSlot3Sub(string gamename)
        {
            TopGames res = new TopGames();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = $"SELECT trans_id, blind, nickname, win_cash, create_time, description FROM slot3_glory WHERE gamename='{gamename}' ORDER BY trans_id DESC LIMIT 50";
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                TopGame his = new TopGame();
                                his.GameSession = long.Parse(reader["trans_id"].ToString());
                                his.Blind = long.Parse(reader["blind"].ToString());
                                his.Nickname = reader["nickname"].ToString();
                                his.WinCash = long.Parse(reader["win_cash"].ToString());
                                his.CreateTime = DateTime.Parse(reader["create_time"].ToString()).ToString("dd-MM-yyyy HH:mm:ss");
                                his.Description = reader["description"].ToString();
                                res.Tops.Add(his);
                            }
                        }

                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Get top mini game fail " + ex.ToString());
                return res;
            }
        }
    }

    public class TopGame
    {
        public long GameSession { get; set; }
        public string Nickname { get; set; }
        public long Blind { get; set; }
        public long WinCash { get; set; }
        public string Description { get; set; }
        public string CreateTime { get; set; }

        public SimpleJSON.JSONNode ToJson()
        {
            var res = new SimpleJSON.JSONObject();
            res["GameSession"] = GameSession;
            res["Nickname"] = Nickname;
            res["Blind"] = Blind;
            res["WinCash"] = WinCash;
            res["Description"] = Description;
            res["CreateTime"] = CreateTime;
            return res;
        }
    }

    public class TopGames
    {
        public List<TopGame> Tops;
        public TopGames()
        {
            Tops = new List<TopGame>();
        }

        public SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();
            var res = new SimpleJSON.JSONArray();
            foreach (var item in Tops)
            {
                res.Add(item.ToJson());
            }
            data["data"] = res;
            return data;
        }
    }

    public class BigSmallHistory
    {
        public long GameSession { get; set; }
        public long TotalBigBet { get; set; }
        public long TotalSmallBet { get; set; }
        public long TotalBigRefund { get; set; }
        public long TotalSmallRefund { get; set; }
        public byte[] Dices { get; set; }
        public long WinCash { get; set; }
        public string CreateTime { get; set; }

        public SimpleJSON.JSONNode ToJson()
        {
            var res = new SimpleJSON.JSONObject();
            res["GameSession"] = GameSession;
            res["TotalBigBet"] = TotalBigBet;
            res["TotalSmallBet"] = TotalSmallBet;
            res["TotalBigRefund"] = TotalBigRefund;
            res["TotalSmallRefund"] = TotalSmallRefund;
            res["Dices"] = SimpleJSON.JSON.ListToJson(Dices);
            res["WinCash"] = WinCash;
            res["CreateTime"] = CreateTime;
            return res;
        }
    }

    public class BigSmallHistories
    {
        public int ErrorCode = 0;
        public List<BigSmallHistory> Histories;
        public BigSmallHistories()
        {
            Histories = new List<BigSmallHistory>();
        }

        public SimpleJSON.JSONNode ToJson()
        {
            var res = new SimpleJSON.JSONObject();
            res["ErrorCode"] = ErrorCode;
            var arr = new SimpleJSON.JSONArray();
            res["Histories"] = arr;
            foreach (var item in Histories)
            {
                arr.Add(item.ToJson());
            }
            return res;
        }
    }
}
