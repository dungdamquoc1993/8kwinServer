using BanCa.Redis;
using BanCa.Sql;
using BanCa.WebService;
using Database;
using Entites.General;
using MySqlProcess.Genneral;
using PAYHandler;
using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BanCa.Libs
{
    public class LobbyService
    {
        private NetworkServer NetworkServer;
        private TaskRunner TaskRun;
        private ConcurrentDictionary<string, User> loggedInUser = new ConcurrentDictionary<string, User>();
        private ConcurrentDictionary<long, User> loggedInByUserId = new ConcurrentDictionary<long, User>();
        private ConcurrentDictionary<string, User> loggedInByUsername = new ConcurrentDictionary<string, User>();
        //private static string capchaUrl;

        private static List<long> ItemCards = new List<long> { 10000, 20000, 50000, 100000, 200000, 500000 };
        //private Dictionary<string, string> clientIdToCapcha = new Dictionary<string, string>();
        private BanCaServer server;

        public LobbyService(BanCaServer server, NetworkServer NetworkServer, TaskRunner TaskRun)
        {
            this.server = server;
            this.NetworkServer = NetworkServer;
            this.TaskRun = TaskRun;
        }

        public int Count { get { return loggedInUser.Count; } }

        public async void onRemovePeer(string clientId)
        {
            try
            {
                if (!string.IsNullOrEmpty(clientId) && loggedInUser.ContainsKey(clientId))
                {
                    var user = loggedInUser[clientId];
                    loggedInUser.TryRemove(clientId, out var a);
                    loggedInByUserId.TryRemove(user.UserId, out var b);
                    loggedInByUsername.TryRemove(user.Username, out var c);
                    //Logger.Info("Remove user " + user.UserId);

                    var onlineTime = TimeUtil.TimeStamp - user.TimestampLogin;
                    await RedisManager.TotalOnlineTimeInc(user.UserId, onlineTime);

                    var uid = user.UserId;
                    var port = NetworkServer.Port;
                    if (RedisManager.TryLockUserId(uid))
                    {
                        var loginServer = RedisManager.GetPlayerServer(uid);
                        if (loginServer == port) // this user has correct login
                        {
                            RedisManager.RemovePlayerServer(uid);
                            SqlLogger.LogLogout(uid, await RedisManager.GetUserCash(uid), user.Username + " " + NetworkServer.Port + " " + user.DeviceId + " " + user.IP);
                            SqlLogger.LogLogoutStart(uid);
                        }
                        RedisManager.ReleaseLockUserId(uid);
                    }
                }

                //if (!string.IsNullOrEmpty(clientId))
                //{
                //    lock (clientIdToCapcha)
                //    {
                //        if (clientIdToCapcha.ContainsKey(clientId))
                //        {
                //            clientIdToCapcha.Remove(clientId);
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Logger.Error("onRemovePeer ex: " + ex.ToString());
            }
        }

        public User IsLogin(long userId)
        {
            if (loggedInByUserId.TryGetValue(userId, out var a))
                return a;
            return null;
        }

        public User IsLogin(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId) && loggedInUser.TryGetValue(clientId, out var a))
                return a;
            return null;
        }

        public JSONArray getOnlineUsers()
        {
            return JSON.ListToJson(loggedInByUserId.Keys);
        }

        public async Task<User> Login(string clientId, string username, string password)
        {
            if (clientId != null && loggedInUser.TryGetValue(clientId, out var res)) // that peer is request login again, do nothing
            {
                var redisCash = await BanCa.Redis.RedisManager.GetUserCash(res.UserId);
                res.Cash = redisCash >= 0 ? redisCash : res.Cash;

                var player = new Player();
                player.Id = res.UserId;
                await SqlLogger.LoadImportantPlayerProperties(res.UserId, player);
                res.BcLevel = player.Level;
                res.BcExp = player.Exp;

                if (res != null)
                {
                    //res.error = 1;
                    //if (Config.IsMaintain)
                    //{
                    //    server.Alert(res.UserId, "Hello " + res.Nickname);
                    //    //return res;
                    //}
                    //Logger.Info("Peer relogin user: " + res.UserId);
                    return res;
                }
            }
            else if (username != null && loggedInByUsername.TryGetValue(username, out var old)) // new peer is connecting to same acc, same server, kick old acc, new acc will login
            {
                // same user but with new client id
                if (!string.IsNullOrEmpty(old.ClientId))
                {
                    NetworkServer.Kick(old.ClientId);
                    onRemovePeer(old.ClientId);
                    Logger.Info("New peer connect to same user: " + old.UserId);
                }

                //var user = new User();
                //user.error = 304;
                //return user;
            }

            var res2 = await MySqlUser.Login(username, password);
            if (res2.error == 0)
            {
                var redisCash = await BanCa.Redis.RedisManager.GetUserCash(res2.UserId);
                res2.Cash = redisCash >= 0 ? redisCash : res2.Cash;
                if (redisCash < 0)
                {
                    RedisManager.SetUserCash(res2.UserId, res2.Cash);
                }

                var player = new Player();
                player.Id = res2.UserId;
                await SqlLogger.LoadImportantPlayerProperties(res2.UserId, player);
                res2.BcLevel = player.Level;
                res2.BcExp = player.Exp;
                RedisManager.SavePlayerIfNotExist(player);

                if (loggedInByUserId.TryGetValue(res2.UserId, out var old))
                {
                    if (!string.IsNullOrEmpty(old.ClientId))
                    {
                        NetworkServer.Kick(old.ClientId);
                        onRemovePeer(old.ClientId);
                        Logger.Info("New peer connect to same user (2): " + old.UserId);
                    }
                }
                loggedInUser[clientId] = res2;
                loggedInByUserId[res2.UserId] = res2;
                loggedInByUsername[res2.Username] = res2;

                res2.TimestampLogin = TimeUtil.TimeStamp;
                //res2.error = 1;
                //if (Config.IsMaintain)
                //{
                //    server.Alert(res2.UserId, "Hello " + res2.Nickname);
                //    //return res2;
                //}
                return res2;
            }
            else
            {
                return res2;
            }
        }

        public bool onClientNotify(BanCaServer server, string clientId, string route, JSONNode msg)
        {
            return false;
        }

        public bool onClientRequest(BanCaServer server, string clientId, int msgId, string route, JSONNode msg)
        {
            switch (route)
            {
                case "getbtcaddress":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        var userid = user.UserId;
                        string coinSymbol = msg.HasKey("coin") ? msg["coin"].Value : "BTC"; // BTC LTC ETH TUSD USDC USDT.ERC20
                        TaskRunner.RunOnPool(async () =>
                        {
                            try
                            {
                                var res = new JSONObject();
                                res["code"] = 200;
                                var ip = ConfigJson.Config["webservice-ip"].Value;
                                var port = ConfigJson.Config["webservice-port"].Value;
                                //string ipn = string.Format("http://{0}:{1}/bancaapi/coinpaymentipn/{2}", ip, port, userid);
                                var dns = ConfigJson.Config["webservice-dns"].Value;
                                string ipn = string.Format("https://{0}/bancaapi/coinpaymentipn/{1}", dns, userid);
                                res["ipn"] = ipn;
                                res["data"] = await libCoinPaymentsNET.CoinPayments.GetCallbackAddress(ipn, coinSymbol);
                                var rep = res.ToString();
                                Logger.Info("getbtcaddress rep: " + rep);

                                NetworkServer.ResponseToClient(clientId, msgId, res);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error getbtcaddress: " + ex.ToString());
                            }
                        });
                    }
                    return true;
                case "xxengCashin":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        //var ccash = msg["ccash"].ToString();
                        var ccash = msg["ccash"].AsLong;
                        TaskRunner.RunOnPool(async () =>
                        {
                            if (ccash > 0) // xxeng => bc
                            {
                                var msg2 = new JSONObject();
                                msg2["ok"] = false;
                                var res = await EpicApi.viprikCashIn(user.Nickname, ccash); // < 0
                                if (res["success"].AsBool)
                                {
                                    var newCash = await BanCa.Redis.RedisManager.IncEpicCash(user.UserId, ccash, "xxeng", "xxeng cashin", BanCa.Redis.TransType.XXENG_CHANGE_CASH);
                                    msg2["ok"] = true;
                                    msg2["newCash"] = newCash;
                                    msg2["Cash"] = res["data"];
                                }
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                            else // bc => xxeng
                            {
                                var msg2 = new JSONObject();
                                msg2["ok"] = false;
                                
                                var newCash = await BanCa.Redis.RedisManager.IncEpicCash(user.UserId, ccash, "xxeng", "xxeng cashout", BanCa.Redis.TransType.XXENG_CHANGE_CASH);
                                if (newCash >= 0) // success
                                {
                                    var res = await EpicApi.viprikCashIn(user.Nickname, ccash);
                                    if (res["success"].AsBool)
                                    {
                                        msg2["ok"] = true;
                                        msg2["newCash"] = newCash;
                                        msg2["Cash"] = res["data"];
                                    }

                                }
                                
                                
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                        });
                    }
                    return true;
                case "xxenglogin":
                    {
                        string xusername = msg["username"];
                        string xpassword = msg["password"];
                        string platform = msg["platform"];
                        string ip = NetworkServer.GetEndPoint(clientId);
                        xusername = xusername.ToLower();
                        TaskRunner.RunOnPool(async () =>
                        {
                            var Xres = await EpicApi.xxengLogIn(xusername, xpassword, platform);
                            //{"success":true,"errorCode":"0","sessionKey":"base64 str","accessToken":""}
                            if (!Xres["success"].AsBool)
                            {
                                var msg3 = new JSONObject();
                                msg3["ok"] = false;
                                msg3["err"] = -100;
                                NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                return;
                            }
                            var s = Convert.FromBase64String(Xres["sessionKey"].Value);
                            var str = Encoding.UTF8.GetString(s);
                            var Xres2 = JSON.Parse(str);
                            // {"nickname":"dxv34s","avatar":"11","vinTotal":256,"xuTotal":2170000,"vippoint":2,"vippointSave":2,"createTime":"04-12-2019","ipAddress":"10.140.0.8","certificate":false,"luckyRotate":1,"daiLy":0,"mobileSecure":1,"birthday":""}

                            // quicklogin
                            const string _secret = "bcx2020";
                            string deviceId = md5(xusername + _secret);
                            string username = MySqlProcess.Util.GenString(deviceId.Length >= 45 ? deviceId.Substring(0, 45) : deviceId);
                            string password = deviceId;

                            var res = await MySqlUser.LoginByDevice(username, password, username, platform, deviceId, ip, await RedisManager.AccOnDeviceCount(deviceId), Config.NumberOfAccountPerDevice, "vi");
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            if (res.error == 0) // success
                            {
                                var res2 = await Login(clientId, res.Username, password);
                                if (res2.error == 0 && RedisManager.TryLockUserId(res2.UserId))
                                {
                                    var loginServer = RedisManager.GetPlayerServer(res2.UserId);
                                    if (RedisManager.CheckDupplicateLogin && loginServer != -1 && loginServer != this.server.Port) // this user has already login (on different port)
                                    {
                                        if (Config.KickDuplicateUsers == 1)
                                        {
                                            Logger.Info("Duplicate login, kick old acc: " + res2.UserId);
                                            BanCaLib.AddUserIdKickOnPort(res2.UserId, loginServer);
                                        }
                                        RedisManager.RemovePlayerServer(res2.UserId);
                                    }
                                    RedisManager.AddAccToDevice(deviceId, res2.UserId);
                                    var config = LobbyConfig.OpenAllConfig;
                                    res2.Language = "vi";
                                    res2.Platform = platform;
                                    res2.IP = ip;
                                    res2.AppId = "xxeng";
                                    if (string.IsNullOrEmpty(res2.Cp))
                                        res2.Cp = "xxeng";
                                    res2.VersionCode = 0;
                                    res2.ClientId = clientId;
                                    //Logger.Info("Set login code " + vcode);

                                    msg2["ok"] = true;
                                    msg2["username"] = res.Username;
                                    msg2["password"] = password;
                                    msg2["userId"] = res2.UserId;
                                    msg2["avatar"] = res2.Avatar = Xres2["avatar"];
                                    msg2["nickname"] = res2.Nickname = Xres2["nickname"];
                                    msg2["vipId"] = res2.VipId;
                                    msg2["vipPoint"] = res2.VipPoint;
                                    msg2["level"] = res2.BcLevel;
                                    msg2["exp"] = res2.BcExp;
                                    msg2["phone"] = res2.PhoneNumber;
                                    msg2["verifyLogin"] = res2.VerifyLogin;
                                    msg2["onlineTime"] = await RedisManager.GetTotalOnlineTime(res2.UserId);
                                    res2.IapCount = await RedisManager.GetIapCount(res2.UserId);
                                    msg2["iap"] = res2.IapCount;
                                    res2.CardInCount = await RedisManager.GetCardInCount(res2.UserId);
                                    msg2["cardin"] = res2.CardInCount;
                                    msg2["meta"] = config.ToJson();
                                    msg2["config"] = Config.ToJson();
                                    RedisManager.SetPlayerServer(res2.UserId, NetworkServer.Port);
                                    if (res.NewAcc && await RedisManager.isFirstAcc(res2.DeviceId, res2.UserId))
                                    {
                                        RedisManager.logFirstUserAcc(res2.DeviceId, res2.UserId);
                                        await RedisManager.AddRapidFireItem(res2.UserId, config.FirstAccRapidFireGift);
                                        await RedisManager.AddSnipeItem(res2.UserId, config.FirstAccSnipeGift);
                                    }
                                    msg2["cash"] = res2.Cash;
                                    RedisManager.ReleaseLockUserId(res2.UserId);
                                    SqlLogger.LogLogin(res2.UserId, res2.Cash, res2.Username + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                    SqlLogger.LogLoginStart(res2.UserId, deviceId, platform, ip);
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);

                                    await MySqlUser.UpdateNicknameAvatar(res2.UserId, res2.Nickname, res2.Avatar);
                                    postLogin(server, clientId, res2);
                                    return;
                                }
                                else
                                {
                                    msg2["err"] = res2.error != 0 ? 100 + res2.error : -2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            else
                            {
                                msg2["err"] = res.error;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                            //
                        });
                    }
                    return true;
                case "topCash":
                    {
                        var limit = msg["limit"].AsInt;
                        if (limit > 100 || limit < 0)
                        {
                            limit = 100;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var list = new JSONArray();
                            var msg2 = new JSONObject();
                            var results = await BanCa.Redis.RedisManager.GetTopCash(limit);

                            foreach (var item in results)
                            {
                                list.Add(item.ToJson());
                            }
                            msg2["data"] = list;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topWeek":
                    {
                        var limit = msg["limit"].AsInt;
                        var index = msg["index"].AsInt;
                        if (limit > 50 || limit < 0)
                        {
                            limit = 50;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            if (index >= 0)
                            {
                                var list = await BanCa.Redis.RedisManager.GetTopWeek(limit);
                                msg2["data"] = list;
                            }
                            else
                            {
                                var lbItems = await SqlLogger.GetLeaderboardMonthOrWeek(true, index);
                                var _data = new JSONArray();
                                for (int i = 0, n = lbItems.Count > limit ? limit : lbItems.Count; i < n; i++)
                                {
                                    var _it = lbItems[i];
                                    var item = new JSONObject();
                                    //item["username"] = _it.Username;
                                    item["avatar"] = _it.Avatar;
                                    item["cash"] = _it.CashGain;
                                    item["level"] = _it.Level;
                                    item["nickname"] = _it.Nickname;
                                    item["prize"] = _it.Prize;
                                    _data.Add(item);
                                }
                                msg2["data"] = _data;
                            }
                            //Logger.Info("Lb old week: " + msg2.ToString());
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topWeekRank":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = await RedisManager.GetTopWeekRank(user.Username);
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topMonth":
                    {
                        var limit = msg["limit"].AsInt;
                        var index = msg["index"].AsInt;
                        if (limit > 50 || limit < 0)
                        {
                            limit = 50;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            if (index >= 0)
                            {
                                var list = await BanCa.Redis.RedisManager.GetTopMonth(limit);
                                msg2["data"] = list;
                            }
                            else
                            {
                                var lbItems = await SqlLogger.GetLeaderboardMonthOrWeek(false, index);
                                var _data = new JSONArray();
                                for (int i = 0, n = lbItems.Count > limit ? limit : lbItems.Count; i < n; i++)
                                {
                                    var _it = lbItems[i];
                                    var item = new JSONObject();
                                    //item["username"] = _it.Username;
                                    item["avatar"] = _it.Avatar;
                                    item["cash"] = _it.CashGain;
                                    item["level"] = _it.Level;
                                    item["nickname"] = _it.Nickname;
                                    item["prize"] = _it.Prize;
                                    _data.Add(item);
                                }
                                msg2["data"] = _data;
                                //Logger.Info("Lb old month: " + msg2.ToString());
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topMonthRank":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = await RedisManager.GetTopMonthRank(user.Username);
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getTopPrizes":
                    {
                        var isWeek = msg["isWeek"].AsBool;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            var list = await BanCa.Redis.RedisManager.GetTopPrize(isWeek);
                            msg2["data"] = list;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topLevel":
                    {
                        var limit = msg["limit"].AsInt;
                        if (limit > 100 || limit < 0)
                        {
                            limit = 100;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var list = new JSONArray();
                            var msg2 = new JSONObject();
                            var results = await BanCa.Redis.RedisManager.GetTopLevel(limit);

                            foreach (var item in results)
                            {
                                list.Add(item.ToJson());
                            }
                            msg2["data"] = list;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "rankCash":
                    {
                        var myCash = msg["cash"].AsLong;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var result = await BanCa.Redis.RedisManager.GetRankByCash(myCash);
                            msg2["data"] = result;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "rankLevel":
                    {
                        var myLevel = msg["level"].AsInt;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var result = await BanCa.Redis.RedisManager.GetRankByLevel(myLevel);
                            msg2["data"] = result;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getPendingMessages":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var result = await BanCa.Redis.RedisManager.FlushPendingMessage(user.UserId);
                            msg2["data"] = result;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "register":
                    {
                        string username = msg["username"];
                        username = username.Trim().ToLower();
                        string password = msg["password"];
                        password = password.Trim().ToLower();
                        string deviceId = msg["deviceId"];
                        string platform = msg["platform"];
                        string language = msg["language"];
                        //string capcha = msg["capcha"];
                        string appId = msg["appId"];
                        string cp = msg["cp"];
                        int vcode = msg["vcode"].AsInt;
                        string ip = NetworkServer.GetEndPoint(clientId);
                        if (string.IsNullOrEmpty(deviceId) || deviceId.Length < 10 || !MySqlUser.ValidString(username) || !MySqlUser.ValidString(password) || username.Equals(password))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }

                        if (RedisManager.IsBlockDeviceId(deviceId) || RedisManager.IsBlockIp(ip))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -10;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            if (await RedisManager.AccOnDeviceCount(deviceId) >= Config.NumberOfAccountPerDevice)
                            {
                                msg2["ok"] = false;
                                msg2["err"] = -11;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }

                            if (!await RedisManager.CanRegisterInDay(deviceId))
                            {
                                msg2["ok"] = false;
                                msg2["err"] = -15;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }

                            var _password = password;
                            //password = MySqlCommon.md5(password);
                            var res = await MySqlUser.Register(username, password, platform, deviceId, ip, language);
                            msg2["ok"] = false;
                            if (res.error == 0) // success
                            {
                                RedisManager.IncRegisterInDay(deviceId);
                                var res2 = await Login(clientId, username, password);
                                if (res2.error == 0 && RedisManager.TryLockUserId(res2.UserId))
                                {
                                    var config = LobbyConfig.GetConfig(appId, vcode);
                                    RedisManager.AddAccToDevice(deviceId, res2.UserId);
                                    res2.Language = language;
                                    res2.Platform = platform;
                                    res2.DeviceId = deviceId;
                                    res2.IP = ip;
                                    res2.AppId = appId;
                                    if (string.IsNullOrEmpty(res2.Cp))
                                        res2.Cp = cp;
                                    res2.VersionCode = vcode;
                                    res2.ClientId = clientId;
                                    res2.Password = _password;

                                    msg2["ok"] = true;
                                    msg2["username"] = username;
                                    msg2["password"] = _password;
                                    msg2["userId"] = res2.UserId;
                                    msg2["avatar"] = res2.Avatar;
                                    msg2["nickname"] = res2.Nickname;
                                    msg2["vipId"] = res2.VipId;
                                    msg2["vipPoint"] = res2.VipPoint;
                                    msg2["level"] = res2.BcLevel;
                                    msg2["exp"] = res2.BcExp;
                                    res2.IapCount = await RedisManager.GetIapCount(res2.UserId);
                                    msg2["iap"] = res2.IapCount;
                                    res2.CardInCount = await RedisManager.GetCardInCount(res2.UserId);
                                    msg2["cardin"] = res2.CardInCount;
                                    msg2["onlineTime"] = await RedisManager.GetTotalOnlineTime(res2.UserId);
                                    msg2["phone"] = res2.PhoneNumber;
                                    msg2["verifyLogin"] = res2.VerifyLogin;
                                    msg2["meta"] = config.ToJson();
                                    msg2["config"] = Config.ToJson();
                                    RedisManager.SetPlayerServer(res2.UserId, NetworkServer.Port);
                                    if (await RedisManager.isFirstAcc(deviceId, res2.UserId))
                                    {
                                        RedisManager.logFirstUserAcc(deviceId, res2.UserId);
                                        res2.Cash = config.DailyCash;
                                        await RedisManager.IncEpicCash(res2.UserId, config.DailyCash, res2.Platform, "DAILY_CASH_REGISTER", TransType.NEW_ACCOUNT);
                                        RedisManager.LogFreeCashTime(deviceId, res2.UserId);
                                        await RedisManager.AddRapidFireItem(res2.UserId, config.FirstAccRapidFireGift);
                                        await RedisManager.AddSnipeItem(res2.UserId, config.FirstAccSnipeGift);
                                        SqlLogger.LogDailyCash(res2.UserId, res2.Cash, config.DailyCash, server.Port);
                                        msg2["bonus"] = config.DailyCash;
                                    }

                                    var msgEvent = await RedisManager.CheckCointEvent(res2);
                                    if (!string.IsNullOrEmpty(msgEvent))
                                    {
                                        server.AlertStr(res2.UserId, msgEvent);
                                    }

                                    msg2["cash"] = res2.Cash;
                                    RedisManager.ReleaseLockUserId(res2.UserId);
                                    SqlLogger.LogLogin(res2.UserId, res2.Cash, res2.Username + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                    SqlLogger.LogLoginStart(res2.UserId, deviceId, platform, ip);
                                    MySqlUser.SaveAppId(res2.UserId, appId, vcode, cp);
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    postLogin(server, clientId, res2);
                                    return;
                                }
                                else
                                {
                                    msg2["err"] = res2.error != 0 ? 100 + res2.error : -2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            else
                            {
                                msg2["err"] = res.error;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "login":
                    {
                        string username = msg["username"];
                        username.Trim().ToLower();
                        string password = msg["password"];
                        password.Trim().ToLower();
                        string deviceId = msg["deviceId"];
                        string appId = msg["appId"];
                        string cp = msg["cp"];
                        int vcode = msg["vcode"].AsInt;
                        string ip = NetworkServer.GetEndPoint(clientId);
                        if (!MySqlUser.ValidString(username) || !MySqlUser.ValidString(password))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (RedisManager.IsBlockDeviceId(deviceId) || RedisManager.IsBlockIp(ip))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -10;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var _password = password;
                            password = MySqlCommon.md5(password);

                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            var res2 = await Login(clientId, username, password);
                            if (res2.error == 0 && RedisManager.TryLockUserId(res2.UserId))
                            {
                                var loginServer = RedisManager.GetPlayerServer(res2.UserId);
                                if (RedisManager.CheckDupplicateLogin && loginServer != -1 && loginServer != this.server.Port) // this user has already login (on different port)
                                {
                                    if (Config.KickDuplicateUsers == 1)
                                    {
                                        Logger.Info("Duplicate login, kick old acc: " + res2.UserId);
                                        BanCaLib.AddUserIdKickOnPort(res2.UserId, loginServer);
                                    }
                                    RedisManager.RemovePlayerServer(res2.UserId);
                                }
                                RedisManager.AddAccToDevice(deviceId, res2.UserId);
                                var config = IsUserFromLandingPage(res2) ? LobbyConfig.OpenAllConfig : LobbyConfig.GetConfig(appId, vcode);
                                res2.IP = ip;
                                res2.AppId = appId;
                                if (string.IsNullOrEmpty(res2.Cp))
                                    res2.Cp = cp;
                                res2.VersionCode = vcode;
                                res2.ClientId = clientId;
                                res2.Password = _password;

                                msg2["ok"] = true;
                                msg2["username"] = username;
                                msg2["password"] = _password;
                                msg2["userId"] = res2.UserId;
                                msg2["avatar"] = res2.Avatar;
                                msg2["nickname"] = res2.Nickname;
                                msg2["vipId"] = res2.VipId;
                                msg2["vipPoint"] = res2.VipPoint;
                                msg2["level"] = res2.BcLevel;
                                msg2["exp"] = res2.BcExp;
                                msg2["phone"] = res2.PhoneNumber;
                                msg2["verifyLogin"] = res2.VerifyLogin;
                                msg2["onlineTime"] = await RedisManager.GetTotalOnlineTime(res2.UserId);
                                res2.IapCount = await RedisManager.GetIapCount(res2.UserId);
                                msg2["iap"] = res2.IapCount;
                                res2.CardInCount = await RedisManager.GetCardInCount(res2.UserId);
                                msg2["cardin"] = res2.CardInCount;
                                msg2["meta"] = config.ToJson();
                                msg2["config"] = Config.ToJson();
                                RedisManager.SetPlayerServer(res2.UserId, NetworkServer.Port);
                                //if (RedisManager.isFirstAcc(res2.DeviceId, res2.UserId))
                                //{
                                //    RedisManager.logFirstUserAcc(res2.DeviceId, res2.UserId);
                                //    if (res2.Cash < config.DailyCash && RedisManager.CanGetFreeCash(res2.DeviceId, res2.UserId))
                                //    {
                                //        var change = config.DailyCash - res2.Cash;
                                //        res2.Cash = config.DailyCash;
                                //        RedisManager.IncEpicCash(res2.UserId, change, res2.Platform, "DAILY_CASH_LOGIN", TransType.DAILY_LOGIN);
                                //        RedisManager.LogFreeCashTime(res2.DeviceId, res2.UserId);
                                //        RedisManager.AddRapidFireItem(res2.UserId, config.FirstAccRapidFireGift);
                                //        RedisManager.AddSnipeItem(res2.UserId, config.FirstAccSnipeGift);
                                //        SqlLogger.LogDailyCash(res2.UserId, res2.Cash, change, server.Port);
                                //        msg2["bonus"] = config.DailyCash;
                                //    }
                                //}
                                {
                                    var msgEvent = await RedisManager.CheckCointEvent(res2);
                                    if (!string.IsNullOrEmpty(msgEvent))
                                    {
                                        server.AlertStr(res2.UserId, msgEvent);
                                    }
                                }
                                {
                                    var msgEvent = await RedisManager.CheckReloginEvent(res2);
                                    if (!string.IsNullOrEmpty(msgEvent))
                                    {
                                        server.AlertStr(res2.UserId, msgEvent);
                                    }
                                }
                                msg2["cash"] = res2.Cash;
                                //msg2["cash"] = 100000;
                                RedisManager.ReleaseLockUserId(res2.UserId);
                                SqlLogger.LogLogin(res2.UserId, res2.Cash, res2.Username + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                SqlLogger.LogLoginStart(res2.UserId, deviceId, res2.Platform, ip);

                                if (string.IsNullOrEmpty(res2.DeviceId) && !string.IsNullOrEmpty(deviceId))
                                {
                                    res2.DeviceId = deviceId;
                                    MySqlUser.SaveDeviceId(res2.UserId, deviceId);
                                }
                                MySqlUser.SaveAppId(res2.UserId, appId, vcode, cp);
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                postLogin(server, clientId, res2);
                                return;
                            }
                            else
                            {
                                msg2["err"] = res2.error != 0 ? 100 + res2.error : -2;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "quickLogin":
                    {
                        string deviceId = msg["deviceId"];
                        string platform = msg["platform"];
                        string language = msg["language"];
                        string appId = "viprik";
                        string cp = msg["cp"];
                        int vcode = msg["vcode"].AsInt;
                        //Logger.Info("Quick login " + msg.ToString());
                        string ip = NetworkServer.GetEndPoint(clientId);
                        if (string.IsNullOrEmpty(deviceId))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (RedisManager.IsBlockDeviceId(deviceId) || RedisManager.IsBlockIp(ip))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -10;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            string username = MySqlProcess.Util.GenString(deviceId.Length >= 45 ? deviceId.Substring(0, 45) : deviceId);
                            //string username = MySqlProcess.Util.GenString(deviceId);
                            string password = deviceId;

                            var res = await MySqlUser.LoginByDevice(username, password, username, platform, deviceId, ip, await RedisManager.AccOnDeviceCount(deviceId), Config.NumberOfAccountPerDevice, language);
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            if (res.error == 0) // success
                            {
                                var res2 = await Login(clientId, res.Username, password);
                                if (res2.error == 0 && RedisManager.TryLockUserId(res2.UserId))
                                {
                                    var redisCash = await BanCa.Redis.RedisManager.GetUserCash(res2.UserId);
                                    //#if DEBUG
                                    msg2["cash"] = redisCash;
                                    RedisManager.SetUserCash(res2.UserId, redisCash);
                                    //#endif
                                    var loginServer = RedisManager.GetPlayerServer(res2.UserId);
                                    if (RedisManager.CheckDupplicateLogin && loginServer != -1 && loginServer != this.server.Port) // this user has already login (on different port)
                                    {
                                        if (Config.KickDuplicateUsers == 1)
                                        {
                                            Logger.Info("Duplicate login, kick old acc: " + res2.UserId);
                                            BanCaLib.AddUserIdKickOnPort(res2.UserId, loginServer);
                                        }
                                        RedisManager.RemovePlayerServer(res2.UserId);
                                    }
                                    RedisManager.AddAccToDevice(deviceId, res2.UserId);
                                    var config = IsUserFromLandingPage(res2) ? LobbyConfig.OpenAllConfig : LobbyConfig.GetConfig(appId, vcode);
                                    res2.Language = language;
                                    res2.Platform = platform;
                                    res2.IP = ip;
                                    res2.AppId = appId;
                                    if (string.IsNullOrEmpty(res2.Cp))
                                        res2.Cp = cp;
                                    res2.VersionCode = vcode;
                                    res2.ClientId = clientId;
                                    //Logger.Info("Set login code " + vcode);

                                    msg2["ok"] = true;
                                    msg2["username"] = res.Username;
                                    msg2["password"] = password;
                                    msg2["userId"] = res2.UserId;
                                    msg2["avatar"] = res2.Avatar;
                                    msg2["nickname"] = res2.Nickname;
                                    msg2["vipId"] = res2.VipId;
                                    msg2["vipPoint"] = res2.VipPoint;
                                    msg2["level"] = res2.BcLevel;
                                    msg2["exp"] = res2.BcExp;
                                    msg2["phone"] = res2.PhoneNumber;
                                    msg2["verifyLogin"] = res2.VerifyLogin;
                                    msg2["onlineTime"] = await RedisManager.GetTotalOnlineTime(res2.UserId);
                                    res2.IapCount = await RedisManager.GetIapCount(res2.UserId);
                                    msg2["iap"] = res2.IapCount;
                                    res2.CardInCount = await RedisManager.GetCardInCount(res2.UserId);
                                    msg2["cardin"] = res2.CardInCount;
                                    msg2["meta"] = config.ToJson();
                                    msg2["config"] = Config.ToJson();
                                    RedisManager.SetPlayerServer(res2.UserId, NetworkServer.Port);

                                    if (res.NewAcc && await RedisManager.isFirstAcc(res2.DeviceId, res2.UserId))
                                    {
                                        msg2["cash"] = 0;
                                        RedisManager.SetUserCash(res2.UserId, 0);
                                        RedisManager.logFirstUserAcc(res2.DeviceId, res2.UserId);
                                        //await RedisManager.AddRapidFireItem(res2.UserId, config.FirstAccRapidFireGift);
                                        //await RedisManager.AddSnipeItem(res2.UserId, config.FirstAccSnipeGift);
                                    }
                                    
                                    
                                    RedisManager.ReleaseLockUserId(res2.UserId);
                                    SqlLogger.LogLogin(res2.UserId, res2.Cash, res2.Username + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                    SqlLogger.LogLoginStart(res2.UserId, deviceId, platform, ip);
                                    MySqlUser.SaveAppId(res2.UserId, appId, vcode, cp);
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    postLogin(server, clientId, res2);
                                    return;
                                }
                                else
                                {
                                    msg2["err"] = res2.error != 0 ? 100 + res2.error : -2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            else
                            {
                                msg2["err"] = res.error;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "loginFb":
                    {
                        string fullname = msg["fullname"];
                        string facebookID = msg["facebookID"];
                        string deviceId = msg["deviceId"];
                        string platform = msg["platform"];
                        string language = msg["language"];
                        string avatar = msg["avatar"];
                        string appId = msg["appId"];
                        string cp = msg["cp"];
                        int vcode = msg["vcode"].AsInt;
                        string nickname = "";
                        string ip = NetworkServer.GetEndPoint(clientId);
                        if (string.IsNullOrEmpty(deviceId) || deviceId.Length < 10)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (RedisManager.IsBlockDeviceId(deviceId) || RedisManager.IsBlockIp(ip))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = -10;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            string username = MySqlProcess.Util.GenString(fullname + facebookID);
                            username = username.Length >= 45 ? username.Substring(0, 45) : username;

                            string password = facebookID;
                            var res = await MySqlUser.LoginFacebook(facebookID, username, nickname, password, platform, avatar, deviceId, ip, await RedisManager.AccOnDeviceCount(deviceId), Config.NumberOfAccountPerDevice, language);
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            if (res.error == 0) // success
                            {
                                var res2 = await Login(clientId, res.Username, password);
                                if (res2.error == 0 && RedisManager.TryLockUserId(res2.UserId))
                                {
                                    var loginServer = RedisManager.GetPlayerServer(res2.UserId);
                                    if (RedisManager.CheckDupplicateLogin && loginServer != -1 && loginServer != this.server.Port) // this user has already login (on different port)
                                    {
                                        if (Config.KickDuplicateUsers == 1)
                                        {
                                            Logger.Info("Duplicate login, kick old acc: " + res2.UserId);
                                            BanCaLib.AddUserIdKickOnPort(res2.UserId, loginServer);
                                        }
                                        RedisManager.RemovePlayerServer(res2.UserId);
                                    }
                                    RedisManager.AddAccToDevice(deviceId, res2.UserId);
                                    var config = IsUserFromLandingPage(res2) ? LobbyConfig.OpenAllConfig : LobbyConfig.GetConfig(appId, vcode);
                                    res2.Language = language;
                                    res2.Platform = platform;
                                    res2.IP = ip;
                                    res2.AppId = appId;
                                    if (string.IsNullOrEmpty(res2.Cp))
                                        res2.Cp = cp;
                                    res2.VersionCode = vcode;
                                    res2.ClientId = clientId;

                                    msg2["ok"] = true;
                                    msg2["username"] = res.Username;
                                    msg2["password"] = password;
                                    msg2["userId"] = res2.UserId;
                                    msg2["avatar"] = res2.Avatar;
                                    msg2["nickname"] = res2.Nickname;
                                    msg2["vipId"] = res2.VipId;
                                    msg2["vipPoint"] = res2.VipPoint;
                                    msg2["level"] = res2.BcLevel;
                                    msg2["exp"] = res2.BcExp;
                                    msg2["phone"] = res2.PhoneNumber;
                                    msg2["verifyLogin"] = res2.VerifyLogin;
                                    msg2["onlineTime"] = await RedisManager.GetTotalOnlineTime(res2.UserId);
                                    res2.IapCount = await RedisManager.GetIapCount(res2.UserId);
                                    msg2["iap"] = res2.IapCount;
                                    res2.CardInCount = await RedisManager.GetCardInCount(res2.UserId);
                                    msg2["cardin"] = res2.CardInCount;
                                    msg2["meta"] = config.ToJson();
                                    msg2["config"] = Config.ToJson();
                                    RedisManager.SetPlayerServer(res2.UserId, NetworkServer.Port);
                                    if (res.NewAcc && await RedisManager.isFirstAcc(res2.DeviceId, res2.UserId))
                                    {
                                        RedisManager.logFirstUserAcc(res2.DeviceId, res2.UserId);
                                        if (res2.Cash < config.DailyCash && await RedisManager.CanGetFreeCash(res2.DeviceId, res2.UserId))
                                        {
                                            var change = config.DailyCash - res2.Cash;
                                            res2.Cash = config.DailyCash;
                                            await RedisManager.IncEpicCash(res2.UserId, change, res2.Platform, "DAILY_CASH_LOGINFB", TransType.DAILY_LOGIN_FB);
                                            RedisManager.LogFreeCashTime(res2.DeviceId, res2.UserId);
                                            await RedisManager.AddRapidFireItem(res2.UserId, config.FirstAccRapidFireGift);
                                            await RedisManager.AddSnipeItem(res2.UserId, config.FirstAccSnipeGift);
                                            SqlLogger.LogDailyCash(res2.UserId, res2.Cash, change, server.Port);
                                            msg2["bonus"] = config.DailyCash;
                                        }
                                    }
                                    {
                                        var msgEvent = await RedisManager.CheckCointEvent(res2);
                                        if (!string.IsNullOrEmpty(msgEvent))
                                        {
                                            server.AlertStr(res2.UserId, msgEvent);
                                        }
                                    }
                                    {
                                        var msgEvent = await RedisManager.CheckReloginEvent(res2);
                                        if (!string.IsNullOrEmpty(msgEvent))
                                        {
                                            server.AlertStr(res2.UserId, msgEvent);
                                        }
                                    }
                                    msg2["cash"] = res2.Cash;
                                    RedisManager.ReleaseLockUserId(res2.UserId);
                                    SqlLogger.LogLogin(res2.UserId, res2.Cash, res2.Username + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                    SqlLogger.LogLoginStart(res2.UserId, deviceId, platform, ip);
                                    MySqlUser.SaveAppId(res2.UserId, appId, vcode, cp);
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    postLogin(server, clientId, res2);
                                    return;
                                }
                                else
                                {
                                    msg2["err"] = res2.error != 0 ? 100 + res2.error : -2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            else
                            {
                                msg2["err"] = res.error;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "getProfile":
                    {
                        var targetUserId = msg["userId"].AsLong;
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string peerId = null;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            if (targetUserId == user.UserId)
                            {
                                msg2["ok"] = true;
                                msg2["username"] = user.Username;
                                msg2["userId"] = user.UserId;
                                msg2["avatar"] = user.Avatar;
                                msg2["cash"] = user.Cash;
                                msg2["nickname"] = user.Nickname;
                                msg2["vipId"] = user.VipId;
                                msg2["vipPoint"] = user.VipPoint;
                                msg2["level"] = user.BcLevel;
                                msg2["exp"] = user.BcExp;
                                msg2["phone"] = user.PhoneNumber;
                                peerId = user.ClientId;
                            }
                            else
                            {
                                var targetUser = default(User);
                                if (loggedInByUserId.TryGetValue(targetUserId, out targetUser))
                                {
                                }
                                else
                                {
                                    targetUser = await MySqlUser.GetUserInfo(targetUserId);
                                    if (targetUser.error != 0)
                                    {
                                        msg2["err"] = targetUser.error;
                                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                        return;
                                    }
                                    loggedInByUserId[targetUserId] = targetUser; // cache
                                }
                                msg2["ok"] = true;
                                msg2["username"] = "";
                                msg2["userId"] = targetUser.UserId;
                                msg2["avatar"] = targetUser.Avatar;
                                msg2["cash"] = targetUser.Cash;
                                msg2["nickname"] = targetUser.Nickname;
                                msg2["vipId"] = targetUser.VipId;
                                msg2["vipPoint"] = targetUser.VipPoint;
                                msg2["level"] = targetUser.BcLevel;
                                msg2["exp"] = targetUser.BcExp;
                                msg2["phone"] = "";
                                peerId = targetUser.ClientId;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    break;
                case "changePassword":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(() =>
                        {
                            string oldPassword = msg["oldPass"];
                            oldPassword = oldPassword.Trim().ToLower();
                            string newPassword = msg["newPass"];
                            newPassword = newPassword.Trim().ToLower();
                            var msg2 = new JSONObject();
                            if (!string.IsNullOrEmpty(user.Password) && user.Password.Equals(oldPassword) && !oldPassword.Equals(newPassword)
                            && !string.IsNullOrEmpty(newPassword) && MySqlUser.ValidString(newPassword) && !newPassword.Equals(user.Username))
                            {
                                msg2["ok"] = true;
                                msg2["password"] = newPassword;
                                MySqlUser.ChangePassword(user.UserId, newPassword);
                            }
                            else
                            {
                                msg2["ok"] = false;
                            }

                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getMessages":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var data = await RedisManager.GetHomeMessages();
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = data;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getAnnouncement":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var data = await SqlLogger.GetAnnouncement(user.UserId);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = data;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "topeventcashin":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        int limit = msg["limit"].AsInt;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var data = await SqlLogger.GetTopEventCashIn(limit);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = data;
                            msg2["prize"] = await RedisManager.GetTopCashInPrize();
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "delAnnouncement":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        var Id = msg["Id"].AsLong;
                        TaskRunner.RunOnPool(() =>
                        {
                            SqlLogger.DeleteAnnouncement(user.UserId, Id);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "readAnnouncement":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        var Id = msg["Id"].AsLong;
                        TaskRunner.RunOnPool(() =>
                        {
                            SqlLogger.ReadAnnouncement(user.UserId, Id);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "feedback":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string content = msg["content"];
                        content.Trim();
                        if (content.Length > 500)
                        {
                            content = content.Substring(0, 500);
                        }
                        TaskRunner.RunOnPool(() =>
                        {
                            SqlLogger.FeedBack(user.UserId, content);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getEvents":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            try
                            {
                                var announce = await MySqlEvent.GetList(user.Language);
                                msg2["data"] = announce;
                                msg2["ok"] = true;
                            }
                            catch (Exception ex)
                            {
                                msg2["err"] = 1;
                                Logger.Error("Error in getEvents: " + ex.ToString());
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getAnnounces":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        byte cate = msg["cate"].AsByte;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var announce = await MySqlAnnounce.GetAnnounces(user.UserId, cate);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = announce.ToJson();
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "cardIn":
                    {
                        string Seri = msg["seri"];
                        string CardNumber = msg["number"];
                        string Telco = msg["telco"];
                        int Cash = msg["cash"].AsInt;
                        string provider = msg["provider"];
                        int platform = msg["platform"].AsInt;
                        Telco = Telco.Trim().ToLower();
                        string telCode = "";
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                            msg2["ok"] = false;

                            string seri = Seri.Trim();
                            string cardnumber = CardNumber.Trim();
                            if (Telco.Contains("viettel"))
                                Telco = "viettel";
                            if (Telco.Contains("mobi"))
                                Telco = "mobifone";
                            if (Telco.Contains("vina"))
                                Telco = "vinaphone";
                            if (seri == cardnumber)
                            {
                                //response.ErrorMsg = "Mã seri và number không được giống nhau!";
                                msg2["err"] = 4;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                            if (Telco == "viettel")
                            {
                                telCode = "VTT";
                                //telCode = "vt";
                                Telco = "viettel";
                                if (!config.ActiveViettel)
                                {
                                    //response.ErrorMsg = "Nhà mạng viettel đang bảo trì vui lòng nạp thời gian khác!";
                                    msg2["err"] = 5;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                                if (!((seri.Length >= 11 && cardnumber.Length <= 15) || (seri.Length >= 11 && cardnumber.Length <= 15)))
                                {
                                    //response.ErrorMsg = "Lỗi sai định dạng thẻ viettel.\n seri 11 hoặc 14 ký tự,mã thẻ 13 hoặc 15 ký tự";
                                    msg2["err"] = 6;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            if (Telco == "mobifone")
                            {
                                telCode = "VMS";
                                //telCode = "mb";
                                Telco = "mobi";
                                if (!config.ActiveMobiphone)
                                {
                                    //response.ErrorMsg = "Nhà mạng mobifone đang bảo trì vui lòng nạp thời gian khác!";
                                    msg2["err"] = 7;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                                if (!((seri.Length >= 12 && cardnumber.Length <= 16) || (seri.Length >= 12 && cardnumber.Length <= 16)))
                                {
                                    //response.ErrorMsg = "Lỗi sai định dạng thẻ mobifone.\n seri 15 ký tự. mã thẻ 12 ký tự";
                                    msg2["err"] = 8;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            if (Telco == "vinaphone")
                            {
                                telCode = "VNP";
                                //telCode = "vn";
                                Telco = "vina";
                                if (!config.ActiveVinaphone)
                                {
                                    //response.ErrorMsg = "Nhà mạng vinaphone đang bảo trì vui lòng nạp thời gian khác!";
                                    msg2["err"] = 9;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                                if (!((seri.Length >= 12 && cardnumber.Length <= 16) || (seri.Length >= 12 && cardnumber.Length <= 16)))
                                {
                                    //response.ErrorMsg = "Lỗi sai định dạng thẻ vinaphone.\n seri 12 hoặc 14 ký tự, mã thẻ 14 ký tự";
                                    msg2["err"] = 10;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                            }
                            if (Telco == "GATE")
                            {
                                telCode = "GATE";
                                //telCode = "vn";
                                Telco = "GATE";
                            }

                            try
                            {
                                // TODO: charging service
                                // susi
                                //await VerifyChargingMuaCard(user, msg2, cardnumber, seri, telCode, Telco, Cash, provider, platform);

                                // 2020
                                //await VerifyChargingPushTheV4(user, msg2, cardnumber, seri, telCode, Telco, Cash, provider, platform);

                                //yoyo
                                await VerifyChargingVinPay(user, msg2, cardnumber, seri, telCode, Telco, Cash, provider, platform);

                                //Logger.Info("Cardin response: " + msg2.ToString());
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error in card in: " + ex.ToString());
                                msg2["ok"] = false;
                                msg2["err"] = 20;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "chargingHistory":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            //Logger.Info("chargingHistory of " + user.UserId);

                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["err"] = 0;
                            await CardChargingHistory(user, msg2);
                            msg2["giftCode"] = await MySqlPayment.GiftCodeHistory(user.UserId);
                            msg2["iap"] = await MySqlUser.GetIapHistories(user.UserId);
                            //Logger.Info("chargingHistory result " + msg2.ToString());
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "cashOutHistory":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            //Logger.Info("cashOutHistory of " + user.UserId);
                            var history = await MySqlPayment.GetCashOutHistories(user.Username, user.VersionCode);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = history.ToJson();
                            //Logger.Info("cashOutHistory result " + msg2.ToString());
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "cancelCashOut":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        long transId = msg["transId"].AsLong;
                        long price = msg["price"].AsLong;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var ok = await MySqlPayment.CancelCashOut(transId, user.UserId, price);
                            var msg2 = new JSONObject();
                            msg2["ok"] = ok;
                            if (ok)
                            {
                                var newCash = await RedisManager.IncEpicCash(user.UserId, price, "server", "cancel cashout", TransType.CANCEL_CASH_OUT);
                                msg2["cash"] = newCash;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "cashOut":
                    {
                        long cashRequest = msg["cash"].AsLong;
                        string telco = msg["telco"];
                        User user = CheckLogin(clientId, msgId);
                        if (user == null || cashRequest <= 0)
                            return true;
                        string cp = !string.IsNullOrEmpty(user.Cp) ? user.Cp : "default";
                        TaskRunner.RunOnPool(async () =>
                        {
                            bool playing = RedisManager.IsPlaying(user.UserId);
                            if (playing)
                            {
                                var response = new JSONObject();
                                response["err"] = -1; // "Yêu cầu không hợp lệ"
                                NetworkServer.Kick(clientId);
                                Logger.Info("#HACK: Cannot cashOut while playing BC: " + user.UserId);
                                RedisManager.AddHacker(user.UserId, RedisManager.HackType.CashOutWhilePlayBC, msg.ToString());
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var loginServer = RedisManager.GetPlayerServer(user.UserId);
                            if (RedisManager.CheckDupplicateLogin && loginServer != NetworkServer.Port)
                            {
                                var response = new JSONObject();
                                response["err"] = -1; // "Yêu cầu không hợp lệ"
                                NetworkServer.Kick(clientId);
                                Logger.Info("#HACK: Invalid server request cashOut: " + user.UserId);
                                RedisManager.AddHacker(user.UserId, RedisManager.HackType.CashOutInvalidServer, msg.ToString());
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            if (await RedisManager.IsBlockCashOut(user.UserId)) // black list
                            {
                                var response = new JSONObject();
                                response["err"] = 12;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                            int remain = await RedisManager.canCashOutCountPerDay(user.UserId, config);
                            if (remain <= 0) // out of quota per day
                            {
                                var response = new JSONObject();
                                response["err"] = 13;
                                response["extra"] = remain;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var remain2 = await RedisManager.canCashOutCountGoldPerDay(user.UserId, config);
                            if (!(remain2 >= cashRequest)) // out of gold quota per day
                            {
                                var response = new JSONObject();
                                response["err"] = 14;
                                response["extra"] = remain2;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }
                            var remain3 = await RedisManager.canCashOutCountGoldServerPerDay(config);
                            if (!(remain3 >= cashRequest)) // out of gold quota per day
                            {
                                var response = new JSONObject();
                                response["err"] = 15;
                                response["extra"] = remain3;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var iapTotalCash = await RedisManager.GetCardInCount(user.UserId);
                            if (iapTotalCash < config.CashOutCashInIAP)
                            {
                                var response = new JSONObject();
                                response["err"] = 16;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var cardInTotalCash = await GetTotalCardIn(user.UserId);
                            if (cardInTotalCash == -1 || cardInTotalCash < config.CashOutCashInCard || cardInTotalCash + iapTotalCash < config.CashOutCashInTotal)
                            {
                                var response = new JSONObject();
                                response["err"] = 16;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            if ((DateTime.Now - user.TimestampRegister).TotalMilliseconds < config.CashOutTimeFromRegisterMS)
                            {
                                Logger.Info("Cash out not enough register time " + user.UserId + " " + user.TimestampRegister.ToLongDateString() + " vs " + config.CashOutTimeFromRegisterMS);
                                msg2["err"] = 17;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }

                            try
                            {
                                string lang_code = user.Language;
                                float factor = config.CashOutRate;
                                if (factor <= 0) factor = 1.15f;

                                if (!ItemCards.Contains(cashRequest))
                                {
                                    cashRequest = (long)Math.Round((cashRequest / factor));
                                }
                                long cashpay = (long)(cashRequest * factor);

                                var res_cash = await RedisManager.IncEpicCash(user.UserId, -cashpay, user.Platform, "CASH_OUT", TransType.CASH_OUT, realmoney: cashRequest, rate: factor);
                                if (res_cash < 0)
                                {
                                    //"Quý khách không đủ tiền để thực hiện chức năng này";
                                    Logger.Info("Cash out not enough cash " + user.UserId + " cashpay " + cashpay + " factor " + factor + " usercash " + user.Cash + " require remain " + config.CashOutGoldRemain);
                                    msg2["err"] = 1;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }

                                if (res_cash < config.CashOutGoldRemain)
                                {
                                    await RedisManager.IncEpicCash(user.UserId, cashpay, user.Platform, "CASH_OUT remain not enough", TransType.CASH_OUT, realmoney: cashRequest, rate: factor);
                                    Logger.Info("Cash out not enough remain " + user.UserId + " cashpay " + cashpay + " factor " + factor + " usercash " + user.Cash + " require remain " + config.CashOutGoldRemain);
                                    msg2["err"] = 11;
                                    msg2["require"] = config.CashOutGoldRemain;
                                    msg2["remain"] = res_cash;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                                user.Cash = res_cash;

                                //string itemId = telco + " " + cashRequest;
                                //string query = "INSERT INTO `cashouts` ( `item_id`, `price`, `username`, `telco`, `user_id`, `platform`, `device_id`, `time_cashout`, `to_money`, `cp`) VALUES ";
                                //query += "('{0}', {1}, '{2}', '{3}', {4}, '{5}', '{6}',now(), {7}, '{8}')";
                                //query = string.Format(query, itemId, cashpay, user.Username, telco, user.UserId, user.Platform, user.DeviceId, cashRequest, cp);
                                //MySqlCommon.ExecuteNonQuery(query);
                                //SqlLogger.LogCardOut(user.UserId, user.Cash, -cashpay, itemId);
                                var status = await CardOutApi(server, user, telco, cashRequest, cashpay, cp, config);
                                msg2["ok"] = status == 0 || status == 1;
                                msg2["err"] = status == 0 || status == 1 ? 0 : -1;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error in cash out: " + ex.ToString());
                                msg2["err"] = 2;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    return true;
                case "giftCode":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        long UserID = (long)user.UserId;
                        string GiftCodeID = msg["code"];
                        //string capcha = msg["capcha"];
                        GiftCodeID = GiftCodeID.Trim();
                        // test capcha
                        //lock (clientIdToCapcha)
                        //{
                        //    if (clientId != null && clientIdToCapcha.ContainsKey(clientId)) // is wrong capcha?
                        //    {
                        //        var _capcha = clientIdToCapcha[clientId];
                        //        if (!_capcha.Equals(capcha))
                        //        {
                        //            var msg2 = new JSONClass();
                        //            msg2["ok"] = false;
                        //            msg2["err"] = -2;
                        //            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        //            return true;
                        //        }
                        //        clientIdToCapcha.Remove(clientId);
                        //    }
                        //    else // don't have capcha
                        //    {
                        //        var msg2 = new JSONClass();
                        //        msg2["ok"] = false;
                        //        msg2["err"] = -3;
                        //        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        //        return true;
                        //    }
                        //}

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = await GiftCodeHandle.GiftCode(user, GiftCodeID);
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "giftCard":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        long UserID = (long)user.UserId;
                        long GiftCardID = msg["code"].AsLong;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            Dictionary<string, string> _giftcard = await MySqlUser.ReadOneData("giftcards", "user_id,code,price,status", "user_id,code,price,status", "id=" + GiftCardID + " AND user_id=" + UserID + " AND status=0");

                            if (_giftcard.Count == 0)
                            {
                                //"GiftCard không hợp lệ";
                                msg2["err"] = 1;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }

                            var res_cash = await RedisManager.IncEpicCash(UserID, long.Parse(_giftcard["price"]), user.Platform, "GIFT_CARD", TransType.GIFT_CARD);
                            if (res_cash >= 0)
                            {
                                //update giftcard is used
                                string update_giftcard = "UPDATE giftcards SET status=1 WHERE id=" + GiftCardID;
                                MySqlCommon.ExecuteNonQuery(update_giftcard);

                                // "Giftcard được nạp thành công";
                                msg2["ok"] = true;
                                user.Cash = res_cash;
                            }
                            else
                            {
                                msg2["err"] = 2;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getIapItem":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        string appId = msg["appId"];
                        long versionCode = msg["versionCode"].AsLong;
                        //Logger.Info("Request from " + appId + " version " + versionCode);
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await IapItemManager.GetIapItems(appId, versionCode);
                            msg2["ok"] = res != null;
                            msg2["err"] = res != null ? 0 : 1;
                            msg2["data"] = res;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "verifyApple":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string lang_code = user.Language;
                        string ProductId = msg["productId"];
                        string receipt = msg["receipt"];
                        string AppId = msg["appId"];
                        long appVersionCode = msg["versionCode"].AsLong;
                        Logger.Info("verifyApple: " + msg.ToString());
                        bool ok = false;
                        var added = 0L;
                        TaskRunner.RunOnPool(async () =>
                        {
                            long CashofPackageIds = InAppPurcharse.GetPriceofPackageId(ProductId, "iphone", AppId);
                            var msg2 = new JSONObject();
                            if (CashofPackageIds > 0)
                            {
                                var res = await RedisManager.IncEpicCash(user.UserId, CashofPackageIds, user.Platform, "APPLE_BILLING", TransType.APPLE_BILLING);
                                if (res >= 0)
                                {
                                    msg2["ok"] = ok = true;
                                    msg2["err"] = 0;
                                    msg2["amount"] = added = CashofPackageIds;
                                    msg2["cash"] = user.Cash;
                                    user.Cash = res;
                                    //RedisManager.LogCashIn(user.UserId, CashofPackageIds, CashType.IapApple);
                                    user.IapCount = await RedisManager.IncIapCount(user.UserId);
                                    user.CardInCount = await RedisManager.CardInInc(user.UserId, CashofPackageIds);
                                    SqlLogger.LogIap(user.UserId, user.Cash, added, AppId + " " + appVersionCode);
                                }
                                else
                                {
                                    msg2["ok"] = false;
                                    msg2["err"] = 1;
                                }
                            }
                            if (ok)
                            {
                                var json = new SimpleJSON.JSONObject();
                                json["userid"] = user.UserId;
                                json["newCash"] = user.Cash;
                                json["changeCash"] = added;
                                json["reason"] = "";

                                NetworkServer.PushToClient(user.ClientId, "reloadCash", json, SendMode.ReliableOrdered);
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "verifyGoogle":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string lang_code = user.Language;
                        string signedData = msg["signedData"];
                        string signature = msg["signature"];
                        string AppId = msg["appId"];
                        long appVersionCode = msg["versionCode"].AsLong;
                        Logger.Info("verifyGoogle: " + msg.ToString());
                        bool ok = false;
                        TaskRunner.RunOnPool(async () =>
                        {
                            var tup = await PAY4_Handler.OnUserOperationRequest(user, signedData, signature, AppId, appVersionCode);
                            var code = tup.Item1;
                            var added = tup.Item2;
                            var msg2 = new JSONObject();
                            msg2["err"] = code;
                            msg2["ok"] = ok = code == 0;
                            msg2["amount"] = added;
                            msg2["cash"] = user.Cash;

                            if (code == 0)
                            {
                                user.IapCount = await RedisManager.IncIapCount(user.UserId);
                                user.CardInCount = await RedisManager.CardInInc(user.UserId, added);
                                SqlLogger.LogIap(user.UserId, user.Cash, added, AppId + " " + appVersionCode);
                            }
                            if (ok)
                            {
                                var json = new SimpleJSON.JSONObject();
                                json["userid"] = user.UserId;
                                json["newCash"] = user.Cash;
                                json["changeCash"] = added;
                                json["reason"] = "";

                                NetworkServer.PushToClient(user.ClientId, "reloadCash", json, SendMode.ReliableOrdered);
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "updateNickname":
                    {
                        string nickname = msg["nickname"];
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            if (user.Nickname.Equals(nickname))
                            {
                                msg2["ok"] = true;
                            }
                            else if (user.Username.Equals(nickname))
                            {
                                msg2["ok"] = false;
                                msg2["err"] = 3;
                            }
                            else if (string.IsNullOrEmpty(nickname) || nickname.Length < 6 || nickname.Length > 20)
                            {
                                msg2["ok"] = false;
                                msg2["err"] = 4;
                            }
                            else
                            {
                                var res = await MySqlUser.UpdateNickname(user.UserId, nickname);
                                bool ok = res.error == 0;
                                if (ok)
                                {
                                    user.Nickname = nickname;
                                }
                                msg2["ok"] = ok;
                                msg2["err"] = res.error;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "updateProfile":
                    {
                        string avatar = msg["avatar"];
                        string phone = msg["phone"];
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await MySqlUser.UpdateProfile(user.UserId, phone, avatar);
                            msg2["ok"] = res.error == 0;
                            msg2["err"] = res.error;

                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getJackpot":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        var msg2 = new JSONObject();
                        msg2["ok"] = true;
                        msg2["data"] = FundManager.GetJackpotJson();
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    return true;
                case "getJackpotHistory":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        int limit = msg["limit"].AsInt;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = await SqlLogger.GetJackpotHistory(limit);
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "checkVersion":
                    {
                        var msg2 = new JSONObject();
                        msg2["ok"] = true;
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    return true;
                case "lastTimeViewAds":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        int type = msg["type"].AsInt; // 0 = no skip, 1 = can skip
                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await RedisManager.LastTimeViewVideoAds(user.UserId);
                            msg2["ok"] = true;
                            msg2["data"] = res;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "viewAds":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        int type = msg["type"].AsInt; // 0 = no skip, 1 = can skip
                        if (type != 0 && type != 1)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 10;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        var reward = type == 0 ? Config.VideoAdsRewardType0 : Config.VideoAdsRewardType1;
                        if (reward <= 0)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 11;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await RedisManager.SeeVideoAds(user.UserId, type);
                            var ok = res >= 0;
                            //Logger.Info("Userid " + user.UserId + " see ads " + type + " result " + res + " reward " + reward + " raw " + msg.ToString());
                            msg2["ok"] = ok;
                            if (ok)
                            {
                                var cc = await RedisManager.IncEpicCash(user.UserId, reward, user.Platform, string.IsNullOrEmpty(user.DeviceId) ? "" : user.DeviceId, type == 0 ? TransType.VIDEO_ADS : TransType.VIDEO_ADS_SKIP);
                                msg2["cash"] = cc;
                                msg2["add"] = reward;
                                msg2["count"] = res;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getFreeGoldList":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await RedisManager.GetDailyFreeGoldList();
                            msg2["ok"] = true;
                            msg2["data"] = res;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "getDailyItems":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await RedisManager.AcquireDailyFreeGoldItem(user.UserId);
                            var res2 = await RedisManager.AcquireDailyFreeItem(user.UserId);
                            msg2["day"] = res.Item2;
                            msg2["add"] = res.Item3;
                            msg2["cash"] = res.Item1;
                            if (res2 != null)
                            {
                                msg2["rf"] = res2.Item1;
                                msg2["sn"] = res2.Item2;
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                //case "getDailyGold":
                //    {
                //        User user = CheckLogin(clientId, msgId);
                //        if (user == null)
                //            return true;

                //        int day = msg["day"].AsInt;
                //        TaskRunner.RunOnPool(() =>
                //        {
                //            var msg2 = new JSONClass();
                //            var res = RedisManager.AcquireDailyFreeGold(user.UserId, day);
                //            msg2["ok"] = res >= 0;
                //            msg2["cash"] = res;
                //            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                //        });
                //    }
                //    return true;
                case "getDailyStatus":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            var res = await RedisManager.GetDailyStatus(user.UserId);
                            msg2["ok"] = true;
                            msg2["data"] = res;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    return true;
                case "verifyPhone":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string phone = msg["phone"];
                        if (!string.IsNullOrEmpty(phone))
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                var msg2 = new JSONObject();
                                var res = await EpicApi.SendOtpSms(phone);
                                msg2["ok"] = true;
                                msg2["data"] = res;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "verifyPin":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string phone = msg["phone"];
                        string pin = msg["pin"];
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pin) && config != null)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                var msg2 = new JSONObject();
                                var res = await EpicApi.VerifyPin(phone, pin);
                                long newCash = 0;
                                if (res.HasKey("data"))
                                {
                                    var data = res["data"].AsObject;
                                    if (data.HasKey("verified"))
                                    {
                                        var verified = data["verified"].AsBool;
                                        if (verified)
                                        {
                                            user.PhoneNumber = phone;
                                            MySqlUser.SavePhoneToDb(user.UserId, phone);

                                            newCash = await RedisManager.IncEpicCash(user.UserId, config.VerifyGiftCash, "server", "verify " + phone, TransType.VERIFY_PHONE, true);
                                        }
                                    }
                                }

                                msg2["ok"] = true;
                                msg2["data"] = res;
                                msg2["cash"] = newCash;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "setLoginSmsVerify":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        if (!string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            bool on = msg["on"].AsBool;
                            user.VerifyLogin = on;
                            MySqlUser.SaveVerifySmsLoginStateToDb(user.UserId, on);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["on"] = on;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "sendSmsLoginVerify":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string phone = user.PhoneNumber;
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!string.IsNullOrEmpty(phone) && config != null)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                bool playing = RedisManager.IsPlaying(user.UserId);
                                if (playing)
                                {
                                    NetworkServer.Kick(clientId);
                                    Logger.Info("#HACK: Cannot sendSmsLoginVerify while playing BC: " + user.UserId);
                                    RedisManager.AddHacker(user.UserId, RedisManager.HackType.SmsWhilePlayBC, msg.ToString());
                                    return;
                                }

                                var cash = await RedisManager.IncEpicCash(user.UserId, -config.VerifySmsLoginCost, "server", "login_verify:" + phone, TransType.VERIFY_LOGIN_SMS, true);
                                if (cash >= 0)
                                {
                                    var msg2 = new JSONObject();
                                    var res = await EpicApi.SendOtpSms(phone);
                                    msg2["ok"] = true;
                                    msg2["data"] = res;
                                    msg2["cash"] = cash;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                                else
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["require"] = config.VerifySmsLoginCost;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "verifySmsLoginPin":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string phone = user.PhoneNumber;
                        string pin = msg["pin"];

                        if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pin))
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                var msg2 = new JSONObject();
                                var res = await EpicApi.VerifyPin(phone, pin);
                                if (res.HasKey("data"))
                                {
                                    var data = res["data"].AsObject;
                                    if (data.HasKey("verified"))
                                    {
                                        var verified = data["verified"].AsBool;
                                        if (verified)
                                        {
                                            // ok
                                        }
                                    }
                                }

                                msg2["ok"] = true;
                                msg2["data"] = res;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "sendSmsTransferVerify": // TODO
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        string phone = user.PhoneNumber;
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!string.IsNullOrEmpty(phone) && config != null)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                bool playing = RedisManager.IsPlaying(user.UserId);
                                if (playing)
                                {
                                    NetworkServer.Kick(clientId);
                                    Logger.Info("#HACK: Cannot sendSmsTransferVerify while playing BC: " + user.UserId);
                                    RedisManager.AddHacker(user.UserId, RedisManager.HackType.SmsWhilePlayBC, msg.ToString());
                                    return;
                                }

                                if (await RedisManager.GetUserCash(user.UserId) < config.TransferCashMin)
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["err"] = 1;
                                    msg2["atleast"] = config.TransferCashMin;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }

                                var cash = await RedisManager.IncEpicCash(user.UserId, -config.TransferCashCost, "server", "transfer_verify:" + phone, TransType.VERIFY_TRANSFER_SMS, true);
                                if (cash >= 0)
                                {
                                    string otp = this.GenOtp();
                                    var res = await EpicApi.SendSms(phone, otp);
                                    if (res != null && res.HasKey("CodeResult") && res["CodeResult"].AsInt == 100)
                                    {
                                        user.LastOtp = otp;
                                        user.LastGetOtp = TimeUtil.TimeStamp;
                                    }

                                    var msg2 = new JSONObject();
                                    //var res = await EpicApi.SendOtpSms(phone);
                                    msg2["ok"] = true;
                                    msg2["data"] = res;
                                    msg2["cash"] = cash;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                                else
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["err"] = 2;
                                    msg2["require"] = config.VerifySmsLoginCost;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 3;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "transferCash":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        const int OneMin = 60000;
                        string phone = user.PhoneNumber;
                        string pin = msg["pin"];
                        long uid = msg["uid"].AsLong;
                        long cashToTransfer = msg["cash"].AsLong;
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!string.IsNullOrEmpty(user.LastOtp) && TimeUtil.TimeStamp - user.LastGetOtp < OneMin)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 3;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pin))
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                bool playing = RedisManager.IsPlaying(user.UserId);
                                if (playing)
                                {
                                    NetworkServer.Kick(clientId);
                                    Logger.Info("#HACK: Cannot transferCash while playing BC: " + user.UserId);
                                    RedisManager.AddHacker(user.UserId, RedisManager.HackType.SmsWhilePlayBC, msg.ToString());
                                    return;
                                }

                                if (cashToTransfer < config.TransferCashMin)
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["atleast"] = config.TransferCashMin;
                                    msg2["err"] = 1;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }

                                var targetCash = await RedisManager.GetUserCash(uid);
                                if (targetCash < 0)
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["err"] = 2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }

                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["err"] = 4;
                                    long remain = -1;
                                    //var res = await EpicApi.VerifyPin(phone, pin);
                                    //if (res.HasKey("data"))
                                    //{
                                    //    var data = res["data"].AsObject;
                                    //    if (data.HasKey("verified"))
                                    //    {
                                    //        var verified = data["verified"].AsBool;
                                    //        if (verified)
                                    //        {
                                    //            var fee = (long)Math.Round(cashToTransfer * config.TransferCostFeePercent);
                                    //            var cost = cashToTransfer + fee;
                                    //            // ok
                                    //            remain = await RedisManager.IncEpicCash(user.UserId, -cost, "server", "transfer_to:" + uid, TransType.TRANSFER_CASH, true);
                                    //            if (remain >= 0)
                                    //            {
                                    //                var newCash = await RedisManager.IncEpicCash(uid, cashToTransfer, "server", "transfer_from:" + user.UserId, TransType.TRANSFER_CASH, true);
                                    //                if (newCash >= 0)
                                    //                {
                                    //                    var json = new SimpleJSON.JSONObject();
                                    //                    json["userid"] = uid;
                                    //                    json["newCash"] = newCash;
                                    //                    json["changeCash"] = cashToTransfer;
                                    //                    json["reason"] = string.Format("Bạn nhận được {0} gold chuyển từ {1}", cashToTransfer, user.Nickname);

                                    //                    var redis = RedisManager.GetRedis();
                                    //                    await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", uid, json.SaveToCompressedBase64()));
                                    //                    SqlLogger.LogTransferCash(user.UserId, uid, cost, cashToTransfer);
                                    //                    msg2["ok"] = true;
                                    //                    msg2["err"] = 0;
                                    //                }
                                    //                else
                                    //                {
                                    //                    msg2["err"] = 5;
                                    //                    Logger.Info("Transfer fail to " + uid + " " + newCash);
                                    //                    remain = await RedisManager.IncEpicCash(user.UserId, cost, "server", "refund_transfer_to:" + uid, TransType.TRANSFER_CASH, true);
                                    //                }
                                    //            }
                                    //            else
                                    //            {
                                    //                msg2["err"] = 6;
                                    //            }
                                    //        }
                                    //    }
                                    //}
                                    if (user.LastOtp.Equals(pin))
                                    {
                                        var fee = (long)Math.Round(cashToTransfer * config.TransferCostFeePercent);
                                        var cost = cashToTransfer + fee;
                                        // ok
                                        remain = await RedisManager.IncEpicCash(user.UserId, -cost, "server", "transfer_to:" + uid, TransType.TRANSFER_CASH, true);
                                        if (remain >= 0)
                                        {
                                            var newCash = await RedisManager.IncEpicCash(uid, cashToTransfer, "server", "transfer_from:" + user.UserId, TransType.TRANSFER_CASH, true);
                                            if (newCash >= 0)
                                            {
                                                var json = new SimpleJSON.JSONObject();
                                                json["userid"] = uid;
                                                json["newCash"] = newCash;
                                                json["changeCash"] = cashToTransfer;
                                                json["reason"] = string.Format("Bạn nhận được {0} gold chuyển từ {1}", cashToTransfer, user.Nickname);

                                                var redis = RedisManager.GetRedis();
                                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", uid, json.SaveToCompressedBase64()));
                                                SqlLogger.LogTransferCash(user.UserId, uid, cost, cashToTransfer);
                                                msg2["ok"] = true;
                                                msg2["err"] = 0;
                                            }
                                            else
                                            {
                                                msg2["err"] = 5;
                                                Logger.Info("Transfer fail to " + uid + " " + newCash);
                                                remain = await RedisManager.IncEpicCash(user.UserId, cost, "server", "refund_transfer_to:" + uid, TransType.TRANSFER_CASH, true);
                                            }
                                        }
                                        else
                                        {
                                            msg2["err"] = 6;
                                        }
                                    }

                                    //msg2["data"] = res;
                                    msg2["cash"] = remain;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 3;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "transferHistory":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = await SqlLogger.GetTransferHistory(user.UserId);
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    break;
                case "verifyPhone2":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        const int OneMin = 60000;
                        if (!string.IsNullOrEmpty(user.LastOtp) && TimeUtil.TimeStamp - user.LastGetOtp < OneMin)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (!string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 3;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        string phone = msg["phone"];
                        if (!string.IsNullOrEmpty(phone))
                        {
                            phone = NormalizePhone(phone);
                            var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                            TaskRunner.RunOnPool(async () =>
                            {
                                if (await RedisManager.IsVerifiedPhone(phone))
                                {
                                    var msg3 = new JSONObject();
                                    msg3["ok"] = false;
                                    msg3["err"] = 4;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                    return;
                                }

                                string otp = this.GenOtp();
                                var msg2 = new JSONObject();
                                var res = await EpicApi.SendSms(phone, otp);
                                msg2["ok"] = true;
                                msg2["data"] = res;
                                msg2["err"] = 0;
                                var newCash = await RedisManager.IncEpicCash(user.UserId, -config.VerifyPhoneSmsCost, "server", "verify " + phone, TransType.VERIFY_PHONE, true, true);
                                msg2["cash"] = newCash;
                                if (res != null && res.HasKey("CodeResult") && res["CodeResult"].AsInt == 100)
                                {
                                    user.LastOtp = otp;
                                    user.LastGetOtp = TimeUtil.TimeStamp;
                                }

                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 2;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "verifyPin2":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;
                        if (string.IsNullOrEmpty(user.LastOtp))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        if (!string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 3;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            return true;
                        }
                        string phone = msg["phone"];
                        string pin = msg["pin"];
                        var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);
                        if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pin) && config != null)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                phone = NormalizePhone(phone);
                                if (await RedisManager.IsVerifiedPhone(phone))
                                {
                                    var msg3 = new JSONObject();
                                    msg3["ok"] = false;
                                    msg3["err"] = 4;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                    return;
                                }

                                var msg2 = new JSONObject();
                                var res = user.LastOtp.Equals(pin);
                                long newCash = 0;
                                if (res)
                                {
                                    user.PhoneNumber = phone;
                                    user.LastOtp = null;
                                    MySqlUser.SavePhoneToDb(user.UserId, phone);
                                    RedisManager.AddVerifiedPhone(phone);
                                    newCash = await RedisManager.IncEpicCash(user.UserId, config.VerifyGiftCash, "server", "verify " + phone, TransType.VERIFY_PHONE, true);
                                }

                                msg2["ok"] = true;
                                msg2["cash"] = newCash;
                                msg2["err"] = 0;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["err"] = 2;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "momoInfo":
                    {
                        User user = CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            try
                            {
                                var info = await RedisManager.GetMomoInfo();
                                var msg2 = new JSONObject();
                                msg2["ok"] = info != null;
                                if (info != null)
                                    msg2["data"] = info;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Fail to get momoInfo: " + ex.ToString());
                                var msg2 = new JSONObject();
                                msg2["ok"] = false;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                        });
                    }
                    break;
                case "bankMomoCashOut":
                    {
                        long cashRequest = msg["Cash"].AsLong;
                        User user = CheckLogin(clientId, msgId);
                        byte Type = msg["Type"]; // 0: bank, 1: momo
                        if (user == null || cashRequest <= 0 || (Type != 0 && Type != 1))
                            return true;

                        string cp = !string.IsNullOrEmpty(user.Cp) ? user.Cp : "default";
                        string BankName = msg["BankName"];
                        string BankAccId = msg["BankAccId"];
                        string BankAccName = msg["BankAccName"];

                        TaskRunner.RunOnPool(async () =>
                        {
                            bool playing = RedisManager.IsPlaying(user.UserId);
                            if (playing)
                            {
                                var response = new JSONObject();
                                response["err"] = -1; // "Yêu cầu không hợp lệ"
                                NetworkServer.Kick(clientId);
                                Logger.Info("#HACK: Cannot cashOut bank while playing BC: " + user.UserId);
                                RedisManager.AddHacker(user.UserId, RedisManager.HackType.CashOutWhilePlayBC, msg.ToString());
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var loginServer = RedisManager.GetPlayerServer(user.UserId);
                            if (RedisManager.CheckDupplicateLogin && loginServer != NetworkServer.Port)
                            {
                                var response = new JSONObject();
                                response["err"] = -1; // "Yêu cầu không hợp lệ"
                                NetworkServer.Kick(clientId);
                                Logger.Info("#HACK: Invalid server request cashOut bank: " + user.UserId);
                                RedisManager.AddHacker(user.UserId, RedisManager.HackType.CashOutInvalidServer, msg.ToString());
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            if (await RedisManager.IsBlockCashOut(user.UserId)) // black list
                            {
                                var response = new JSONObject();
                                response["err"] = 12;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            var config = LobbyConfig.GetConfig(user.AppId, user.VersionCode);

                            var momoCount = await RedisManager.GetMoMoCount(user.UserId); // must cashin momo first to cashout momo
                            if (momoCount == 0)
                            {
                                var response = new JSONObject();
                                response["err"] = 18;
                                NetworkServer.ResponseToClient(clientId, msgId, response);
                                return;
                            }

                            try
                            {
                                string lang_code = user.Language;
                                float factor = Type == 0 ? config.BankCashOutRate : config.MomoCashOutRate;
                                if (factor <= 0) factor = 1.15f;
                                long cashpay = (long)Math.Round(cashRequest * factor);
                                var res_cash = await RedisManager.IncEpicCash(user.UserId, -cashpay, user.Platform, "CASH_OUT_BANK", TransType.BANK_OUT, realmoney: cashRequest, rate: factor);
                                if (res_cash < 0)
                                {
                                    //"Quý khách không đủ tiền để thực hiện chức năng này";
                                    Logger.Info("Cash out bank not enough cash " + user.UserId + " cashpay " + cashpay + " factor " + factor + " usercash " + user.Cash + " require remain " + config.CashOutGoldRemain);
                                    msg2["err"] = 1;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }

                                if (res_cash < config.CashOutGoldRemain)
                                {
                                    await RedisManager.IncEpicCash(user.UserId, cashpay, user.Platform, "CASH_OUT remain not enough", TransType.BANK_OUT, realmoney: cashRequest, rate: factor);
                                    Logger.Info("Cash out bank not enough remain " + user.UserId + " cashpay " + cashpay + " factor " + factor + " usercash " + user.Cash + " require remain " + config.CashOutGoldRemain);
                                    msg2["err"] = 11;
                                    msg2["require"] = config.CashOutGoldRemain;
                                    msg2["remain"] = res_cash;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                    return;
                                }
                                user.Cash = res_cash;

                                await SqlLogger.LogBankCashout(user, BankName, BankAccId, BankAccName, cashRequest, factor, Type);
                                msg2["ok"] = true;
                                msg2["err"] = 0;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error in cash out bank: " + ex.ToString());
                                msg2["err"] = 2;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }
                        });
                    }
                    break;
                case "bankMomoCashOutHistory":
                    {
                        User user = CheckLogin(clientId, msgId);
                        byte Type = msg["Type"]; // 0: bank, 1: momo
                        if (user == null || (Type != 0 && Type != 1))
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            try
                            {
                                var info = await SqlLogger.GetBankLog(user, Type);
                                var msg2 = new JSONObject();
                                msg2["ok"] = info != null;
                                if (info != null)
                                    msg2["data"] = info;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                            catch (Exception ex)
                            {
                                Logger.Info("Fail to get bankCashOutHistory: " + ex.ToString());
                                var msg2 = new JSONObject();
                                msg2["ok"] = false;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                        });
                    }
                    break;
            }

            return false;
        }

        public static string NormalizePhone(string phone)
        {
            if (phone.StartsWith("0"))
            {
                return "+84" + phone.Substring(1);
            }
            else if (phone.StartsWith("84"))
            {
                return "+" + phone;
            }
            else if (phone.StartsWith("+84"))
            {
                return phone;
            }

            return "+84" + phone;
        }

        private object otpLock = new object();
        private Random otpRandom;
        public string GenOtp()
        {
            lock (otpLock)
            {
                if (otpRandom == null)
                {
                    otpRandom = new Random();
                }

                var otpNumber = otpRandom.Next(1000000);
                return otpNumber.ToString().PadLeft(6, '0');
            }
        }

        private void postLogin(BanCaServer server, string clientId, User user)
        {
            if (clientId == null || !loggedInUser.ContainsKey(clientId) || !NetworkServer.HasPeer(clientId)) // login procedure is finished, but the peer is removed
            {
                var uid = user.UserId;
                Logger.Info("User " + uid + " finished login but disconnected");
                //if (RedisManager.TryLockUserId(uid))
                {
                    RedisManager.RemovePlayerServer(uid);
                    RedisManager.SetNotPlaying(uid);
                    RedisManager.ReleaseLockUserId(uid);
                }
                TaskRun.QueueAction(() => { server.onRemovePeer(clientId); });
            }
        }

        public User CheckLogin(string clientId, int msgId)
        {
            User user = null;
            if (clientId != null) { loggedInUser.TryGetValue(clientId, out user); };
            if (user == null)
            {
                var msg2 = new JSONObject();
                msg2["ok"] = false;
                msg2["err"] = -1;
                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                return null;
            }
            //Logger.Info("User logged in " + user.UserId);
            return user;
        }

        private static async Task VerifyChargingMuaCard(User user, JSONNode msg, string code, string serial, string telCode, string telco, int money, string provider_code, int platform)
        {
            try
            {
                var tranID = await SqlLogger.LogRequestCard(user, msg, code, serial, telCode, telco, money, provider_code, platform);
                if (tranID != -1)
                {
                    string urlPush = "https://muacard.vn/api/charging?provider={0}&serial={1}&code={2}&amount={3}&api_key={4}&charging_type={5}&trans_id={6}&callback={7}";
                    string urlCallBack = "http://149.28.138.254:8080/bancaapi/muacardcallback";
                    const string apiKey = "543f51e72b0c0979ca18736b19042633";
                    urlPush = string.Format(urlPush, telco, serial, code, money, apiKey, 1, tranID, Nancy.Helpers.HttpUtility.UrlEncode(urlCallBack));
                    string response = await EpicApi.Get(urlPush);
                    var res = JSON.Parse(response);
                    //100: Chưa đăng nhập
                    //101: API key không hợp lệ
                    //108: Loại thẻ không hợp lệ
                    //109: Mệnh giá thẻ không hợp lệ hoặc không active
                    //114: Tài khoản bị khóa
                    //200: Thành công
                    //  {"code":112,"description":"M\u00e3 th\u1ebb c\u00e0o ph\u1ea3i l\u00e0 13 ho\u1eb7c 15 k\u00fd t\u1ef1","message":null,"data":null}
                    var ok = res["code"].AsInt;
                    msg["ok"] = ok == 200;
                    msg["err"] = 1000 + ok;
                    msg["msg"] = res.HasKey("description") ? res["description"].Value : "";

                    if (ok != 200)
                    {
                        SqlLogger.UpdateRequestCard(tranID.ToString(), false, ok.ToString(), money);
                    }
                }
                else
                {
                    msg["ok"] = false;
                    msg["err"] = 21;
                    Logger.Error("Card charge fail: no tranId");
                }
            }
            catch (Exception ex)
            {
                msg["ok"] = false;
                msg["err"] = 21;
                Logger.Error("Card charge fail: " + ex.ToString());
            }
        }

        private static async Task VerifyChargingPushTheV4(User user, JSONNode msg, string code, string serial, string telCode, string telco, int money, string provider_code, int platform)
        {
            try
            {
                var tranID = await SqlLogger.LogRequestCard(user, msg, code, serial, telCode, telco, money, provider_code, platform);
                if (tranID != -1)
                {
                    string urlPush = "https://api.push247.net/pushthev4.aspx";
                    string urlCallBack = "http://123.31.12.133:8080/bancaapi/push247callback";
                    const string apiKey = "ec8eed35d7434e541842c22b723ae378";
                    string type = telCode; // vt, mb, vn
                    string param = "apiKey=" + apiKey + "&type=" + type + "&code=" + code + "&serial=" + serial + "&money=" + money + "&callbackurl=" + urlCallBack + "&tranid=" + tranID + "&realtime=false";
                    string response = await EpicApi.Get(urlPush, param, apiKey);

                    var jdata = JSON.Parse(response);
                    var data2 = jdata["data"].AsObject;

                    //00: thành công
                    //24: thẻ đã dùng rồi
                    //07: thẻ sai
                    //08: hệ thống bận
                    //99: hệ thống đang xử lý
                    string statuscode = data2["statuscode"];
                    statuscode = "10" + statuscode;
                    var ok = msg["ok"] = statuscode == "99" || statuscode == "00" || statuscode == "0";
                    msg["err"] = statuscode;
                    msg["msg"] = jdata.HasKey("msg") ? jdata["msg"].Value : "";

                    if (ok != 200)
                    {
                        SqlLogger.UpdateRequestCard(tranID.ToString(), false, ok.ToString(), money);
                    }
                }
                else
                {
                    msg["ok"] = false;
                    msg["err"] = 21;
                    Logger.Error("Card charge fail: no tranId");
                }
            }
            catch (Exception ex)
            {
                msg["ok"] = false;
                msg["err"] = 21;
                Logger.Error("Card charge fail: " + ex.ToString());
            }
        }

        private static async Task VerifyChargingVinPay(User user, JSONNode msg, string code, string serial, string telCode, string telco, int money, string provider_code, int platform)
        {
            try
            {
                var tranID = await SqlLogger.LogRequestCard(user, msg, code, serial, telCode, telco, money, provider_code, platform);
                if (tranID != -1)
                {
                    string ip = user.IP;
                    string version = "1.1";
                    string date = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string urlPush = "https://vin-pay.net/api-nap-telco.html";
                    const string apiKey = "cQ6tdmkLDI";
                    string type = telCode; // vt, mb, vn
                    var hash = EpicApi.GetHashSha256(tranID + serial + code + telco + ip + version + date);
                    var param = string.Format("v_idref={0}&v_key={1}&v_serial={2}&v_pin={3}&v_telco={4}&v_value={5}&v_ip={6}&v_date={7}&v_ver={8}&v_securehash={9}",
                    tranID, apiKey, serial, code, telco, money, ip, date, version, hash);
                    string response = await EpicApi.PostFormUrlencoded(urlPush, param);

                    var jdata = JSON.Parse(response);
                    int statuscode = jdata["e"];
                    msg["err"] = statuscode;
                    msg["msg"] = jdata.HasKey("r") ? jdata["r"].Value : "";

                    if (statuscode != 0)
                    {
                        SqlLogger.UpdateRequestCard(tranID.ToString(), false, statuscode.ToString(), money);
                    }
                }
                else
                {
                    msg["ok"] = false;
                    msg["err"] = 21;
                    Logger.Error("Card charge fail: no tranId");
                }
            }
            catch (Exception ex)
            {
                msg["ok"] = false;
                msg["err"] = 21;
                Logger.Error("Card charge fail: " + ex.ToString());
            }
        }

        private static async Task VerifyChargingOld(User user, JSONNode msg, string carpin, string seri, string telCode, string telco, int amount, string provider_code, int platform)
        {
            //            o app_id: GAME_CA
            //o   api_key: public_key
            //o   user_id: user game id
            //o   username: game username
            //o pin: mã thẻ
            //o serial: seri thẻ
            //o type: loại thẻ(= code: mã loại thẻ được hỗ trợ VTT, VMS, …)
            //o money: mệnh giá thẻ
            //o   no_encrypt: 1

            try
            {
                var param = string.Format("app_id={0}&api_key={1}&user_id={2}&username={3}&pin={4}&serial={5}&type={6}&money={7}&provider_code={8}&platform={9}&no_encrypt=1",
                    "GAME_CA", "kD9g5MrGqExhPQnRZBvFsPDtVEmVJLDx", user.UserId, user.Username, carpin, seri, telCode, amount, provider_code, platform);
                var res = await EpicApi.PostFormUrlencoded("http://card.vuongquocca.club/card-charge", param);
                Logger.Info(string.Format("Cardin param {0} res\n{1}", param, res));
                var json = JSON.Parse(res);
                var status = json["status"].AsInt;

                var ok = status == 1 || status == 100;
                msg["ok"] = ok;
                msg["err"] = status;
                // {"status":1,"message":"N\u1ea1p th\u1ebb 59200006832006 th\u00e0nh c\u00f4ng, b\u1ea1n \u0111\u01b0\u1ee3c c\u1ed9ng th\u00eam 20000 v\u00e0o t\u00e0i kho\u1ea3n","gold":20000}
                // ok
                if (status == 1)
                {
                    var add = json["gold"].AsLong;
                    string message = json["message"];
                    var reasonText = "card-charge";
                    var newCash = await RedisManager.IncEpicCash(user.UserId, add, "webapi", reasonText, TransType.CARD_IN);
                    if (newCash < 0) // error
                    {
                        msg["ok"] = false;
                        msg["err"] = 22;
                        return;
                    }
                    user.CardInCount = await RedisManager.CardInInc(user.UserId, add);
                    msg["err"] = 0;
                    var json2 = new SimpleJSON.JSONObject();
                    json2["userid"] = user.UserId;
                    json2["newCash"] = newCash;
                    json2["changeCash"] = add;
                    json2["reason"] = message;

                    var redis = RedisManager.GetRedis();
                    await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", user.UserId, json2.SaveToCompressedBase64()));

                    MySqlUser.SaveCashToDb(user.UserId, newCash);
                    SqlLogger.LogCashChangeByCms(user.UserId, newCash, add, reasonText);
                }
                else if (json.HasKey("message"))
                {
                    string mess = json["message"];
                    msg["msg"] = mess;
                }
            }
            catch (Exception ex)
            {
                msg["ok"] = false;
                msg["err"] = 21;
                Logger.Error("Card charge fail: " + ex.ToString());
            }
        }

        public static async Task<long> GetTotalCardIn(long userId)
        {
            try
            {
                var param = string.Format("app_id={0}&api_key={1}&user_id={2}&no_encrypt=1",
                    "GAME_CA", "kD9g5MrGqExhPQnRZBvFsPDtVEmVJLDx", userId);
                var res = await EpicApi.PostFormUrlencoded("http://card.vuongquocca.club/total-paid-by-user", param);
                Logger.Info(string.Format("Total cardin param {0} res\n{1}", param, res));
                // {"status":1,"message":"Success","total_paid":"140000"}
                var json = JSON.Parse(res);
                var status = json["status"].AsInt;
                if (status == 1)
                {
                    if (json.HasKey("total_paid")) return json["total_paid"].AsLong;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetTotalCardIn fail: " + ex.ToString());
            }
            return -1;
        }

        public static async Task<long> CardOutApi(BanCaServer server, User user, string telco, long cashRequest, long cashpay, string cp, VersionConfig config)
        {
            try
            {
                string telCode = "";
                if (telco.Contains("viettel"))
                    telco = "viettel";
                if (telco.Contains("mobi"))
                    telco = "mobifone";
                if (telco.Contains("vina"))
                    telco = "vinaphone";

                if (telco == "viettel")
                {
                    telCode = "VTT";
                }
                else if (telco == "mobifone")
                {
                    telCode = "VMS";
                }
                else if (telco == "vinaphone")
                {
                    telCode = "VNP";
                }

                JSONNode json = null;
                string itemId = telco + " " + cashRequest;
                string message = null;
                int status;
                // TODO: off cashout for test
                //var real = true;
                //if (real)
                //{
                //    var param = string.Format("api_key={0}&user_id={1}&type={2}&money={3}",
                //        "kD9g5MrGqExhPQnRZBvFsPDtVEmVJLDx", user.UserId, telCode, cashRequest);
                //    var res = await EpicApi.PostFormUrlencoded("http://card.vuongquocca.club/card-top-up", param);
                //    Logger.Info(string.Format("CardOutApi param {0} res\n{1}", param, res));
                //    json = JSON.Parse(res);
                //    status = json["status"].AsInt;
                //    message = json["message"];
                //}

                var isAuto = await RedisManager.IsCardOutAuto();
                if (isAuto)
                {
                    Logger.Info(string.Format("Cashout request {0} {1} {2}", user.UserId, telCode, cashRequest));
                    json = await SqlLogger.CardOut(user, telCode, cashRequest);
                    Logger.Info(string.Format("Cashout request result {0} {1}", user.UserId, json != null ? json.ToString() : "null"));
                }

                if (json != null)
                {
                    string serial = json["serial"];
                    string pin = json["pin"];
                    message = "Đổi thẻ thành công";
                    status = 1;
                    string query = "INSERT INTO `cashouts` ( `item_id`, `price`, `username`, `telco`, `user_id`, `platform`, `device_id`, `time_cashout`, `to_money`, `cp`, `seri`, `number_card`, `status`) VALUES ";
                    query += "('{0}', {1}, '{2}', '{3}', {4}, '{5}', '{6}',now(), {7}, '{8}', '{9}', '{10}', {11})";
                    query = string.Format(query, itemId, cashpay, user.Username, telco, user.UserId, user.Platform, user.DeviceId, cashRequest, cp, serial, pin, 1);
                    MySqlCommon.ExecuteNonQuery(query);
                }
                else
                {
                    string query = "INSERT INTO `cashouts` ( `item_id`, `price`, `username`, `telco`, `user_id`, `platform`, `device_id`, `time_cashout`, `to_money`, `cp`, `status`) VALUES ";
                    message = "Đã ghi nhận yêu cầu đổi thẻ. Vui lòng chờ hệ thống duyệt trong ít phút.";
                    status = 0;
                    query += "('{0}', {1}, '{2}', '{3}', {4}, '{5}', '{6}',now(), {7}, '{8}', {9})";
                    query = string.Format(query, itemId, cashpay, user.Username, telco, user.UserId, user.Platform, user.DeviceId, cashRequest, cp, 0);
                    MySqlCommon.ExecuteNonQuery(query);
                }

                await RedisManager.substractCashOutCountPerDay(user.UserId, config);
                await RedisManager.substractCashOutCountGoldPerDay(user.UserId, cashRequest, config);
                await RedisManager.substractCashOutCountGoldServerPerDay(cashRequest, config);

                SqlLogger.LogCardOut(user.UserId, user.Cash, -cashpay, itemId);

                if (!string.IsNullOrEmpty(message) && server != null)
                {
                    long userId = user.UserId;
                    server.TaskRun.QueueAction(3000, () =>
                    {
                        if (server != null)
                            server.AlertStr(userId, message);
                    });
                }
                return status;
            }
            catch (Exception ex)
            {
                Logger.Error("CardOut fail: " + ex.ToString());
            }
            return -1;
        }

        private static async Task CardChargingHistory(User user, JSONNode msg)
        {
            var res = await SqlLogger.GetRequestCards(user.UserId);
            msg["cardsIn"] = res;
        }

        private static async Task CardChargingHistoryOld(User user, JSONNode msg)
        {
            // 
            //o app_id: GAME_CA
            //o   api_key: public_key
            //o   user_id: game user id
            //o   page: trang(default = 1)
            //o limit: số lượng bản ghi(default = 10 max 30)
            //o no_encrypt: 1

            try
            {
                var url = "http://card.vuongquocca.club/card-history";
                var param = string.Format("app_id={0}&api_key={1}&user_id={2}&page=1&limit=30&no_encrypt=1",
                    "GAME_CA", "kD9g5MrGqExhPQnRZBvFsPDtVEmVJLDx", user.UserId);
                var res = await EpicApi.PostFormUrlencoded(url, param);

                Logger.Info(string.Format("Cardin history params {0} res:\n{1}", param, res));
                var json = JSON.Parse(res);
                var status = json["status"].AsInt;

                msg["ok"] = status == 1;
                msg["err"] = status == 1 ? 0 : status;
                if (json.HasKey("data"))
                {
                    msg["cardsIn"] = json["data"].AsArray;
                }
            }
            catch (Exception ex)
            {
                msg["ok"] = false;
                msg["err"] = 20;
                Logger.Error("Card charge history fail: " + ex.ToString());
            }
        }

        public bool IsUserFromLandingPage(User user)
        {
            const string plf = "WebGLPlayer";
            const string fullOpen = "full";
            return plf.Equals(user.Platform) || (!string.IsNullOrEmpty(user.Platform) && user.Platform.EndsWith(fullOpen));
        }

        public static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public static string md5(string source_str)
        {
            MD5 encrypter = new MD5CryptoServiceProvider();
            Byte[] original_bytes = ASCIIEncoding.Default.GetBytes(source_str);
            Byte[] encoded_bytes = encrypter.ComputeHash(original_bytes);
            return BitConverter.ToString(encoded_bytes).Replace("-", "").ToLower();
        }

        public class ChargingResponse
        {
            public string transId;
            public string transRef;
            public string serial;
            public string status;
            public int amount;
            public string description;
        }

        public class ChargingResponseReTopup
        {
            public int status;
            public string content;
        }
    }
}
