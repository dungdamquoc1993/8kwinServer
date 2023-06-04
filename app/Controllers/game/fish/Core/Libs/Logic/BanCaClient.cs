using System;
using SimpleJSON;
using static BanCa.Libs.Config;

namespace BanCa.Libs
{
    public class BanCaClient
    {
        private static int idCount = 0;
        public readonly int Id = idCount++;

        private NetworkClient client;
        private bool useWs = false;

        public bool Connected
        {
            get
            {
                if (client != null)
                {
                    return client.Connected;
                }

                return false;
            }
        }

        public BanCaClient(string ip, int port, bool useWs, bool wss)
        {
            this.useWs = useWs;
            client = useWs ? (NetworkClient)new BcWebSocketClient(ip, port, wss) : (NetworkClient)new LiteNetClient(ip, port);
            client.OnPush = OnPush;
            client.OnDisconnect = OnDisconnected;
        }

        public void Update()
        {
            if (client != null)
            {
                client.Update();
            }

            _updateEngine();
        }

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        // logic
        public string MyName { get; private set; }
        public GameBanCa bancaClient = new GameBanCa(-1, null);
        private long lastUpdate = 0;

        public void Ping(Action cb = null)
        {
            var start = TimeUtil.TimeStamp;
            client.Request("ping", null, (data) =>
            {
#if SERVER
                var end = TimeUtil.TimeStamp; // due to polling, we substract average half delta time
#else
                var end = TimeUtil.TimeStamp;// - (long)(1000 * UnityEngine.Time.smoothDeltaTime + Config.SERVER_UPDATE_MS); // due to polling, we substract average half delta time (when send and receive, also on server)
#endif
                var time = data["time"].AsLong;
                //UnityEngine.Debug.Log("Server time: " + time);
                TimeUtil.DelayMs = (end - start) / 2; // make guess that msg to server and return use same time
                TimeUtil.ClientServerTimeDifferentMs = time - (start + TimeUtil.DelayMs);
                //                    time record at server vs time at server guess by client

                //UnityEngine.Debug.Log("Delay " + TimeUtil.DelayMs + " diff time " + TimeUtil.ClientServerTimeDifferentMs);
                Logger.Info("Ping response: " + data.ToString());
                if (cb != null)
                {
                    cb();
                }
            });
        }

        public void OnDisconnected()
        {
            Logger.Info("Client disconnected " + Id);
        }

        public void OnPush(string route, JSONNode msg)
        {
            //UnityEngine.Debug.Log("Push: " + route + " | " + (msg != null ? msg.ToString() : ""));
            switch (route)
            {
                case "OnShoot":
                    //Logger.Log("Onshoot: " + msg.ToString());
                    try
                    {
                        var type = (Config.BulletType)msg["type"].AsInt;
                        bancaClient.Shoot(msg["playerId"], msg["rad"].AsFloat, type);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Parse onshoot fail: " + ex.ToString());
                    }
                    break;

                case "OnUpdateObject":
                    bancaClient.UpdateObject(msg["id"].AsInt, msg);
                    break;
                case "OnUseBomb":
                    Logger.Log("OnUseBomb: " + msg.ToString());
                    break;
                case "OnUpdateCash":
                    var p = bancaClient.getPlayer(msg["playerId"]);
                    if (p != null)
                    {
                        p.Cash = msg["cash"].AsLong;
                        p.CashGain = msg["cashGain"].AsLong;
                        p.ExpGain = msg["expGain"].AsLong;
                    }
                    break;

                case "OnObjectDie":
                    {
                        int id = msg["id"].AsInt;
                        long time = msg["time"].AsLong;
                        //UnityEngine.Debug.Log("Kill fish: " + id);
                        bancaClient.KillObject(id, time);
                    }
                    break;

                case "OnNewState":
                    bancaClient.WorldState = (State)msg["state"].AsInt;
                    Logger.Info("On new state: " + bancaClient.WorldState);

                    if (msg.HasKey("world"))
                    {
                        var world = msg["world"].AsObject;
                        bancaClient.ParseUpdateJson(world);
                    }
                    break;
                case "OnEnterPlayer":
                    bancaClient.RegisterPlayer(msg["playerId"], msg["posIndex"].AsInt, msg["data"].AsObject);
                    break;
                case "OnItemUse":
                    bancaClient.UseItem(msg);
                    break;
                case "OnLevelUp":
                    bancaClient.LevelUp(msg);
                    break;
                case "OnLeavePlayer":
                    Logger.Log("Player leave: " + msg.ToString());
                    bancaClient.RemovePlayer(msg["playerId"]);
                    break;
                case "OnChat":
                    Logger.Log(msg.ToString());
                    break;
                case "OnRemoveAllObject":
                    {
                        long time = msg["time"].AsLong;
                        bancaClient.KillAllObject(time);
                    }
                    break;
                case "reloadCash":
                    // msg: {"userid":"1178", "newCash":"123321", "changeCash":"321", "reason":"ta la vo doi"}
                    break;
                case "OnUpdateJackpot":
                    //Logger.Info("update jp: " + msg.ToString());
                    break;
            }
        }

