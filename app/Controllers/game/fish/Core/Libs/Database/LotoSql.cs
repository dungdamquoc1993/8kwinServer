using BanCa.Redis;
using BanCa.Sql;
using MySql.Data.MySqlClient;
using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace LotoService
{
    public enum LotoLocation : int
    {
        MienBac = 0,
        MienTrung = 1,
        MienNam = 2
    }

    public enum LotoChannel : int
    {
        None = 0,

        MienBac = 1,

        PhuYen = 2,
        ThuaThienHue = 3,
        DakLak = 4,
        QuangNam = 5,
        DaNang = 6,
        BinhDinh = 7,
        QuangBinh = 8,
        QuangTri = 9,
        GiaLai = 10,
        NinhThuan = 11,
        DacNong = 12,
        QuangNgai = 13,
        KhanhHoa = 14,
        KonTum = 15,

        CaMau = 16,
        DongThap = 17,
        HCM = 18,
        BacLieu = 19,
        BenTre = 20,
        VungTau = 21,
        CanTho = 22,
        DongNai = 23,
        SocTrang = 24,
        AnGiang = 25,
        BinhThuan = 26,
        TayNinh = 27,
        BinhDuong = 28,
        TraVinh = 29,
        VinhLong = 30,
        BinhPhuoc = 31,
        HauGiang = 32,
        LongAn = 33,
        KienGiang = 34,
        TienGiang = 35,
        LamDong = 36
    }

    public enum LotoGameMode : int
    {
        None = 0,
        BaoLo2So = 1,
        BaoLo3So = 2,
        LoXien2 = 3,
        LoXien3 = 4,
        LoXien4 = 5,
        Dau = 6,
        Duoi = 7,
        DeDau = 8,
        DeDacBiet = 9,
        DanhDauDuoi = 10,
        BaCang = 11,
        BaCangDau = 12,
        BaCangDuoi = 13,
        BaCangDauDuoi = 14,
        LoTruotXien4 = 15,
        LoTruotXien8 = 16,
        LoTruotXien10 = 17,
        XiuChuDau = 18,
        XiuChuDuoi = 19,
        XiuChuDauDuoi = 20,
        Da2 = 21,
        Da3 = 22,
        Da4 = 23,
    }

    public class LotoSql
    {
        private static readonly HashSet<LotoChannel> LocationBac = new HashSet<LotoChannel>() {
            LotoChannel.MienBac
        };
        private static readonly HashSet<LotoChannel> LocationTrung = new HashSet<LotoChannel>() {
            LotoChannel.PhuYen,
            LotoChannel.ThuaThienHue,
            LotoChannel.DakLak,
            LotoChannel.QuangNam,
            LotoChannel.DaNang,
            LotoChannel.BinhDinh,
            LotoChannel.QuangBinh,
            LotoChannel.QuangTri,
            LotoChannel.GiaLai,
            LotoChannel.NinhThuan,
            LotoChannel.DacNong,
            LotoChannel.QuangNgai,
            LotoChannel.KhanhHoa,
            LotoChannel.KonTum,
        };
        private static readonly HashSet<LotoChannel> LocationNam = new HashSet<LotoChannel>() {
            LotoChannel.CaMau,
            LotoChannel.DongThap,
            LotoChannel.HCM,
            LotoChannel.BacLieu,
            LotoChannel.BenTre,
            LotoChannel.VungTau,
            LotoChannel.CanTho,
            LotoChannel.DongNai,
            LotoChannel.SocTrang,
            LotoChannel.AnGiang,
            LotoChannel.BinhThuan,
            LotoChannel.TayNinh,
            LotoChannel.BinhDuong,
            LotoChannel.TraVinh,
            LotoChannel.VinhLong,
            LotoChannel.BinhPhuoc,
            LotoChannel.HauGiang,
            LotoChannel.LongAn,
            LotoChannel.KienGiang,
            LotoChannel.TienGiang,
            LotoChannel.LamDong
        };

        private static readonly HashSet<LotoGameMode> arrNumber = new HashSet<LotoGameMode>() {
            LotoGameMode.LoXien2, LotoGameMode.LoXien3, LotoGameMode.LoXien4,
            LotoGameMode.LoTruotXien4, LotoGameMode.LoTruotXien8, LotoGameMode.LoTruotXien10,
            LotoGameMode.Da2, LotoGameMode.Da3, LotoGameMode.Da4
        };
        public static bool NeedArrayOfNumbers(LotoGameMode mode)
        {
            return arrNumber.Contains(mode);
        }

        public static int GetTodayTimestamp()
        {
            var now = DateTime.Now;
            return now.Year * 10000 + now.Month * 100 + now.Day;
        }

        private static ConcurrentDictionary<string, float> payRateCache = new ConcurrentDictionary<string, float>();
        public static void UpdatePayRate(LotoGameMode gameMode, LotoChannel channel, float rate)
        {
            var key = "loto_pay_rate_" + ((int)gameMode * 100 + (int)channel);
            payRateCache[key] = rate;
            var redis = RedisManager.GetRedis();
            redis.StringSet(key, rate, flags: StackExchange.Redis.CommandFlags.FireAndForget);
        }
        public static async Task<float> GetPayRate(LotoGameMode gameMode, LotoChannel channel)
        {
            var key = "loto_pay_rate_" + ((int)gameMode * 100 + (int)channel);
            if (payRateCache.TryGetValue(key, out var r)) return r;
            var redis = RedisManager.GetRedis();
            var rate = await redis.StringGetAsync(key);
            if (rate.HasValue)
            {
                var r2 = (float)rate;
                payRateCache[key] = r2;
                return r2;
            }

            var r3 = 0f;
            switch (gameMode)
            {
                case LotoGameMode.BaoLo2So:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  22;
                        if (LocationTrung.Contains(channel))
                            r3 =  15;
                        if (LocationNam.Contains(channel))
                            r3 =  15;
                    }
                    break;
                case LotoGameMode.BaoLo3So:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  23;
                        if (LocationTrung.Contains(channel))
                            r3 =  17;
                        if (LocationNam.Contains(channel))
                            r3 =  17;
                    }
                    break;
                case LotoGameMode.LoXien2:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.LoXien3:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.LoXien4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.Dau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.Duoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.DeDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  4;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.DeDacBiet:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.DanhDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  2;
                        if (LocationTrung.Contains(channel))
                            r3 =  2;
                        if (LocationNam.Contains(channel))
                            r3 =  2;
                    }
                    break;
                case LotoGameMode.BaCang:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.BaCangDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.BaCangDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.BaCangDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  2;
                        if (LocationTrung.Contains(channel))
                            r3 =  2;
                        if (LocationNam.Contains(channel))
                            r3 =  2;
                    }
                    break;
                case LotoGameMode.LoTruotXien4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.LoTruotXien8:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.LoTruotXien10:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.XiuChuDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.XiuChuDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.XiuChuDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  2;
                        if (LocationTrung.Contains(channel))
                            r3 =  2;
                        if (LocationNam.Contains(channel))
                            r3 =  2;
                    }
                    break;
                case LotoGameMode.Da2:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.Da3:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
                case LotoGameMode.Da4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  1;
                        if (LocationTrung.Contains(channel))
                            r3 =  1;
                        if (LocationNam.Contains(channel))
                            r3 =  1;
                    }
                    break;
            }
            payRateCache[key] = r3;
            return r3;
        }

        private static ConcurrentDictionary<string, float> winRateCache = new ConcurrentDictionary<string, float>();
        public static void UpdateWinRate(LotoGameMode gameMode, LotoChannel channel, float rate)
        {
            var key = "loto_win_rate_" + ((int)gameMode * 100 + (int)channel);
            winRateCache[key] = rate;
            var redis = RedisManager.GetRedis();
            redis.StringSet(key, rate, flags: StackExchange.Redis.CommandFlags.FireAndForget);
        }
        public static async Task<float> GetWinRate(LotoGameMode gameMode, LotoChannel channel)
        {
            var key = "loto_win_rate_" + ((int)gameMode * 100 + (int)channel);
            if (winRateCache.TryGetValue(key, out var r)) return r;
            var redis = RedisManager.GetRedis();
            var rate = await redis.StringGetAsync(key);
            if (rate.HasValue)
            {
                var r2 = (float)rate;
                winRateCache[key] = r2;
                return r2;
            }

            var r3 = 0f;
            switch (gameMode)
            {
                case LotoGameMode.BaoLo2So:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  81;
                        if (LocationTrung.Contains(channel))
                            r3 =  81;
                        if (LocationNam.Contains(channel))
                            r3 =  81;
                    }
                    break;
                case LotoGameMode.BaoLo3So:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  810;
                        if (LocationTrung.Contains(channel))
                            r3 =  810;
                        if (LocationNam.Contains(channel))
                            r3 =  810;
                    }
                    break;
                case LotoGameMode.LoXien2:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  15;
                        if (LocationTrung.Contains(channel))
                            r3 =  28;
                        if (LocationNam.Contains(channel))
                            r3 =  28;
                    }
                    break;
                case LotoGameMode.LoXien3:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  50;
                        if (LocationTrung.Contains(channel))
                            r3 =  150;
                        if (LocationNam.Contains(channel))
                            r3 =  150;
                    }
                    break;
                case LotoGameMode.LoXien4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  150;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.Dau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  9;
                        if (LocationTrung.Contains(channel))
                            r3 =  9;
                        if (LocationNam.Contains(channel))
                            r3 =  9;
                    }
                    break;
                case LotoGameMode.Duoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  9;
                        if (LocationTrung.Contains(channel))
                            r3 =  9;
                        if (LocationNam.Contains(channel))
                            r3 =  9;
                    }
                    break;
                case LotoGameMode.DeDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  83;
                        if (LocationTrung.Contains(channel))
                            r3 =  82;
                        if (LocationNam.Contains(channel))
                            r3 =  83;
                    }
                    break;
                case LotoGameMode.DeDacBiet:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  83;
                        if (LocationTrung.Contains(channel))
                            r3 =  83;
                        if (LocationNam.Contains(channel))
                            r3 =  83;
                    }
                    break;
                case LotoGameMode.DanhDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  85;
                        if (LocationTrung.Contains(channel))
                            r3 =  85;
                        if (LocationNam.Contains(channel))
                            r3 =  85;
                    }
                    break;
                case LotoGameMode.BaCang:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.BaCangDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  879;
                        if (LocationTrung.Contains(channel))
                            r3 =  879;
                        if (LocationNam.Contains(channel))
                            r3 =  879;
                    }
                    break;
                case LotoGameMode.BaCangDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  879;
                        if (LocationTrung.Contains(channel))
                            r3 =  879;
                        if (LocationNam.Contains(channel))
                            r3 =  879;
                    }
                    break;
                case LotoGameMode.BaCangDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.LoTruotXien4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  2.5f;
                        if (LocationTrung.Contains(channel))
                            r3 =  2;
                        if (LocationNam.Contains(channel))
                            r3 =  2;
                    }
                    break;
                case LotoGameMode.LoTruotXien8:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  8;
                        if (LocationTrung.Contains(channel))
                            r3 =  3.5f;
                        if (LocationNam.Contains(channel))
                            r3 =  3.5f;
                    }
                    break;
                case LotoGameMode.LoTruotXien10:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  11;
                        if (LocationTrung.Contains(channel))
                            r3 =  4.5f;
                        if (LocationNam.Contains(channel))
                            r3 =  4.5f;
                    }
                    break;
                case LotoGameMode.XiuChuDau:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.XiuChuDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.XiuChuDauDuoi:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
                case LotoGameMode.Da2:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  28;
                        if (LocationTrung.Contains(channel))
                            r3 =  28;
                        if (LocationNam.Contains(channel))
                            r3 =  28;
                    }
                    break;
                case LotoGameMode.Da3:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  150;
                        if (LocationTrung.Contains(channel))
                            r3 =  150;
                        if (LocationNam.Contains(channel))
                            r3 =  150;
                    }
                    break;
                case LotoGameMode.Da4:
                    {
                        if (LocationBac.Contains(channel))
                            r3 =  710;
                        if (LocationTrung.Contains(channel))
                            r3 =  710;
                        if (LocationNam.Contains(channel))
                            r3 =  710;
                    }
                    break;
            }

            winRateCache[key] = r3;
            return r3;
        }

        public static async Task<long> AddPlayRequest(string appId, string username, int session, LotoGameMode gameMode, string number, LotoChannel channel, long pay)
        {
            var today = GetTodayTimestamp();
            if (today > session) return 0; // ended

            var rate = await GetPayRate(gameMode, channel);
            var sql = string.Format("Insert into loto_request (AppId,Username,Session,GameMode,Number,Channel,Pay,PayRate,Status) values ('{0}','{1}',{2},{3},'{4}',{5},{6},{7},{8});",
                MySqlHelper.EscapeString(appId), MySqlHelper.EscapeString(username), session, (int)gameMode, MySqlHelper.EscapeString(number), (int)channel, pay, rate, 0); // 0 = pending
            SqlLogger.ExecuteNonQuery(sql);
            return (long)Math.Round(rate * pay);
        }
        private static void updatePlayRequest(ulong id, int status, long win, JSONArray winNumber = null)
        {
            var sql = string.Format("Update loto_request set Status={0},Win={1},WinNumber='{2}' where Id={3};", status, win, winNumber != null ? winNumber.ToString() : "[]", id);
            SqlLogger.ExecuteNonQuery(sql);
        }

        public static async Task<(int, long)> AddResult(int session, LotoChannel channel, JSONArray results, string timeResult)
        {
            var today = GetTodayTimestamp();
            if (today != session) return (0, 0); // invalid

            var sql = string.Format("Insert into loto_result (Session,Channel,ResultSp,Result1,Result2,Result3,Result4,Result5,Result6,Result7,Result8,TimeResult) values ({0},{1},'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}');",
                session, (int)channel,
                results.Count > 0 ? results[0].ToString() : string.Empty,
                results.Count > 1 ? results[1].ToString() : string.Empty,
                results.Count > 2 ? results[2].ToString() : string.Empty,
                results.Count > 3 ? results[3].ToString() : string.Empty,
                results.Count > 4 ? results[4].ToString() : string.Empty,
                results.Count > 5 ? results[5].ToString() : string.Empty,
                results.Count > 6 ? results[6].ToString() : string.Empty,
                results.Count > 7 ? results[7].ToString() : string.Empty,
                results.Count > 8 ? results[8].ToString() : string.Empty,
                MySqlHelper.EscapeString(timeResult));
            return await SqlLogger.ExecuteNonQueryAsync(sql);
        }

        private static readonly List<LotoRequest> emptyRequest = new List<LotoRequest>();
        private static readonly Dictionary<int, DayResult> dayResultsCache = new Dictionary<int, DayResult>();
        public static async Task<List<LotoRequest>> GetCalculateResult(int session, string appId, string username)
        {
            var res = new List<LotoRequest>();
            List<LotoRequest> totalResult = null;
            lock (dayResultsCache)
            {
                if (dayResultsCache.ContainsKey(session)) totalResult = dayResultsCache[session].Requests;
            }
            if (totalResult != null)
            {
                foreach (var item in totalResult)
                {
                    if (item.AppId == appId && (string.IsNullOrEmpty(username) || item.Username == username)) res.Add(item);
                }
                return res;
            }

            // no cache
            //var today = GetTodayTimestamp();
            //if (today <= session) // may no result yet
            //    return res;

            var today = session;
            var sql = string.Format("select * from loto_request where Session={0}", today);
            List<LotoRequest> requests = res;
            lock (dayResultsCache)
            {
                dayResultsCache[today] = new DayResult() { Requests = emptyRequest, Session = today };
            }
            try
            {
                using (MySqlConnection con = SqlLogger.getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var lotoRequest = new LotoRequest();
                                    lotoRequest.Id = reader.GetUInt64("Id");
                                    lotoRequest.AppId = reader["AppId"].ToString();
                                    lotoRequest.Username = reader["Username"].ToString();
                                    lotoRequest.Session = reader.GetInt32("Session");
                                    lotoRequest.GameMode = (LotoGameMode)reader.GetInt32("GameMode");
                                    lotoRequest.Number = reader["Number"].ToString().Trim();
                                    lotoRequest.Channel = (LotoChannel)reader.GetInt32("Channel");
                                    lotoRequest.Pay = reader.GetInt64("Pay");
                                    lotoRequest.PayRate = reader.GetFloat("PayRate");
                                    lotoRequest.Win = reader.GetInt64("Win");
                                    lotoRequest.Status = reader.GetInt32("Status");
                                    lotoRequest.TimePlay = reader["TimePlay"].ToString();
                                    lotoRequest.TimeUpdate = reader["TimeUpdate"].ToString();
                                    var wn = reader["WinNumber"].ToString();
                                    lotoRequest.WinNumber = string.IsNullOrEmpty(wn) ? new JSONArray() : JSON.Parse(wn).AsArray;
                                    requests.Add(lotoRequest);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("CalculateResult fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }
            lock (dayResultsCache)
            {
                dayResultsCache[today].Requests = requests;
            }

            res = new List<LotoRequest>();
            foreach (var item in requests)
            {
                if (item.AppId == appId && (string.IsNullOrEmpty(username) || item.Username == username)) res.Add(item);
            }
            return res;
        }
        public static async Task<List<LotoRequest>> CalculateResult()
        {
            var today = GetTodayTimestamp();
            return await CalculateResult(today);
        }
        public static async Task<List<LotoRequest>> CalculateResult(int session)
        {
            var today = session;
            lock (dayResultsCache)
            {
                if (dayResultsCache.ContainsKey(today)) return dayResultsCache[today].Requests;
            }
            var sql = string.Format("select * from loto_request where Session={0}", today);
            List<LotoRequest> requests = new List<LotoRequest>();
            lock (dayResultsCache)
            {
                dayResultsCache[today] = new DayResult() { Requests = emptyRequest, Session = today };
            }
            try
            {
                using (MySqlConnection con = SqlLogger.getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var lotoRequest = new LotoRequest();
                                    lotoRequest.Id = reader.GetUInt64("Id");
                                    lotoRequest.AppId = reader["AppId"].ToString();
                                    lotoRequest.Username = reader["Username"].ToString();
                                    lotoRequest.Session = reader.GetInt32("Session");
                                    lotoRequest.GameMode = (LotoGameMode)reader.GetInt32("GameMode");
                                    lotoRequest.Number = reader["Number"].ToString().Trim();
                                    lotoRequest.Channel = (LotoChannel)reader.GetInt32("Channel");
                                    lotoRequest.Pay = reader.GetInt64("Pay");
                                    lotoRequest.PayRate = reader.GetFloat("PayRate");
                                    lotoRequest.Win = reader.GetInt64("Win");
                                    lotoRequest.Status = reader.GetInt32("Status");
                                    lotoRequest.TimePlay = reader["TimePlay"].ToString();
                                    lotoRequest.TimeUpdate = reader["TimeUpdate"].ToString();
                                    var wn = reader["WinNumber"].ToString();
                                    lotoRequest.WinNumber = string.IsNullOrEmpty(wn) ? new JSONArray() : JSON.Parse(wn).AsArray;
                                    var result = await GetLotoResult(lotoRequest.Channel, lotoRequest.Session);
                                    if (result.Session == 0 || string.IsNullOrEmpty(result.ResultSp)) // no result yet
                                        continue;
                                    try
                                    {
                                        calculateResult(lotoRequest, result);
                                        updatePlayRequest(lotoRequest.Id, 1, lotoRequest.Win, lotoRequest.WinNumber); // done

                                        // TODO: fix
                                        if (lotoRequest.Win > 0)
                                        {
                                            await RedisManager.IncEpicCash(long.Parse(lotoRequest.Username), lotoRequest.Win, "server", "loto_win:" + lotoRequest.GameMode.ToString(), TransType.LOTO_WIN);
                                        }

                                        requests.Add(lotoRequest);
                                    }
                                    catch (Exception ex)
                                    {
                                        updatePlayRequest(lotoRequest.Id, 3, lotoRequest.Win); // error
                                        Logger.Error("Error in calculateResult: " + ex.ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("CalculateResult fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }
            lock (dayResultsCache)
            {
                dayResultsCache[today].Requests = requests;
            }
            return requests;
        }

        public static bool checkInputValid(LotoGameMode gameMode, JSONNode number)
        {
            switch (gameMode)
            {
                case LotoGameMode.BaoLo2So:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 2) return false;
                    }
                    break;
                case LotoGameMode.BaoLo3So:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 3) return false;
                    }
                    break;
                case LotoGameMode.LoXien2:
                case LotoGameMode.Da2:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 2) return false;
                        for(int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
                case LotoGameMode.LoXien3:
                case LotoGameMode.Da3:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 3) return false;
                        for (int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
                case LotoGameMode.LoXien4:
                case LotoGameMode.Da4:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 4) return false;
                        for (int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
                case LotoGameMode.Dau:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 1) return false;
                    }
                    break;
                case LotoGameMode.Duoi:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 1) return false;
                    }
                    break;
                case LotoGameMode.DeDau:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 2) return false;
                    }
                    break;
                case LotoGameMode.DeDacBiet:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 2) return false;
                    }
                    break;
                case LotoGameMode.DanhDauDuoi: // = DeDau + DeDacBiet
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 2) return false;
                    }
                    break;
                case LotoGameMode.BaCang:
                case LotoGameMode.BaCangDuoi:
                case LotoGameMode.XiuChuDuoi:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 3) return false;
                    }
                    break;
                case LotoGameMode.BaCangDau:
                case LotoGameMode.XiuChuDau:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 3) return false;
                    }
                    break;
                case LotoGameMode.BaCangDauDuoi: // = BaCangDau + BaCangDuoi
                case LotoGameMode.XiuChuDauDuoi:
                    {
                        if (number is JSONArray) return false;
                        if (number.Value.Length != 3) return false;
                    }
                    break;
                case LotoGameMode.LoTruotXien4:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 4) return false;
                        for (int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
                case LotoGameMode.LoTruotXien8:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 8) break;
                        for (int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
                case LotoGameMode.LoTruotXien10:
                    {
                        if (!(number is JSONArray)) return false;
                        var numbers = number.AsArray;
                        if (numbers.Count != 10) break;
                        for (int i = 0, n = numbers.Count; i < n; i++)
                        {
                            if (numbers[i].Value.Length != 2) return false;
                        }
                    }
                    break;
            }
            return true;
        }

        private static async Task calculateResult(LotoRequest request, LotoResult result)
        {
            if (request.Status != 0) return; // already has calculated
            request.Win = 0;
            switch (request.GameMode)
            {
                case LotoGameMode.BaoLo2So:
                    {
                        if (request.Number.Length != 2) break;
                        var total = result.GetTotalResults();
                        var winTime = 0;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > 1)
                                        arr.Add(item.Substring(item.Length - 2));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(request.Number))
                            {
                                winTime++;
                            }
                        }
                        if (winTime > 0)
                            request.Win = (long)Math.Round(winTime * request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.BaoLo3So:
                    {
                        if (request.Number.Length != 3) break;
                        var total = result.GetTotalResults();
                        var winTime = 0;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > 2)
                                        arr.Add(item.Substring(item.Length - 3));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(request.Number))
                            {
                                winTime++;
                            }
                        }
                        if (winTime > 0)
                            request.Win = (long)Math.Round(winTime * request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.LoXien2:
                case LotoGameMode.Da2:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 2) break;
                        var total = result.GetTotalResults();
                        bool match1 = false, match2 = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > numbers[0].Value.Length)
                                        arr.Add(item.Substring(item.Length - numbers[0].Value.Length));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                match1 = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                match2 = true;
                            }
                            if (match1 && match2)
                                break;
                        }
                        if (match1 && match2)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.LoXien3:
                case LotoGameMode.Da3:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 3) break;
                        var total = result.GetTotalResults();
                        bool match1 = false, match2 = false, match3 = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > numbers[0].Value.Length)
                                        arr.Add(item.Substring(item.Length - numbers[0].Value.Length));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                match1 = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                match2 = true;
                            }
                            if (item.EndsWith(numbers[2].Value))
                            {
                                match3 = true;
                            }
                            if (match1 && match2 && match3)
                                break;
                        }
                        if (match1 && match2 && match3)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.LoXien4:
                case LotoGameMode.Da4:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 4) break;
                        var total = result.GetTotalResults();
                        bool match1 = false, match2 = false, match3 = false, match4 = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > numbers[0].Value.Length)
                                        arr.Add(item.Substring(item.Length - numbers[0].Value.Length));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                match1 = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                match2 = true;
                            }
                            if (item.EndsWith(numbers[2].Value))
                            {
                                match3 = true;
                            }
                            if (item.EndsWith(numbers[3].Value))
                            {
                                match4 = true;
                            }
                            if (match1 && match2 && match3 && match4)
                                break;
                        }
                        if (match1 && match2 && match3 && match4)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.Dau:
                    {
                        if (request.Number.Length != 1) break;
                        if (!string.IsNullOrEmpty(result.ResultSp))
                        {
                            var arr = JSON.Parse(result.ResultSp);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > 1)
                                            arr2.Add(item[1].ToString());
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.Length > 1 && item[1] == request.Number[0])
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.Duoi:
                    {
                        if (request.Number.Length != 1) break;
                        if (!string.IsNullOrEmpty(result.ResultSp))
                        {
                            var arr = JSON.Parse(result.ResultSp);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > 0)
                                            arr2.Add(item[item.Length - 1].ToString());
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.Length > 0 && item[item.Length - 1] == request.Number[0])
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.DeDau:
                    {
                        if (request.Number.Length != 2) break;
                        var res = !string.IsNullOrEmpty(result.Result8) ? result.Result8 : (!string.IsNullOrEmpty(result.Result7) ? result.Result7 : "");
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > request.Number.Length)
                                            arr2.Add(item.Substring(item.Length - request.Number.Length));
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.DeDacBiet:
                    {
                        if (request.Number.Length != 2) break;
                        var res = result.ResultSp;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > request.Number.Length)
                                            arr2.Add(item.Substring(item.Length - request.Number.Length));
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.DanhDauDuoi: // = DeDau + DeDacBiet
                    {
                        if (request.Number.Length != 2) break;
                        var res = !string.IsNullOrEmpty(result.Result8) ? result.Result8 : (!string.IsNullOrEmpty(result.Result7) ? result.Result7 : "");
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > request.Number.Length)
                                            arr2.Add(item.Substring(item.Length - request.Number.Length));
                                    }

                                    if (!string.IsNullOrEmpty(result.ResultSp))
                                    {
                                        var arr3 = JSON.Parse(result.ResultSp);
                                        for (int i = 0, n = arr3.Count; i < n; i++)
                                        {
                                            var item = arr3[i].Value.Trim();
                                            if (item.Length > request.Number.Length)
                                                arr2.Add(item.Substring(item.Length - request.Number.Length));
                                        }
                                    }

                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    {
                        if (request.Number.Length != 2) break;
                        var res = result.ResultSp;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.BaCang:
                case LotoGameMode.BaCangDuoi:
                case LotoGameMode.XiuChuDuoi:
                    {
                        if (request.Number.Length != 3) break;
                        var res = result.ResultSp;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > 3)
                                            arr2.Add(item.Substring(item.Length - 3));
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.BaCangDau:
                case LotoGameMode.XiuChuDau:
                    {
                        if (request.Number.Length != 3) break;
                        var res = result.Result7;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > 3)
                                            arr2.Add(item.Substring(item.Length - 3));
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.BaCangDauDuoi: // = BaCangDau + BaCangDuoi
                case LotoGameMode.XiuChuDauDuoi:
                    {
                        if (request.Number.Length != 3) break;
                        var res = result.ResultSp;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            lock (result.WinResultByMode)
                            {
                                if (!result.WinResultByMode.ContainsKey(request.GameMode))
                                {
                                    var arr2 = result.WinResultByMode[request.GameMode] = new JSONArray();
                                    for (int i = 0, n = arr.Count; i < n; i++)
                                    {
                                        var item = arr[i].Value.Trim();
                                        if (item.Length > 3)
                                            arr2.Add(item.Substring(item.Length - 3));
                                    }

                                    if (!string.IsNullOrEmpty(result.Result7))
                                    {
                                        var arr3 = JSON.Parse(result.Result7);
                                        for (int i = 0, n = arr3.Count; i < n; i++)
                                        {
                                            var item = arr3[i].Value.Trim();
                                            if (item.Length > 3)
                                                arr2.Add(item.Substring(item.Length - 3));
                                        }
                                    }
                                }
                                request.WinNumber = result.WinResultByMode[request.GameMode];
                            }
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    {
                        if (request.Number.Length != 3) break;
                        var res = result.Result7;
                        if (!string.IsNullOrEmpty(res))
                        {
                            var arr = JSON.Parse(res);
                            for (int i = 0, n = arr.Count; i < n; i++)
                            {
                                var item = arr[i].Value.Trim();
                                if (item.EndsWith(request.Number))
                                    request.Win += (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                            }
                        }
                    }
                    break;
                case LotoGameMode.LoTruotXien4:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 4) break;
                        var total = result.GetTotalResults();
                        bool fail = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > 1)
                                        arr.Add(item.Substring(item.Length - 2));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[2].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[3].Value))
                            {
                                fail = true;
                            }
                            if (fail)
                                break;
                        }
                        if (!fail)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.LoTruotXien8:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 8) break;
                        var total = result.GetTotalResults();
                        bool fail = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > 1)
                                        arr.Add(item.Substring(item.Length - 2));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[2].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[3].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[4].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[5].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[6].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[7].Value))
                            {
                                fail = true;
                            }
                            if (fail)
                                break;
                        }
                        if (!fail)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
                case LotoGameMode.LoTruotXien10:
                    {
                        var numbers = JSON.Parse(request.Number);
                        if (numbers.Count != 10) break;
                        var total = result.GetTotalResults();
                        bool fail = false;
                        lock (result.WinResultByMode)
                        {
                            if (!result.WinResultByMode.ContainsKey(request.GameMode))
                            {
                                var arr = result.WinResultByMode[request.GameMode] = new JSONArray();
                                for (int i = 0, n = total.Count; i < n; i++)
                                {
                                    var item = total[i].Value.Trim();
                                    if (item.Length > 1)
                                        arr.Add(item.Substring(item.Length - 2));
                                }
                            }
                            request.WinNumber = result.WinResultByMode[request.GameMode];
                        }
                        for (int i = 0, n = total.Count; i < n; i++)
                        {
                            var item = total[i].Value.Trim();
                            if (item.EndsWith(numbers[0].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[1].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[2].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[3].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[4].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[5].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[6].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[7].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[8].Value))
                            {
                                fail = true;
                            }
                            if (item.EndsWith(numbers[9].Value))
                            {
                                fail = true;
                            }
                            if (fail)
                                break;
                        }
                        if (!fail)
                            request.Win = (long)Math.Round(request.Pay * await GetWinRate(request.GameMode, request.Channel));
                    }
                    break;
            }
        }

        private static readonly List<LotoResult> resultsCache = new List<LotoResult>();
        private static readonly HashSet<int> pendingQueries = new HashSet<int>();
        public static async Task<LotoResult> GetLotoResult(LotoChannel channel, int session)
        {
            lock (resultsCache)
            {
                foreach (var item in resultsCache)
                {
                    if (item.Channel == channel && item.Session == session) return item;
                }
            }
            var result = new LotoResult();
            var key = (int)channel + session * 100;
            lock (pendingQueries)
            {
                if (pendingQueries.Contains(key)) // on going, prevent concurent query
                {
                    result.Channel = channel;
                    result.Session = session;
                    return result;
                }
            }

            lock (pendingQueries) pendingQueries.Add(key);
            var sql = string.Format("select * from loto_result where Session={0}", session);
            try
            {
                using (MySqlConnection con = SqlLogger.getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            if (await reader.ReadAsync())
                            {
                                try
                                {
                                    result.Session = reader.GetInt32("Session");
                                    result.Channel = (LotoChannel)reader.GetInt32("Channel");
                                    result.ResultSp = reader["ResultSp"].ToString();
                                    result.Result1 = reader["Result1"].ToString();
                                    result.Result2 = reader["Result2"].ToString();
                                    result.Result3 = reader["Result3"].ToString();
                                    result.Result4 = reader["Result4"].ToString();
                                    result.Result5 = reader["Result5"].ToString();
                                    result.Result6 = reader["Result6"].ToString();
                                    result.Result7 = reader["Result7"].ToString();
                                    result.Result8 = reader["Result8"].ToString();
                                    result.TimeAdd = reader["TimeAdd"].ToString();
                                    result.TimeResult = reader["TimeResult"].ToString();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("CalculateResult fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }

            lock (resultsCache)
            {
                resultsCache.Add(result);
                if (resultsCache.Count > 54) resultsCache.RemoveAt(0);
            }
            lock (pendingQueries) pendingQueries.Remove(key);

            return result;
        }

        private static JSONArray gameModesCache = null;
        public static async Task<JSONArray> GetGameModes()
        {
            if (gameModesCache == null) gameModesCache = new JSONArray();
            else
            {
                lock (gameModesCache)
                {
                    return gameModesCache;
                }
            }

            var sql = "select * from loto_gamemode";
            var arr = new JSONArray();
            try
            {
                using (MySqlConnection con = SqlLogger.getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var result = new JSONObject();
                                    result["name"] = reader["Name"].ToString();
                                    result["help"] = reader["Help"].ToString();
                                    result["groupName"] = reader["GroupName"].ToString();
                                    result["group"] = reader.GetInt32("Group");
                                    result["location"] = reader.GetInt32("Location");
                                    result["gameMode"] = reader.GetInt32("GameMode");
                                    arr.Add(result);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("CalculateResult fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }

            gameModesCache = arr;

            return arr;
        }

        private static volatile int queryCount = 0;
        public static async Task<List<LotoRequest>> GetPlayRequest(string appId, string username)
        {
            if (queryCount > 32)
            {
                return null;
            }
            queryCount++;
            var sql = string.Format("select * from loto_request where AppId='{0}' and Username='{1}' order by Id desc limit 50", MySqlHelper.EscapeString(appId), MySqlHelper.EscapeString(username));
            List<LotoRequest> requests = new List<LotoRequest>();
            try
            {
                using (MySqlConnection con = SqlLogger.getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                    var lotoRequest = new LotoRequest();
                                    lotoRequest.Id = reader.GetUInt64("Id");
                                    lotoRequest.AppId = reader["AppId"].ToString();
                                    lotoRequest.Username = reader["Username"].ToString();
                                    lotoRequest.Session = reader.GetInt32("Session");
                                    lotoRequest.GameMode = (LotoGameMode)reader.GetInt32("GameMode");
                                    lotoRequest.Number = reader["Number"].ToString().Trim();
                                    lotoRequest.Channel = (LotoChannel)reader.GetInt32("Channel");
                                    lotoRequest.Pay = reader.GetInt64("Pay");
                                    lotoRequest.PayRate = reader.GetFloat("PayRate");
                                    lotoRequest.Win = reader.GetInt64("Win");
                                    lotoRequest.Status = reader.GetInt32("Status");
                                    lotoRequest.TimePlay = reader["TimePlay"].ToString();
                                    lotoRequest.TimeUpdate = reader["TimeUpdate"].ToString();
                                    var wn = reader["WinNumber"].ToString();
                                    lotoRequest.WinNumber = string.IsNullOrEmpty(wn) ? new JSONArray() : JSON.Parse(wn).AsArray;
                                    requests.Add(lotoRequest);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("CalculateResult fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }
            queryCount--;
            return requests;
        }

        public static void ClearCache()
        {
            gameModesCache = null;
            resultsCache.Clear();
            dayResultsCache.Clear();
        }
    }

    public class LotoResult
    {
        public int Session = 0;
        public LotoChannel Channel;
        public string ResultSp = string.Empty, Result1 = string.Empty, Result2 = string.Empty, Result3 = string.Empty, Result4 = string.Empty, Result5 = string.Empty, Result6 = string.Empty, Result7 = string.Empty, Result8 = string.Empty;
        public string TimeAdd = string.Empty, TimeResult = string.Empty;

        public readonly Dictionary<LotoGameMode, JSONArray> WinResultByMode = new Dictionary<LotoGameMode, JSONArray>();

        public JSONNode ToJson()
        {
            var res = new JSONObject();
            res["session"] = Session;
            res["channel"] = (int)Channel;
            if (!string.IsNullOrEmpty(ResultSp))
                res["resultSp"] = ResultSp;
            if (!string.IsNullOrEmpty(Result1))
                res["result1"] = Result1;
            if (!string.IsNullOrEmpty(Result2))
                res["result2"] = Result2;
            if (!string.IsNullOrEmpty(Result3))
                res["result3"] = Result3;
            if (!string.IsNullOrEmpty(Result4))
                res["result4"] = Result4;
            if (!string.IsNullOrEmpty(Result5))
                res["result5"] = Result5;
            if (!string.IsNullOrEmpty(Result6))
                res["result6"] = Result6;
            if (!string.IsNullOrEmpty(Result7))
                res["result7"] = Result7;
            if (!string.IsNullOrEmpty(Result8))
                res["result8"] = Result8;
            res["timeResult"] = TimeResult;
            return res;
        }

        private JSONArray cacheTotal;
        public JSONArray GetTotalResults()
        {
            if (cacheTotal != null) return cacheTotal;

            var arr = new JSONArray();
            if (!string.IsNullOrEmpty(ResultSp))
            {
                var result = JSON.Parse(ResultSp).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result1))
            {
                var result = JSON.Parse(Result1).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result2))
            {
                var result = JSON.Parse(Result2).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result3))
            {
                var result = JSON.Parse(Result3).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result4))
            {
                var result = JSON.Parse(Result4).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result5))
            {
                var result = JSON.Parse(Result5).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result6))
            {
                var result = JSON.Parse(Result6).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result7))
            {
                var result = JSON.Parse(Result7).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            if (!string.IsNullOrEmpty(Result8))
            {
                var result = JSON.Parse(Result8).AsArray;
                for (int i = 0, n = result.Count; i < n; i++)
                {
                    arr.Add(result[i].Value);
                }
            }
            cacheTotal = arr;
            return arr;
        }
    }

    public class LotoRequest : IJsonSerializable
    {
        public ulong Id;
        public string AppId;
        public string Username;
        public int Session;
        public LotoGameMode GameMode;
        public string Number;
        public LotoChannel Channel;
        public long Pay;
        public float PayRate;
        public long Win;
        public int Status;
        public string TimePlay;
        public string TimeUpdate;

        public JSONArray WinNumber;

        void IJsonSerializable.ParseJson(JSONNode data)
        {
            throw new NotImplementedException();
        }

        JSONNode IJsonSerializable.ToJson()
        {
            var res = new JSONObject();
            res["id"] = Id;
            res["appId"] = AppId;
            res["username"] = Username;
            res["session"] = Session;
            res["gameMode"] = (int)GameMode;
            if (int.TryParse(Number, out var _))
                res["number"] = Number;
            else
                res["number"] = JSON.Parse(Number); // arr
            res["channel"] = (int)Channel;
            res["pay"] = Pay;
            res["payRate"] = PayRate;
            res["win"] = Win;
            res["status"] = Status;
            res["timePlay"] = TimePlay;
            res["timeUpdate"] = TimeUpdate;
            res["winNumber"] = WinNumber;
            return res;
        }
    }

    public class DayResult
    {
        public List<LotoRequest> Requests;
        public int Session;
    }
}
