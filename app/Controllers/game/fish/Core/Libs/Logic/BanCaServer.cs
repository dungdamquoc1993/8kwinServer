using BanCa.Libs.Bots;
using BanCa.Redis;
using BanCa.Sql;
using Loto;
using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace BanCa.Libs
{
    public class BanCaServer
    {
        #region connector
        private long lastLogCCU;
        private long nextLogTooFast;
        public int CCU { get; private set; }
        public int FreeSlot { get; private set; }
        public bool Started { get; internal set; }

        private Thread serverThread;
        private bool running = true;
        public readonly int Port;
        public int WsPort { get; private set; }
        public bool EnableSsl { get; private set; }

        public bool AllowSolo = false;

        public class ServerStat
        {
            public int CCU;
            public int FreeSlot;
            public long Profit;

            public void Set(int ccu, int freeSlot, long profit)
            {
                this.CCU = ccu;
                this.FreeSlot = freeSlot;
                this.Profit = profit;
            }
        }

        public TaskRunner TaskRun = new TaskRunner();
        private List<GameBanCa> bancaWorlds = new List<GameBanCa>();
        private Dictionary<int, GameBanCa> worldMap = new Dictionary<int, GameBanCa>();
        private Dictionary<string, int> peerToWorldMap = new Dictionary<string, int>();
        private List<GameBanCa> worldPools = new List<GameBanCa>();
        private Dictionary<int, ServerStat> stats = new Dictionary<int, ServerStat>();
        private long lastUpdate;

        public NetworkServer NetworkServer { get; private set; }

        public LobbyService Lobby { get; private set; }

        private LotoGame GameLoto;

        public readonly ConcurrentQueue<Tuple<string, JSONNode>> pendingPushAll = new ConcurrentQueue<Tuple<string, JSONNode>>();

        private int[] totalTimes = new int[10];
        private int totalTimeIndex = -1;

        public BanCaServer(int port, int ws_port, bool ssl) : this(port)
        {
            WsPort = ws_port;
            EnableSsl = ssl;
        }

        public BanCaServer(int port)
        {
            this.Port = port;
            Started = false;

            Thread t = new Thread(runner);
            t.Priority = ThreadPriority.AboveNormal;
            t.Name = "server_" + port;
            serverThread = t;
            running = true;
            t.Start();

            Config.OnConfigChange += onConfigChange;
        }
        private void onConfigChange()
        {
            TaskRun.QueueAction(() =>
            {
                for (int i = 0, n = bancaWorlds.Count; i < n; i++)
                {
                    var world = bancaWorlds[i];
                    if (world != null)
                    {
                        world.CanRecycle = false;
                        world.ConfigChange();
                    }
                }
                worldPools.Clear(); // force new world to use new config
            });
        }

        bool startBot = false;
        public void StartBot()
        {
            startBot = true;
        }

        internal void removePeerToWorldMap(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId)) peerToWorldMap.Remove(clientId);
        }

        private void runner()
        {
            Thread.Sleep(100); // wait for all constructor finish
            NetworkServer = WsPort > 0 ? (NetworkServer)(new CombinedServer(this.Port, this.WsPort, EnableSsl)) : (NetworkServer)(new LiteNetServer(this.Port));
            //NetworkServer = new BcWebSocketServer(this.Port);
            NetworkServer.SetOnClientNotify(onClientNotify);
            NetworkServer.SetOnClientRequest(onClientRequest);
            NetworkServer.SetOnRemovePeer(onRemovePeer);
            running = true;
            Started = true;
            lastUpdate = TimeUtil.TimeStamp;
            Lobby = new LobbyService(this, NetworkServer, TaskRun);
            GameLoto = LotoGame.Instance;
            //Slot3Sub = Slot3SubRoll.Instance;
            //SlotGame3 = PokeGoGame.Instance;
            //MiniPoker = MiniPokerManager.Instance;
            //Rooms = new RoomServer(1, NetworkServer, Lobby);

            int frameIndex = 0;
            while (running)
            {
                var mainFrame = frameIndex % 2 == 0;

                var start = TimeUtil.TimeStamp;
                if (NetworkServer != null)
                    NetworkServer.Update();
                var networkTime = TimeUtil.TimeStamp;

                TaskRun.Update();
                var taskTime = TimeUtil.TimeStamp;

                if (mainFrame)
                    _updateEngine();
                var engineTime = TimeUtil.TimeStamp;

                if (!mainFrame)
                    updateBot(Config.SERVER_UPDATE_LOOP_MS);
                var botTime = TimeUtil.TimeStamp;

                try
                {
                    while (pendingPushAll.TryDequeue(out var p))
                    {
                        NetworkServer.PushAll(p.Item1, p.Item2);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Push all fail: " + ex.ToString());
                }

                var end = TimeUtil.TimeStamp;
                var totalTime = (int)(end - start);
                var delta = Config.SERVER_UPDATE_LOOP_MS - totalTime;
                if (delta > Config.SERVER_UPDATE_LOOP_MS)
                {
                    Logger.Info(string.Format("Delta is too large, delta {0}, start {1}, end {2}", delta, start, end));
                    delta = Config.SERVER_UPDATE_LOOP_MS;
                }

                totalTimeIndex++;
                if (totalTimeIndex >= totalTimes.Length) totalTimeIndex = 0;
                totalTimes[totalTimeIndex] = totalTime < Config.SERVER_UPDATE_LOOP_MS ? Config.SERVER_UPDATE_LOOP_MS : totalTime;

                if (delta > 0)
                {
                    Thread.Sleep(delta);
                }
                else if (end > nextLogTooFast)
                {
                    nextLogTooFast = end + 3000;
                    Logger.Info(string.Format("Server full speed, frame {0}, total {1}, task {2}, engine {3}, bot {4}, network {5}, push {6}, avg fps {7}", frameIndex, totalTime,
                        taskTime - networkTime, engineTime - taskTime, botTime - engineTime, networkTime - start, end - botTime, getFps()));
                }

                frameIndex++;
                if (frameIndex == int.MaxValue)
                {
                    frameIndex = 1;
                }
            }
            Started = false;
            Dispose();
        }

        private float getFps()
        {
            int sum = 0;
            int count = 0;
            for (int i = 0; i < totalTimes.Length; i++)
            {
                if (totalTimes[i] == 0) continue;
                sum += totalTimes[i];
                count++;
            }
            if (sum != 0)
                return 1000f * count / sum;
            return float.NaN;
        }

        public void Stop()
        {
            running = false;
        }

        const long OneMinute = 60000;
        public bool IsDead()
        {
            var deltaTime = TimeUtil.TimeStamp - lastUpdate;
            return deltaTime > OneMinute || serverThread == null || !serverThread.IsAlive || NetworkServer == null || !NetworkServer.IsRunning();
        }

        public void Dispose()
        {
            running = false;

            if (GameLoto != null)
            {
                GameLoto.Dispose();
                GameLoto.Dispose();
            }

            OneTwoThree.OneTwoThreeBoard.Instance.Dispose();
            Config.OnConfigChange -= onConfigChange;

            if (NetworkServer != null)
            {
                NetworkServer.Dispose();
                NetworkServer = null;
                Lobby = null;
            }

            try
            {
                var deltaTime = TimeUtil.TimeStamp - lastUpdate;
                if (deltaTime > OneMinute && serverThread != null && serverThread.IsAlive) // if time out and still alive
                {
                    serverThread.Abort();
                }
                serverThread = null;
            }
            catch (Exception ex)
            {
                Logger.Error("Fail to abort server thread: " + ex.ToString());
            }
        }

        public async void Revive()
        {
            try
            {
                if (IsDead())
                {
#if SERVER
                    Dispose();

                    // saving crashed people
                    try
                    {
                        var redis = RedisManager.GetRedis();
                        var batch = redis.CreateBatch();
                        for (int i = 0, n = bancaWorlds.Count; i < n; i++)
                        {
                            var world = bancaWorlds[i];
                            if (world == null)
                                continue;
                            for (int j = 0, m = world.players.Length; j < m; j++)
                            {
                                var player = world.players[j];
                                if (player == null)
                                    continue;

                                RedisManager.LogCrashCash(batch, player.PlayerId, player.Cash);
                                await RedisManager.IncEpicCash(player.Id, player.Profit, "revive", "save cash on revive", TransType.EMERGENCY);
                                RedisManager.IncTop(player.PlayerId, player.CashGain);
                                RedisManager.RemovePlayerServer(player.Id);
                                RedisManager.ReleaseLockUserId(player.Id);
                                RedisManager.SetNotPlaying(player.Id);
                            }
                        }
                        batch.Execute();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to save people: " + ex.ToString());
                    }
                    // end saving

                    bancaWorlds.Clear();
                    worldMap.Clear();
                    peerToWorldMap.Clear();

                    Thread t = new Thread(runner);
                    serverThread = t;
                    t.Priority = ThreadPriority.AboveNormal;
                    t.Name = "revived_server_" + Port;
                    t.Start();
                    Logger.Info("Done revive server " + Port);
#endif
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Revive server fail: " + ex.ToString());
                throw ex;
            }
        }
        #endregion

        #region logic
        private GameBanCa createNewWorld()
        {
            var world = new GameBanCa(Port, this.TaskRun);
            world.OnEnterPlayer = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnEnterPlayer", msg);
            };

            world.OnLeavePlayer = (clientId, msg) =>
            {
                removePeerToWorldMap(clientId);
                for (int i = 0, n = bots.Count; i < n; i++)
                {
                    bots[i].OnPush("OnLeavePlayer", msg);
                }

                NetworkServer.PushToClientsInWorld(world, "OnLeavePlayer", msg, SendMode.ReliableOrdered);
            };

            world.OnShoot = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnShoot", msg, SendMode.Sequenced);
            };

            world.OnUpdateObject = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnUpdateObject", msg);
            };

            world.OnObjectTeleport = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnObjectTeleport", msg);
            };

            world.OnUpdateObjectSequence = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnUpdateObject", msg, SendMode.ReliableOrdered);
            };

            world.OnUpdateCash = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnUpdateCash", msg, SendMode.Sequenced);
            };

            world.OnObjectDie = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnObjectDie", msg);
            };

            world.OnRemoveAllObject = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnRemoveAllObject", msg);
            };

            world.OnNewState = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnNewState", msg);
            };

            world.OnItemUse = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnItemUse", msg);
            };
            world.OnLevelUp = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnLevelUp", msg);
            };

            world.OnEndSolo = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnEndSolo", msg);
            };

            world.OnRefundBullet = (msg) =>
            {
                NetworkServer.PushToClientsInWorld(world, "OnRefundBullet", msg);
            };

            world.OnBotRequest = () =>
            {
                //Logger.Info("World " + world.Id + " request bot");
                requestBot(world.Id, world.TableBlindIndex);
            };
            return world;
        }

        private GameBanCa getEmptyWorld(long startCash, int tableId, bool solo, long userId)
        {
            var index = Config.GetTableBlindIndexForPlayer(startCash);
            return getEmptyWorld(index, tableId, solo, userId);
        }
        private GameBanCa getEmptyWorld(int index, int tableId, bool solo, long userId)
        {
            if (tableId != 0)
            {
                if (worldMap.ContainsKey(tableId))
                {
                    var world = worldMap[tableId];
                    if (world != null && world.TableBlindIndex == index && !world.IsFull(userId) && world.SoloMode == solo)
                        return world;
                }
            }

            for (int i = 0, n = bancaWorlds.Count; i < n; i++)
            {
                var world = bancaWorlds[i];
                if (world != null && world.TableBlindIndex == index && !world.IsFull() && world.SoloMode == solo)
                    return world;
            }

            while (worldPools.Count > 0)
            {
                var wp = worldPools[worldPools.Count - 1];
                worldPools.RemoveAt(worldPools.Count - 1);
                if (!wp.IsEmpty())
                {
                    Logger.Info("Found world in pool not empty: " + wp.Id);
                    continue;
                }

                //Logger.Info("Pool world " + wp.Id);
                wp.Recycle(index, solo);
                bancaWorlds.Add(wp);
                worldMap[wp.Id] = wp;
                wp.SoloMode = solo;
                //Logger.Info("Pool world new id " + wp.Id);
                return wp;
            }

            var nw = createNewWorld();
            bancaWorlds.Add(nw);
            worldMap[nw.Id] = nw;
            nw.startInit(index, solo);
            nw.SoloMode = solo;
            return nw;
        }

        private int lastCCUlog = 0;
        //private int lastCCUlogText = 0;
        public void updateCCU()
        {
            try
            {
                var ccu = 0;
                var freeSlot = 0;

                // reset stat
                foreach (var stat in stats)
                {
                    stat.Value.Set(0, 0, 0);
                }

                for (int i = 0, n = bancaWorlds.Count; i < n; i++)
                {
                    var world = bancaWorlds[i];

                    if (world.IsEmpty())
                    {
                        removeWorld(string.Empty, world.Id, world.TableBlindIndex);
                        Logger.Info("Found empty world: " + world.Id);
                        if (n != bancaWorlds.Count) // remove successfully
                        {
                            n = bancaWorlds.Count;
                            i--;
                        }
                        continue; // next
                    }

                    ccu += world.NumberOfPlayer;
                    freeSlot += (4 - world.NumberOfPlayer);
                    int statIndex = world.TableBlindIndex;
                    if (!stats.ContainsKey(statIndex))
                    {
                        stats[statIndex] = new ServerStat();
                    }

                    var stat = stats[statIndex];
                    stat.CCU += world.NumberOfPlayer;
                    stat.FreeSlot += (4 - world.NumberOfPlayer);
                }

                CCU = ccu;
                FreeSlot = freeSlot;

#if SERVER
                var now = TimeUtil.TimeStamp;
                var diff = now - lastLogCCU;
                var ccuLogin = NetworkServer.Count;
                //lastCCUlog
                int sum = Port.GetHashCode() ^ ccuLogin.GetHashCode();
                if (Lobby != null)
                {
                    sum = sum ^ Lobby.Count;
                }
                if (sum != lastCCUlog)
                {
                    lastCCUlog = sum;
                    foreach (var stat in stats)
                    {
                        RedisManager.updateCache(Port, stat.Key, stat.Value.FreeSlot, NetworkServer.Count);
                    }

                    //if (sum != lastCCUlogText)
                    {
                        var log = string.Format("{0} bot: {1}, peer: {2}, ccu: {3}", Port, bots.Count, NetworkServer.Count, Lobby != null ? Lobby.Count : 0);
                        Logger.Info(log);
                        //lastCCUlogText = sum;
                    }
                }
                if (diff > 60000) // prevent stress db
                {
                    lastLogCCU = now;
                    RedisManager.LogServer(Port, ccu, freeSlot, 0);
                    foreach (var stat in stats)
                    {
                        RedisManager.LogBlindsServer(Port, stat.Key, stat.Value.CCU, stat.Value.FreeSlot, 0);
                    }

                    //BanCa.Sql.SqlLogger.LogCCU(Port, NetworkServer.Count, bots.Count, freeSlot);
                    BanCa.Sql.SqlLogger.LogCCU(Port, Lobby != null ? Lobby.Count : 0, 0, freeSlot);

                    //if (sum != lastCCUlogText)
                    //{
                    //    var log = string.Format("{0} {1} {2} {3} {4}", Port, ccu, freeSlot, Lobby != null ? Lobby.Count : 0, NetworkServer.Count);
                    //    Logger.Info(log);
                    //    lastCCUlogText = sum;
                    //}
                }
#endif
            }
            catch (Exception ex)
            {
                Logger.Error("Fail to update ccu: " + ex.ToString());
            }
        }

        private void removeWorld(string clientId, int worldId, int worldIndex)
        {
            if (!string.IsNullOrEmpty(clientId)) peerToWorldMap.Remove(clientId);

            if (worldMap.ContainsKey(worldId))
            {
                var world = worldMap[worldId];
                worldMap.Remove(worldId);
                for (int i = 0, n = bancaWorlds.Count; i < n; i++)
                {
                    if (bancaWorlds[i].Id == worldId)
                    {
                        bancaWorlds[i] = bancaWorlds[bancaWorlds.Count - 1];
                        bancaWorlds.RemoveAt(bancaWorlds.Count - 1);
                        break;
                    }
                }
#if SERVER
                BanCa.Sql.SqlLogger.LogTableEnd(worldId, worldIndex, Port, world.SoloMode);
#endif
                if (world != null)
                {
                    if (world.CanRecycle)
                    {
                        worldPools.Add(world);
                    }
                }
            }
        }

        internal void onRemovePeer(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                var world = getWorldFromPeer(clientId);
                var cash = -1L;
                if (world != null)
                {
                    var p = world.RemovePlayerByPeerId(clientId, Config.QuitReason.Disconnect);
                    if (p != null)
                    {
                        cash = p.Cash;
                    }
                    if (world.IsEmpty())
                    {
                        removeWorld(clientId, world.Id, world.TableBlindIndex);
                    }
                }
                //else
                //{
                //    Logger.Info("No world to remove: " + clientId + " on port " + Port);
                //}

                if (Lobby != null)
                {
                    Lobby.onRemovePeer(clientId);
                }

                if (GameLoto != null)
                {
                    GameLoto.onRemovePeer(clientId);
                }

                OneTwoThree.OneTwoThreeBoard.Instance.onRemovePeer(clientId);

                updateCCU();
                peerToWorldMap.Remove(clientId);
                lastRequestTime.Remove(clientId);
                requestCount.Remove(clientId);
            }
            else
            {
                Logger.Info("Cannot remove peer, clientId is empty");
            }
        }

        private HashSet<Config.BulletType> invalidBullets = new HashSet<Config.BulletType>() { Config.BulletType.Basic, Config.BulletType.Bullet5, Config.BulletType.Bullet6 };
        private void onClientNotify(string clientId, string route, JSONNode msg)
        {
            if (Lobby != null && Lobby.onClientNotify(this, clientId, route, msg)) return;
            if (GameLoto != null && GameLoto.onClientNotify(this, clientId, route, msg)) return;
            if (OneTwoThree.OneTwoThreeBoard.Instance.onClientNotify(this, clientId, route, msg)) return;

            switch (route)
            {
                case "shoot":
                    GameBanCa world = getWorldFromPeer(clientId);
                    if (world != null)
                    {
                        float rad = msg["rad"].AsFloat;
                        int targetId = msg.HasKey("target") ? msg["target"].AsInt : -1;
                        bool rapidFire = msg.HasKey("rapidFire") ? msg["rapidFire"].AsBool : false;
                        bool auto = msg.HasKey("auto") ? msg["auto"].AsBool : false;
                        try
                        {
                            var type = (Config.BulletType)msg["type"].AsInt;
                            if (invalidBullets.Contains(type))
                            {
                                var user = Lobby.IsLogin(clientId);
                                if (user != null)
                                {
                                    Logger.Info("#HACK: Detect user shoot invalid bullets: " + user.UserId);
                                    RedisManager.AddHacker(user.UserId, RedisManager.HackType.InvalidBullet, msg.ToString());
                                }
                                else
                                {
                                    Logger.Info("#HACK: Detect unknown user shoot invalid bullets: " + clientId);
                                }
                                NetworkServer.Kick(clientId);
                                return;
                            }
                            world.ShootByPeerId(clientId, rad, type, targetId, rapidFire, auto);
                        }
                        catch (Exception ex)
                        {
                            var user = Lobby.IsLogin(clientId);
                            if (user != null)
                            {
                                Logger.Info("Exception in shoot, id: " + user.UserId + ", ex: " + ex.ToString());
                                RedisManager.AddHacker(user.UserId, RedisManager.HackType.InvalidBullet, msg.ToString());
                            }
                            else
                            {
                                Logger.Info("Exception in shoot, cid: " + clientId + ", ex: " + ex.ToString());
                            }
                        }
                    }
                    break;
            }
        }

        private Dictionary<string, int> requestCount = new Dictionary<string, int>();
        private Dictionary<string, long> lastRequestTime = new Dictionary<string, long>();
        private void onClientRequest(string clientId, int msgId, string route, JSONNode msg)
        {
            if (!lastRequestTime.ContainsKey(clientId))
            {
                lastRequestTime[clientId] = TimeUtil.TimeStamp;
                requestCount[clientId] = 1;
            }
            else
            {
                var delta = TimeUtil.TimeStamp - lastRequestTime[clientId];
                if (!requestCount.ContainsKey(clientId))
                {
                    requestCount[clientId] = 1;
                }
                else
                {
                    requestCount[clientId]++;
                }

                if (delta > Config.RequestSampleRateMs)
                {
                    var avg = requestCount[clientId] * 1000f / delta; // avg request / s
                    bool isHack = false;
                    if (avg > Config.MaxRequestPerSecond)
                    {
                        var user = Lobby.IsLogin(clientId);
                        if (user != null)
                        {
                            Logger.Info("#HACK: Detect user hack speed: " + avg + " " + user.UserId);
                            RedisManager.AddHacker(user.UserId, RedisManager.HackType.HackSpeed, avg.ToString());
                        }
                        else
                        {
                            Logger.Info("#HACK: Detect user hack speed on peerId: " + avg + " " + clientId);
                        }
                        isHack = true;
                    }

                    lastRequestTime[clientId] = TimeUtil.TimeStamp;
                    requestCount[clientId] = 0;
                    if (isHack)
                    {
                        NetworkServer.Kick(clientId);
                        return;
                    }
                }
            }

            if (Lobby != null && Lobby.onClientRequest(this, clientId, msgId, route, msg)) return;
            if (GameLoto != null && GameLoto.onClientRequest(this, clientId, msgId, route, msg)) return;
            if (OneTwoThree.OneTwoThreeBoard.Instance.onClientRequest(this, clientId, msgId, route, msg)) return;

            switch (route)
            {
                case "ping":
                    {
                        var msg2 = new JSONObject();
                        msg2["time"] = TimeUtil.TimeStamp;
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    break;
                case "state":
                    {
                        var user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return;

                        GameBanCa world = getWorldFromPeer(clientId);
                        if (world != null)
                        {
                            var json = world.ToJson(true, true, user.UserId);
                            NetworkServer.ResponseToClient(clientId, msgId, json);
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "play":
                    {
                        if (!Config.IsMaintain && !peerToWorldMap.ContainsKey(clientId))
                        {
                            string playerId = msg["playerId"];
                            string password = msg["password"];
                            string appId = msg["appId"];
                            int vcode = msg["vcode"].AsInt;
                            int tableIndex = msg["index"].AsInt;
                            int tableId = msg.HasKey("tableId") ? msg["tableId"].AsInt : 0; // prefer tableid
                            bool solo = (msg.HasKey("solo") ? msg["solo"].AsInt : 0) != 0;
                            //Logger.Info("Request play solo " + solo);
                            if (tableIndex < 0 || tableIndex >= Config.TableStartingBlinds.Length)
                            {
                                tableIndex = 0;
                            }

                            if (solo && !AllowSolo)
                            {
                                var msg2 = new JSONObject();
                                msg2["ok"] = false;
                                msg2["maintain"] = Config.IsMaintain;
                                msg2["err"] = 5;
                                msg2["port"] = Port;
                                Logger.Info("Cannot play solo on port " + Port);
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                return;
                            }

                            string deviceId = "", ip = NetworkServer.GetEndPoint(clientId);
                            TaskRun.QueueAction(async () =>
                            {
                                var res = await Lobby.Login(clientId, playerId, password);
                                if (res != null)
                                {
                                    var data = res;
                                    if (data.error == 0 && RedisManager.TryLockUserId(res.UserId))
                                    {
                                        var loginServer = RedisManager.GetPlayerServer(res.UserId);
                                        if (RedisManager.CheckDupplicateLogin && loginServer != -1 && loginServer != Port) // this user has already login
                                        {
                                            RedisManager.ReleaseLockUserId(res.UserId);
                                            Logger.Info(string.Format("User login at different server to play BC {0} vs {1} : {2}", loginServer, Port, res.UserId));
                                            return null;
                                        }

                                        //data.IapCount = RedisManager.GetIapCount(data.UserId);
                                        data.CardInCount = await RedisManager.GetCardInCount(data.UserId);

                                        var Id = data.UserId;
                                        var player = new Player();
                                        res.VersionCode = vcode;
                                        res.AppId = appId;
                                        res.ClientId = clientId;
                                        player.Id = Id;
                                        player.Cash = data.Cash;
                                        player.CardIn = data.CardInCount;
                                        player.SetLevel(data.BcLevel);
                                        player.Exp = data.BcExp;
                                        player.Nickname = data.Nickname;
                                        player.Avatar = data.Avatar;
                                        deviceId = data.DeviceId;
                                        ip = data.IP;
                                        player.TimeUseSnipe = await RedisManager.GetSnipeTimestamp(playerId);
                                        player.TimeUseRapidFire = await RedisManager.GetRapidFireTimestamp(playerId);
                                        RedisManager.GetSnipeCount(Id); // initialize cache
                                        RedisManager.GetRapidFireCount(Id); // initialize cache
                                        RedisManager.SetPlayerServer(res.UserId, NetworkServer.Port); // player has switch server
                                        RedisManager.ReleaseLockUserId(res.UserId);
                                        return player;
                                    }
                                }
                                return null;
                            }, (o) =>
                            {
                                // {"ErrorCode":1,"ErrorMsg":"1.55.18.44","Age":0,"Avatar":null,"Cash":0,"CashSafe":0,"CashSilver":0,"Description":null,"Games":null,"Gender":null,"IsExChange":0,"Language":null,"Level":0,"Like":0,"Married":null,"Nickname":null,"PhoneNumber":null,"PublicProfile":0,"TimeLogin":null,"TotalFriend":0,"UrlFacebook":null,"UrlTwitter":null,"UserId":0,"VipId":0,"VipPoint":0}
                                if (o != null)
                                {
                                    var _loginPlayer = (Player)o;
                                    long cash = _loginPlayer.Cash;
                                    GameBanCa world = tableIndex == 0 ? getEmptyWorld(cash, tableId, solo, _loginPlayer.Id) : getEmptyWorld(tableIndex, tableId, solo, _loginPlayer.Id);
                                    tableIndex = world.TableBlindIndex;
                                    var cashToJoin = solo ? Config.TableSoloCashIn[world.TableBlindIndex] : world.TableBlind;
                                    if (cash < cashToJoin)
                                    {
                                        var msg3 = new JSONObject();
                                        msg3["ok"] = false;
                                        msg3["maintain"] = Config.IsMaintain;
                                        msg3["err"] = 4;
                                        NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                        return;
                                    }

                                    if (_loginPlayer.CardIn < Config.TableRequireCardIn[world.TableBlindIndex])
                                    {
                                        var msg3 = new JSONObject();
                                        msg3["ok"] = false;
                                        msg3["maintain"] = Config.IsMaintain;
                                        msg3["err"] = 6;
                                        msg3["myCardIn"] = _loginPlayer.CardIn;
                                        msg3["require"] = Config.TableRequireCardIn[world.TableBlindIndex];
                                        NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                        return;
                                    }

                                    var player = world.RegisterPlayer(playerId, _loginPlayer.Id, cash);
                                    if (player == null)
                                    {
                                        var msg3 = new JSONObject();
                                        msg3["ok"] = false;
                                        msg3["maintain"] = Config.IsMaintain;
                                        msg3["err"] = 4;
                                        NetworkServer.ResponseToClient(clientId, msgId, msg3);
                                        return;
                                    }
                                    player.Id = _loginPlayer.Id;
                                    player.PeerId = clientId;
                                    player.Nickname = _loginPlayer.Nickname;
                                    player.Avatar = _loginPlayer.Avatar;
                                    player.Exp = _loginPlayer.Exp;
                                    player.SetLevel(_loginPlayer.Level);
                                    player.TimeUseRapidFire = _loginPlayer.TimeUseRapidFire;
                                    player.TimeUseSnipe = _loginPlayer.TimeUseSnipe;
                                    player.CardIn = _loginPlayer.CardIn;
                                    peerToWorldMap[clientId] = world.Id;

                                    if (world.OnEnterPlayer != null)
                                    {
                                        var msg3 = new JSONObject();
                                        msg3["playerId"] = playerId;
                                        msg3["posIndex"] = player.PosIndex;
                                        msg3["data"] = player.ToJson();
                                        world.OnEnterPlayer(msg3);
                                    }
                                    RedisManager.SetPlaying(player.Id, world.Id);
                                    BanCa.Sql.SqlLogger.LogPlayerEnter(world.Id, world.TableBlindIndex, player.Id, player.Cash, BanCa.Sql.Reason.PlayerEnter, world.ServerId,
                                        _loginPlayer.PlayerId + " " + NetworkServer.Port + " " + deviceId + " " + ip);
                                    var _id = player.Id;
                                    //ThreadPool.QueueUserWorkItem(oo => { RedisManager.LockEpicCash(_id); });
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = true;
                                    msg2["config"] = Config.ToJson();
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);

                                    //if (Lobby.IsLogin(player.Id) == null)
                                    if (!NetworkServer.HasPeer(clientId))
                                    {
                                        var uid = player.Id;
                                        Logger.Info("User " + uid + " finished play request but disconnected");
                                        //if (RedisManager.TryLockUserId(uid))
                                        {
                                            RedisManager.RemovePlayerServer(uid);
                                            RedisManager.SetNotPlaying(uid);
                                            RedisManager.ReleaseLockUserId(uid);
                                        }
                                        onRemovePeer(clientId);
                                    }

                                    updateCCU();
                                }
                                else
                                {
                                    var msg2 = new JSONObject();
                                    msg2["ok"] = false;
                                    msg2["maintain"] = Config.IsMaintain;
                                    msg2["err"] = 2;
                                    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                }
                            });
                        }
                        else
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            msg2["maintain"] = Config.IsMaintain;
                            msg2["err"] = 1;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "useBombItem": // only in solo
                    {
                        try
                        {
                            var type = (Config.BulletType)msg["type"].AsInt;
                            if (invalidBullets.Contains(type))
                            {
                                var user = Lobby.IsLogin(clientId);
                                if (user != null)
                                {
                                    Logger.Info("#HACK: Detect user bomb invalid bullets: " + user.UserId);
                                    RedisManager.AddHacker(user.UserId, RedisManager.HackType.InvalidBullet, msg.ToString());
                                }
                                else
                                {
                                    Logger.Info("#HACK: Detect unknown user bomb invalid bullets: " + clientId);
                                }
                                NetworkServer.Kick(clientId);
                                return;
                            }

                            var world = getWorldFromPeer(clientId);
                            if (world != null)
                            {
                                var p = world.getPlayerByPeerId(clientId);
                                if (p != null)
                                {
                                    p.IdleTimeS = 0;
                                    int err = world.UseBombItem(p.PlayerId, type);
                                    if (err == 0)
                                    {
                                        var msg3 = new JSONObject();
                                        msg3["playerId"] = p.PlayerId;
                                        msg3["type"] = msg["type"];
                                        //Logger.Info("OnUseBomb: " + msg3.ToString());
                                        NetworkServer.PushToClientsInWorld(world, "OnUseBomb", msg3);

                                        var msg2 = new JSONObject();
                                        msg2["ok"] = true;
                                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error on use bomb: " + ex.ToString());
                        }

                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "soloInvite": // only in solo
                    {
                        var world = getWorldFromPeer(clientId);
                        if (world != null)
                        {
                            var msg3 = new JSONObject();
                            var p = world.getPlayerByPeerId(clientId);
                            if (p != null)
                            {
                                p.IdleTimeS = 0;
                                msg3["playerId"] = p.PlayerId;
                                msg3["nickname"] = p.Nickname;
                                msg3["blindIndex"] = world.TableBlindIndex;
                                BanCaLib.PushAll("OnInviteSolo", msg3);
                            }
                        }

                        var msg2 = new JSONObject();
                        msg2["ok"] = true;
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    break;
                case "chat":
                    {
                        var world = getWorldFromPeer(clientId);
                        string message = msg["msg"];
                        if (world != null)
                        {
                            var msg3 = new JSONObject();
                            var p = world.getPlayerByPeerId(clientId);
                            if (p != null)
                            {
                                p.IdleTimeS = 0;
                                msg3["playerId"] = p.PlayerId;
                                msg3["msg"] = message;
                                NetworkServer.PushToClientsInWorld(world, "OnChat", msg3);
                            }
                        }

                        var msg2 = new JSONObject();
                        msg2["ok"] = true;
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    break;
                case "quit":
                    {
                        var msg2 = new JSONObject();
                        var world = getWorldFromPeer(clientId);
                        msg2["ok"] = true;
                        long userId = -1;
                        JSONNode me = null;
                        if (world != null)
                        {
                            // return all current players playing in board to show end game dialog
                            var players = new JSONArray();
                            var myP = world.getPlayerByPeerId(clientId);
                            for (int i = 0; i < world.players.Length; i++)
                            {
                                var _p = world.players[i];
                                if (_p != null)
                                {
                                    var js = _p.ToJson();
                                    if (_p == myP)
                                    {
                                        me = js;
                                    }

                                    players.Add(js);
                                }
                            }

                            msg2["players"] = players;
                            var p = world.RemovePlayerByPeerId(clientId);

                            if (p != null)
                            {
                                p.CashGain = 0;
                                p.ExpGain = 0;
                                userId = p.Id;
                            }
                            if (world.IsEmpty())
                            {
                                removeWorld(clientId, world.Id, world.TableBlindIndex);
                            }

                            if (peerToWorldMap.Remove(clientId))
                            {
                                updateCCU();
                            }
                        }

                        if (userId != -1)
                        {
                            long cash = -1L;
                            var user = Lobby.IsLogin(userId);
#if SERVER
                            TaskRun.QueueAction(async () =>
                            {
                                if (user != null)
                                {
                                    var msgEvent = await RedisManager.CheckCointEvent(user);
                                    if (!string.IsNullOrEmpty(msgEvent))
                                    {
                                        AlertStr(user.UserId, msgEvent);
                                    }
                                }

                                cash = await RedisManager.GetUserCash(userId);
                                if (cash != -1)
                                {
                                    msg2["cash"] = cash;
                                    if (me != null) me["cash"] = cash;
                                }

                                var loginServer = RedisManager.GetPlayerServer(userId);
                                if (loginServer != NetworkServer.Port)
                                {
                                    Logger.Info("Invalid server request on quit: " + userId);
                                }

                                return msg2;
                            }, (o) =>
                            {
#endif
                                if (user != null && cash != -1L)
                                {
                                    user.Cash = cash;
                                }
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
#if SERVER
                            });
#endif
                        }
                        else
                        {
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "bestServer":
                    {
                        int blindIndex = msg["blindIndex"].AsInt;
                        bool useWs = msg.HasKey("ws") ? msg["ws"].AsInt != 0 : false;
                        TaskRun.QueueAction(() =>
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
#if SERVER
                            msg2["best"] = RedisManager.GetBestFitServer(blindIndex, useWs);
#else
                            msg2["best"] = useWs ? this.WsPort : this.Port;
#endif
                            return msg2;
                        }, (o) =>
                        {
                            NetworkServer.ResponseToClient(clientId, msgId, (JSONNode)o);
                        });
                    }
                    break;
                case "report":
                    {
                        var report = msg.ToString();
                        Logger.Info("#REPORT: Client report: " + report);
                        var user = Lobby.IsLogin(clientId);
                        if (user != null)
                        {
                            RedisManager.AddHacker(user.UserId, RedisManager.HackType.ClientReport, report);
                        }
                        var msg2 = new JSONObject();
                        msg2["ok"] = true;
                        NetworkServer.ResponseToClient(clientId, msgId, msg2);
                    }
                    break;
                case "getBcHistory":
                    {
                        var user = Lobby.IsLogin(clientId);
                        if (user == null)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            break;
                        }
                        TaskRunner.RunOnPool(async () =>
                        {
                            var result = await SqlLogger.GetBcHistory(user.UserId);
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            msg2["data"] = result;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        });
                    }
                    break;
                case "openTx": // transfer all cash from bc back to redis
                    {
                        var user = Lobby.IsLogin(clientId);
                        if (user == null)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            break;
                        }

                        var world = getWorldFromPeer(clientId);
                        if (world != null)
                        {
                            if (world.SoloMode)
                            {
                                //if(world.WorldState == State.Waiting) // if solo has not start, cannot play tx cause player must pay fee
                                //{
                                //    var msg2 = new JSONClass();
                                //    msg2["ok"] = false;
                                //    NetworkServer.ResponseToClient(clientId, msgId, msg2);
                                //    break;
                                //}
                                RedisManager.SetNotPlaying(user.UserId);
                            }
                            else
                            {
                                var p = world.getPlayerByPeerId(clientId);
                                if (p != null)
                                {
                                    RedisManager.IncEpicCashCache(p.Id, p.Profit, user.Platform, "openTx", TransType.TAIXIU);
                                    RedisManager.SetNotPlaying(user.UserId);
                                    p.Cash = 0;
                                    p.Profit = 0;
                                }
                            }
                        }

                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = true;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                        }
                    }
                    break;
                case "closeTx":
                    {
                        var user = Lobby.IsLogin(clientId);
                        if (user == null)
                        {
                            var msg2 = new JSONObject();
                            msg2["ok"] = false;
                            NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            break;
                        }

                        TaskRun.QueueAction(async () =>
                        {
                            return await RedisManager.GetUserCash(user.UserId);
                        }, o =>
                        {
                            long cash = (long)o;
                            var world = getWorldFromPeer(clientId);
                            if (world != null)
                            {
                                var p = world.getPlayerByPeerId(clientId);
                                if (p != null && !world.SoloMode)
                                {
                                    p.Cash = cash;
                                }
                                RedisManager.SetPlaying(user.UserId, world.Id);
                            }

                            {
                                var msg2 = new JSONObject();
                                msg2["ok"] = true;
                                msg2["cash"] = cash;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                        });
                    }
                    break;
            }
        }

        public void AddCashToBc(string clientId, long addCash)
        {
            var world = getWorldFromPeer(clientId);
            if (world != null)
            {
                var p = world.getPlayerByPeerId(clientId);
                if (p != null)
                {
                    p.Cash += addCash;
                }
            }
        }

        internal void GetListTable(Action<int, List<int>> cb)
        {
            List<int> ret = new List<int>();
            TaskRun.QueueAction(() =>
            {
                for (int i = 0; i < bancaWorlds.Count; i++)
                {
                    ret.Add(bancaWorlds[i].Id);
                }
                cb(Port, ret);
            });
        }

        internal void GetTable(int wid, Action<JSONNode> cb)
        {
            TaskRun.QueueAction(() =>
            {
                cb(worldMap.ContainsKey(wid) ? worldMap[wid].ToJson(false, false) : null);
            });
        }

        internal JSONArray GetOnlines()
        {
            if (Lobby != null) return Lobby.getOnlineUsers();
            return new JSONArray();
        }

        /// <summary>
        /// Use in maintain only
        /// </summary>
        public void RemoveAllPlayers()
        {
            NetworkServer.RemoveAllPeers();
        }

        public void AlertStr(long userId, string message)
        {
            var msg = new JSONObject();
            msg["content"] = message;
            if (userId == -1)
            {
                NetworkServer.PushToClient(string.Empty, "onAlert", msg, SendMode.ReliableOrdered);
            }
            else
            {
                var user = Lobby.IsLogin(userId);
                if (user != null)
                {
                    NetworkServer.PushToClient(user.ClientId, "onAlert", msg, SendMode.ReliableOrdered);
                }
            }
        }

        public void Alert(long userId, JSONNode message)
        {
            if (userId == -1)
            {
                NetworkServer.PushToClient(string.Empty, "onAlert", message, SendMode.ReliableOrdered);
            }
            else
            {
                var user = Lobby.IsLogin(userId);
                if (user != null)
                {
                    NetworkServer.PushToClient(user.ClientId, "onAlert", message, SendMode.ReliableOrdered);
                }
            }
        }

        public void Kick(long userId)
        {
            var user = Lobby.IsLogin(userId);
            if (user != null)
            {
                NetworkServer.Kick(user.ClientId);
            }
        }

        public void Kick(string clientId)
        {
            NetworkServer.Kick(clientId);
        }

        public void ReloadCash(long userId, JSONNode message)
        {
            var user = Lobby.IsLogin(userId);
            if (user != null && !string.IsNullOrEmpty(user.ClientId))
            {
                if (message.HasKey("newCash"))
                {
                    var newestCash = user.Cash = message["newCash"].AsLong;

                    // TODO: stop reload cash while playing, because cash in redis can play both BC and epic at the same time
                    if (message.HasKey("changeCash"))
                    {
                        var changeCash = message["changeCash"].AsLong;
                        var world = getWorldFromPeer(user.ClientId);
                        if (world != null) // player is playing
                        {
                            var player = world.getPlayerByPeerId(user.ClientId);
                            if (player != null)
                            {
                                player.Cash += changeCash;
                                if (player.Cash < 0)
                                {
                                    player.Cash = 0;
                                }
                                newestCash = player.Cash;
                            }

                            if (changeCash < 0)
                            {
                                Logger.Info("Remove player " + message.ToString() + " name " + user.Nickname);
                                world.RemovePlayerByPeerId(user.ClientId, Config.QuitReason.Kick);
                            }
                        }
                    }
                    message["newCash"] = newestCash;
                }
                NetworkServer.PushToClient(user.ClientId, "reloadCash", message, SendMode.ReliableOrdered);
                //Logger.Info("Push " + message.ToString() + " to " + user.Nickname);
            }
            //else
            //{
            //    Logger.Info("User not login " + message.ToString() + " server " + Port);
            //}
        }

        public void UpdateJackpot(JSONNode jackpotJson)
        {
            NetworkServer.PushAll("OnUpdateJackpot", jackpotJson, SendMode.ReliableOrdered);
        }

        internal GameBanCa getWorldFromPeer(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return null;
            }
            int wid = peerToWorldMap.ContainsKey(clientId) ? peerToWorldMap[clientId] : -1;
            GameBanCa world = worldMap.ContainsKey(wid) ? worldMap[wid] : null;
            return world;
        }

        void _updateEngine()
        {
            var now = TimeUtil.TimeStamp;
            var delta = now - lastUpdate;
            bool _updateCCU = false;
            if (delta > 0)
            {
                var deltaF = (now - lastUpdate) / 1000f;
                for (int i = 0, n = bancaWorlds.Count; i < n; i++)
                {
                    var world = bancaWorlds[i];
                    world.Update(deltaF);
                }
                for (int i = 0, n = bancaWorlds.Count; i < n; i++) // detect world empty due to kick, timeout,...
                {
                    var world = bancaWorlds[i];
                    if (world.IsEmpty())
                    {
                        removeWorld(string.Empty, world.Id, world.TableBlindIndex);
                        i--;
                        n = bancaWorlds.Count;
                        _updateCCU = true;
                    }
                }
                lastUpdate = now;
            }

            if (_updateCCU || now - lastLogCCU > 60000) // 1'
            {
                updateCCU();
            }
        }
        #endregion

        #region BOT

        private List<BcBot> bots = new List<BcBot>();
        private BcBot spawnBot()
        {
            var bot = new BcBot(this);
            bots.Add(bot);
            return bot;
        }

        private BcBot requestBot(int tableId, int tableIndex)
        {
            if (!BotConfig.BotActive) return null;

            for (int i = 0, n = bots.Count; i < n; i++)
            {
                if (bots[i].State == BotState.Free)
                {
                    bots[i].JoinTable(tableId, tableIndex);
                    return bots[i];
                }
            }

            var bot = spawnBot();
            bot.JoinTable(tableId, tableIndex);
            return bot;
        }

        private void updateBot(int delta)
        {
            for (int i = 0, n = bots.Count; i < n; i++)
            {
                bots[i].Update(delta);

                if (bots.Count > Bots.BotConfig.MaxAllowBot && bots[i].State == BotState.Free)
                {
                    bots[i] = bots[n - 1];
                    bots.RemoveAt(n - 1);
                    i--;
                    n--;
                }
            }
        }

        internal GameBanCa botJoinGame(BcBot bot, int tableId)
        {
            if (!worldMap.ContainsKey(tableId))
            {
                return null;
            }

            string playerId = bot.UserBot.Username;
            string clientId = bot.UserBot.ClientId;

            long cash = bot.UserBot.Cash;
            GameBanCa world = worldMap[tableId];
            var tableIndex = world.TableBlindIndex;
            var player = world.RegisterPlayer(playerId, bot.UserBot.UserId, cash, true);
            if (player != null)
            {
                player.Id = bot.Id;
                player.PeerId = clientId;
                player.Nickname = bot.UserBot.Nickname;
                player.Avatar = bot.UserBot.Avatar;
                player.Exp = 0;
                player.SetLevel(bot.UserBot.Level);
                player.TimeUseRapidFire = 0;
                player.TimeUseSnipe = 0;
                player.IsBot = true;
                peerToWorldMap[clientId] = world.Id;

                if (world.OnEnterPlayer != null)
                {
                    var msg3 = new JSONObject();
                    msg3["playerId"] = playerId;
                    msg3["posIndex"] = player.PosIndex;
                    msg3["data"] = player.ToJson();
                    world.OnEnterPlayer(msg3);
                }

                BanCa.Sql.SqlLogger.LogPlayerEnter(world.Id, world.TableBlindIndex, player.Id, player.Cash, BanCa.Sql.Reason.PlayerEnter, world.ServerId,
                    playerId + " " + NetworkServer.Port + " BOT");
                return world;
            }
            return null;
        }

        internal void quitGame(BcBot bot)
        {
            string clientId = bot.UserBot.ClientId;
            var world = getWorldFromPeer(clientId);
            if (world != null)
            {
                var p = world.RemovePlayerByPeerId(clientId);
                if (p != null)
                {
                    bot.UserBot.Cash = p.Cash;
                }
                if (world.IsEmpty())
                {
                    removeWorld(clientId, world.Id, world.TableBlindIndex);
                }

                peerToWorldMap.Remove(clientId);
            }
        }
        #endregion
    }
}
