using BanCa.Libs;
using BanCa.Redis;
using Database;
using Entites.General;
using MySql.Data.MySqlClient;
using MySqlProcess.Genneral;
using NCrontab;
using SimpleJSON;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BanCa.Redis.RedisManager;

namespace BanCa.Sql
{
    public enum Reason : int
    {
        TableStart = 0,
        TableEnd = 1,
        PlayerEnter = 2,
        PlayerLeave = 3,
        KillFish = 4,
        CashUpdate = 5,
        ItemActive = 6,
        TopWeekGift = 7,
        TopMonthGift = 8,
        Jackpot = 9,
        DailyCash = 10,
        GiftCode = 11,
        CardIn = 12,
        CardOut = 13,
        Iap = 14,
        Login = 15,
        Logout = 16,
        Slot5 = 17,
        CmsChangeCash = 18,
        Shoot = 19,
        TableStartSolo = 20,
        TableEndSolo = 21,
        BuyItem = 22
    }

    public class SqlLogger
    {
        public static MySqlConnection getConnection(string db = "")
        {
            if (string.IsNullOrEmpty(db)) db = ConfigJson.Config["mysql-defaul-db"].Value;
            string connetionString = ConfigJson.Config["mysql-connection"].Value;
            connetionString = string.Format(connetionString, db);
            MySqlConnection connect = new MySqlConnection(connetionString);
            return connect;
        }

        private static object lockbcLogBuffer = new object();
        private static List<string> bcLogBuffer = new List<string>(128);
        //private static List<string> bcFishLogBuffer = new List<string>(128);
        private static object lockbcTransBuffer = new object();
        private static List<string> bcTransBuffer = new List<string>(128);

        private static object locksqlBuffer = new object();
        private static List<string> sqlBuffer = new List<string>(128);

        private static object lock_sqlBuffer = new object();
        private static List<string> _sqlBuffer = new List<string>();

        private static System.Timers.Timer timer;
        private static System.Timers.Timer cleanUpTimer;
        public static void InitDb()
        {
            StringBuilder logSql = new StringBuilder(4096);
            timer = new System.Timers.Timer(50);

            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, a) =>
            {
                //string _sql = null;
                lock (logSql)
                {
                    logSql.Length = 0;
                    lock (lockbcLogBuffer)
                    {
                        if (bcLogBuffer.Count > 0)
                        {
                            logSql.Append("INSERT INTO `bc_log` (`TableId`,`TableBlind`,`UserId`,`Cash`,`ChangeCash`,`Reason`,`Time`,`ServerId`,`Item`,`Extra`) VALUES ");
                            for (int i = 0, n = bcLogBuffer.Count; i < n; i++)
                            {
                                logSql.Append(bcLogBuffer[i]);
                                logSql.Append(',');
                            }
                            logSql.Length = logSql.Length - 1;
                            logSql.Append(';');
                            bcLogBuffer.Clear();
                        }
                    }

                    //lock (bcFishLogBuffer)
                    //{
                    //    if (bcFishLogBuffer.Count > 0)
                    //    {
                    //        logSql.Append("INSERT INTO `bc_fish_log` (`TableId`,`TableBlind`,`UserId`,`Cash`,`ChangeCash`,`FishType`,`Time`,`ServerId`,`Item`) VALUES ");
                    //        for (int i = 0, n = bcFishLogBuffer.Count; i < n; i++)
                    //        {
                    //            logSql.Append(bcFishLogBuffer[i]);
                    //            logSql.Append(',');
                    //        }
                    //        logSql.Length = logSql.Length - 1;
                    //        logSql.Append(';');
                    //        bcFishLogBuffer.Clear();
                    //    }
                    //}

                    lock (locksqlBuffer)
                    {
                        if (sqlBuffer.Count > 0)
                        {
                            for (int i = 0, n = sqlBuffer.Count; i < n; i++)
                            {
                                logSql.Append(sqlBuffer[i]);
                            }
                            sqlBuffer.Clear();
                        }
                    }

                    lock (lockbcTransBuffer)
                    {
                        if (bcTransBuffer.Count > 0)
                        {
                            logSql.Append("INSERT INTO `bc_trans_log` (`Time`,`UserId`,`Cash`,`CashGain`,`Extra`,`Type`,`Money`,`Rate`) VALUES ");
                            for (int i = 0, n = bcTransBuffer.Count; i < n; i++)
                            {
                                logSql.Append(bcTransBuffer[i]);
                                logSql.Append(',');
                            }
                            logSql.Length = logSql.Length - 1;
                            logSql.Append(';');
                            bcTransBuffer.Clear();
                        }
                    }
                    string _sql = logSql.ToString();
                    if (!string.IsNullOrEmpty(_sql))
                    {
                        lock (lock_sqlBuffer) _sqlBuffer.Add(_sql);
                    }
                }
            });
            timer.Start();

            cleanUpTimer = new System.Timers.Timer(4 * 60 * 60 * 1000); // every 4h
            cleanUpTimer.Elapsed += new System.Timers.ElapsedEventHandler(async (s, a) =>
            {
                const string SQL = @"DELETE FROM `bc_log` WHERE Time < NOW() - INTERVAL 30 DAY;
DELETE FROM `big_small_histoies` WHERE create_time < NOW() - INTERVAL 100 DAY;
DELETE FROM `big_small_transactions` WHERE create_time < NOW() - INTERVAL 100 DAY;
DELETE FROM `slot5_glory` WHERE create_time < NOW() - INTERVAL 100 DAY;
DELETE FROM `slot5_histories` WHERE create_time < NOW() - INTERVAL 100 DAY;";
                await executeNonQueryAsync(SQL);
            });
            cleanUpTimer.Start();

