using BanCa.Libs;
using BanCa.Sql;
using BanCa.WebService;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using SimpleJSON;
using static BanCa.Libs.Config;
using Entites.General;
using System.Collections.Concurrent;
using System.Threading;
using Database;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace BanCa.Redis
{
    class Table
    {
        public int Id { get; set; }
        public int TableId { get; set; }
    }

    public class Server
    {
        public int Id { get; set; }
        public int Port { get; set; }
        public long TotalProfit { get; set; }
        public long TotalBank { get; set; }
    }

    public enum CashType : int
    {
        Card = 0, IapGoogle = 1, IapApple = 2, GiftCode = 3
    }

    public enum TransType : int
    {
        NONE = 0, BAN_CA = 1, CMS = 2, DAILY_LOGIN = 3, DAILY_LOGIN_FB = 4, DAILY_QUICK_LOGIN = 5, NEW_ACCOUNT = 6, SLOT5_PAY = 7, SLOT5_WIN = 8,
        GOOGLE_BILLING = 9, APPLE_BILLING = 10, CASH_OUT = 11, GIFT_CODE = 12, GIFT_CARD = 13, EMERGENCY = 14, MONTHLY_PRIZE = 15, WEEKLY_PRIZE = 16,
        TAIXIU_REFUND = 17, TAIXIU_BET = 18, VIDEO_ADS = 19, ADD_COIN_EVENT = 20, RELOGIN_EVENT = 21, DAILY_GOLD = 22, CARD_IN = 23, SOLO_FEE = 24,
        VIDEO_ADS_SKIP = 25, TAIXIU = 26, CASH_IN_GIFT = 27, CANCEL_CASH_OUT = 28, VERIFY_PHONE = 29, VERIFY_LOGIN_SMS = 30, VERIFY_TRANSFER_SMS = 31, TRANSFER_CASH = 32, MOMO_IN = 33,
        MOMO_OUT = 34, BANK_OUT = 35, MINIPOKER_PAY = 36, MINIPOKER_WIN = 37, CASH_SAVE = 38, SLOT3_SUB_PAY = 39, SLOT3_SUB_WIN = 40,
        SLOT5_25_PAY = 41, SLOT5_25_WIN = 42, SLOT3_EX_PAY = 43, SLOT3_EX_WIN = 44, SLOT3_PAY = 45, SLOT3_WIN = 46, SMS_IN = 47, ONE_TWO_THREE_PAY = 48, ONE_TWO_THREE_WIN = 49,
        XXENG_CHANGE_CASH = 50, LOTO_PAY = 51, LOTO_WIN = 52, COIN_PAYMENT_IPN = 53
    }

    public class CashInOutLog
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public long Cash { get; set; }
        public CashType CashType { get; set; }
        public DateTime Time { get; set; }
    }
    public enum MessageType : int
    {
        Raw = 0,
        CashChangeByAdmin = 1,
        CashChangeByTopWeek = 2,
        CashChangeByTopMonth = 3,
        CashChangeByTopCashIn = 4,
    }
    public class RedisManager
    {
        public volatile static List<int> ServerList;
        public volatile static List<int> WsServerList;
        private volatile static JSONNode originalConfig;
        private volatile static JSONNode originalLobbyConfig;
        private volatile static JSONNode originalTxConfig;
        private volatile static JSONNode originalBotConfig;

        private static ConnectionMultiplexer _redis = null;
        private static ConnectionMultiplexer[] _redisPools = new ConnectionMultiplexer[4];
        private static volatile int _currentRedis = 0;

        public const string KEY_MAINTAIN = "bc_maintain";
        public const string KEY_LOG_SERVER = "bc_server";
        public const string KEY_LOG_BLIND_SERVER = "bc_blindServer:";
        public const string CONFIG_BC = "bc_config";
        public const string CONFIG_LOBBY_BC = "bc_lobby_config";
        public const string CONFIG_TX_BC = "bc_tx_config";
        public const string CHAT_TX_BC = "bc_tx_chats";
        //public const string ALL_PLAYER_CASH_BC = "bc_allPlayerCash";
        public const string PLAYER_ONLINE_BC = "bc_playerOnline";
        public const string CRASH_PLAYER_CASH_BC = "bc_crash_playerCash";
        public const string PLAYER_DATA_BC = "bc_players:";
        public const string HOME_MESSAGES_BC = "bc_home_messages";

        public const string PLAYER_TOP_WEEK_BC = "bc_playerTopWeek";
        public const string PLAYER_TOP_MONTH_BC = "bc_playerTopMonth";
        public const string PLAYER_CASH_WEEK_BC = "bc_playerCashWeek";
        public const string PLAYER_CASH_MONTH_BC = "bc_playerCashMonth";
        public const string CURRENT_LEADERBOARD_WEEK_PRIZE_BC = "bc_prize_week";
        public const string CURRENT_LEADERBOARD_MONTH_PRIZE_BC = "bc_prize_month";

        public const string PENDING_MESSAGES_BC = "bc_pending_messages:";
        public const string PLAYER_SERVER_BC = "bc_player_server";

        public const string ITEM_SNIPE_BC = "bc_it_snipe:";
        public const string ITEM_RAPID_FIRE_BC = "bc_it_rfire:";

        public const string PROFIT_BC = "bc_profit";
        public const string FUND_BC = "bc_fund";
        public const string BOMB_FUND_BC = "bc_bomb_fund";
        public const string JACKPOT_BC = "bc_jackpot";

        public const string VIDEO_ADS_BC = "bc_video_ads:";
        public const string VIDEO_ADS_COUNT_BC = "bc_video_ads_count:";

        public const string DEVICE_ID_USERID = "bc_deviceid_userid"; // hash map, hold unique <device id, first account userid>
        //public const string DEVICE_ID_USERID2 = "bc_deviceid2_userid"; // hash map, hold unique <device id, first account userid>

        private static ConcurrentDictionary<int, ConcurrentDictionary<int, int>> freeSlotCache = new ConcurrentDictionary<int, ConcurrentDictionary<int, int>>(); // <server, <blind index, free slot>>
        private static volatile int maxCCU = 20; // redirect request to another server if current server is too crowd
        private static ConcurrentDictionary<int, int> ccuCache = new ConcurrentDictionary<int, int>();

        public const string BLACK_DID_BC = "bc_bdid";
        public const string BLACK_IP_BC = "bc_bip";
        private static HashSet<string> blackDeviceIds = new HashSet<string>();
        private static HashSet<string> blackIps = new HashSet<string>();
        private static object lockblackDeviceIds = new object();
        private static object lockblackIps = new object();

        public const string IAP_COUNT_BC = "bc_iap_count:";
        public const string CARD_IN_COUNT_BC = "bc_cardin_count:";
        public const string MOMO_COUNT_BC = "bc_momo_count:";
        public const string BANK_COUNT_BC = "bc_bank_count:";
        public const string TOTAL_USER_ONLINE_TIME_BC = "bc_online_time:";
        public const string PLAYING_BC_TABLE = "bc_tables:"; // <userId, tableId>

        public const string EVENT_CASHIN_START = "bc_cis";
        public const string EVENT_CASHIN_END = "bc_cie";
        public const string EVENT_CASHIN_GIFT = "bc_cig";

        public const string REGISTER_COUNT_IN_DAY = "bc_rgid:";

        public const string CASH_SAVE_KEY = "cash_save:{0}:{1}";

        public static void _init()
        {
            var address = ConfigJson.Config["redis-address"].Value;
            ConfigurationOptions option = new ConfigurationOptions
            {
                AbortOnConnectFail = true,
                EndPoints = { address },
                Password = ConfigJson.Config["redis-password"].Value
            };
            for (int i = 0; i < _redisPools.Length; i++)
            {
                _redisPools[i] = ConnectionMultiplexer.Connect(option);
            }
            _redis = _redisPools[_redisPools.Length - 1];
        }

        public static IDatabase GetRedis()
        {
            int cR = Interlocked.Increment(ref _currentRedis);
            if (cR < 0)
            {
                cR = 0;
                Interlocked.Exchange(ref _currentRedis, 0);
            }
            var redis = _redisPools[cR % _redisPools.Length];
            return redis.GetDatabase();
        }

        public static ISubscriber GetSubscriber()
        {
            return _redis.GetSubscriber();
        }

        //public static void IncEpicCashAsync(long user_id, long cash, string platform, string source, TransType reason)
        //{
        //    var redis = GetRedis();

        //    ThreadPool.QueueUserWorkItem((o) =>
        //    {
        //        //try
        //        //{
        //        //    string key = "User_Cash:{0}";
        //        //    //Logger.Log("Inc cash " + user_id + " " + cash);
        //        //    key = string.Format(key, user_id);

        //        //    var dataInRedis = redis.StringGet(key);
        //        //    if (string.IsNullOrEmpty(dataInRedis))
        //        //    {
        //        //        //res.Error = 2;
        //        //        Logger.Log("Account not exist " + user_id + " " + cash);
        //        //        return;
        //        //    }
        //        //    if (cash < 0)
        //        //    {
        //        //        var cashInRedis = 0L;
        //        //        if (long.TryParse(dataInRedis, out cashInRedis))
        //        //        {
        //        //            if (cashInRedis + cash < 0)
        //        //            {
        //        //                //res.Error = 1;
        //        //                //res.Message = "Số tiền không đủ để thực hiện";
        //        //                Logger.Log("Account not enough cash " + user_id + " " + cash + " vs " + cashInRedis);
        //        //                return;
        //        //            }
        //        //        }
        //        //    }

        //        //    redis.StringIncrement(key, cash);
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    //res.Error = 3;
        //        //    //res.Message = "Chức năng đang bảo trì.";
        //        //    Logger.Log("Update epic cash fail " + user_id + " " + cash + ":\n" + ex.ToString());
        //        //}
        //        IncEpicCash(user_id, cash, platform, source, reason);
        //    });
        //}

        //public static void LockEpicCash(long user_id)
        //{
        //    try
        //    {
        //        var redis = GetRedis();
        //        string key = "User_Cash:" + user_id;
        //        string keyLock = "User_Cash_Lock:" + user_id;
        //        key = string.Format(key, user_id);

        //        var dataInRedis = redis.StringGet(key);
        //        if (string.IsNullOrEmpty(dataInRedis))
        //        {
        //            redis.StringSet(key, "0");
        //            return;
        //        }

        //        redis.StringSet(key, "0");
        //        redis.StringSet(keyLock, dataInRedis);
        //        redis.KeyExpire(keyLock, DateTime.Now.AddDays(7));
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("LockEpicCash fail " + user_id + ":\n" + ex.ToString());
        //    }
        //}


        private static ConcurrentDictionary<long, long> userCashCache = new ConcurrentDictionary<long, long>();
        public static async Task<long> GetUserCash(long user_id)
        {
            long res = -1;
            if (userCashCache.TryGetValue(user_id, out res)) return res;

            string key = "User_Cash:{0}";
            key = string.Format(key, user_id);
            var redis = GetRedis();
            var cash = await redis.StringGetAsync(key);
            if (cash.IsNullOrEmpty)
            {
                return -1;
            }
            else
            {
                var _cash = -1L;
                if (long.TryParse(cash, out _cash))
                {
                    userCashCache[user_id] = _cash;
                    return _cash;
                }
                return _cash;
            }
        }

        public static void SetUserCash(long user_id, long cash)
        {
            string key = "User_Cash:{0}";
            key = string.Format(key, user_id);
            try
            {
                var redis = GetRedis();
                redis.StringSet(key, cash, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }

            userCashCache[user_id] = cash;
        }

        public static void ResetConfig()
        {
            if (originalConfig != null)
            {
                try
                {
                    Config.ParseJson(originalConfig);
                }
                catch (Exception ex)
                {
                    Logger.Error("Reset config fail: " + ex.ToString());
                }
            }
        }

        public static async void SaveConfig(bool saveToFile = true)
        {
            try
            {
                var config = Config.ToJsonString();
                var redis = GetRedis();
                redis.StringSet(CONFIG_BC, config, flags: CommandFlags.FireAndForget); // no interest in response

                if (saveToFile)
                    await WriteTextAsync("gameconfig.json", config);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task LoadConfig(bool createIfNotExist)
        {
            var redis = GetRedis();
            var data = await redis.StringGetAsync(CONFIG_BC);
            if (string.IsNullOrEmpty(data))
            {
                var path = "gameconfig.json";
                try
                {
                    if (File.Exists(path))
                    {
                        string json = await ReadTextAsync(path);
                        Config.ParseJson(JSON.Parse(json));
                        if (createIfNotExist)
                        {
                            SaveConfig(false);
                        }
                    }
                    else if (createIfNotExist)
                    {
                        SaveConfig();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Load default config fail: " + ex.ToString());
                }
            }
            else
            {
                try
                {
                    Config.ParseJson(JSON.Parse(data));
                }
                catch (Exception ex)
                {
                    Logger.Error("Load config fail: " + ex.ToString());
                }
            }

            if (originalConfig == null)
            {
                originalConfig = Config.ToJson();
            }
        }

        public static async void SaveLobbyConfig(bool saveToFile = true)
        {
            try
            {
                var config = LobbyConfig.ToJsonString();
                var redis = GetRedis();
                redis.StringSet(CONFIG_LOBBY_BC, config, flags: CommandFlags.FireAndForget); // no interest in response

                if (saveToFile)
                    await WriteTextAsync("lobbyconfig.json", config);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task LoadLobbyConfig(bool createIfNotExist)
        {
            var redis = GetRedis();
            var data = await redis.StringGetAsync(CONFIG_LOBBY_BC);
            if (string.IsNullOrEmpty(data))
            {
                var path = "lobbyconfig.json";
                try
                {
                    if (File.Exists(path))
                    {
                        string json = await ReadTextAsync(path);
                        LobbyConfig.ParseJson(JSON.Parse(json));
                        if (createIfNotExist)
                        {
                            SaveLobbyConfig(false);
                        }
                    }
                    else if (createIfNotExist)
                    {
                        SaveLobbyConfig();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Load default lobby config fail: " + ex.ToString());
                }
            }
            else
            {
                try
                {
                    LobbyConfig.ParseJson(JSON.Parse(data));
                }
                catch (Exception ex)
                {
                    Logger.Error("Load lobby config fail: " + ex.ToString());
                }
            }

            if (originalLobbyConfig == null)
            {
                originalLobbyConfig = LobbyConfig.ToJson();
            }
        }

        public static void LogServer(int id, int ccu, int freeSlot, long profit)
        {
            var data = string.Format("{0} {1} {2}", ccu, freeSlot, profit);
            try
            {
                var redis = GetRedis();
                redis.HashSet(KEY_LOG_SERVER, id, data, flags: CommandFlags.FireAndForget); // no interest in response
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static void LogBlindsServer(int id, int blindIndex, int ccu, int freeSlot, long profit)
        {
            var data = string.Format("{0} {1} {2}", ccu, freeSlot, profit);
            try
            {
                var redis = GetRedis();
                redis.HashSet(KEY_LOG_BLIND_SERVER + id, blindIndex, data, flags: CommandFlags.FireAndForget); // no interest in response
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task ClearLogBlind(int id)
        {
            var redis = GetRedis();
            await redis.KeyDeleteAsync(KEY_LOG_BLIND_SERVER + id);
            freeSlotCache[id] = new ConcurrentDictionary<int, int>();
            maxCCU = 20;
        }

        public static int getCache(int blindIndex, bool useWs, List<int> servers, List<int> wsServers)
        {
            for (int i = 0, n = servers.Count; i < n; i++)
            {
                var id = servers[i];
                if (freeSlotCache.TryGetValue(id, out var blinds)) // does cache contain server id
                {
                    if (blinds != null && blinds.TryGetValue(blindIndex, out var free)) // has free slot at that blind?
                    {
                        blinds[blindIndex] = free - 1;
                        if (free > 0)
                        {
                            return useWs && wsServers.Count > i ? wsServers[i] : id;
                        }
                    }
                }
            }

            return -1;
        }

        public static void updateCache(int id, int blindIndex, int freeSlot, int ccu)
        {
            //Logger.Info("update cache " + id + " " + blindIndex + " " + freeSlot + " " + ccu);
            ccuCache[id] = ccu;

            if (!freeSlotCache.ContainsKey(id))
            {
                freeSlotCache[id] = new ConcurrentDictionary<int, int>();
            }

            freeSlotCache[id][blindIndex] = freeSlot;
        }

        public static int GetBestFitServer(int blindIndex, bool useWs = false, List<int> servers = null, List<int> wsServers = null)
        {
            if (servers == null)
            {
                servers = ServerList;
            }
            if (servers == null)
            {
                return -1;
            }
            if (wsServers == null)
            {
                wsServers = WsServerList;
            }
            if (wsServers == null)
            {
                useWs = false;
            }

            var cacheResult = getCache(blindIndex, useWs, servers, wsServers);
            if (cacheResult != -1)
            {
                //Logger.Info("Best server from cache: " + cacheResult);
                return cacheResult;
            }

            int totalCCU = 0;
            int bestCCU = int.MaxValue;
            int bestServer = servers.Count > 0 ? servers[0] : -1;

            for (int i = 0, n = servers.Count; i < n; i++) // find server with lowest ccu
            {
                var id = servers[i];
                var ccu = 0;
                if (ccuCache.TryGetValue(id, out ccu))
                { }

                totalCCU += ccu;
                if (ccu < bestCCU)
                {
                    bestCCU = ccu;
                    bestServer = useWs && wsServers.Count > i ? wsServers[i] : id;
                }
            }

            if (totalCCU > maxCCU * servers.Count)
            {
                maxCCU = 2 * totalCCU / servers.Count;
            }
            else
            {
                maxCCU = 20;
            }

            // no cache
            //var redis = GetRedis();
            //for (int i = 0, n = servers.Count; i < n; i++) // find server with empty slot
            //{
            //    var _ccu = 0;
            //    var id = servers[i];
            //    lock (lockccuCache)
            //    {
            //        if (ccuCache.ContainsKey(id))
            //        {
            //            _ccu = ccuCache[id];
            //        }
            //    }
            //    var data = redis.HashGet(KEY_LOG_BLIND_SERVER + id, blindIndex);
            //    if (!string.IsNullOrEmpty(data))
            //    {
            //        var tokens = ((string)data).Split(' ');
            //        //var ccu = int.Parse(tokens[0]);
            //        var freeSlot = int.Parse(tokens[1]);
            //        //var profit = long.Parse(tokens[2]);

            //        if (freeSlot > 0) // priority fill empty slot first
            //        {

            //            if (_ccu < maxCCU)
            //            {
            //                updateCache(id, blindIndex, freeSlot - 1, _ccu);
            //                Logger.Info("Best server from redis: " + id);
            //                return id;
            //            }
            //            //else // too crowded
            //            //{
            //            //    updateCache(id, blindIndex, freeSlot, _ccu);
            //            //}
            //        }
            //        //else
            //        //{
            //        //    updateCache(id, blindIndex, freeSlot, _ccu);
            //        //}
            //    }
            //}

            //Logger.Info("Best server by minimum ccu: " + bestServer);
            return bestServer;
        }

        public static void UpdateCrashCash(string playerId, long cash)
        {
            try
            {
                var redis = GetRedis();
                redis.HashSet(CRASH_PLAYER_CASH_BC, playerId, cash, flags: CommandFlags.FireAndForget); // no interest in response
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static void LogCrashCash(IBatch batch, string playerId, long cash)
        {
            var str = string.Format("{0} {1}", TimeUtil.TimeStamp, cash);
            try
            {
                batch.HashSetAsync(CRASH_PLAYER_CASH_BC, playerId, str, flags: CommandFlags.FireAndForget); // save cash at crash time
                batch.HashDeleteAsync(PLAYER_ONLINE_BC, playerId, flags: CommandFlags.FireAndForget); // no longer online
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        //public static void UpdateCash(string playerId, long cash)
        //{
        //    try
        //    {
        //        var redis = GetRedis();
        //        redis.HashSet(ALL_PLAYER_CASH_BC, playerId, cash, flags: CommandFlags.FireAndForget);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error("Redis error: " + ex.ToString());
        //    }
        //}

        public static void IncTop(string username, long addCash)
        {
            if (addCash == 0) return;
            try
            {
                var redis = GetRedis();
                var batch = redis.CreateBatch();
                username = username.Trim().ToLower();
                batch.SortedSetIncrementAsync(PLAYER_CASH_WEEK_BC, username, addCash, flags: CommandFlags.FireAndForget); // leaderboard
                batch.SortedSetIncrementAsync(PLAYER_CASH_MONTH_BC, username, addCash, flags: CommandFlags.FireAndForget);
                batch.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            //Logger.Info("Inc top: " + username + " " + addCash);
        }

        public static async Task<JSONArray> GetTopPrize(bool isWeek)
        {
            var redis = GetRedis();
            var _prizes = await redis.StringGetAsync(isWeek ? CURRENT_LEADERBOARD_WEEK_PRIZE_BC : CURRENT_LEADERBOARD_MONTH_PRIZE_BC);
            return string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;
        }

        public static async void SetTopPrize(bool isWeek, JSONArray prizes)
        {
            try
            {
                var redis = GetRedis();
                await redis.StringGetSetAsync(isWeek ? CURRENT_LEADERBOARD_WEEK_PRIZE_BC : CURRENT_LEADERBOARD_MONTH_PRIZE_BC, prizes.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task SetTopWeek()
        {
            var redis = GetRedis();
            var leaderboard = await redis.SortedSetRangeByRankWithScoresAsync(PLAYER_CASH_WEEK_BC, start: 0, stop: 50, order: Order.Descending);
            var arr = new JSONArray();
            var _prizes = await redis.StringGetAsync(CURRENT_LEADERBOARD_WEEK_PRIZE_BC);
            var prizes = string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;
            for (int i = 0, n = leaderboard.Length; i < n; i++)
            {
                var lb = leaderboard[i];
                var item = new JSONObject();
                var username = lb.Element.ToString();
                var player = await GetPlayer(username);
                //if (player == null) // no data
                //{
                //    continue;
                //}
                var code = Math.Abs(username.GetHashCode());
                item["userId"] = player != null ? player.Id : 0;
                item["username"] = username;
                item["cash"] = lb.Score.ToString();
                item["avatar"] = player != null && player.Avatar != null ? player.Avatar : "" + (code % 6);
                item["level"] = player != null ? player.Level : 3 + code % 10;
                item["nickname"] = player != null && player.Nickname != null ? player.Nickname : username;
                item["prize"] = prizes.Count > i ? (string)prizes[i] : "";
                arr.Add(item);
            }
            await redis.StringSetAsync(PLAYER_TOP_WEEK_BC, arr.ToString());
        }
        public static async Task SetTopMonth()
        {
            var redis = GetRedis();
            var leaderboard = await redis.SortedSetRangeByRankWithScoresAsync(PLAYER_CASH_MONTH_BC, start: 0, stop: 50, order: Order.Descending);
            var arr = new JSONArray();
            var _prizes = await redis.StringGetAsync(CURRENT_LEADERBOARD_MONTH_PRIZE_BC);
            var prizes = string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;
            for (int i = 0, n = leaderboard.Length; i < n; i++)
            {
                var lb = leaderboard[i];
                var item = new JSONObject();
                var username = lb.Element.ToString();
                var player = await GetPlayer(username);
                //if (player == null) // no data
                //{
                //    continue;
                //}
                var code = Math.Abs(username.GetHashCode());
                item["userId"] = player != null ? player.Id : 0;
                item["username"] = username;
                item["cash"] = lb.Score.ToString();
                item["avatar"] = player != null && player.Avatar != null ? player.Avatar : "" + (code % 6);
                item["level"] = player != null ? player.Level : 3 + code % 10;
                item["nickname"] = player != null && player.Nickname != null ? player.Nickname : username;
                item["prize"] = prizes.Count > i ? (string)prizes[i] : "";
                arr.Add(item);
            }
            await redis.StringSetAsync(PLAYER_TOP_MONTH_BC, arr.ToString());
        }

        public static async Task<JSONArray> GetTopWeek(int limit = 20)
        {
            var redis = GetRedis();
            var topStr = await redis.StringGetAsync(PLAYER_TOP_WEEK_BC);
            var topArr = string.IsNullOrEmpty(topStr) ? new JSONArray() : JSON.Parse(topStr).AsArray;
            while (topArr.Count > limit)
            {
                topArr.Remove(topArr.Count - 1);
            }
            return topArr;
        }

        public static async Task<JSONArray> GetTopMonth(int limit = 20)
        {
            var redis = GetRedis();
            var topStr = await redis.StringGetAsync(PLAYER_TOP_MONTH_BC);
            var topArr = string.IsNullOrEmpty(topStr) ? new JSONArray() : JSON.Parse(topStr).AsArray;
            while (topArr.Count > limit)
            {
                topArr.Remove(topArr.Count - 1);
            }
            return topArr;
        }

        public static async Task<JSONNode> GetTopWeekRank(string username)
        {
            var redis = GetRedis();
            var rank = await redis.SortedSetRankAsync(PLAYER_CASH_WEEK_BC, username, order: Order.Descending);
            var lb = await redis.SortedSetScoreAsync(PLAYER_CASH_WEEK_BC, username);
            var _prizes = await redis.StringGetAsync(CURRENT_LEADERBOARD_WEEK_PRIZE_BC);
            var prizes = string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;

            int intRank = rank == null || !rank.HasValue ? -1 : (int)rank.Value;
            var item = new JSONObject();
            //item["username"] = username;
            item["cash"] = lb != null && lb.HasValue ? lb.ToString() : "0";
            var player = await GetPlayer(username);
            item["avatar"] = player != null && player.Avatar != null ? player.Avatar : "";
            item["nickname"] = player != null && player.Nickname != null ? player.Nickname : "";
            item["level"] = player != null ? player.Level : 1;
            item["prize"] = intRank != -1 && prizes.Count > intRank ? (string)prizes[intRank] : "";
            item["rank"] = intRank;
            return item;
        }

        public static async Task<JSONNode> GetTopMonthRank(string username)
        {
            var redis = GetRedis();
            var rank = await redis.SortedSetRankAsync(PLAYER_CASH_MONTH_BC, username, order: Order.Descending);
            var lb = await redis.SortedSetScoreAsync(PLAYER_CASH_MONTH_BC, username);
            var _prizes = await redis.StringGetAsync(CURRENT_LEADERBOARD_MONTH_PRIZE_BC);
            var prizes = string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;

            int intRank = rank == null || !rank.HasValue ? -1 : (int)rank.Value;
            var item = new JSONObject();
            //item["username"] = username;
            item["cash"] = lb != null && lb.HasValue ? lb.ToString() : "0";
            var player = await GetPlayer(username);
            item["avatar"] = player != null && player.Avatar != null ? player.Avatar : "";
            item["level"] = player != null ? player.Level : 1;
            item["nickname"] = player != null && player.Nickname != null ? player.Nickname : "";
            item["prize"] = intRank != -1 && prizes.Count > intRank ? (string)prizes[intRank] : "";
            item["rank"] = intRank;
            return item;
        }

        public static async Task<bool> IsMaintain()
        {
            var redis = GetRedis();
            var maintain = await redis.StringGetAsync(KEY_MAINTAIN);
            if (string.IsNullOrEmpty(maintain) || maintain == "0")
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void SavePlayer(Player player)
        {
            //// Get Players collection
            //var players = PlayersCollection;            

            //// Insert or update customer document
            //players.Upsert(player.Id, player);
            ////Logger.Log("save player " + player.Id + " level " + player.Level);
            SqlLogger.SavePlayer(player);

            try
            {
                var redis = GetRedis(); // save to redis also, might use in cms
                var data = player.ToJson();
                var key = PLAYER_DATA_BC + player.PlayerId;
                redis.StringSet(key, data.ToString(), flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            //redis.KeyExpire(key, DateTime.Now.AddMonths(1).AddDays(1));
        }

        public static void SavePlayerIfNotExist(Player player)
        {
            try
            {
                var redis = GetRedis(); // save to redis also, might use in top
                var data = player.ToJson();
                var key = PLAYER_DATA_BC + player.PlayerId;
                redis.StringSet(key, data.ToString(), when: When.NotExists, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<Player> GetPlayer(string username)
        {
            var redis = GetRedis();
            username = username.Trim().ToLower();
            var key = PLAYER_DATA_BC + username;
            var data = await redis.StringGetAsync(key); // no interest in response
            if (string.IsNullOrEmpty(data))
                return null;
            var player = new Player();
            player.ParseJson(JSON.Parse(data));
            return player;
        }

        public const string TableIdRedis = "bc_tableid";
        public static async Task LoadTableId()
        {
            var redis = GetRedis();
            var res = await redis.StringGetAsync(TableIdRedis);
            var id = 0;
            int.TryParse(res, out id);

            GameBanCa.setTableStartId(id);
        }

        public static void IncTableId()
        {
            try
            {
                var redis = GetRedis();
                redis.StringIncrement(TableIdRedis, 1, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        private static object locktopCashCache = new object();
        private static List<Player> topCashCache;
        private static DateTime topCashcacheTime;
        public static async Task<List<Player>> GetTopCash(int limit = 20)
        {
            List<Player> result = new List<Player>();
            if ((DateTime.Now - topCashcacheTime).TotalMinutes > 15)
            {
                lock (locktopCashCache)
                {
                    if (topCashCache != null)
                    {
                        topCashCache = null;
                    }
                }
            }
            if (topCashCache != null)
            {
                lock (locktopCashCache)
                {
                    if (topCashCache != null)
                        for (int i = 0, n = limit < topCashCache.Count ? limit : topCashCache.Count; i < n; i++)
                        {
                            result.Add(topCashCache[i]);
                        }
                    return result;
                }
            }
            else
            {
                topCashCache = await SqlLogger.GetTopCash(50);
                topCashcacheTime = DateTime.Now;
                lock (locktopCashCache)
                {
                    if (topCashCache != null)
                    {
                        for (int i = 0, n = limit < topCashCache.Count ? limit : topCashCache.Count; i < n; i++)
                        {
                            result.Add(topCashCache[i]);
                        }
                        return result;
                    }
                }
            }

            return result;
        }

        public static async Task<int> GetRankByCash(long cash)
        {
            //// Get Players collection
            //var players = PlayersCollection;
            //var result = players.Count(Query.GT("Cash", cash));
            //return result;
            return await SqlLogger.GetRankByCash(cash);
        }

        private static object locktopLevelCache = new object();
        private static List<Player> topLevelCache;
        private static DateTime topLevelcacheTime;
        public static async Task<List<Player>> GetTopLevel(int limit = 20)
        {
            List<Player> result = new List<Player>();
            if ((DateTime.Now - topLevelcacheTime).TotalMinutes > 15)
            {
                if (topLevelCache != null)
                {
                    lock (locktopLevelCache)
                    {
                        topLevelCache = null;
                    }
                }
            }
            if (topLevelCache != null)
            {
                lock (locktopLevelCache)
                {
                    if (topLevelCache != null)
                        for (int i = 0, n = limit < topLevelCache.Count ? limit : topLevelCache.Count; i < n; i++)
                        {
                            result.Add(topLevelCache[i]);
                        }
                    return result;
                }
            }
            else
            {
                topLevelCache = await SqlLogger.GetTopLevel(50);
                topLevelcacheTime = DateTime.Now;
                if (topLevelCache != null)
                {
                    lock (locktopLevelCache)
                    {
                        if (topLevelCache != null)
                            for (int i = 0, n = limit < topLevelCache.Count ? limit : topLevelCache.Count; i < n; i++)
                            {
                                result.Add(topLevelCache[i]);
                            }
                        return result;
                    }
                }
            }

            return result;
        }

        public static async Task<int> GetRankByLevel(int level)
        {
            //// Get Players collection
            //var players = PlayersCollection;
            //var result = players.Count(Query.GT("Level", level));
            //return result;
            return await SqlLogger.GetRankByLevel(level);
        }

        //public static void SaveServer(int port, long profit, long bank)
        //{
        //    ThreadPool.QueueUserWorkItem(o =>
        //    {
        //        try
        //        {
        //            // Get Players collection
        //            var servers = BanCaDatabase.GetCollection<Server>("Servers");
        //            servers.EnsureIndex("Port");

        //            var s = new Server { Id = port, Port = port, TotalProfit = profit, TotalBank = bank };

        //            // Insert or update customer document
        //            servers.Upsert(s.Id, s);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("Error in save server: " + ex.ToString());
        //        }
        //    });
        //}

        //public static void LogCashIn(long userId, long cash, CashType cashType)
        //{
        //    ThreadPool.QueueUserWorkItem(o =>
        //    {
        //        try
        //        {
        //            // Get Players collection
        //            var cin = BanCaDatabase.GetCollection<CashInOutLog>("CashIn");
        //            cin.EnsureIndex("UserId");
        //            cin.EnsureIndex("Time");

        //            var log = new CashInOutLog { UserId = userId, Cash = cash, CashType = cashType, Time = DateTime.UtcNow };

        //            // Insert or update customer document
        //            cin.Insert(log);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("Error in LogCashIn: " + ex.ToString());
        //        }
        //    });
        //}

        //public static void LogCashOut(long userId, long cash, CashType cashType)
        //{
        //    ThreadPool.QueueUserWorkItem(o =>
        //    {
        //        try
        //        {
        //            // Get Players collection
        //            var cout = BanCaDatabase.GetCollection<CashInOutLog>("CashOut");
        //            cout.EnsureIndex("UserId");
        //            cout.EnsureIndex("Time");

        //            var log = new CashInOutLog { UserId = userId, Cash = cash, CashType = cashType, Time = DateTime.UtcNow };

        //            // Insert or update customer document
        //            cout.Insert(log);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("Error in LogCashOut: " + ex.ToString());
        //        }
        //    });
        //}

        //public static Server LoadServer(int port)
        //{
        //    // Get Players collection
        //    var servers = BanCaDatabase.GetCollection<Server>("Servers");
        //    var s = servers.FindById(port);
        //    return s;
        //}

        public static async Task<int> BlockCard(long user_id)
        {
            int is_block = 0;
            var redis = GetRedis();
            var key = "BlockCard:" + user_id;
            var res = await redis.StringGetAsync(key);
            if (string.IsNullOrEmpty(res)) return 0;
            int.TryParse(res, out is_block);

            return is_block;
        }
        public static async Task<int> WrongCard(long user_id)
        {
            int is_block = 0;
            var redis = GetRedis();
            var key = "WrongCard:" + user_id;
            var res = await redis.StringGetAsync(key);
            if (string.IsNullOrEmpty(res)) return 0;
            int.TryParse(res, out is_block);

            return is_block;
        }

        public static async void SetBlockCard(long user_id, int number)
        {
            try
            {
                var redis = GetRedis();
                var key = "BlockCard:" + user_id;
                await redis.StringSetAsync(key, number);
                await redis.KeyExpireAsync(key, DateTime.Now.AddHours(1));
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async void SetWrongCard(long user_id, int number)
        {
            try
            {
                var redis = GetRedis();
                var key = "WrongCard:" + user_id;
                await redis.StringSetAsync(key, number);
                await redis.KeyExpireAsync(key, DateTime.Now.AddDays(1));
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }
        public static async Task<long> IncWrongCard(long user_id)
        {
            var redis = GetRedis();
            var key = "WrongCard:" + user_id;
            var count = await redis.StringIncrementAsync(key, 1);
            await redis.KeyExpireAsync(key, DateTime.Now.AddDays(1));
            return count;
        }

        public static async void DelBlockCard(long user_id)
        {
            try
            {
                var redis = GetRedis();
                var key = "BlockCard:" + user_id;
                await redis.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async void DelWrongCard(long user_id)
        {
            try
            {
                var redis = GetRedis();
                var key = "WrongCard:" + user_id;
                await redis.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static void LogHomeMessages(long userId, string nickname, long cashGain, FishType fishType, long tableIndex, BulletType bType, string gameId = "banca")
        {
            try
            {
                var redis = GetRedis();
                var msg = new JSONObject();
                msg["userId"] = userId;
                msg["nickname"] = nickname;
                msg["cashGain"] = cashGain;
                msg["fishType"] = (int)fishType;
                msg["tableIndex"] = tableIndex;
                msg["bulletType"] = (int)bType;
                msg["gameId"] = gameId;
                redis.ListLeftPush(HOME_MESSAGES_BC, msg.ToString(), flags: CommandFlags.FireAndForget);
                redis.ListTrim(HOME_MESSAGES_BC, 0, 30, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONArray> GetHomeMessages()
        {
            var redis = GetRedis();
            var data = await redis.ListRangeAsync(HOME_MESSAGES_BC);
            var res = new JSONArray();
            for (int i = 0, n = data.Length; i < n; i++)
            {
                string raw = data[i];
                try
                {
                    var item = JSON.Parse(raw);
                    res.Add(item);
                }
                catch
                {
                    continue;
                }
            }
            return res;
        }

        public static async Task<long> IncEpicCash(long user_id, long cash, string platform, string source, TransType reason, bool useCache = true, bool allowNegative = false, long realmoney = 0, float rate = 0)
        {
            try
            {
                var redis = GetRedis();
                string key = "User_Cash:" + user_id;
                if (useCache && userCashCache.TryGetValue(user_id, out var _longKey)) // has cache
                {
                    if (cash < 0 && !allowNegative && _longKey < -cash)
                    {
                        //"Số tiền không đủ để thực hiện";
                        return -2;
                    }

                    var CurrentCash = userCashCache[user_id] = userCashCache[user_id] + cash;
                    redis.StringIncrement(key, cash, flags: CommandFlags.FireAndForget);
                    SqlLogger.LogTransaction(user_id, CurrentCash, cash, source, (int)reason, realmoney, rate);

                    var config = LobbyConfig.GetDefaultConfig();
                    if (cash < 0 && CurrentCash < config.CashSaveMin && reason != TransType.CMS && reason != TransType.CASH_OUT &&
                        reason != TransType.TRANSFER_CASH && reason != TransType.MOMO_OUT && reason != TransType.BANK_OUT)
                    {
                        CheckCashSave(user_id, config.CashSaveMin, config.CashSaveAmount, platform, reason == TransType.TAIXIU_BET ? 30000 : 10000);
                    }
                    return CurrentCash;
                }

                {
                    var valKey = await redis.StringGetAsync(key);

                    if (valKey.IsNullOrEmpty)
                    {
                        var userCash = await MySqlProcess.Genneral.MySqlUser.GetUserCash(user_id);
                        //"Tài khoản không tồn tại";
                        if (userCash == -1)
                            return -1;

                        if (cash < 0 && !allowNegative && userCash + cash < 0)
                        {
                            //"Số tiền không đủ để thực hiện";
                            return -4;
                        }

                        redis.StringSet(key, userCash + cash, flags: CommandFlags.FireAndForget);
                        return userCash + cash;
                    }

                    long longKey = (long)valKey;
                    if (cash < 0 && !allowNegative && longKey < -cash)
                    {
                        //"Số tiền không đủ để thực hiện";
                        return -5;
                    }

                    var CurrentCash = (long)await redis.StringIncrementAsync(key, cash);
                    userCashCache[user_id] = CurrentCash;

                    //if (platform != "BOT")
                    //{
                    //    string message = string.Format("({0},{1},{2},'{3}','{4}',{5},'{6}'),", user_id, cash, CurrentCash, platform, cnid, game_session, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    //    redis.ListRightPush(key_trans, Encoding.UTF8.GetBytes(message));
                    //}
                    SqlLogger.LogTransaction(user_id, CurrentCash, cash, source, (int)reason, realmoney, rate);

                    var config = LobbyConfig.GetDefaultConfig();
                    if (cash < 0 && CurrentCash < config.CashSaveMin && reason != TransType.CMS && reason != TransType.CASH_OUT &&
                        reason != TransType.TRANSFER_CASH && reason != TransType.MOMO_OUT && reason != TransType.BANK_OUT)
                    {
                        CheckCashSave(user_id, config.CashSaveMin, config.CashSaveAmount, platform, reason == TransType.TAIXIU_BET ? 30000 : 10000);
                    }
                    return CurrentCash;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in IncEpicCash " + ex.ToString());
                return -3;
            }
        }

        public static long IncEpicCashCache(long user_id, long cash, string platform, string source, TransType reason)
        {
            try
            {
                var redis = GetRedis();
                string key = "User_Cash:" + user_id;
                if (userCashCache.TryGetValue(user_id, out var _longKey)) // has cache
                {
                    if (cash < 0 && _longKey < -cash)
                    {
                        //"Số tiền không đủ để thực hiện";
                        return -2;
                    }

                    var CurrentCash = userCashCache[user_id] = userCashCache[user_id] + cash;
                    redis.StringIncrement(key, cash, flags: CommandFlags.FireAndForget);
                    SqlLogger.LogTransaction(user_id, CurrentCash, cash, source, (int)reason, 0, 0);

                    var config = LobbyConfig.GetDefaultConfig();
                    if (cash < 0 && CurrentCash < config.CashSaveMin && reason != TransType.CMS && reason != TransType.CASH_OUT &&
                        reason != TransType.TRANSFER_CASH && reason != TransType.MOMO_OUT && reason != TransType.BANK_OUT)
                    {
                        CheckCashSave(user_id, config.CashSaveMin, config.CashSaveAmount, platform, reason == TransType.TAIXIU_BET ? 30000 : 10000);
                    }
                    return CurrentCash;
                }

                return -1;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in IncEpicCash " + ex.ToString());
                return -3;
            }
        }

        public static async void QueuePendingMessage(long userId, string message)
        {
            try
            {
                var redis = GetRedis();
                var key = PENDING_MESSAGES_BC + userId;
                await redis.ListRightPushAsync(key, message);
                await redis.KeyExpireAsync(key, DateTime.Now.AddDays(7));
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONArray> FlushPendingMessage(long userId)
        {
            var redis = GetRedis();
            var key = PENDING_MESSAGES_BC + userId;
            var res = await redis.ListRangeAsync(key, 0, -1);
            var result = new JSONArray();
            await redis.KeyDeleteAsync(key);
            for (int i = 0, n = res.Length; i < n; i++)
            {
                var item = res[i];
                result.Add(item.ToString());
            }
            return result;
        }

        private static ConcurrentDictionary<long, int> playersServer = new ConcurrentDictionary<long, int>();
        public static void SetPlayerServer(long userid, int serverId)
        {
            //var redis = GetRedis();
            //redis.HashSet(PLAYER_SERVER_BC, userid, serverId);
            playersServer[userid] = serverId;
        }

        public static void RemovePlayerServer(long userid)
        {
            //var redis = GetRedis();
            //redis.HashDelete(PLAYER_SERVER_BC, userid);
            playersServer.TryRemove(userid, out var v);
        }

        public static int GetPlayerServer(long userid)
        {
            //var redis = GetRedis();
            //string server = redis.HashGet(PLAYER_SERVER_BC, userid);
            //if (string.IsNullOrEmpty(server) || server.Equals("0"))
            //{
            //    return -1;
            //}
            //int si = -1;
            //int.TryParse(server, out si);
            //return si;
            return playersServer.TryGetValue(userid, out var res) ? res : -1;
        }

        public static JSONNode GetPlayerServerJson()
        {
            //var redis = GetRedis();
            //string server = redis.HashGet(PLAYER_SERVER_BC, userid);
            //if (string.IsNullOrEmpty(server) || server.Equals("0"))
            //{
            //    return -1;
            //}
            //int si = -1;
            //int.TryParse(server, out si);
            //return si;
            var res = new JSONObject();
            foreach (var pair in playersServer)
            {
                res[pair.Key.ToString()] = pair.Value;
            }
            return res;
        }

        public static void ClearPlayerServer()
        {
            //var redis = GetRedis();
            //redis.KeyDelete(PLAYER_SERVER_BC);
            playersServer.Clear();
        }

        public static void SaveFund(Dictionary<int, double> profit, Dictionary<int, double> fund, Dictionary<int, double> bombFund, Dictionary<int, double> jackpot)
        {
            try
            {
                var redis = GetRedis();
                var batch = redis.CreateBatch();
                foreach (var pair in profit)
                {
                    batch.HashSetAsync(PROFIT_BC, pair.Key, pair.Value, flags: CommandFlags.FireAndForget);
                }
                foreach (var pair in fund)
                {
                    batch.HashSetAsync(FUND_BC, pair.Key, pair.Value, flags: CommandFlags.FireAndForget);
                }
                foreach (var pair in bombFund)
                {
                    batch.HashSetAsync(BOMB_FUND_BC, pair.Key, pair.Value, flags: CommandFlags.FireAndForget);
                }
                foreach (var pair in jackpot)
                {
                    batch.HashSetAsync(JACKPOT_BC, pair.Key, pair.Value, flags: CommandFlags.FireAndForget);
                }
                batch.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async void LoadFund(Dictionary<int, double> profit, Dictionary<int, double> fund, Dictionary<int, double> bombFund, Dictionary<int, double> jackpot)
        {
            var redis = GetRedis();
            try
            {
                var profits = await redis.HashGetAllAsync(PROFIT_BC);
                foreach (var item in profits)
                {
                    profit[int.Parse(item.Name)] = double.Parse(item.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Load profits fail " + ex.ToString());
            }

            try
            {
                var funds = await redis.HashGetAllAsync(FUND_BC);
                foreach (var item in funds)
                {
                    fund[int.Parse(item.Name)] = double.Parse(item.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Load funds fail " + ex.ToString());
            }

            try
            {
                var bombs = await redis.HashGetAllAsync(BOMB_FUND_BC);
                foreach (var item in bombs)
                {
                    bombFund[int.Parse(item.Name)] = double.Parse(item.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Load bombs fail " + ex.ToString());
            }

            try
            {
                var jacks = await redis.HashGetAllAsync(JACKPOT_BC);
                foreach (var item in jacks)
                {
                    int key = int.Parse(item.Name);
                    jackpot[key] = double.Parse(item.Value);
                    var index = key;
                    var min = (double)(Config.JackpotInitial * Config.GetBulletValue(index, (Config.BulletType)(key % 10)));
                    if (jackpot[key] < min) // make sure minimum is initial
                    {
                        jackpot[key] = min;
                    }
                }

                if (jackpot.Count == 0)
                {
                    jackpot[11] = (double)(Config.JackpotInitial * Config.GetBulletValue(1, Config.BulletType.Bullet1));
                    jackpot[12] = (double)(Config.JackpotInitial * Config.GetBulletValue(1, Config.BulletType.Bullet2));
                    jackpot[13] = (double)(Config.JackpotInitial * Config.GetBulletValue(1, Config.BulletType.Bullet3));
                    jackpot[14] = (double)(Config.JackpotInitial * Config.GetBulletValue(1, Config.BulletType.Bullet4));

                    jackpot[21] = (double)(Config.JackpotInitial * Config.GetBulletValue(2, Config.BulletType.Bullet1));
                    jackpot[22] = (double)(Config.JackpotInitial * Config.GetBulletValue(2, Config.BulletType.Bullet2));
                    jackpot[23] = (double)(Config.JackpotInitial * Config.GetBulletValue(2, Config.BulletType.Bullet3));
                    jackpot[24] = (double)(Config.JackpotInitial * Config.GetBulletValue(2, Config.BulletType.Bullet4));

                    jackpot[31] = (double)(Config.JackpotInitial * Config.GetBulletValue(3, Config.BulletType.Bullet1));
                    jackpot[32] = (double)(Config.JackpotInitial * Config.GetBulletValue(3, Config.BulletType.Bullet2));
                    jackpot[33] = (double)(Config.JackpotInitial * Config.GetBulletValue(3, Config.BulletType.Bullet3));
                    jackpot[34] = (double)(Config.JackpotInitial * Config.GetBulletValue(3, Config.BulletType.Bullet4));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Load jp fail " + ex.ToString());
            }
        }

        public static void SetItemTimestamp(string username, long snipeTimestamp, long rFireTimestamp)
        {
            try
            {
                var redis = GetRedis();
                username = username.Trim().ToLower();
                redis.StringSet(ITEM_SNIPE_BC + username, snipeTimestamp, new TimeSpan(0, 0, (int)Config.SnipeCoolDownS), flags: CommandFlags.FireAndForget);
                redis.StringSet(ITEM_RAPID_FIRE_BC + username, rFireTimestamp, new TimeSpan(0, 0, (int)Config.FastFireCoolDownS), flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("SetItemTimestamp fail " + ex.ToString());
            }
        }

        public static async Task<long> GetSnipeTimestamp(string username)
        {
            var redis = GetRedis();
            username = username.Trim().ToLower();
            var rval = await redis.StringGetAsync(ITEM_SNIPE_BC + username);
            var res = 0L;
            long.TryParse(rval, out res);
            return res;
        }

        public static async Task<long> GetRapidFireTimestamp(string username)
        {
            var redis = GetRedis();
            username = username.Trim().ToLower();
            var rval = await redis.StringGetAsync(ITEM_RAPID_FIRE_BC + username);
            var res = 0L;
            long.TryParse(rval, out res);
            return res;
        }

        private const string RapidFireCountItem = "bc_rf_count";
        private static ConcurrentDictionary<long, int> RapidFireCountCache = new ConcurrentDictionary<long, int>();
        public static int GetRapidFireCount(long userId)
        {
            if (RapidFireCountCache.TryGetValue(userId, out int count)) return count;

            var key = RapidFireCountItem;
            var redis = GetRedis();
            count = (int)redis.HashGet(key, userId);
            RapidFireCountCache[userId] = count;
            return count;
        }

        public static async Task<int> AddRapidFireItem(long userId, int count)
        {
            if (count != 0)
            {
                var redis = GetRedis();
                var key = RapidFireCountItem;
                if (!RapidFireCountCache.ContainsKey(userId))
                {
                    var val = await redis.HashGetAsync(key, userId);
                    RapidFireCountCache[userId] = val.IsNullOrEmpty ? 0 : (int)val;
                }

                RapidFireCountCache[userId] += count;
                redis.HashIncrement(key, userId, count, CommandFlags.FireAndForget);
                return RapidFireCountCache[userId];
            }

            return -1;
        }

        private const string SnipeCountItem = "bc_sn_count";
        private static ConcurrentDictionary<long, int> SnipeCountCache = new ConcurrentDictionary<long, int>();
        public static int GetSnipeCount(long userId)
        {
            if (SnipeCountCache.TryGetValue(userId, out int count)) return count;

            var key = SnipeCountItem;
            var redis = GetRedis();
            count = (int)redis.HashGet(key, userId);
            SnipeCountCache[userId] = count;
            return count;
        }

        public static async Task<int> AddSnipeItem(long userId, int count)
        {
            if (count != 0)
            {
                var redis = GetRedis();
                var key = SnipeCountItem;
                if (!SnipeCountCache.ContainsKey(userId))
                {
                    var val = await redis.HashGetAsync(key, userId);
                    SnipeCountCache[userId] = val.IsNullOrEmpty ? 0 : (int)val;
                }

                SnipeCountCache[userId] += count;
                await redis.HashIncrementAsync(key, userId, count, CommandFlags.FireAndForget);
                return SnipeCountCache[userId];
            }

            return -1;
        }

        private static object lockjpUsers = new object();
        private static HashSet<long> jpUsers = new HashSet<long>();
        public static JSONArray GetJpUsers()
        {
            lock (lockjpUsers)
            {
                var res = new JSONArray();
                foreach (var user in jpUsers)
                {
                    res.Add(user);
                }
                return res;
            }
        }

        public static void SetJpUsers(JSONArray users)
        {
            lock (lockjpUsers)
            {
                jpUsers.Clear();
                for (int i = 0, n = users.Count; i < n; i++)
                {
                    var user = users[i].AsLong;
                    jpUsers.Add(user);
                }
            }
        }

        public static bool IsJpUser(long userId)
        {
            lock (lockjpUsers)
            {
                return jpUsers.Contains(userId);
            }
        }

        public static async void logFirstUserAcc(string deviceId, long userid)
        {
            try
            {
                var redis = GetRedis();

                if (!await redis.HashExistsAsync(DEVICE_ID_USERID, deviceId)) // not exist in first slot, set and return
                {
                    await redis.HashSetAsync(DEVICE_ID_USERID, deviceId, userid, When.NotExists);
                    return;
                }
                //else // exist, return if is same uid
                //{
                //    var rv = (string)redis.HashGet(DEVICE_ID_USERID, deviceId);
                //    var ruid = 0L;
                //    long.TryParse(rv, out ruid);
                //    if (ruid == userid) return;
                //}

                //if (!redis.HashExists(DEVICE_ID_USERID2, deviceId))
                //{
                //    redis.HashSet(DEVICE_ID_USERID2, deviceId, userid, When.NotExists);
                //    return;
                //}
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<bool> isFirstAcc(string deviceId, long userid)
        {
            var redis = GetRedis();
            var rv = (string)await redis.HashGetAsync(DEVICE_ID_USERID, deviceId);
            if (string.IsNullOrEmpty(rv))
            {
                return true;
            }

            var ruid = 0L;
            long.TryParse(rv, out ruid);
            if (ruid == userid) return true;

            //rv = (string)redis.HashGet(DEVICE_ID_USERID2, deviceId);
            //if (string.IsNullOrEmpty(rv))
            //{
            //    return true;
            //}
            //ruid = 0;
            //long.TryParse(rv, out ruid);
            //return ruid == userid;
            return false;
        }

        public static async void LogFreeCashTime(string deviceId, long userId)
        {
            try
            {
                var redis = GetRedis();
                var rv = (string)await redis.HashGetAsync(DEVICE_ID_USERID, deviceId);
                if (!string.IsNullOrEmpty(rv) && rv.Equals(userId.ToString())) // belong to first group?
                {
                    var key = DEVICE_ID_USERID + deviceId;
                    if (!await redis.KeyExistsAsync(key))
                    {
                        await redis.StringSetAsync(key, TimeUtil.TimeStamp);
                        await redis.KeyExpireAsync(key, new TimeSpan(1, 0, 0, 0));
                    }
                    return;
                }

                //rv = (string)redis.HashGet(DEVICE_ID_USERID2, deviceId);
                //if (!string.IsNullOrEmpty(rv) && rv.Equals(userId.ToString())) // belong to second group?
                //{
                //    var key = DEVICE_ID_USERID2 + deviceId;
                //    if (!redis.KeyExists(key))
                //    {
                //        redis.StringSet(key, TimeUtil.TimeStamp);
                //        redis.KeyExpire(key, new TimeSpan(1, 0, 0, 0));
                //    }
                //    return;
                //}
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<bool> CanGetFreeCash(string deviceId, long userId)
        {
            var redis = GetRedis();
            var rv = (string)await redis.HashGetAsync(DEVICE_ID_USERID, deviceId);
            if (!string.IsNullOrEmpty(rv) && rv.Equals(userId.ToString())) // belong to first group?
            {
                return !await redis.KeyExistsAsync(DEVICE_ID_USERID + deviceId);
            }

            //rv = (string)redis.HashGet(DEVICE_ID_USERID2, deviceId);
            //if (!string.IsNullOrEmpty(rv) && rv.Equals(userId.ToString())) // belong to second group?
            //{
            //    return !redis.KeyExists(DEVICE_ID_USERID2 + deviceId);
            //}

            return false;
        }

        public static void AddBlackIp(string ip)
        {
            lock (lockblackIps)
                blackIps.Add(ip);
        }

        public static void RemoveBlackIp(string ip)
        {
            lock (lockblackIps)
                blackIps.Remove(ip);
        }

        public static JSONArray GetBlackIp()
        {
            lock (lockblackIps)
            {
                var arr = new JSONArray();
                foreach (var item in blackIps)
                {
                    arr.Add(item);
                }
                return arr;
            }
        }

        public static void AddBlackDeviceId(string did)
        {
            lock (lockblackIps)
                blackDeviceIds.Add(did);
        }

        public static void RemoveBlackDeviceId(string did)
        {
            lock (lockblackIps)
                blackDeviceIds.Remove(did);
        }

        public static JSONArray GetBlackDeviceIds()
        {
            lock (lockblackIps)
            {
                var arr = new JSONArray();
                foreach (var item in blackDeviceIds)
                {
                    arr.Add(item);
                }
                return arr;
            }
        }

        public static void SaveBlackList()
        {
            try
            {
                var redis = GetRedis();
                var batch = redis.CreateBatch();
                redis.KeyDelete(BLACK_IP_BC);
                redis.KeyDelete(BLACK_DID_BC);

                lock (lockblackIps)
                    foreach (var item in blackDeviceIds)
                    {
                        batch.SetAddAsync(BLACK_DID_BC, item, CommandFlags.FireAndForget);
                    }

                lock (lockblackIps)
                    foreach (var item in blackIps)
                    {
                        batch.SetAddAsync(BLACK_IP_BC, item, CommandFlags.FireAndForget);
                    }

                batch.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async void LoadBlackList()
        {
            try
            {
                var redis = GetRedis();
                var bdid = await redis.SetMembersAsync(BLACK_DID_BC);
                lock (lockblackIps)
                {
                    blackDeviceIds.Clear();
                    if (bdid != null)
                        foreach (var item in bdid)
                        {
                            blackDeviceIds.Add(item);
                        }
                }

                var bip = await redis.SetMembersAsync(BLACK_IP_BC);
                lock (lockblackIps)
                {
                    blackIps.Clear();
                    if (bip != null)
                        foreach (var item in bip)
                        {
                            blackIps.Add(item);
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static bool IsBlockDeviceId(string did)
        {
            lock (lockblackIps)
            {
                return blackDeviceIds.Contains(did);
            }
        }

        public static bool IsBlockIp(string ip)
        {
            lock (lockblackIps)
            {
                return blackIps.Contains(ip);
            }
        }

        public static async Task<int> GetIapCount(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringGetAsync(IAP_COUNT_BC + userId);
        }

        public static async Task<int> IncIapCount(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringIncrementAsync(IAP_COUNT_BC + userId);
        }

        public static async Task<int> GetCardInCount(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringGetAsync(CARD_IN_COUNT_BC + userId);
        }

        public static async Task<long> CardInInc(long userId, long value)
        {
            var redis = GetRedis();
            return await redis.StringIncrementAsync(CARD_IN_COUNT_BC + userId, value);
        }

        public static async Task<int> GetMoMoCount(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringGetAsync(MOMO_COUNT_BC + userId);
        }

        public static async Task<long> MoMoInc(long userId, long value)
        {
            var redis = GetRedis();
            return await redis.StringIncrementAsync(MOMO_COUNT_BC + userId, value);
        }

        public static async Task<int> GetBankCount(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringGetAsync(BANK_COUNT_BC + userId);
        }

        public static async Task<long> BankInc(long userId, long value)
        {
            var redis = GetRedis();
            return await redis.StringIncrementAsync(BANK_COUNT_BC + userId, value);
        }

        public static async Task<int> GetTotalOnlineTime(long userId)
        {
            var redis = GetRedis();
            return (int)await redis.StringGetAsync(TOTAL_USER_ONLINE_TIME_BC + userId);
        }

        public static async Task<long> TotalOnlineTimeInc(long userId, long value)
        {
            try
            {
                var redis = GetRedis();
                return await redis.StringIncrementAsync(TOTAL_USER_ONLINE_TIME_BC + userId, value);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
                return 0;
            }
        }

        private static ConcurrentDictionary<long, long> userLock = new ConcurrentDictionary<long, long>();
        public static bool TryLockUserId(long userId)
        {
            //var redis = GetRedis();
            //string uid = userId.ToString();
            //RedisKey key = uid;
            //RedisValue val = uid;
            //return redis.LockTake(key, val, new TimeSpan(0, 1, 0));
            var now = TimeUtil.TimeStamp;

            if (userLock.TryGetValue(userId, out var time)) // already lock?
            {
                if (now - time < 10000) // 10s, lock still in effect
                    return false;

                userLock[userId] = now; // renew lock
                return true;
            }

            userLock[userId] = now; // new lock
            return true;
        }

        public static bool ReleaseLockUserId(long userId)
        {
            //var redis = GetRedis();
            //string uid = userId.ToString();
            //RedisKey key = uid;
            //RedisValue val = uid;
            //return redis.LockRelease(key, val);
            if (userLock.ContainsKey(userId))
            {
                userLock.TryRemove(userId, out var a);
                return true;
            }

            return false;
        }

        private static ConcurrentDictionary<long, long> playingTables = new ConcurrentDictionary<long, long>();
        public static void SetPlaying(long userId, long tableId)
        {
            //var redis = GetRedis();
            //redis.StringSet(PLAYING_BC_TABLE + userId, tableId, flags: CommandFlags.FireAndForget);
            playingTables[userId] = tableId;
        }

        public static void SetNotPlaying(long userId)
        {
            //var redis = GetRedis();
            //redis.KeyDelete(PLAYING_BC_TABLE + userId, flags: CommandFlags.FireAndForget);
            playingTables.TryRemove(userId, out var a);
        }

        public static bool IsPlaying(long userId)
        {
            //var redis = GetRedis();
            //var tableid = redis.StringGet(PLAYING_BC_TABLE + userId);
            //return !string.IsNullOrEmpty(tableid);
            return playingTables.ContainsKey(userId);
        }

        public static void PushClientReport(string report)
        {
            try
            {
                var redis = GetRedis();
                var key = "bc_client_report";
                redis.ListRightPush(key, report, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static void SetVideoAds(long userId, int type)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}_{2}", VIDEO_ADS_BC, userId, type);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            redis.StringSet(key, TimeUtil.TimeStamp, remaining, When.NotExists, CommandFlags.FireAndForget);
        }

        public static async Task<int> GetVideoAdsCount(long userId, int type)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, type);
            var count = await redis.StringGetAsync(key);
            if (count.IsNull)
            {
                if (type == 0) return Config.VideoAdsRewardCount0;
                else return Config.VideoAdsRewardCount1;
            }
            else
            {
                return (int)count;
            }
        }

        public static async Task<JSONNode> LastTimeViewVideoAds(long userId)
        {
            var res = new JSONObject();
            var redis = GetRedis();
            var key = string.Format("{0}{1}_{2}", VIDEO_ADS_BC, userId, 0);
            string time = await redis.StringGetAsync(key);
            res["time0"] = string.IsNullOrEmpty(time) ? -1 : long.Parse(time);

            key = string.Format("{0}{1}_{2}", VIDEO_ADS_BC, userId, 1);
            time = await redis.StringGetAsync(key);
            res["time1"] = string.IsNullOrEmpty(time) ? -1 : long.Parse(time);

            var key0 = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, 0);
            var count0 = await redis.StringGetAsync(key0);
            res["count0"] = count0.IsNull ? Config.VideoAdsRewardCount0 : (int)count0;

            var key1 = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, 1);
            var count1 = await redis.StringGetAsync(key1);
            res["count1"] = count1.IsNull ? Config.VideoAdsRewardCount1 : (int)count1;
            return res;
        }

        public static async Task<int> SeeVideoAds(long userId, int type)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}_{2}", VIDEO_ADS_BC, userId, type);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            var result = -1;
            if (await redis.KeyExistsAsync(key)) // in day
            {
                var key2 = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, type);
                var count = await redis.StringGetAsync(key2);
                if (count.IsNull) // not initialize
                {
                    if (type == 0) result = Config.VideoAdsRewardCount0 - 1; else result = Config.VideoAdsRewardCount1 - 1;
                    redis.StringSet(key2, result, new TimeSpan(24, 0, 0), flags: CommandFlags.FireAndForget); // set
                }
                else // inited, can see ads if count > 0
                {
                    int intCount = (int)count;
                    if (intCount > 0)
                    {
                        result = intCount - 1;
                        redis.StringSet(key2, result, new TimeSpan(24, 0, 0), flags: CommandFlags.FireAndForget);
                    }
                }
            }
            else // new day
            {
                redis.StringSet(key, TimeUtil.TimeStamp, remaining, flags: CommandFlags.FireAndForget); // set

                var key0 = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, 0);
                redis.StringSet(key0, type == 0 ? Config.VideoAdsRewardCount0 - 1 : Config.VideoAdsRewardCount0, new TimeSpan(24, 0, 0), flags: CommandFlags.FireAndForget); // set

                var key1 = string.Format("{0}{1}_{2}", VIDEO_ADS_COUNT_BC, userId, 1);
                redis.StringSet(key1, type == 1 ? Config.VideoAdsRewardCount1 - 1 : Config.VideoAdsRewardCount1, new TimeSpan(24, 0, 0), flags: CommandFlags.FireAndForget); // set

                if (type == 0) result = Config.VideoAdsRewardCount0 - 1; else result = Config.VideoAdsRewardCount1 - 1;
            }

            return result;
        }

        public static async void IncRegisterInDay(string deviceId)
        {
            try
            {
                var redis = GetRedis();
                var key = string.Format("{0}{1}", REGISTER_COUNT_IN_DAY, deviceId);
                var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
                await redis.StringIncrementAsync(key);
                await redis.KeyExpireAsync(key, remaining);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<bool> CanRegisterInDay(string deviceId)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}", REGISTER_COUNT_IN_DAY, deviceId);
            var count = await redis.StringGetAsync(key);
            if (count.IsNullOrEmpty) return true;
            else return (int)count < Config.NumberOfAccountPerDay;
        }

        #region check cashout
        private const string CASHOUT_PER_DAY_KEY = "bc_copd:";
        public static async Task<int> canCashOutCountPerDay(long userId, VersionConfig config)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}", CASHOUT_PER_DAY_KEY, userId);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            var remain = await redis.StringGetAsync(key);
            if (!remain.IsNullOrEmpty)
            {
                return (int)remain;
            }
            else
            {
                var remain2 = config.CashOutPerDay;
                redis.StringSet(key, remain2, remaining, flags: CommandFlags.FireAndForget);
                return remain2;
            }
        }
        public static async Task<int> substractCashOutCountPerDay(long userId, VersionConfig config)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}", CASHOUT_PER_DAY_KEY, userId);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            if (await redis.KeyExistsAsync(key))
            {
                var remain = (int)await redis.StringIncrementAsync(key, -1);
                await redis.KeyExpireAsync(key, remaining);
                return remain;
            }
            else
            {
                var remain = config.CashOutPerDay - 1;
                redis.StringSet(key, remain, remaining, flags: CommandFlags.FireAndForget);
                return remain;
            }
        }

        private const string CASHOUT_GOLD_PER_DAY_KEY = "bc_cogpd:";
        public static async Task<int> canCashOutCountGoldPerDay(long userId, VersionConfig config)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}", CASHOUT_GOLD_PER_DAY_KEY, userId);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            var remain = await redis.StringGetAsync(key);
            if (!remain.IsNullOrEmpty)
            {
                return (int)remain;
            }
            else
            {
                var remain2 = config.CashOutMaxCashPerDay;
                if (remain2 >= 0)
                    redis.StringSet(key, remain2, remaining, flags: CommandFlags.FireAndForget);
                return remain2;
            }
        }
        public static async Task<int> substractCashOutCountGoldPerDay(long userId, long cash, VersionConfig config)
        {
            var redis = GetRedis();
            var key = string.Format("{0}{1}", CASHOUT_GOLD_PER_DAY_KEY, userId);
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            if (await redis.KeyExistsAsync(key))
            {
                int remain = (int)await redis.StringIncrementAsync(key, -cash);
                if (remain < 0)
                {
                    await redis.StringIncrementAsync(key, cash);
                }
                await redis.KeyExpireAsync(key, remaining);
                return remain;
            }
            else
            {
                int remain = (int)(config.CashOutMaxCashPerDay - cash);
                if (remain >= 0)
                    redis.StringSet(key, remain, remaining, flags: CommandFlags.FireAndForget);
                return remain;
            }
        }

        private const string CASHOUT_GOLD_SERVER_PER_DAY_KEY = "bc_cogspd";
        public static async Task<int> canCashOutCountGoldServerPerDay(VersionConfig config)
        {
            var redis = GetRedis();
            var key = CASHOUT_GOLD_SERVER_PER_DAY_KEY;
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            var remain = await redis.StringGetAsync(key);
            if (!remain.IsNullOrEmpty)
            {
                return (int)remain;
            }
            else
            {
                var remain2 = config.CashOutMaxCashServerPerDay;
                if (remain2 >= 0)
                    redis.StringSet(key, remain2, remaining, flags: CommandFlags.FireAndForget);
                return remain2;
            }
        }
        public static async Task<int> substractCashOutCountGoldServerPerDay(long cash, VersionConfig config)
        {
            var redis = GetRedis();
            var key = CASHOUT_GOLD_SERVER_PER_DAY_KEY;
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            if (await redis.KeyExistsAsync(key))
            {
                int remain = (int)await redis.StringIncrementAsync(key, -cash);
                if (remain < 0)
                {
                    await redis.StringIncrementAsync(key, cash);
                }
                await redis.KeyExpireAsync(key, remaining);
                return remain;
            }
            else
            {
                int remain = (int)(config.CashOutMaxCashServerPerDay - cash);
                if (remain >= 0)
                    redis.StringSet(key, remain, remaining, flags: CommandFlags.FireAndForget);
                return remain;
            }
        }

        public static void ClearCashoutFilterData()
        {
            var redis = GetRedis();
            redis.KeyDelete(CASHOUT_GOLD_SERVER_PER_DAY_KEY, flags: CommandFlags.FireAndForget);
        }
        #endregion

        private const string BC_BOT_CONFIG = "bc_bot";
        public static void ResetBotConfig()
        {
            if (originalBotConfig != null)
            {
                try
                {
                    BanCa.Libs.Bots.BotConfig.ParseJson(originalBotConfig);
                }
                catch (Exception ex)
                {
                    Logger.Error("Reset bot config fail: " + ex.ToString());
                }
            }
        }

        public static void SaveBotConfig()
        {
            try
            {
                var redis = GetRedis();
                redis.StringSet(BC_BOT_CONFIG, BanCa.Libs.Bots.BotConfig.ToJson().ToString(), flags: CommandFlags.FireAndForget); // no interest in response
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task LoadBotConfig(bool createIfNotExist)
        {
            var redis = GetRedis();
            var data = await redis.StringGetAsync(BC_BOT_CONFIG);
            if (originalBotConfig == null)
            {
                originalBotConfig = BanCa.Libs.Bots.BotConfig.ToJson();
            }
            if (string.IsNullOrEmpty(data))
            {
                if (createIfNotExist)
                {
                    SaveBotConfig();
                }
            }
            else
            {
                try
                {
                    BanCa.Libs.Bots.BotConfig.ParseJson(JSON.Parse(data));
                }
                catch (Exception ex)
                {
                    Logger.Error("Load bot config fail: " + ex.ToString());
                }
            }
        }
        #region Hack
        public const string HackerKey = "Hacker";
        public enum HackType : int
        {
            HackSpeed = 0,
            InvalidBullet = 1,
            PlaySlotWhilePlayBC = 2,
            PlaySlotInvalidServer = 3,
            PlayTxWhilePlayBC = 4,
            PlayTxInvalidServer = 5,
            CashOutWhilePlayBC = 6,
            CashOutInvalidServer = 7,
            ClientReport = 8,
            CannotIncCash = 9,
            TxInvalidParams = 10,
            SmsWhilePlayBC = 11,
            PlaySlot3WhilePlayBC = 12,
            PlayMiniPokerWhilePlayBC = 13,
        }
        public static void AddHacker(long userId, HackType type, string extra)
        {
            try
            {
                var redis = GetRedis();
                var json = new JSONObject();
                json["userId"] = userId;
                json["type"] = (int)type;
                json["typeName"] = type.ToString();
                json["extra"] = extra;
                var time = json["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                redis.ListRightPush(HackerKey, json.ToString(), flags: CommandFlags.FireAndForget);
                SqlLogger.AddHacker(userId, time, type, extra);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONArray> GetHackers()
        {
            var redis = GetRedis();
            var data = await redis.ListRangeAsync(HackerKey);
            var jsonarr = new JSONArray();
            foreach (var item in data)
            {
                jsonarr.Add(JSON.Parse(item));
            }
            return jsonarr;
        }
        #endregion

        #region Event add coin
        private const string EventCoinAddAmountKey = "bc_event_coin_amount";
        private const string EventCoinMinToAddAmountKey = "bc_event_coin_min";
        private const string EventCoinAddKey = "bc_event_coin";
        private const string EventCoinAddDurationKey = "bc_event_coin_duration";
        private const string EventCoinAddMessageKey = "bc_event_coin_message";
        private const string EventCoinUserPrefixKey = "bc_event_coin_user:";

        // set event in redis
        public static async void AddCointEvent(long coinAmount, long minAmount, int durationMs, string message)
        {
            try
            {
                var redis = GetRedis();
                var batch = redis.CreateBatch();
                await batch.StringSetAsync(EventCoinAddAmountKey, coinAmount, flags: CommandFlags.FireAndForget);
                await batch.StringSetAsync(EventCoinMinToAddAmountKey, minAmount, flags: CommandFlags.FireAndForget);
                await batch.StringSetAsync(EventCoinAddMessageKey, message, flags: CommandFlags.FireAndForget);
                await batch.StringSetAsync(EventCoinAddDurationKey, durationMs, flags: CommandFlags.FireAndForget);
                batch.Execute();

                await redis.StringSetAsync(EventCoinAddKey, TimeUtil.TimeStamp);
                await redis.KeyExpireAsync(EventCoinAddKey, new TimeSpan(0, 0, 0, 0, durationMs));
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONNode> GetCoinEvent()
        {
            var json = new JSONObject();
            try
            {
                var redis = GetRedis();
                string start = redis.StringGet(EventCoinAddKey);
                if (string.IsNullOrEmpty(start))
                {
                    json["start"] = "";
                }
                else
                {
                    json["start"] = start;
                    json["min"] = (string)await redis.StringGetAsync(EventCoinMinToAddAmountKey); // 1000
                    json["amount"] = (string)await redis.StringGetAsync(EventCoinAddAmountKey); // 1000
                    json["message"] = (string)await redis.StringGetAsync(EventCoinAddMessageKey); // abc
                    json["duration"] = (string)await redis.StringGetAsync(EventCoinAddDurationKey); // abc

                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return json;
        }

        // return alert message if qualify
        public static async Task<string> CheckCointEvent(User user)
        {
            try
            {
                var redis = GetRedis();
                long userId = user.UserId;
                string platform = user.Platform;
                string start = await redis.StringGetAsync(EventCoinAddKey);
                if (!string.IsNullOrEmpty(start)) // has event (not expired)
                {
                    var keyUser = EventCoinUserPrefixKey + userId;
                    string time = await redis.StringGetAsync(keyUser); // each user can only get once
                    if (string.IsNullOrEmpty(time))
                    {
                        var min = (long)await redis.StringGetAsync(EventCoinMinToAddAmountKey); // 1000
                        var userCash = await GetUserCash(userId);
                        if (userCash < min)
                        {
                            var amount = (long)await redis.StringGetAsync(EventCoinAddAmountKey); // 1000
                            if (amount > 0)
                            {
                                var message = (string)await redis.StringGetAsync(EventCoinAddMessageKey); // abc
                                var duration = (long)await redis.StringGetAsync(EventCoinAddDurationKey); // abc

                                var newCash = await IncEpicCash(userId, amount, platform, "Add coin event", TransType.ADD_COIN_EVENT);
                                if (newCash > -1)
                                {
                                    user.Cash = newCash;
                                }

                                var now = TimeUtil.TimeStamp;
                                var remain = (int)(duration - (now - long.Parse(start)));
                                await redis.StringSetAsync(keyUser, now);
                                await redis.KeyExpireAsync(keyUser, new TimeSpan(0, 0, 0, 0, remain));
                                return message;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return null;
        }
        #endregion

        #region Ban cashout
        private const string BcBanCashOutKey = "bc_block_cashout";
        public static void AddBlockCashOut(long userId)
        {
            var redis = GetRedis();
            redis.SetAdd(BcBanCashOutKey, userId, flags: CommandFlags.FireAndForget);
        }

        public static void RemoveBlockCashOut(long userId)
        {
            var redis = GetRedis();
            redis.SetRemove(BcBanCashOutKey, userId, flags: CommandFlags.FireAndForget);
        }

        public static async Task<bool> IsBlockCashOut(long userId)
        {
            var redis = GetRedis();
            return await redis.SetContainsAsync(BcBanCashOutKey, userId);
        }

        public static async Task<JSONArray> GetBlockCashOutList()
        {
            var redis = GetRedis();
            var res = await redis.SetMembersAsync(BcBanCashOutKey);
            var jsonarr = new JSONArray();
            foreach (var item in res)
            {
                jsonarr.Add((long)item);
            }
            return jsonarr;
        }
        #endregion

        #region Gift for old relogin
        private const string EventRelogAmountKey = "bc_relog_amount";
        private const string EventRelogDaysKey = "bc_relog_days";
        private const string EventRelogMinCashInKey = "bc_relog_cashin";
        private const string EventRelogMessageKey = "bc_relog_message";

        public static void SetReloginEvent(int daysToGift, long giftCash, long minCashIn, string message)
        {
            try
            {
                var redis = GetRedis();
                var batch = redis.CreateBatch();
                batch.StringSetAsync(EventRelogAmountKey, giftCash, flags: CommandFlags.FireAndForget);
                batch.StringSetAsync(EventRelogDaysKey, daysToGift, flags: CommandFlags.FireAndForget); // -1 = disable
                batch.StringSetAsync(EventRelogMinCashInKey, minCashIn, flags: CommandFlags.FireAndForget); // -1 = disable
                batch.StringSetAsync(EventRelogMessageKey, message, flags: CommandFlags.FireAndForget); // -1 = disable
                batch.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONNode> GetReloginEvent()
        {
            var json = new JSONObject();
            try
            {
                var redis = GetRedis();
                string days = await redis.StringGetAsync(EventRelogDaysKey);
                if (string.IsNullOrEmpty(days))
                {
                    json["days"] = "-1";
                    json["amount"] = "0";
                    json["cashIn"] = "0";
                    json["message"] = "";
                }
                else
                {
                    json["days"] = days;
                    json["amount"] = (string)await redis.StringGetAsync(EventRelogAmountKey);
                    json["cashIn"] = (string)await redis.StringGetAsync(EventRelogMinCashInKey);
                    json["message"] = (string)await redis.StringGetAsync(EventRelogMessageKey);

                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return json;
        }

        // return alert message if qualify
        public static async Task<string> CheckReloginEvent(User user)
        {
            try
            {
                var redis = GetRedis();
                long userId = user.UserId;
                string platform = user.Platform;
                string days = await redis.StringGetAsync(EventRelogDaysKey);
                if (!string.IsNullOrEmpty(days)) // has event
                {
                    var daysL = int.Parse(days);
                    var lastLoginTime = string.IsNullOrEmpty(user.TimeLogin) ? DateTime.Now : DateTime.Parse(user.TimeLogin);
                    var totalTime = (DateTime.Now - lastLoginTime).TotalDays;
                    if (daysL >= 0 && daysL <= totalTime)
                    {
                        var min = (long)await redis.StringGetAsync(EventRelogMinCashInKey);
                        long totalCashIn = await MySqlProcess.Genneral.MySqlUser.GetUserTotalCashIn(user.UserId);
                        if (totalCashIn >= min)
                        {
                            var amount = (long)await redis.StringGetAsync(EventRelogAmountKey);
                            if (amount > 0)
                            {
                                var message = (string)await redis.StringGetAsync(EventRelogMessageKey); // abc
                                var newCash = await IncEpicCash(userId, amount, platform, "Add relogin event", TransType.RELOGIN_EVENT);
                                if (newCash > -1)
                                {
                                    user.Cash = newCash;
                                }
                                return message;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return null;
        }
        #endregion

        #region acc per device count
        private const string AccOnDeviceMap = "BC_AccOnDevice:";
        public static void AddAccToDevice(string deviceId, long userId)
        {
            var redis = GetRedis();
            redis.SetAdd(AccOnDeviceMap + deviceId, userId, CommandFlags.FireAndForget);
        }

        public static async Task<int> AccOnDeviceCount(string deviceId)
        {
            var redis = GetRedis();
            return (int)await redis.SetLengthAsync(AccOnDeviceMap + deviceId);
        }
        #endregion

        #region Free Gold
        public const string DailyFreeGoldListKey = "DailyFreeGoldList";
        public const string DailyFreeGoldKey = "DailyFreeGoldUid:";
        public const string DailyFreeGoldDaysKey = "DailyFreeGoldDaysUid"; // hash
        public const string ChestItemAccquireKey = "ItemChestKey"; // hash
        private static JSONArray DailyFreeGoldCache = null;
        public static async Task<JSONArray> GetDailyFreeGoldList()
        {
            if (DailyFreeGoldCache == null)
            {
                var redis = GetRedis();
                string _data = await redis.StringGetAsync(DailyFreeGoldListKey);
                if (string.IsNullOrEmpty(_data))
                {
                    // default: [+rapidFire, +Snipe, +Gold]
                    _data = @"[
[2,2,0],
[3,3,1000],
[2,2,0],
[3,3,0],
[2,2,1500],
[4,4,0],
[2,2,0],
[3,3,0],
[2,2,0],
[3,3,2000],
[2,2,0],
[4,4,0],
[5,5,0],
[2,2,0],
[3,3,2500],
[2,2,0],
[3,3,0],
[2,2,0],
[4,4,0],
[2,2,5000],
[3,3,0],
[2,2,0],
[3,3,0],
[2,2,0],
[4,4,0],
[5,5,0],
[3,3,0],
[2,2,0],
[3,3,0],
[2,2,0],
[3,3,10000]
]";
                    DailyFreeGoldCache = JSON.Parse(_data).AsArray;
                    await redis.StringSetAsync(DailyFreeGoldListKey, _data);
                }
                else
                {
                    DailyFreeGoldCache = JSON.Parse(_data).AsArray;
                }
            }

            return DailyFreeGoldCache;
        }

        public static void SetDailyFreeGoldList(JSONArray data)
        {
            var redis = GetRedis();
            string _data = data.ToString();
            DailyFreeGoldCache = data;
            redis.StringSet(DailyFreeGoldListKey, _data, flags: CommandFlags.FireAndForget);
        }

        public static async Task<Tuple<long, int, long>> AcquireDailyFreeGoldItem(long userId)
        {
            int day;
            long addCash;
            var key = DailyFreeGoldKey + userId;
            var redis = GetRedis();
            var _data = await redis.StringGetAsync(key);
            day = 0;
            addCash = 0;
            if (await redis.KeyExistsAsync(key)) // must wait for next day
            {
                return new Tuple<long, int, long>(-10, day, addCash);
            }

            DateTime today = DateTime.Today;
            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            await redis.StringSetAsync(key, TimeUtil.TimeStamp, remaining);
            day = (int)await redis.HashIncrementAsync(DailyFreeGoldDaysKey, userId, 1) - 1;
            var gifts = await GetDailyFreeGoldList();
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            if (gifts != null && gifts.Count > day)
            {
                //var currentGift = gifts[day].AsArray;
                //if (currentGift.Count > 2)
                //{
                //    //var rf = AddRapidFireItem(userId, currentGift[0].AsInt);
                //    //var sn = AddSnipeItem(userId, currentGift[1].AsInt);
                //    //return new Tuple<int, int>(rf, sn);
                //}

                if (daysInMonth == day + 1) day = gifts.Count - 1; // force last gift
                var todayGift = gifts[day].AsArray;
                if (todayGift.Count > 2)
                {
                    day = day + 1;
                    addCash = todayGift[2].AsLong;
                    if (addCash == 0)
                        return new Tuple<long, int, long>(0, day, addCash);
                    var newCash = await IncEpicCash(userId, addCash, "server", "daily chest", TransType.DAILY_GOLD);
                    return new Tuple<long, int, long>(newCash, day, addCash);
                }
            }

            return new Tuple<long, int, long>(-11, day, addCash);
        }

        public static async Task<Tuple<int, int>> AcquireDailyFreeItem(long userId)
        {
            var today = DateTime.Today;
            var day = today.Day - 1;
            if (day < 0) return null;
            var redis = GetRedis();
            string dayGold = await redis.HashGetAsync(ChestItemAccquireKey, userId);
            var gifts = await GetDailyFreeGoldList();
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            DateTime endOfMonth = new DateTime(today.Year, today.Month, daysInMonth);
            JSONArray dayGoldArr;
            if (string.IsNullOrEmpty(dayGold))
            {
                dayGoldArr = new JSONArray();
                for (int i = 0, n = gifts.Count; i < n; i++)
                {
                    dayGoldArr.Add(0);
                }
            }
            else
            {
                dayGoldArr = JSON.Parse(dayGold).AsArray;
            }

            if (dayGoldArr.Count > day && dayGoldArr[day].AsInt == 0 && gifts != null && gifts.Count > day)
            {
                var todayGift = gifts[day].AsArray;
                if (todayGift.Count > 1)
                {
                    dayGoldArr[day] = 1;
                    redis.HashSet(ChestItemAccquireKey, userId, dayGoldArr.ToString(), flags: CommandFlags.FireAndForget);
                    var rf = await AddRapidFireItem(userId, todayGift[0].AsInt);
                    var sn = await AddSnipeItem(userId, todayGift[1].AsInt);
                    return new Tuple<int, int>(rf, sn);
                }
            }

            return null;
        }

        public static async Task<JSONNode> GetDailyStatus(long userId)
        {
            var itemKey = DailyFreeGoldKey + userId;

            var redis = GetRedis();
            string item = await redis.StringGetAsync(itemKey);
            string dayGold = await redis.HashGetAsync(DailyFreeGoldDaysKey, userId);
            string dayItem = await redis.HashGetAsync(ChestItemAccquireKey, userId);

            var res = new JSONObject();
            if (!string.IsNullOrEmpty(item))
                res["lastCheckIn"] = item;
            if (!string.IsNullOrEmpty(dayItem))
                res["dayItem"] = JSON.Parse(dayItem).AsArray;
            if (!string.IsNullOrEmpty(dayGold))
                res["dayGold"] = dayGold;
            return res;
        }

        public static void ClearDailyGifts()
        {
            var redis = GetRedis();
            redis.KeyDelete(DailyFreeGoldDaysKey, CommandFlags.FireAndForget);
            redis.KeyDelete(ChestItemAccquireKey, CommandFlags.FireAndForget);
        }
        #endregion

        #region Test
        public static volatile bool CheckDupplicateLogin = true;
        #endregion

        #region EventCashin
        public static async Task<string> EventCashinStart()
        {
            try
            {
                var redis = GetRedis();
                string res = await redis.StringGetAsync(EVENT_CASHIN_START);
                return string.IsNullOrEmpty(res) ? "9999-12-10 11:00:00" : res;
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return "9999-12-10 11:00:00";
        }
        public static async Task<string> EventCashinEnd()
        {
            try
            {
                var redis = GetRedis();
                string res = await redis.StringGetAsync(EVENT_CASHIN_END);
                return string.IsNullOrEmpty(res) ? "9999-12-10 11:00:01" : res;
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return "9999-12-10 11:00:01";
        }
        public static void SetEventCashinTime(string start, string end)
        {
            try
            {
                var redis = GetRedis();
                redis.StringSet(EVENT_CASHIN_START, start, flags: CommandFlags.FireAndForget);
                redis.StringSet(EVENT_CASHIN_END, end, flags: CommandFlags.FireAndForget);
                SqlLogger.eventCashInEnded = false;
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<JSONArray> GetTopCashInPrize()
        {
            var redis = GetRedis();
            var _prizes = await redis.StringGetAsync(EVENT_CASHIN_GIFT);
            return string.IsNullOrEmpty(_prizes) ? new JSONArray() : JSON.Parse(_prizes).AsArray;
        }

        public static void SetTopCashInPrize(JSONArray prizes)
        {
            var redis = GetRedis();
            redis.StringSet(EVENT_CASHIN_GIFT, prizes.ToString(), flags: CommandFlags.FireAndForget);
        }
        #endregion

        #region phone verify
        private const string verifiedPhoneKey = "verified_phone";
        public static void AddVerifiedPhone(string phone)
        {
            try
            {
                var redis = GetRedis();
                redis.SetAdd(verifiedPhoneKey, phone, flags: CommandFlags.FireAndForget); // no interest in response
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task<bool> IsVerifiedPhone(string phone)
        {
            try
            {
                var redis = GetRedis();
                return await redis.SetContainsAsync(verifiedPhoneKey, phone);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return false;
        }
        #endregion

        private const string CardOutAutoKey = "CardOutAuto";
        public static async Task<bool> IsCardOutAuto()
        {
            try
            {
                var redis = GetRedis();
                var res = await redis.StringGetAsync(CardOutAutoKey);
                if (res.IsNullOrEmpty)
                {
                    return false;
                }
                else
                {
                    return ((int)res) != 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
            return false;
        }

        public static void SetCardOutAuto(bool auto)
        {
            try
            {
                var redis = GetRedis();
                redis.StringSet(CardOutAutoKey, auto ? 1 : 0, flags: CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("Redis error: " + ex.ToString());
            }
        }

        public static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        public static async Task<string> ReadTextAsync(string filePath)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.Unicode.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }

        public static async void CheckCashSave(long userId, long cashMin, long cashSave, string platform, int delayMs = 1000)
        {
            try
            {
                await Task.Delay(delayMs);
                var userCash = await GetUserCash(userId);
                if (userCash > -1 && userCash < cashMin)
                {
                    var user = await MySqlProcess.Genneral.MySqlUser.GetUserInfo(userId);
                    if (user != null)
                    {
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!config.CheckVerifyPhone || (!string.IsNullOrEmpty(user.PhoneNumber) && !string.IsNullOrEmpty(user.DeviceId)))
                        {
                            var redis = GetRedis();
                            var key = config.CheckVerifyPhone ? string.Format(CASH_SAVE_KEY, user.DeviceId, user.PhoneNumber)
                                : string.Format(CASH_SAVE_KEY, user.Username, user.UserId);
                            var remaining = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
                            var check = await redis.StringGetAsync(key);
                            if (check.IsNullOrEmpty)
                            {
                                // ok
                                redis.StringSet(key, TimeUtil.TimeStamp, remaining, When.NotExists, CommandFlags.FireAndForget);
                                var newCash = await IncEpicCash(userId, cashSave, platform, "cash save", TransType.CASH_SAVE);
                                if (newCash < 0)
                                {
                                    Logger.Error("Cash save fail, result: " + newCash);
                                }
                                else
                                {
                                    var json = new SimpleJSON.JSONObject();
                                    json["userid"] = userId;
                                    json["newCash"] = newCash;
                                    json["changeCash"] = cashSave;
                                    json["reason"] = "Hệ thống gửi tặng bạn gói cứu trợ";
                                    await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json.SaveToCompressedBase64()));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("CheckCashSave ex: " + ex.ToString());
            }
        }

        private const string MoMoInfoKey = "momo-info";
        public static async Task<JSONNode> GetMomoInfo()
        {
            try
            {
                var redis = GetRedis();
                var data = await redis.StringGetAsync(MoMoInfoKey);
                if (!data.IsNullOrEmpty)
                {
                    return JSON.Parse(data.ToString());
                }

                var json = await EpicApi.GetMoMoAcc();
                if (json != null) redis.StringSet(MoMoInfoKey, json.ToString(), TimeSpan.FromMinutes(5), flags: CommandFlags.FireAndForget);
                return json;
            }
            catch (Exception ex)
            {
                Logger.Error("GetMomoInfo ex: " + ex.ToString());
            }
            return null;
        }

        public static void ClearMomoInfoCache()
        {
            try
            {
                var redis = GetRedis();
                redis.KeyDelete(MoMoInfoKey, CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                Logger.Error("ClearMomoInfoCache ex: " + ex.ToString());
            }
        }
    }
}