        public void SyncState(Action cb = null)
        {
            //UnityEngine.Debug.Log("Request world state");
            client.Request("state", null, (data) =>
            {
                //Logger.Log("Get world state: " + data.ToString());
                bancaClient.ParseJson(data);
                if (cb != null) cb();
            });
        }

        public void QuickLogin(string deviceId, string platform, string language, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["deviceId"] = deviceId;
            msg["platform"] = platform;
            msg["language"] = language;
            client.Request("quickLogin", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void LoginFacebook(string facebookID, string fullname, string avatar, string deviceId, string platform, string language, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["deviceId"] = deviceId;
            msg["platform"] = platform;
            msg["language"] = language;
            msg["fullname"] = fullname;
            msg["facebookID"] = facebookID;
            msg["avatar"] = avatar;

            client.Request("loginFb", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void CheckVersion(int versionCode, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["vcode"] = versionCode;
            client.Request("checkVersion", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void GetCapcha(Action<JSONNode> cb = null)
        {
            client.Request("getCapcha", null, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void Register(string username, string password, string deviceId, string platform, string language, string capcha, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["deviceId"] = deviceId;
            msg["platform"] = platform;
            msg["language"] = language;
            msg["username"] = username;
            msg["password"] = password;
            msg["capcha"] = capcha;

            client.Request("register", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void Login(string username, string password, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["username"] = username;
            msg["password"] = password;

            client.Request("login", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void giftCode(string code, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["code"] = code;
            client.Request("giftCode", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void getIapItems(string appId, long versionCode, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["appId"] = appId;
            msg["versionCode"] = versionCode;
            client.Request("getIapItem", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void iapGoogle(string signedData, string signature, string appId, long versionCode, string receipt, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["signedData"] = signedData;
            msg["signature"] = signature;
            msg["appId"] = appId;
            msg["versionCode"] = versionCode;
            msg["receipt"] = receipt;
            client.Request("verifyGoogle", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void iapApple(string productId, string receipt, string appId, long versionCode, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["productId"] = productId;
            msg["receipt"] = receipt;
            msg["appId"] = appId;
            msg["versionCode"] = versionCode;
            client.Request("verifyApple", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void UpdateNickname(string nickname, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["nickname"] = nickname;
            client.Request("updateNickname", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void GetProfile(long userId, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["userId"] = userId;
            client.Request("getProfile", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void UpdateProfile(string phone, string avatar, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["avatar"] = avatar;
            msg["phone"] = phone;
            client.Request("updateProfile", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void ChargingHistory(Action<JSONNode> cb = null)
        {
            client.Request("chargingHistory", null, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void CashOutHistory(Action<JSONNode> cb = null)
        {
            client.Request("cashOutHistory", null, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void CashOut(string telco, long cash, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["cash"] = cash;
            msg["telco"] = telco;

            client.Request("cashOut", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void CardIn(string telco, string seri, string number, int cash, Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            msg["seri"] = seri;
            msg["number"] = number;
            msg["telco"] = telco;
            msg["cash"] = cash;
            client.Request("cardIn", msg, (data) =>
            {
                if (cb != null) cb(data);
            });
        }

        public void PlayNow(string playerId, string password, int index, bool solo, Action cb = null)
        {
            var msg = new JSONObject();
            MyName = msg["playerId"] = playerId;
            msg["password"] = password;
            msg["index"] = index;
            msg["solo"] = solo ? 1 : 0;
            client.Request("play", msg, (data) =>
            {
                if (data.HasKey("config"))
                {
#if SERVER // bot run multiple instances so skip load config
                    if (!Config.ConfigLoaded)
                    {
                        Config.ConfigLoaded = true;
                        Config.ParseJson(data["config"].AsObject);
                    }
#else
                    Config.ConfigLoaded = true;
                    Config.ParseJson(data["config"].AsObject);
#endif
                }
                Logger.Info("Play now " + data["ok"].AsBool);
                if (cb != null) cb();
            });
        }

        public void Chat(string message, Action cb = null)
        {
            var msg = new JSONObject();
            msg["msg"] = message;
            client.Request("chat", msg, (data) =>
            {
                if (cb != null) cb();
            });
        }

        public void QuitGame(Action<JSONNode> cb = null)
        {
            var msg = new JSONObject();
            client.Request("quit", msg, cb);
        }

        public void GetBestServer(int blindIndex, Action<int> cb)
        {
            var msg = new JSONObject();
            msg["blindIndex"] = blindIndex;
            msg["ws"] = useWs ? 1 : 0;
            client.Request("bestServer", msg, (data) =>
            {
                int bestServer = data["best"].AsInt;
                cb(bestServer);
            });
        }
        public void GetBestServer(long startCash, Action<int> cb)
        {
            var blindIndex = Config.GetTableBlindIndexForPlayer(startCash);
            GetBestServer(blindIndex, cb);
        }

        public void GetAnnounces(byte cate, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["cate"] = cate;
            client.Request("getAnnounces", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetEvents(Action<JSONArray> cb)
        {
            client.Request("getEvents", null, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetMessages(Action<JSONArray> cb)
        {
            client.Request("getMessages", null, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetPendingMessages(Action<JSONArray> cb)
        {
            client.Request("getPendingMessages", null, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetTopPrizes(bool isWeek, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["isWeek"] = isWeek;
            client.Request("getTopPrizes", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetTopWeek(int limit, int index, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["limit"] = limit;
            msg["index"] = index;
            client.Request("topWeek", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetRankWeek(Action<JSONNode> cb)
        {
            client.Request("topWeekRank", null, (data) =>
            {
                var res = data["data"];
                cb(res);
            });
        }

        public void GetRankMonth(Action<JSONNode> cb)
        {
            client.Request("topMonthRank", null, (data) =>
            {
                var res = data["data"];
                cb(res);
            });
        }

        public void GetTopMonth(int limit, int index, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["limit"] = limit;
            msg["index"] = index;
            client.Request("topMonth", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetTopCash(int limit, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["limit"] = limit;
            client.Request("topCash", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetTopLevel(int limit, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["limit"] = limit;
            client.Request("topLevel", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void GetRankCash(long fromCash, Action<int> cb)
        {
            var msg = new JSONObject();
            msg["cash"] = fromCash;
            client.Request("rankCash", msg, (data) =>
            {
                var res = data["data"].AsInt;
                cb(res);
            });
        }

        public void GetRankLevel(int level, Action<int> cb)
        {
            var msg = new JSONObject();
            msg["level"] = level;
            client.Request("rankLevel", msg, (data) =>
            {
                var res = data["data"].AsInt;
                cb(res);
            });
        }

        public void UseBombItem(BulletType type, Action<JSONNode> cb)
        {
            var msg = new JSONObject();
            msg["type"] = (int)type;
            client.Request("useBombItem", msg, (data) =>
            {
                var res = data["data"];
                cb(res);
            });
        }

        public void GetJackpot(Action<JSONNode> cb)
        {
            client.Request("getJackpot", null, (data) =>
            {
                var res = data["data"];
                cb(res);
            });
        }

        public void GetJackpotHistory(int limit, Action<JSONArray> cb)
        {
            var msg = new JSONObject();
            msg["limit"] = limit;
            client.Request("getJackpotHistory", msg, (data) =>
            {
                var res = data["data"].AsArray;
                cb(res);
            });
        }

        public void Shoot(float rad, Config.BulletType type, int targetId = -1, bool rapidFire = false, bool isAuto = false)
        {
            shootMsg["rad"] = rad;
            shootMsg["type"] = (int)type;
            if (targetId != -1) shootMsg["target"] = targetId;
            if (rapidFire) shootMsg["rapidFire"] = rapidFire;
            if (isAuto) shootMsg["auto"] = isAuto;

            client.Notify("shoot", shootMsg);
        }

        public void ShootRandom(Config.BulletType type)
        {
            if (string.IsNullOrEmpty(MyName))
                return;

            var me = bancaClient.getPlayer(MyName);
            if (me != null)
            {
                var rad = 0f;
                var vary = (float)(bancaClient.Random.NextDouble() * 60 - 30) * 3.14f / 180;
                switch (me.PosIndex)
                {
                    case 0:
                        rad = vary + 45 * 3.14f / 180f;
                        break;
                    case 1:
                        rad = vary + 135 * 3.14f / 180f;
                        break;
                    case 2:
                        rad = vary + -135 * 3.14f / 180f;
                        break;
                    default:
                        rad = vary + -45 * 3.14f / 180f;
                        break;
                }

                shootMsg["rad"] = rad;
                shootMsg["type"] = (int)type;
                client.Notify("shoot", shootMsg);
            }
        }

        private JSONNode shootMsg = new JSONObject();
        private void _updateEngine()
        {
            var now = TimeUtil.TimeStamp;
            bancaClient.Update((now - lastUpdate) / 1000f);
            lastUpdate = now;
        }
    }
}