            startProcessBuffer(1);
            startProcessBuffer(2);
        }
        private static void startProcessBuffer(int index)
        {
            Thread sqlHandler = new Thread(processBuffer);
            sqlHandler.Name = "SQL_Handler_" + index;
            sqlHandler.IsBackground = true;
            sqlHandler.Start();
        }
        private static async void processBuffer()
        {
            var buffer = new List<string>();
            while (true)
            {
                try
                {
                    buffer.Clear();
                    lock (lock_sqlBuffer)
                    {
                        buffer.AddRange(_sqlBuffer);
                        _sqlBuffer.Clear();
                    }

                    if (buffer.Count == 0) // run until finish job, continue immediately if not
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        for (int i = 0, n = buffer.Count; i < n; i++)
                        {
                            await executeNonQueryAsync(buffer[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("ProcessBuffer error: " + ex.ToString());
                }
            }
        }

        internal static async Task<int> executeNonQueryAsync(string query)
        {
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        try
                        {
                            cmd.CommandType = CommandType.Text;
                            await con.OpenAsync();
                            var res = await cmd.ExecuteNonQueryAsync();
                            con.Close();
                            return res;
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.ToString();
                            Logger.Error("Error in ExecuteNonQuery2: " + query + "\n" + msg);
                            con.Close();
                            con.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.ToString();
                Logger.Error("Error in ExecuteNonQuery: " + query + "\n" + msg);
            }
            return 0;
        }

        internal static void executeNonQuery(string query)
        {
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        try
                        {
                            cmd.CommandType = CommandType.Text;
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.ToString();
                            Logger.Error("Error in ExecuteNonQuery2: " + query + "\n" + msg);
                            con.Close();
                            con.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.ToString();
                Logger.Error("Error in ExecuteNonQuery: " + query + "\n" + msg);
            }
        }

        public static void ExecuteNonQuery(string query)
        {
            query = query.Trim();
            if (!query.EndsWith(";"))
            {
                query = query + ";";
            }
            lock (locksqlBuffer)
            {
                sqlBuffer.Add(query);
            }
        }

        /// <summary>
        /// Non query like insert or update, execute immediately but fire and forget, return (result, lastInsertedId)
        /// </summary>
        /// <param name="sql"></param>
        public static async Task<(int, long)> ExecuteNonQueryAsync(string sql)
        {
            var result = 0;
            long lastInsertedId = -1;
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        try
                        {
                            cmd.CommandType = CommandType.Text;
                            await con.OpenAsync();
                            result = await cmd.ExecuteNonQueryAsync();
                            lastInsertedId = cmd.LastInsertedId;
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.ToString();
                            Logger.Error(string.Format("Error in ExecuteNonQueryAsync sql |{0}| :\n{1}", sql, msg));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.ToString();
                Logger.Error(string.Format("Error in ExecuteNonQueryAsync2 sql |{0}| :\n{1}", sql, msg));
            }
            return (result, lastInsertedId);
        }

        private static CrontabSchedule MonthlyJob = CrontabSchedule.Parse("0 0 1 * *"); // monthly
        private static CrontabSchedule WeeklyJob = CrontabSchedule.Parse("0 0 * * 1"); // weekly at monday morning
        //private static CrontabSchedule MonthlyJob = CrontabSchedule.Parse("53 11 * * *"); // monthly
        //private static CrontabSchedule WeeklyJob = CrontabSchedule.Parse("54 11 * * *"); // weekly at monday morning
        private static DateTime nextMonthOcc = MonthlyJob.GetNextOccurrence(DateTime.Now.Subtract(TimeSpan.FromMinutes(15)));
        private static DateTime nextWeekOcc = WeeklyJob.GetNextOccurrence(DateTime.Now.Subtract(TimeSpan.FromMinutes(15)));

        public static bool? eventCashInEnded = null;

        // run every 1 minutes
        public static async void OnInterval()
        {
            try
            {
                var now = DateTime.Now;
                if (now >= nextWeekOcc) // weekly event trigger
                {
                    Logger.Info("Logging new weekly leaderboard...");
                    var logTime = nextWeekOcc;
                    nextWeekOcc = WeeklyJob.GetNextOccurrence(now.Add(TimeSpan.FromMinutes(15)));
                    var redis = RedisManager.GetRedis();
                    var leaderboard = await redis.SortedSetRangeByRankWithScoresAsync(RedisManager.PLAYER_CASH_WEEK_BC, start: 0, stop: 50, order: Order.Descending);
                    await redis.KeyDeleteAsync(RedisManager.PLAYER_CASH_WEEK_BC);
                    List<LeaderboardItem> items = new List<LeaderboardItem>();
                    var prizes = await RedisManager.GetTopPrize(true);
                    Logger.Info("This week prize " + prizes.ToString());
                    for (int i = 0, n = leaderboard.Length; i < n; i++)
                    {
                        var lb = leaderboard[i];
                        var username = lb.Element.ToString();
                        var cash = (long)lb.Score;
                        var player = await RedisManager.GetPlayer(username);
                        var userData = await MySqlUser.GetUserInfo(username);
                        var item = new LeaderboardItem
                        {
                            UserId = userData.error != 0 ? (player == null ? 0 : player.Id) : userData.UserId,
                            Username = username,
                            Nickname = userData.error != 0 || string.IsNullOrEmpty(userData.Nickname) ? (player == null ? username : player.Nickname) : userData.Nickname,
                            Avatar = userData.error != 0 || string.IsNullOrEmpty(userData.Avatar) ? (player == null ? "" : player.Avatar) : userData.Avatar,
                            Cash = player == null ? (userData.error != 0 ? 0L : userData.Cash) : player.Cash,
                            Level = player == null ? 1 : player.Level,
                            CashGain = cash,
                            Rank = i,
                            Prize = prizes.Count > i ? (string)prizes[i] : ""
                        };
                        long prizeCash = -1;
                        long.TryParse(item.Prize, out prizeCash);
                        if (prizeCash > -1 && item.UserId > 0)
                        {
                            await IncUserCash(item.UserId, prizeCash, userData.error != 0 ? userData.Platform : "", "WEEKLY_PRIZE_" + prizeCash, TransType.WEEKLY_PRIZE);
                            Logger.Info("This week give " + item.UserId + " prize cash " + prizeCash);
                            LogWeekMonthGift(true, item.UserId, item.Cash, prizeCash, item.Prize);
                        }
                        else
                        {
                            Logger.Info("This week give " + item.UserId + " prize " + item.Prize);
                            LogWeekMonthGift(true, item.UserId, item.Cash, 0, item.Prize);
                        }
                        var message = new JSONObject();
                        message["type"] = (int)MessageType.CashChangeByTopWeek;
                        var data = new JSONObject();
                        data["UserId"] = item.UserId;
                        data["Nickname"] = item.Nickname;
                        data["Score"] = item.CashGain;
                        data["Rank"] = item.Rank;
                        data["Prize"] = item.Prize;
                        message["data"] = data;
                        var _msg = message.ToString();
                        Logger.Info("Queue pending message " + item.UserId + " message " + _msg);
                        RedisManager.QueuePendingMessage(item.UserId, _msg);

                        items.Add(item);
                    }
                    SaveLeaderboard("bc_leaderboard_week", logTime, items);

                    lock (lockLb)
                    {
                        lbWeekCache.Clear();
                    }
                    Logger.Info("Logging new weekly leaderboard successfully");
                }

                if (now >= nextMonthOcc) // monthly event trigger
                {
                    Logger.Info("Logging new monthly leaderboard...");
                    var logTime = nextMonthOcc;
                    nextMonthOcc = MonthlyJob.GetNextOccurrence(now.Add(TimeSpan.FromMinutes(15)));
                    var redis = RedisManager.GetRedis();
                    var leaderboard = await redis.SortedSetRangeByRankWithScoresAsync(RedisManager.PLAYER_CASH_MONTH_BC, start: 0, stop: 50, order: Order.Descending);
                    await redis.KeyDeleteAsync(RedisManager.PLAYER_CASH_MONTH_BC);
                    List<LeaderboardItem> items = new List<LeaderboardItem>();
                    var prizes = await RedisManager.GetTopPrize(false);
                    Logger.Info("This month prize " + prizes.ToString());
                    for (int i = 0, n = leaderboard.Length; i < n; i++)
                    {
                        var lb = leaderboard[i];
                        var username = lb.Element.ToString();
                        var cash = (long)lb.Score;
                        var player = await RedisManager.GetPlayer(username);
                        var userData = await MySqlUser.GetUserInfo(username);
                        var item = new LeaderboardItem
                        {
                            UserId = userData.error != 0 ? (player == null ? 0 : player.Id) : userData.UserId,
                            Username = username,
                            Nickname = userData.error != 0 || string.IsNullOrEmpty(userData.Nickname) ? (player == null ? username : player.Nickname) : userData.Nickname,
                            Avatar = userData.error != 0 || string.IsNullOrEmpty(userData.Avatar) ? (player == null ? "" : player.Avatar) : userData.Avatar,
                            Cash = player == null ? (userData.error != 0 ? 0L : userData.Cash) : player.Cash,
                            Level = player == null ? 1 : player.Level,
                            CashGain = cash,
                            Rank = i,
                            Prize = prizes.Count > i ? (string)prizes[i] : ""
                        };
                        long prizeCash = -1;
                        long.TryParse(item.Prize, out prizeCash);
                        if (prizeCash > -1 && item.UserId > 0)
                        {
                            await IncUserCash(item.UserId, prizeCash, userData.error != 0 ? userData.Platform : "", "MONTHLY_PRIZE_" + prizeCash, TransType.MONTHLY_PRIZE);
                            Logger.Info("This month give " + item.UserId + " prize cash " + prizeCash);
                            LogWeekMonthGift(false, item.UserId, item.Cash, prizeCash, item.Prize);
                        }
                        else
                        {
                            Logger.Info("This month give " + item.UserId + " prize " + item.Prize);
                            LogWeekMonthGift(false, item.UserId, item.Cash, 0, item.Prize);
                        }
                        var message = new JSONObject();
                        message["type"] = (int)MessageType.CashChangeByTopMonth;
                        var data = new JSONObject();
                        data["UserId"] = item.UserId;
                        data["Nickname"] = item.Nickname;
                        data["Score"] = item.CashGain;
                        data["Rank"] = item.Rank;
                        data["Prize"] = item.Prize;
                        message["data"] = data;
                        var _msg = message.ToString();
                        Logger.Info("Queue pending message " + item.UserId + " message " + _msg);
                        RedisManager.QueuePendingMessage(item.UserId, _msg);
                        items.Add(item);
                    }
                    SaveLeaderboard("bc_leaderboard_month", logTime, items);
                    lock (lockLb)
                    {
                        lbMonthCache.Clear();
                    }
                    Logger.Info("Logging new monthly leaderboard successfully");

                    RedisManager.ClearDailyGifts();
                }

                await RedisManager.SetTopWeek(); // update top week
                await RedisManager.SetTopMonth(); // update top month

                // event cash in
                if (eventCashInEnded == null)
                {
                    var end = DateTime.Parse(await RedisManager.EventCashinEnd());
                    eventCashInEnded = end <= now;
                }

                if (!eventCashInEnded.Value)
                {
                    var end = DateTime.Parse(await RedisManager.EventCashinEnd());
                    if (end <= now)
                    {
                        eventCashInEnded = true;

                        // gift players
                        var prizes = await RedisManager.GetTopCashInPrize();
                        if (prizes != null && prizes.Count > 0)
                        {
                            Logger.Info("Logging event cashin gift...");
                            Logger.Info("This cashin prize " + prizes.ToString());
                            var leaderboard = await GetTopEventCashIn(prizes.Count);
                            for (int i = 0, n = leaderboard.Count; i < n; i++)
                            {
                                var lb = leaderboard[i];
                                var uid = lb["uid"].AsLong;
                                var total = lb["total"].AsLong;
                                var nickname = lb["nickname"].Value;
                                var prize = prizes.Count > i ? prizes[i].Value : "";
                                long prizeCash = -1;
                                long.TryParse(prize, out prizeCash);
                                if (prizeCash > -1 && uid > 0)
                                {
                                    await IncUserCash(uid, prizeCash, "system", "CashInTop", TransType.CASH_IN_GIFT);
                                    Logger.Info("Cash in event give " + uid + " prize cash " + prizeCash);
                                }
                                else
                                {
                                    Logger.Info("Cash in event give " + uid + " prize " + prizeCash);
                                }
                                var message = new JSONObject();
                                message["type"] = (int)MessageType.CashChangeByTopCashIn;
                                var data = new JSONObject();
                                data["UserId"] = uid;
                                data["Nickname"] = nickname;
                                data["Score"] = total;
                                data["Rank"] = i;
                                data["Prize"] = prize;
                                message["data"] = data;
                                var _msg = message.ToString();
                                Logger.Info("Queue pending message " + uid + " message " + _msg);
                                RedisManager.QueuePendingMessage(uid, _msg);
                            }
                            Logger.Info("Logging event cashin gift successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("On interval ex: " + ex.ToString());
            }
        }

        public class LeaderboardItem
        {
            public long UserId;
            public string Username;
            public string Nickname;
            public string Avatar;
            public long Cash;
            public int Level;
            public int Rank;
            public long CashGain;
            public string Prize;
        }
        static void SaveLeaderboard(string tableName, DateTime endTime, List<LeaderboardItem> lbItems)
        {
            if (lbItems.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append(@"INSERT INTO `");
                builder.Append(tableName);
                builder.Append("` (`UserId`,`Username`,`Nickname`,`Avatar`,`Cash`,`Level`,`Rank`,`CashGain`,`Time`,`Prize`) VALUES ");

                var end = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                for (int i = 0, n = lbItems.Count; i < n; i++)
                {
                    var item = lbItems[i];
                    builder.Append(string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}'),",
                        item.UserId, MySqlHelper.EscapeString(item.Username), MySqlHelper.EscapeString(item.Nickname),
                        item.Avatar, item.Cash, item.Level,
                        item.Rank, item.CashGain, end, item.Prize));
                }
                builder.Length = builder.Length - 1;
                builder.Append(';');
                var sql = builder.ToString();
                //Logger.Info("Saving leaderboard sql " + sql);
                ExecuteNonQuery(sql);
            }
        }

        private static object lockLb = new object();
        private static Dictionary<int, List<LeaderboardItem>> lbWeekCache = new Dictionary<int, List<LeaderboardItem>>();
        private static Dictionary<int, List<LeaderboardItem>> lbMonthCache = new Dictionary<int, List<LeaderboardItem>>();
        public static async Task<List<LeaderboardItem>> GetLeaderboardMonthOrWeek(bool isWeek, int index)
        {
            if (isWeek)
            {
                lock (lockLb)
                {
                    if (lbWeekCache.ContainsKey(index))
                    {
                        return lbWeekCache[index];
                    }
                }
            }
            else
            {
                lock (lockLb)
                {
                    if (lbMonthCache.ContainsKey(index))
                    {
                        return lbMonthCache[index];
                    }
                }
            }

            List<LeaderboardItem> lbItems = new List<LeaderboardItem>();
            if (index >= 0)
            {
                return lbItems;
            }
            var tableName = isWeek ? "bc_leaderboard_week" : "bc_leaderboard_month";
            var startTime = isWeek ? DateTime.Now.AddDays(index * 7) : DateTime.Now.AddMonths(index);
            var endTime = isWeek ? startTime.AddDays(7) : startTime.AddMonths(1);
            var query = @"SELECT * FROM `{0}` WHERE `Time`>='{1}' AND `Time`<='{2}';";
            query = string.Format(query, tableName, startTime.ToString("yyyy-MM-dd HH:mm:ss"), endTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //Logger.Info("Query top isweek " + isWeek + " index " + index + " \n" + query);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new LeaderboardItem
                                    {
                                        Avatar = reader["Avatar"].ToString(),
                                        Cash = reader.GetInt64("Cash"),
                                        CashGain = reader.GetInt64("CashGain"),
                                        Level = reader.GetInt32("Level"),
                                        Nickname = reader["Nickname"].ToString(),
                                        Prize = reader["Prize"].ToString(),
                                        Rank = reader.GetInt32("Rank"),
                                        UserId = reader.GetInt64("UserId"),
                                        Username = reader["Username"].ToString()
                                    };
                                    lbItems.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetLeaderboardMonthOrWeek fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            if (isWeek)
            {
                lock (lockLb)
                {
                    lbWeekCache[index] = lbItems;
                }
            }
            else
            {
                lock (lockLb)
                {
                    lbMonthCache[index] = lbItems;
                }
            }
            return lbItems;
        }

        public static string Md5(string input)
        {
            // Use input string to calculate MD5 hash
            var md5 = System.Security.Cryptography.MD5.Create();
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

        public static void SavePlayer(Player player)
        {
            var sql = @"INSERT INTO `bc_players` (`Id`,`PlayerId`,`Nickname`,`Avatar`,`Cash`,`Exp`,`Level`)
VALUES ({0}, '{1}', '{2}', '{3}', {4}, {5}, {6})
ON DUPLICATE KEY UPDATE PlayerId='{7}',Nickname='{8}',Avatar='{9}',Cash={10},Exp={11},Level={12}";
            sql = string.Format(sql, player.Id, MySqlHelper.EscapeString(player.PlayerId), MySqlHelper.EscapeString(player.Nickname), MySqlHelper.EscapeString(player.Avatar), player.Cash, player.Exp, player.Level,
                                                 MySqlHelper.EscapeString(player.PlayerId), MySqlHelper.EscapeString(player.Nickname), MySqlHelper.EscapeString(player.Avatar), player.Cash, player.Exp, player.Level);
            ExecuteNonQuery(sql);
            //Logger.Info("Save player sql: " + sql);
            //Logger.Info("Save player result: " + result);
        }

        public static async Task LoadImportantPlayerProperties(long id, Player player)
        {
            var query = @"SELECT * FROM `bc_players` WHERE `Id`={0} LIMIT 0,1";
            query = string.Format(query, id);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            try
                            {
                                await reader.ReadAsync();
                                long Id = long.Parse(reader["Id"].ToString());
                                string PlayerId = reader["PlayerId"].ToString();
                                string Nickname = reader["Nickname"].ToString();
                                string Avatar = reader["Avatar"].ToString();
                                long Cash = long.Parse(reader["Cash"].ToString());
                                long Exp = long.Parse(reader["Exp"].ToString());
                                int Level = int.Parse(reader["Level"].ToString());

                                player.Id = Id;
                                player.PlayerId = PlayerId;
                                player.Nickname = Nickname;
                                player.Avatar = Avatar;
                                player.Cash = Cash;
                                player.Exp = Exp;
                                player.Level = Level;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("LoadImportantPlayerProperties fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }
        }

        public static async Task<List<Player>> GetTopCash(int limit = 20)
        {
            var results = new List<Player>();
            var query = @"SELECT * FROM `bc_players` ORDER BY `Cash` DESC, `Level` DESC LIMIT 0," + limit;
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                //Logger.Info("Reading sql: " + query);
                                //Logger.Info("Reader has rows? " + reader.HasRows);
                                while (await reader.ReadAsync())
                                {
                                    long Id = long.Parse(reader["Id"].ToString());
                                    string PlayerId = reader["PlayerId"].ToString();
                                    string Nickname = reader["Nickname"].ToString();
                                    string Avatar = reader["Avatar"].ToString();
                                    long Cash = long.Parse(reader["Cash"].ToString());
                                    long Exp = long.Parse(reader["Exp"].ToString());
                                    int Level = int.Parse(reader["Level"].ToString());

                                    var player = new Player();
                                    player.Id = Id;
                                    player.PlayerId = PlayerId;
                                    player.Nickname = Nickname;
                                    player.Avatar = Avatar;
                                    player.Cash = Cash;
                                    player.Exp = Exp;
                                    player.Level = Level;
                                    results.Add(player);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTopCash fail: " + ex.ToString());
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            //Logger.Error("Top cash size: " + results.Count);
            return results;
        }

        public static async Task<int> GetRankByCash(long cash)
        {
            var query = @"SELECT COUNT(Id) AS `X` FROM `bc_players` WHERE `Cash`>" + cash;
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            if (reader.HasRows)
                            {
                                try
                                {
                                    await reader.ReadAsync();
                                    return int.Parse(reader["X"].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("GetRankByCash fail: " + ex.ToString());
                                }
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return -1;
        }

        public static async Task<List<Player>> GetTopLevel(int limit = 20)
        {
            var results = new List<Player>();
            var query = @"SELECT * FROM `bc_players` ORDER BY `Level` DESC, `Cash` DESC LIMIT 0," + limit;
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    long Id = long.Parse(reader["Id"].ToString());
                                    string PlayerId = reader["PlayerId"].ToString();
                                    string Nickname = reader["Nickname"].ToString();
                                    string Avatar = reader["Avatar"].ToString();
                                    long Cash = long.Parse(reader["Cash"].ToString());
                                    long Exp = long.Parse(reader["Exp"].ToString());
                                    int Level = int.Parse(reader["Level"].ToString());

                                    var player = new Player();
                                    player.Id = Id;
                                    player.PlayerId = PlayerId;
                                    player.Nickname = Nickname;
                                    player.Avatar = Avatar;
                                    player.Cash = Cash;
                                    player.Exp = Exp;
                                    player.Level = Level;
                                    results.Add(player);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTopLevel fail: " + ex.ToString());
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            //Logger.Error("Top level size: " + results.Count);
            return results;
        }

        public static async Task<int> GetRankByLevel(int level)
        {
            var query = @"SELECT COUNT(Id) AS `X` FROM `bc_players` WHERE `Level`>" + level;
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            if (reader.HasRows)
                            {
                                try
                                {
                                    await reader.ReadAsync();
                                    return int.Parse(reader["X"].ToString());
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("GetRankByLevel fail: " + ex.ToString());
                                }
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return -1;
        }

        public static async Task<JSONNode> GetAnnouncement(long userId, int systemMailMax = 10, int myMailMax = 10)
        {
            var myMails = new JSONArray();
            var query = @"SELECT * FROM `bc_announce` WHERE `UserId`={0} AND `Stat`!=2 ORDER BY `Time` DESC LIMIT {1}";
            query = string.Format(query, userId, myMailMax);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                //Logger.Info("Reading sql: " + query);
                                //Logger.Info("Reader has rows? " + reader.HasRows);
                                while (await reader.ReadAsync())
                                {
                                    long Id = reader.GetInt64("Id");
                                    string Time = reader["Time"].ToString();
                                    string Content = reader["Content"].ToString();
                                    int Type = reader.GetInt32("Type");
                                    int Stat = reader.GetInt32("Stat");

                                    var ann = new JSONObject();
                                    ann["Id"] = Id;
                                    ann["Time"] = Time;
                                    ann["Content"] = Content;
                                    ann["Type"] = Type;
                                    ann["Stat"] = Stat;
                                    myMails.Add(ann);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetAnnouncement fail: " + ex.ToString());
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            var allMails = new JSONArray();
            query = @"SELECT * FROM `bc_announce_all` WHERE `Stat`!=2 ORDER BY `Time` DESC LIMIT {0}";
            query = string.Format(query, systemMailMax);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                //Logger.Info("Reading sql: " + query);
                                //Logger.Info("Reader has rows? " + reader.HasRows);
                                while (await reader.ReadAsync())
                                {
                                    long Id = reader.GetInt64("Id");
                                    string Time = reader["Time"].ToString();
                                    string Content = reader["Content"].ToString();
                                    int Type = reader.GetInt32("Type");
                                    int Stat = reader.GetInt32("Stat");

                                    var ann = new JSONObject();
                                    ann["Id"] = Id;
                                    ann["Time"] = Time;
                                    ann["Content"] = Content;
                                    ann["Type"] = Type;
                                    ann["Stat"] = Stat;
                                    allMails.Add(ann);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetAnnouncement all fail: " + ex.ToString());
                            }
                        }
                        else
                        {
                            Logger.Info("Reader null for sql: " + query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            var results = new JSONObject();
            results["all"] = allMails;
            results["me"] = myMails;
            return results;
        }

        public static void DeleteAnnouncement(long userId, long annId) // delete mine only
        {
            // stat: 0-new, 1-read, 2-deleted
            string sql = "UPDATE `bc_announce` SET `Stat`=2 WHERE `UserId`={0} AND `Id`={1};";
            sql = string.Format(sql, userId, annId);
            ExecuteNonQuery(sql);
        }

        public static void ReadAnnouncement(long userId, long annId) // delete mine only
        {
            // stat: 0-new, 1-read, 2-deleted
            string sql = "UPDATE `bc_announce` SET `Stat`=1 WHERE `UserId`={0} AND `Id`={1};";
            sql = string.Format(sql, userId, annId);
            //Logger.Info("sql read: " + sql);
            ExecuteNonQuery(sql);
        }

        public static void FeedBack(long userId, string content)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "INSERT INTO `bc_feedback` (`Time`,`UserId`,`Content`) " +
                "VALUES ('{0}','{1}','{2}');";
            content = MySqlHelper.EscapeString(content); // prevent sql injection
            sql = string.Format(sql, date_now, userId, content);
            ExecuteNonQuery(sql);
        }

        public static string LogTableStart(int tableId, int tableBlindIndex, int serverId, bool solo)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'')";
            sql = string.Format(sql, tableId, tableBlindIndex, -1, -1, -1, (int)(solo ? Reason.TableStartSolo : Reason.TableStart), date_now, serverId, -1);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogTableEnd(int tableId, int tableBlindIndex, int serverId, bool solo)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'')";
            sql = string.Format(sql, tableId, tableBlindIndex, -1, -1, -1, (int)(solo ? Reason.TableEndSolo : Reason.TableEnd), date_now, serverId, -1);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogPlayerEnter(int tableId, int tableBlindIndex, long userId, long cash, Reason reason, int serverId, string meta)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, 0, (int)Reason.PlayerEnter, date_now, serverId, -1, MySqlHelper.EscapeString(meta));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogPlayerLeave(int tableId, int tableBlindIndex, long userId, long cash, int serverId, Config.QuitReason quitReason)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, 0, (int)Reason.PlayerLeave, date_now, serverId, -1, quitReason);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogCashUpdate(int tableId, int tableBlindIndex, long userId, long cash, long change, int serverId, Config.PowerUp item, long cashGain)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.CashUpdate, date_now, serverId, (int)item, cashGain);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogBuyItem(int tableId, int tableBlindIndex, long userId, long cash, long change, int serverId, Config.PowerUp item)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.BuyItem, date_now, serverId, (int)item);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogKillFish(int tableId, int tableBlindIndex, Config.BulletType type, long userId, long cash, long change, int serverId, Config.PowerUp item, Config.FishType fish, long cashGain)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.KillFish, date_now, serverId, (int)item, type + " " + fish + " " + cashGain);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogItemActive(int tableId, int tableBlindIndex, long userId, long cash, long change, int serverId, Config.PowerUp item)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.ItemActive, date_now, serverId, (int)item);
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static string LogJackpotEvent(int tableId, int tableBlindIndex, Config.BulletType type, long userId, long cash, long change, int serverId, string meta)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.Jackpot, date_now, serverId, (int)type, MySqlHelper.EscapeString(meta));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogDailyCash(long userId, long cash, long change, int serverId)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.DailyCash, date_now, serverId, (int)Config.PowerUp.None, "");
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogGiftCode(long userId, long cash, long change, string code)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.GiftCode, date_now, -1, (int)Config.PowerUp.None, MySqlHelper.EscapeString(code));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogCardIn(long userId, long cash, long change, string code)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.CardIn, date_now, -1, (int)Config.PowerUp.None, MySqlHelper.EscapeString(code));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogCardOut(long userId, long cash, long change, string code)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.CardOut, date_now, -1, (int)Config.PowerUp.None, MySqlHelper.EscapeString(code));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogIap(long userId, long cash, long change, string code)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.Iap, date_now, -1, (int)Config.PowerUp.None, MySqlHelper.EscapeString(code));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogCashChangeByCms(long userId, long cash, long change, string reason)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.CmsChangeCash, date_now, -1, (int)Config.PowerUp.None, MySqlHelper.EscapeString(reason));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogSlot5(long userId, long cash, long change, int serverId, string extra)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, change, (int)Reason.Slot5, date_now, serverId, (int)Config.PowerUp.None, MySqlHelper.EscapeString(extra));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogLogin(long userId, long cash, string meta)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, 0, (int)Reason.Login, date_now, -1, (int)Config.PowerUp.None, "LI: " + MySqlHelper.EscapeString(meta));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogLogout(long userId, long cash, string meta)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, -1, -1, userId, cash, 0, (int)Reason.Logout, date_now, -1, (int)Config.PowerUp.None, "LO: " + MySqlHelper.EscapeString(meta));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            ClearJpHistoriesCache();
            return sql;
        }

        public static string LogGameEvent(int tableId, int tableBlindIndex, long userId, long cash, long change, Reason reason, int serverId, Config.PowerUp item, string extra)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)reason, date_now, serverId, (int)item, MySqlHelper.EscapeString(extra));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        public static void LogCCU(int serverId, int ccu, long profit, int freeSlot)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "INSERT INTO `bc_ccu` (`Time`,`ServerId`,`CCU`,`Profit`,`FreeSlot`) " +
                "VALUES ('{0}',{1},{2},{3},{4});";
            sql = string.Format(sql, date_now, serverId, ccu, profit, freeSlot);
            ExecuteNonQuery(sql);
        }

        public static string LogWeekMonthGift(bool isWeek, long userId, long cash, long change, string gift)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
            sql = string.Format(sql, 0, 0, userId, cash, change, isWeek ? (int)Reason.TopWeekGift : (int)Reason.TopMonthGift, date_now, -1, -1, MySqlHelper.EscapeString(gift));
            lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            return sql;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cashGain"></param>
        /// <param name="tableId"></param>
        /// <param name="blind"></param>
        /// <param name="type">0 = normal, 1 = solo</param>
        public static void LogBcHistory(long userId, long cashGain, int tableId, int blind, int type)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "INSERT INTO `bc_history` (`UserId`,`Time`,`Cash`,`TableId`,`TableBlind`,`Type`) " +
                "VALUES ({0},'{1}',{2},{3},{4}, {5});";
            sql = string.Format(sql, userId, date_now, cashGain, tableId, blind, type);
            ExecuteNonQuery(sql);
        }

        public static async Task<JSONArray> GetBcHistory(long userId, int limit = 50)
        {
            string query = string.Format("SELECT `Time`,`Cash`,`TableId`,`TableBlind`,`Type` FROM `bc_history` WHERE `UserId`={0} ORDER BY `Time` DESC;", userId);
            var res = new SimpleJSON.JSONArray();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
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
                                    int TableBlind = reader.GetInt32("TableBlind");
                                    long Cash = reader.GetInt64("Cash");
                                    int TableId = reader.GetInt32("TableId");
                                    int Type = reader.GetInt32("Type");
                                    string Time = reader["Time"].ToString();
                                    var item = new SimpleJSON.JSONObject();
                                    item["TableBlind"] = TableBlind;
                                    item["Cash"] = Cash;
                                    item["TableId"] = TableId;
                                    item["Type"] = Type;
                                    item["Time"] = Time;
                                    res.Add(item);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("GetBcHistory fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return res;
        }

        private static ConcurrentDictionary<long, JSONArray> userHitFish = new ConcurrentDictionary<long, JSONArray>();
        public static void LogHitFish(Config.BulletType type, long userId, long cash, long change,
            int targetId, bool rapidFire, bool isAuto, Config.FishType fish, DateTime start, DateTime end, int fishId)
        {
            var json = new JSONObject();
            json["type"] = (int)type;
            json["cash"] = cash;
            json["change"] = change;
            json["targetId"] = targetId;
            json["rapidFire"] = rapidFire;
            json["isAuto"] = isAuto;
            json["fish"] = (int)fish;
            json["fishId"] = (int)fishId;
            json["start"] = start.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            json["end"] = end.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            JSONArray arr = null;
            if (userHitFish.TryGetValue(userId, out var a))
            {
                arr = a;
            }
            else
            {
                arr = userHitFish[userId] = new JSONArray();
            }
            arr.Add(json);
        }
        public static void LogBulletHitFish(int tableId, int tableBlindIndex, long userId, long cash, long change, int serverId, bool remove = false)
        {
            string extra = string.Empty;
            if (userHitFish.TryGetValue(userId, out var a))
            {
                extra = a.ToString();
                if (remove) userHitFish.TryRemove(userId, out var b); else a.Clear();
            }
            if (!string.IsNullOrEmpty(extra))
            {
                DateTime dateTime = DateTime.UtcNow;
                string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                string sql = "({0},{1},{2},{3},{4},{5},'{6}',{7},{8},'{9}')";
                sql = string.Format(sql, tableId, tableBlindIndex, userId, cash, change, (int)Reason.Shoot, date_now, serverId, -1, extra);
                lock (lockbcLogBuffer) bcLogBuffer.Add(sql);
            }
        }

        public static async Task<string> GetIapGooglePublicKey(string appId)
        {
            string query = string.Format("SELECT * FROM `bc_google_public_key` WHERE `AppId`='{0}' LIMIT 0,1;", MySqlHelper.EscapeString(appId));
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            try
                            {
                                await reader.ReadAsync();
                                string Key = reader["Key"].ToString();
                                return Key;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetIapGooglePublicKey fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return string.Empty;
        }

        public static async Task<JSONArray> GetIapItem(string appId, long versionCode)
        {
            string query = string.Format("SELECT * FROM `bc_iap` WHERE `AppId`='{0}' AND `VersionCodeMin`<={1};", MySqlHelper.EscapeString(appId), versionCode);
            var res = new SimpleJSON.JSONArray();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                // [{"Id":5,"AppId":"kiennt.app.banca","VersionCodeMin":0,"VersionCodeMax":0,"Cash":10000,"Price":10000,"Title":"IAP - 10000vnd","Description":"IAP - 10000vnd - 10000 coin","ProductId":"kiennt.app.banca.item1"}]
                                try
                                {
                                    string AppId = reader["AppId"].ToString();
                                    long VersionCodeMin = reader.GetInt64("VersionCodeMin");
                                    long VersionCodeMax = reader.GetInt64("VersionCodeMax");
                                    long Cash = reader.GetInt64("Cash");
                                    long Price = reader.GetInt64("Price");
                                    string Title = reader["Title"].ToString();
                                    string Description = reader["Description"].ToString();
                                    string ProductId = reader["ProductId"].ToString();

                                    if (VersionCodeMax <= VersionCodeMin || VersionCodeMax >= versionCode)
                                    {
                                        var item = new SimpleJSON.JSONObject();
                                        item["appId"] = AppId;
                                        item["cash"] = Cash;
                                        item["price"] = Price;
                                        item["title"] = Title;
                                        item["description"] = Description;
                                        item["productId"] = ProductId;
                                        res.Add(item);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("GetIapItem fail: " + ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return res;
        }

        public static void LogTransaction(long userId, long currentCash, long changeCash, string extra, int type, long money, float rate)
        {
            DateTime dateTime = DateTime.UtcNow;
            string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string sql = "('{0}',{1},{2},{3},'{4}',{5},{6},{7})";
            if (money != 0)
            {
                sql = string.Format(sql, date_now, userId, currentCash, changeCash, MySqlHelper.EscapeString(extra), type, money, rate);
            }
            else
            {
                sql = string.Format(sql, date_now, userId, currentCash, changeCash, MySqlHelper.EscapeString(extra), type, "NULL", "NULL");
            }
            lock (lockbcTransBuffer) bcTransBuffer.Add(sql);
        }

        public static void LogLoginStart(long user_id, string device_id, string platform, string ip)
        {
            // user_id	device_id	platform	ip	create_time
            string sql = "INSERT INTO `login_start` (`user_id`,`device_id`,`platform`,`ip`,`create_time`) " +
                "VALUES ({0},'{1}','{2}','{3}',now());";
            sql = string.Format(sql, user_id, MySqlHelper.EscapeString(device_id), MySqlHelper.EscapeString(platform), MySqlHelper.EscapeString(ip));
            ExecuteNonQuery(sql);
        }

        public static void LogLogoutStart(long user_id)
        {
            // user_id	device_id	platform	ip	create_time
            string sql = "UPDATE `login_start` SET `logout_time`=now() WHERE `user_id`={0} ORDER BY `create_time` DESC LIMIT 1;";
            sql = string.Format(sql, user_id);
            ExecuteNonQuery(sql);
        }

        public static void AddHacker(long userId, string time, HackType type, string extra)
        {
            string sql = "INSERT INTO `bc_hacker` (`UserId`,`Time`,`Extra`,`Type`) " +
                "VALUES ({0},'{1}','{2}','{3}');";
            sql = string.Format(sql, userId, MySqlHelper.EscapeString(time), MySqlHelper.EscapeString(extra), (int)type);
            ExecuteNonQuery(sql);
        }

        public static async Task IncUserCash(long userId, long cashChange, string platform, string reason, TransType reasonType)
        {
            var cash = await RedisManager.GetUserCash(userId);
            if (cash >= 0) // user exist in redis, inc
            {
                await RedisManager.IncEpicCash(userId, cashChange, MySqlHelper.EscapeString(platform), MySqlHelper.EscapeString(reason), reasonType);
                return;
            }

            // force save to db
            var targetUser = await MySqlUser.GetUserInfo(userId);
            if (targetUser != null)
            {
                long final = targetUser.Cash + cashChange;
                if (final < 0)
                {
                    final = 0;
                }
                MySqlUser.SaveCashToDb(userId, final);
            }
        }

        private static JSONArray jpHistoriesCache = null;
        private static object lockJp = new object();
        public static async Task<JSONArray> GetJackpotHistory(int limit = 20)
        {
            lock (lockJp)
            {
                if (jpHistoriesCache != null)
                {
                    return jpHistoriesCache;
                }
            }
            var jpHistories = new JSONArray();

            if (limit <= 0)
                limit = 1;
            else if (limit > 50)
                limit = 1;
            string query = string.Format("SELECT * FROM bc_log WHERE Reason={0} ORDER BY Id DESC LIMIT {1};", (int)Reason.Jackpot, limit);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    // {"Id":20516,"TableId":56,"TableBlind":1,"UserId":321174,"Cash":260011,"ChangeCash":0,"Reason":5,"Time":"2018-09-24T10:04:21.000Z","ServerId":6969,"Item":-1,"Extra":""}
                                    var item = new JSONObject();
                                    string Time = reader["Time"].ToString();
                                    long ChangeCash = reader.GetInt64("ChangeCash");
                                    string Extra = reader["Extra"].ToString();
                                    int TableBlind = reader.GetInt32("TableBlind");
                                    string nickname = Extra;
                                    //if (Extra.Length > 3)
                                    //{
                                    //    nickname = Extra.Substring(3);
                                    //}

                                    item["time"] = Time;
                                    item["blind"] = TableBlind;
                                    item["nickname"] = nickname;
                                    item["cash"] = ChangeCash;
                                    jpHistories.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetJackpotHistory fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            lock (lockJp)
            {
                jpHistoriesCache = jpHistories;
            }
            return jpHistories;

        }

        public static void ClearJpHistoriesCache()
        {
            lock (lockJp)
            {
                jpHistoriesCache = null;
            }
        }

        public static async Task<JSONArray> GetTopEventCashIn(int limit = 20)
        {
            if (limit <= 0)
                limit = 1;
            else if (limit > 50)
                limit = 1;
            var start = RedisManager.EventCashinStart();
            var end = RedisManager.EventCashinEnd();
            string query = string.Format("SELECT users.user_id, users.nickname, SUM(request_card.card_amount) AS total FROM request_card JOIN users WHERE request_card.created_time > '{0}' AND request_card.created_time < '{1}' AND request_card.app_user_id = users.user_id GROUP BY app_user_id ORDER BY total DESC LIMIT {2};", start, end, limit);
            var results = new JSONArray();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new JSONObject();
                                    string nickname = reader["nickname"].ToString();
                                    long total = reader.GetInt64("total");
                                    long user_id = reader.GetInt64("user_id");

                                    item["uid"] = user_id;
                                    item["total"] = total;
                                    item["nickname"] = nickname;
                                    results.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTopEventCashIn fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return results;
        }

        public static void LogTransferCash(long fromUserId, long toUserId, long cost, long gain)
        {
            string sql = "INSERT INTO `transfer_history` (`FromUserId`,`ToUserId`,`FromCash`,`ToCash`,`Time`) " +
                "VALUES ({0},'{1}','{2}','{3}',now());";
            sql = string.Format(sql, fromUserId, toUserId, cost, gain);
            ExecuteNonQuery(sql);
        }

        public static async Task<JSONArray> GetTransferHistory(long userId, int limit = 20)
        {
            var transHistories = new JSONArray();
            if (limit <= 0)
                limit = 20;
            else if (limit > 50)
                limit = 20;
            string query = string.Format("SELECT * FROM transfer_history WHERE FromUserId={0} OR ToUserId={0} ORDER BY Id DESC LIMIT {1};", userId, limit);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new JSONObject();
                                    string Time = reader["Time"].ToString();
                                    long FromUserId = reader.GetInt64("FromUserId");
                                    long ToUserId = reader.GetInt64("ToUserId");
                                    long FromCash = reader.GetInt64("FromCash");
                                    long ToCash = reader.GetInt64("ToCash");

                                    item["time"] = Time;
                                    item["fromUserId"] = FromUserId;
                                    item["toUserId"] = ToUserId;
                                    item["fromCash"] = FromCash;
                                    item["toCash"] = ToCash;
                                    transHistories.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTransferHistory fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return transHistories;
        }

        public static async Task<long> LogRequestCard(User user, JSONNode msg, string code, string serial, string telCode, string telco, int money, string provider_code, int platform)
        {
            var sql = @"INSERT INTO request_card (app_id, app_user_id, card_type, card_serial, card_pin, card_amount) VALUES('{0}', {1}, '{2}', '{3}', '{4}', {5});";
            sql = string.Format(sql, MySqlHelper.EscapeString(user.AppId), user.UserId, MySqlHelper.EscapeString(telco), MySqlHelper.EscapeString(serial), MySqlHelper.EscapeString(code), money);
            var tup = await ExecuteNonQueryAsync(sql);
            var result = tup.Item1;
            var id = tup.Item2;
            if (result != 0) // success
            {
                return id;
            }
            return -1;
        }

        public static async Task<JSONNode> GetRequestCard(string transId)
        {
            string query = string.Format("SELECT id, app_user_id, created_time, card_type, card_amount, app_status FROM request_card WHERE id='{0}' LIMIT 1;", MySqlHelper.EscapeString(transId));
            var res = new JSONObject();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    res["id"] = reader["id"].ToString();
                                    res["user_id"] = reader["app_user_id"].ToString();
                                    res["created_time"] = reader["created_time"].ToString();
                                    res["card_type"] = reader["card_type"].ToString();
                                    res["card_amount"] = reader["card_amount"].ToString();
                                    var status = reader.GetInt32("app_status");
                                    res["status"] = status == 0 ? 100 : status;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTransferHistory fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return res;
        }

        public static async Task<JSONNode> GetRequestCards(long userId)
        {
            string query = string.Format("SELECT created_time, card_type, card_amount, app_status FROM request_card WHERE app_user_id='{0}' LIMIT 20;", userId);
            var res = new JSONArray();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new JSONObject();
                                    item["created_time"] = reader["created_time"].ToString();
                                    item["card_type"] = reader["card_type"].ToString();
                                    item["card_amount"] = reader["card_amount"].ToString();
                                    var status = reader.GetInt32("app_status");
                                    item["status"] = status == 0 ? 100 : status;
                                    res.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetTransferHistory fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return res;
        }

        public static void UpdateRequestCard(string transId, bool success, string status, long menhgiathat)
        {
            string sql = "UPDATE `request_card` SET `app_status`={0}, `gw_status`={1}, `process_time`=now(), `gw_amount`={2} WHERE `id`={3};";
            sql = string.Format(sql, success ? 1 : -1, MySqlHelper.EscapeString(status), menhgiathat, MySqlHelper.EscapeString(transId));
            ExecuteNonQuery(sql);
        }

        public static async Task<long> LogBankCashout(User user, string BankName, string BankAccId, string BankAccName, long Cash, float cashOutRate, byte type) // 0: bank, 1: momo
        {
            var sql = @"INSERT INTO bank_momo_cash_out (BankName, BankAccId, BankAccName, UserId, Cash, CashOutRate, Type) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');";
            sql = string.Format(sql, MySqlHelper.EscapeString(BankName), MySqlHelper.EscapeString(BankAccId), MySqlHelper.EscapeString(BankAccName), user.UserId, Cash, cashOutRate, type);
            var tup = await ExecuteNonQueryAsync(sql);
            var result = tup.Item1;
            var id = tup.Item2;
            if (result != 0) // success
            {
                return id;
            }
            return -1;
        }

        public static async Task<JSONNode> GetBankLog(User user, byte type)
        {
            string query = string.Format("SELECT * FROM bank_momo_cash_out WHERE UserId='{0}' AND Type='{1}' ORDER BY Id DESC LIMIT 30;", user.UserId, type);
            var arr = new JSONArray();
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var res = new JSONObject();
                                    res["BankName"] = reader["BankName"].ToString();
                                    res["BankAccId"] = reader["BankAccId"].ToString();
                                    res["BankAccName"] = reader["BankAccName"].ToString();
                                    res["UserId"] = reader["UserId"].ToString();
                                    res["Cash"] = reader["Cash"].ToString();
                                    res["CreateTime"] = reader["CreateTime"].ToString();
                                    res["UpdateTime"] = reader["UpdateTime"].ToString();
                                    res["CashOutRate"] = reader["CashOutRate"].ToString();
                                    res["Status"] = reader.GetInt32("Status");
                                    res["Type"] = reader.GetInt32("Type");
                                    arr.Add(res);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("GetBankLog fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return arr;
        }

        public static async Task<JSONNode> CardOut(User user, string type, long amount)
        {
            string query = string.Format(@"
SET @LastUpdateID := -1;
UPDATE card SET status=1,user_id={0},updated_time=NOW(),updated_by='game',id=(SELECT @LastUpdateID := id) 
WHERE status=0 AND type='{1}' AND amount={2} LIMIT 1;
SELECT type,amount,pin,serial FROM card WHERE id=@LastUpdateID;
", user.UserId, type, amount);
            var res = default(JSONNode);
            try
            {
                using (MySqlConnection con = getConnection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    res = new JSONObject();
                                    res["type"] = reader["type"].ToString();
                                    res["amount"] = reader["amount"].ToString();
                                    res["pin"] = reader["pin"].ToString();
                                    res["serial"] = reader["serial"].ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("CardOut fail: " + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + query + "\n" + ex.ToString());
            }

            return res;
        }
    }
}
