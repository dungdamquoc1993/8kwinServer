using differ.data;
using System;
using System.Collections.Generic;
using SimpleJSON;
using differ.shapes;
using System.Threading;
using static BanCa.Libs.Config;

namespace BanCa.Libs
{
    public enum State : int
    {
        Waiting = 0, // waiting for player, do nothing
        Playing, // play normally
        WaitingForNewWave, // waiting for fish to move all out of screen
        NewWave, // form fish formation and execute, at end of wave turn back to playing
    }

    public enum CashSource : int
    {
        KillFish = 0,
        OutOfCash = 1,
        KillAll = 2,
        GoldenFrog = 3,
    }

    class PendingTask
    {
        public long ExecuteAtTime;
        public Action Action;
    }

    public class GameBanCa // = one game table
    {
        private static volatile int idCount = 0;
        public int Id { get; private set; }
        public readonly int ServerId;
        public List<BanCaBullet> bulletPool = new List<BanCaBullet>();
        public List<BanCaBullet> activeBullets = new List<BanCaBullet>();
        public List<BanCaObject> allObjects = new List<BanCaObject>(); // normal fish, move inside world bound

        public List<BanCaObject>
            allSpecialObjects = new List<BanCaObject>(); // special fish, move inside outer world bound

        public Dictionary<int, BanCaObject> objectMap = new Dictionary<int, BanCaObject>();

        public Player[] players = new Player[4];
        public int NumberOfPlayer { get; private set; }
        public readonly Random Random = new Random();
        public const State StartState = State.Playing;

        private State worldState;

        public State WorldState
        {
            get { return worldState; }
            set
            {
                worldState = value;
                //Logger.Info("Set state to " + value);
            }
        }

        internal ShapeCollision collider = new ShapeCollision();

        private float waveLength = Config.PLAYING_WAVE_DURATION;
        private long WaveStart = 0;
        public readonly bool IsServer = true; // author everything, client only mimic server

        private List<PendingTask> Tasks = new List<PendingTask>();

        public readonly List<Vector> RespawnPoints = new List<Vector>();

        public long TableBlind { get; private set; }
        public int TableBlindIndex { get; private set; }

        private Polygon BoundingBox;
        private Polygon WorldBoundingBox;
        private int objIdCount = 0;

        private float powerUpTimeCount = Config.PowerUpIntervalS;
        private LinkedList<BanCaObject> deadFish = new LinkedList<BanCaObject>();
        private float ReviveRateS = 2;

        private IWave currentWave;
        private IWave[] waveLibrary;

        private HashSet<Config.FishType> annouceFish = new HashSet<Config.FishType>
        {
            Config.FishType.CaThanTai, Config.FishType.GoldenFrog, Config.FishType.MermaidBig, Config.FishType.Phoenix
        };

        private HashSet<Config.FishType> bigFish = new HashSet<Config.FishType>
        {
            Config.FishType.CaThanTai, Config.FishType.Mermaid, Config.FishType.MermaidBig,
            Config.FishType.MermaidSmall, Config.FishType.MerMan
        };

        private int bigFishCount = 0;

        public bool CanRecycle = true;
        private HashSet<int> herdFish = new HashSet<int>();
        private Dictionary<long, long> lastShootTime = new Dictionary<long, long>();

        internal bool needBot = false;
        internal float needBotCountDown = 0;

        private PrimeSearch Prime;

        // solo mode
        private bool soloMode = false;

        public bool SoloMode
        {
            get { return soloMode; }
            set
            {
                soloMode = value;
                SoloPlayerId1 = SoloPlayerId2 = null;
                soloStart = long.MaxValue;
                if (soloMode) // move out of sight
                {
                    foreach (var fish in allSpecialObjects)
                    {
                        fish.Pos.Set(Config.SpawnX, Config.SpawnY);
                    }

                    for (int i = 0, n = allObjects.Count; i < n; i++)
                    {
                        var o = allObjects[i];
                        if (o != null)
                        {
                            o.Health = -1;
                            o.Value = 0;
                            o.Remove();
                            scheduleToRevive(o);
                        }
                    }

                    waveLength = Config.SOLO_DURATION;
                    soloEnded = false;
                }
                else
                {
                    waveLength = Config.PLAYING_WAVE_DURATION;
                }
            }
        }

        private SoloPlayer SoloPlayerId1, SoloPlayerId2;
        private long minBulletValue;
        private long soloStart;
        private bool soloEnded = false;
        private System.Timers.Timer checkEndGame = null;

        public readonly TaskRunner TaskRun;

        internal static void setTableStartId(int id)
        {
            Interlocked.Exchange(ref idCount, id);
        }

        public GameBanCa(int serverId, TaskRunner runner)
        {
            Id = Interlocked.Increment(ref idCount);
            IsServer = serverId > 0;
            ServerId = serverId;
            TaskRun = runner;

            Prime = new PrimeSearch((int) Config.BombThreshold + 1);

            TableBlind = TableBlindIndex = -1;
            needBot = Bots.BotConfig.BotActive;
            needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin1Ms, Bots.BotConfig.TimeToJoinMax1Ms);
#if SERVER
            if (IsServer)
            {
                BanCa.Redis.RedisManager.IncTableId();
            }
#endif

            WorldState = State.Waiting;

            for (int i = 0; i < Config.NUMBER_OF_BULLETS; i++)
            {
                var b = new BanCaBullet(this);
                b.ID = 1000 + objIdCount++;
                bulletPool.Add(b);
            }

            //for (int i = 4; i < 6; i++) // top
            //{
            //    RespawnPoints.Add(new Vector(Config.SpawnX + i * Config.SpawnW / 10, Config.SpawnY));
            //}
            //for (int i = 4; i < 6; i++) // bottom
            //{
            //    RespawnPoints.Add(new Vector(Config.SpawnX + i * Config.SpawnW / 10, Config.SpawnY + Config.SpawnH));
            //}
            for (int i = 6; i < 14; i++) // left
            {
                RespawnPoints.Add(new Vector(Config.SpawnX, Config.SpawnY + i * Config.SpawnH / 20));
            }

            for (int i = 6; i < 14; i++) // right
            {
                RespawnPoints.Add(new Vector(Config.SpawnX + Config.SpawnW, Config.SpawnY + i * Config.SpawnH / 20));
            }

            BoundingBox = Polygon.rectangle(Config.ScreenX, Config.ScreenY, Config.ScreenW, Config.ScreenH, false);
            WorldBoundingBox = Polygon.rectangle(Config.ScreenX - 200, Config.ScreenY - 200, Config.ScreenW + 400,
                Config.ScreenH + 400, false);

            if (IsServer)
            {
                waveLibrary = new IWave[]
                {
                    new CrossOverWave(),
                    //new RespawnWave(),
                    new RoundOutWave(),
                    new HorizontalProtectionWave(),
                    //new BigMermaidWave()
                };
            }
        }

        public void ConfigChange()
        {
            Prime = new PrimeSearch((int) Config.BombThreshold + 1);
        }

        public void Recycle(int index, bool isSolo)
        {
            Id = Interlocked.Increment(ref idCount);
            needBot = Bots.BotConfig.BotActive;
            needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin1Ms, Bots.BotConfig.TimeToJoinMax1Ms);
#if SERVER
            if (IsServer)
            {
                BanCa.Redis.RedisManager.IncTableId();
            }
#endif
            startInit(index, isSolo);
            SoloMode = false;
            for (int i = 0, n = allObjects.Count; i < n; i++)
            {
                var o = allObjects[i];
                if (o != null)
                {
                    //o.Value = 0;
                    o.UpdateBound();
                }
            }
        }

        private void addConstrainFish(List<Config.FishType> constrain, Config.FishType type, int number)
        {
            for (int i = 0; i < number; i++)
            {
                constrain.Add(type);
            }
        }

        private void initFish()
        {
            if (IsServer && allObjects.Count == 0)
            {
                List<Config.FishType> common = new List<Config.FishType>() {Config.FishType.GoldFish};
                // new List<Config.FishType>() { Config.FishType.Cuttle, Config.FishType.FlyingFish, Config.FishType.GoldFish, Config.FishType.LightenFish,
                //Config.FishType.PufferFish, Config.FishType.SeaFish, Config.FishType.SeaTurtle, Config.FishType.Turtle, Config.FishType.Stringray };
                List<Config.FishType> constrain = new List<Config.FishType>();
                addConstrainFish(constrain, Config.FishType.GoldFish, 10);
                addConstrainFish(constrain, Config.FishType.LightenFish, 9); //27
                addConstrainFish(constrain, Config.FishType.Mermaid, 9);
                addConstrainFish(constrain, Config.FishType.Octopus, 9);
                addConstrainFish(constrain, Config.FishType.PufferFish, 6); // 18
                addConstrainFish(constrain, Config.FishType.SeaFish, 6);
                addConstrainFish(constrain, Config.FishType.Shark, 6);
                addConstrainFish(constrain, Config.FishType.Stringray, 5); // 15
                addConstrainFish(constrain, Config.FishType.Turtle, 5);
                addConstrainFish(constrain, Config.FishType.CaThanTai, 5);
                addConstrainFish(constrain, Config.FishType.FlyingFish, 4); // 12
                addConstrainFish(constrain, Config.FishType.SeaTurtle, 4);
                addConstrainFish(constrain, Config.FishType.MerMan, 4);
                addConstrainFish(constrain, Config.FishType.Phoenix, 3); // 9
                addConstrainFish(constrain, Config.FishType.MermaidBig, 3);
                addConstrainFish(constrain, Config.FishType.MermaidSmall, 3);
                addConstrainFish(constrain, Config.FishType.Fish19, 2); // 6
                addConstrainFish(constrain, Config.FishType.Fish20, 2);
                addConstrainFish(constrain, Config.FishType.Fish21, 2);
                addConstrainFish(constrain, Config.FishType.Fish22, 1); // 3
                addConstrainFish(constrain, Config.FishType.Fish23, 1);
                addConstrainFish(constrain, Config.FishType.Fish24, 1);
                //addConstrainFish(constrain, Config.FishType.Fish25, 1);

                for (int i = 0; i < Config.NUMBER_OF_OBJECTS; i++)
                {
                    var o = new BanCaObject(this);
                    o.ID = objIdCount++;
                    FishFactory.RandomFish(TableBlindIndex, o, this.Random, common, constrain);
                    o.isSpecial = false;
                    o.OriginalType = o.Type;
                    allObjects.Add(o);
                    objectMap.Add(o.ID, o);
                }

                // special fish
                {
                    var o = new BanCaObject(this);
                    o.ID = objIdCount++;
                    FishFactory.BuildFish(TableBlindIndex, Config.FishType.GoldenFrog, o, this.Random);
                    o.isSpecial = true;
                    o.OriginalType = o.Type;
                    o.GeneratePath(200);
                    allSpecialObjects.Add(o);
                    objectMap.Add(o.ID, o);
                    //var bomb = o = new BanCaObject(this);
                    //o.ID = objIdCount++;
                    //FishFactory.BuildFish(TableBlindIndex, Config.FishType.BombFish, o, this.Random);
                    //o.isSpecial = true;
                    //o.OriginalType = o.Type;
                    //allSpecialObjects.Add(o);
                    //objectMap.Add(o.ID, o);
                }

                // remove some
                for (int i = 0, n = allObjects.Count; i < n; i++)
                {
                    var o = allObjects[i];
                    if (o != null)
                    {
                        if (soloMode || Random.NextDouble() < 0.3 || bigFish.Contains(o.Type))
                        {
                            o.Health = -1;
                            o.Value = 0;
                            o.Remove();
                            scheduleToRevive(o);
                        }
                        else
                        {
                            o.GeneratePath(200);
                        }
                    }
                }
            }
        }

        private void OnBombUse(long userId, Config.BulletType type)
        {
            Player p = getPlayerByUserId(userId);
            if (p != null)
            {
                p.Item = Config.PowerUp.ClearStage;
                p.ItemDuration = Config.PowerUpDurationS;
                var itemProfit = KillAllFishInScreen(p, type);
                if (itemProfit > 0)
                {
                    var msg = new SimpleJSON.JSONObject();
                    msg["nickname"] = p.Nickname;
                    msg["value"] = itemProfit;
                    msg["blind"] = TableBlindIndex;
                    BanCaLib.PushAll("onBcJackpot", msg);
                    Redis.RedisManager.LogHomeMessages(p.Id, p.Nickname, itemProfit, Config.FishType.BombFish,
                        TableBlindIndex, type);
                    BanCa.Sql.SqlLogger.LogJackpotEvent(Id, TableBlindIndex, type, p.Id, p.Cash, itemProfit, ServerId,
                        p.Nickname);
                }
            }
        }

        private void EndGame()
        {
            //Logger.Info(string.Format("End game: {0} {1} {2} {3} {4}", getPlayer1Cash(), minBulletValue, SoloPlayerId1.BombCount, getPlayer2Cash(), SoloPlayerId2.BombCount));

            if (checkEndGame != null)
            {
                checkEndGame.Dispose();
                checkEndGame = null;
            }

            if (!soloMode || soloEnded) return;
            var winCash = (long) (Config.TableSoloCashIn[TableBlindIndex] * 2 * (1 - Config.SoloFee));
            var msg = new JSONObject();
            var p1Cash = getPlayer1CashGain();
            var p2Cash = getPlayer2CashGain();
            if (p1Cash == p2Cash && SoloPlayerId1.BombCount == SoloPlayerId2.BombCount) // draw
            {
                winCash = Config.TableSoloCashIn[TableBlindIndex];
                msg["winner"] = "";
                msg["winCash"] = winCash;
                var id1 = SoloPlayerId1.Id;
                var id2 = SoloPlayerId2.Id;
                var _id = Id;
                var _index = TableBlindIndex;

                TaskRunner.RunOnPool(async () =>
                {
                    // refund
                    var cash = await BanCa.Redis.RedisManager.IncEpicCash(id1, winCash, "banca",
                        "END_SOLO_BC_DRAW: " + _id + " " + p1Cash + " " + SoloPlayerId1.BombCount,
                        BanCa.Redis.TransType.BAN_CA);
                    if (cash > -1) MySqlProcess.Genneral.MySqlUser.SaveCashToDb(id1, cash);

                    cash = await BanCa.Redis.RedisManager.IncEpicCash(id2, winCash, "banca",
                        "END_SOLO_BC_DRAW: " + _id + " " + p2Cash + " " + SoloPlayerId2.BombCount,
                        BanCa.Redis.TransType.BAN_CA);
                    if (cash > -1) MySqlProcess.Genneral.MySqlUser.SaveCashToDb(id2, cash);

                    BanCa.Sql.SqlLogger.LogBcHistory(id1, winCash, _id, _index, 1);
                    BanCa.Sql.SqlLogger.LogBcHistory(id2, winCash, _id, _index, 1);
                });
            }
            else
            {
                var cashToJoin = Config.TableSoloCashIn[TableBlindIndex];
                var winner = p1Cash > p2Cash ? SoloPlayerId1 : SoloPlayerId2;
                msg["winner"] = winner.PlayerId;
                msg["winCash"] = winCash;
                var id1 = SoloPlayerId1.Id;
                var id2 = SoloPlayerId2.Id;
                var winId = winner.Id;
                var _id = Id;
                var _index = TableBlindIndex;

                TaskRunner.RunOnPool(async () =>
                {
                    var cash = await BanCa.Redis.RedisManager.IncEpicCash(winId, winCash, "banca",
                        "END_SOLO_BC: " + _id + " " + (winId == id1) + " " + p1Cash + " " + SoloPlayerId1.BombCount +
                        " " + p2Cash + " " + SoloPlayerId2.BombCount, BanCa.Redis.TransType.BAN_CA);
                    if (cash > -1) MySqlProcess.Genneral.MySqlUser.SaveCashToDb(winId, cash);

                    BanCa.Sql.SqlLogger.LogBcHistory(id1, winId == id1 ? winCash : -cashToJoin, _id, _index, 1);
                    BanCa.Sql.SqlLogger.LogBcHistory(id2, winId == id2 ? winCash : -cashToJoin, _id, _index, 1);
                });
            }

            OnEndSolo(msg);

            soloEnded = true;
        }

        private long getPlayer1CashGain()
        {
            if (players[0] != null) return players[0].CashGain;
            if (SoloPlayerId1 != null) return SoloPlayerId1.CashGain;
            return 0;
        }

        private long getPlayer2CashGain()
        {
            if (players[2] != null) return players[2].CashGain;
            if (SoloPlayerId2 != null) return SoloPlayerId2.CashGain;
            return 0;
        }

        private long getPlayer1Cash()
        {
            if (players[0] != null) return players[0].Cash;
            if (SoloPlayerId1 != null) return SoloPlayerId1.Cash;
            return 0;
        }

        private long getPlayer2Cash()
        {
            if (players[2] != null) return players[2].Cash;
            if (SoloPlayerId2 != null) return SoloPlayerId2.Cash;
            return 0;
        }

        public void Update(float delta)
        {
            try
            {
                var now = TimeUtil.TimeStamp;
                if (Tasks.Count > 0)
                {
                    for (int i = 0; i < Tasks.Count; i++)
                    {
                        if (Tasks[i].ExecuteAtTime <= now)
                        {
                            Tasks[i].Action();
                            Tasks[i] = Tasks[Tasks.Count - 1];
                            Tasks.RemoveAt(Tasks.Count - 1);
                            i--;
                        }
                    }
                }

                if (IsServer)
                {
                    for (int i = 0, n = players.Length; i < n; i++)
                    {
                        var p = players[i];
                        if (p != null)
                        {
                            p.PlayTimeS += delta;
                            p.IdleTimeS += delta;

                            if (p.TimeToPush > 0)
                            {
                                p.TimeToPush -= delta;
                                if (p.TimeToPush <= 0f)
                                {
                                    if (OnUpdateCash != null)
                                    {
                                        var msg = new JSONObject();
                                        msg["playerId"] = p.PlayerId;
                                        msg["cash"] = p.Cash;
                                        msg["value"] = p.PendingPushCash;
                                        msg["time"] = now;
                                        msg["scr"] = (int) CashSource.GoldenFrog;
                                        msg["cashGain"] = p.CashGain;
                                        msg["expGain"] = p.ExpGain;
                                        OnUpdateCash(msg);
                                    }

                                    p.PendingPushCash = 0;
                                }
                            }

#if SERVER
                            p.LogCashTimeS += delta;
                            if (p.LogCashTimeS > 30 && !p.IsBot)
                            {
                                p.LogCashTimeS = 0;
                                if (IsServer)
                                {
                                    BanCa.Sql.SqlLogger.LogBulletHitFish(Id, TableBlindIndex, p.Id, p.Cash, 0,
                                        ServerId);
                                    BanCa.Sql.SqlLogger.LogCashUpdate(Id, TableBlindIndex, p.Id, p.Cash, p.Profit,
                                        ServerId, p.Item, p.CashGain);
                                }
                            }
#endif

                            if (p.ItemDuration > 0)
                            {
                                p.ItemDuration -= delta;
                                if (p.ItemDuration <= 0)
                                {
                                    p.Item = Config.PowerUp.None;
                                }
                            }

                            if (!soloMode && p.IdleTimeS > Config.MaxIdleTimeS)
                            {
                                p.IdleTimeS = 0;
                                RemovePlayer(p.PlayerId, Config.QuitReason.TimeOut);
                            }
                        }
                    }

                    var oldState = WorldState;
                    if (WorldState == State.Waiting)
                    {
                        return;
                    }

                    if (WorldState == State.Playing)
                    {
                        waveLength -= delta;

                        if (soloMode)
                        {
                            if (soloEnded)
                                return;

                            //Logger.Info(string.Format("{0} {1} {2} {3} {4}", getPlayer1Cash(), minBulletValue, SoloPlayerId1.BombCount, getPlayer2Cash(), SoloPlayerId2.BombCount));
                            //// everyone out of cash
                            if (checkEndGame == null &&
                                (getPlayer1Cash() < minBulletValue && SoloPlayerId1.BombCount == 0) &&
                                (getPlayer2Cash() < minBulletValue && SoloPlayerId2.BombCount == 0))
                            {
                                //Logger.Info("Both out of money");
                                checkEndGame = TaskRun.QueueAction(5000, () =>
                                {
                                    checkEndGame = null;
                                    //Logger.Info("Rechecking...");
                                    if ((getPlayer1Cash() < minBulletValue && SoloPlayerId1.BombCount == 0) &&
                                        (getPlayer2Cash() < minBulletValue && SoloPlayerId2.BombCount == 0))
                                    {
                                        //Logger.Info("End game");
                                        EndGame();
                                    }
                                });
                            }

                            if (waveLength <= 0 || (now - soloStart) / 1000f >= Config.SOLO_DURATION) // game time out
                            {
                                EndGame();
                            }
                        }
                        else
                        {
                            if (waveLength <= 0)
                            {
                                waveLength = Config.WAITING_WAVE_DURATION;
                                WorldState = State.WaitingForNewWave;
                            }
                        }
                    }
                    else if (WorldState == State.WaitingForNewWave)
                    {
                        if (!Config.IsMaintain)
                        {
                            waveLength -= delta;
                            if (waveLength <= 0 || !hasFishInScreen())
                            {
                                waveLength = Config.NEW_WAVE_MAX_TIME;
                                WorldState = State.NewWave;
                                setupFishForNewWave();
                                WaveStart = now;
                            }
                        }
                    }
                    else if (WorldState == State.NewWave)
                    {
                        waveLength -= delta;
                        if (waveLength <= 0)
                        {
                            WorldState = State.Playing;
                            if (currentWave != null)
                            {
                                Logger.Info("Wave time out: " + currentWave.IsEnd() + " " + currentWave.IsEnding() +
                                            " " + currentWave.GetType().ToString() + " " + waveLength);
                                //try
                                //{
                                //    Logger.Info("Wave detail: " + currentWave.GetDetails());
                                //}
                                //catch (Exception ex)
                                //{
                                //    Logger.Info("Wave detail error: " + ex.ToString());
                                //}
                                currentWave.SetEnding();
                                currentWave = null;
                            }

                            waveLength = soloMode ? Config.SOLO_DURATION : Config.PLAYING_WAVE_DURATION;
                            deadFish.Clear();
                            for (int i = 0; i < allSpecialObjects.Count; i++) // re add bomb fish
                            {
                                if (allSpecialObjects[i].Health <= 0)
                                {
                                    deadFish.AddFirst(allSpecialObjects[i]);
                                }
                            }

                            for (int i = 0, n = allObjects.Count; i < n; i++)
                            {
                                var o = allObjects[i];
                                if (o != null && o.Health <= 0)
                                {
                                    deadFish.AddLast(o);
                                }
                            }
                        }
                    }

                    if (currentWave != null)
                    {
                        currentWave.Update(delta);
                        if (currentWave.IsEnd())
                        {
                            currentWave = null;

                            waveLength = soloMode ? Config.SOLO_DURATION : Config.PLAYING_WAVE_DURATION;
                            WorldState = State.Playing;

                            deadFish.Clear();
                            for (int i = 0; i < allSpecialObjects.Count; i++) // re add bomb fish
                            {
                                if (allSpecialObjects[i].Health <= 0)
                                {
                                    deadFish.AddFirst(allSpecialObjects[i]);
                                }
                            }

                            for (int i = 0, n = allObjects.Count; i < n; i++)
                            {
                                var o = allObjects[i];
                                if (o != null && o.Health <= 0)
                                {
                                    deadFish.AddLast(o);
                                }
                            }
                        }
                    }

                    if (needBot)
                    {
                        needBotCountDown -= (delta * 1000);
                        if (needBotCountDown <= 0)
                        {
                            needBot = false;
                            if (OnBotRequest != null) TaskRun.QueueAction(OnBotRequest);
                        }
                    }

                    for (int i = 0, n = allObjects.Count; i < n; i++)
                    {
                        var o = allObjects[i];
                        if (o.Health > 0)
                        {
                            o.Update(delta);

                            if (WorldState != State.WaitingForNewWave) // allow fish to move off screen
                                o.CheckBound(Config.WorldX, Config.WorldY, Config.WorldW, Config.WorldH);
                        }
                    }

                    if (!soloMode)
                    {
                        for (int i = 0, n = allSpecialObjects.Count; i < n; i++)
                        {
                            var o = allSpecialObjects[i];
                            if (o.Health > 0)
                            {
                                o.Update(delta);

                                // unlike normal fish, always in bound
                                o.CheckBound(Config.OutWorldX, Config.OutWorldY, Config.OutWorldW, Config.OutWorldH);
                            }
                        }
                    }

                    for (int i = 0, n = activeBullets.Count; i < n; i++)
                    {
                        var b = activeBullets[i];
                        b.Update(delta);
                        b.CheckBound(Config.ScreenX, Config.ScreenY, Config.ScreenW, Config.ScreenH);
                        if (b.TargetId != -1)
                        {
                            var obj = getObj(b.TargetId);
                            if (obj != null && obj.Health <= 0)
                            {
                                b.TargetId = -1;
                            }
                        }

                        if (b.BoundTime <= 0)
                        {
                            freeBullet(b, Config.FishType.Basic, i);
                            n--; // as list reduce 1 element
                            i--;
                            continue;
                        }

                        // check killing normal fish
                        for (int j = 0, m = allObjects.Count; j < m; j++)
                        {
                            var o = allObjects[j];
                            if (o.Health > 0 && b.TestHit(o))
                            {
                                var p = getPlayer(b.PlayerId);
                                var v = (long) (o.MaxHealth * b.Value / b.Power); // value if die
                                var bankRate = p != null ? Config.GetMinimumBankRate(p.Cash, p.CardIn) : 1f;
                                b.Hit.Set(b.Pos);
                                o.OnHit(b);
                                if (!soloMode)
                                {
                                    o.Health = o.MaxHealth; // fish dead now decide by bank
                                    if (FundManager.GetFund(TableBlindIndex, b.Type, o.Type) >= v * bankRate)
                                    {
                                        o.Health = -1;
                                    }
                                }

                                if (!b.IsBot)
                                    BanCa.Sql.SqlLogger.LogHitFish(b.Type, b.EpicId, b.CashAtShoot, b.CashChangeAtShoot,
                                        b.TargetId, b.RapidFire, b.IsAuto, o.Type, b.Start, DateTime.UtcNow, o.ID);
                                if (o.Health <= 0)
                                {
                                    //Logger.Info("Value: " + v + " " + o.MaxHealth + " " + b.Value + " " + b.Power);
#if NetCore
                                    var res = soloMode || b.IsBot
                                        ? 1
                                        : FundManager.IncFund(TableBlindIndex, b.Type, o.Type,
                                            -v); // some how bank is not enough
                                    if (res < 0) // not enough fund
                                    {
                                        o.Health = 1; // prevent kill
                                    }
                                    else
#endif
                                    {
                                        if (p != null)
                                        {
                                            var exp = (long) o.MaxHealth + (long) Math.Sqrt(v * 100);
                                            p.Cash += v;
                                            p.Profit += v;
                                            p.CashGain += v;
                                            p.Exp += exp;
                                            p.ExpGain += exp;
                                            if (!soloMode && p.DoLevelUpIfApplicable())
                                            {
                                                if (OnLevelUp != null)
                                                {
                                                    OnLevelUp(p.GetLvJson());
                                                }
                                            }

#if SERVER
                                            if (!p.IsBot)
                                            {
                                                BanCa.Sql.SqlLogger.LogBulletHitFish(Id, TableBlindIndex, p.Id, p.Cash,
                                                    0, ServerId);
                                                BanCa.Sql.SqlLogger.LogKillFish(Id, TableBlindIndex, b.Type, p.Id,
                                                    p.Cash, v, ServerId, p.Item, o.Type, p.CashGain);
                                            }

                                            if (annouceFish.Contains(o.Type))
                                            {
                                                Redis.RedisManager.LogHomeMessages(p.Id, p.Nickname, v, o.Type,
                                                    TableBlindIndex, b.Type);
                                            }
#endif

                                            if (OnUpdateCash != null)
                                            {
                                                var msg = new JSONObject();
                                                msg["playerId"] = b.PlayerId;
                                                msg["cash"] = p.Cash;
                                                msg["value"] = v;
                                                msg["time"] = now;
                                                msg["scr"] = (int) CashSource.KillFish;
                                                msg["cashGain"] = p.CashGain;
                                                msg["expGain"] = p.ExpGain;
                                                OnUpdateCash(msg);
                                            }
                                        }

                                        if (OnObjectDie != null)
                                        {
                                            var msg = new JSONObject();
                                            msg["playerId"] = b.PlayerId;
                                            msg["id"] = o.ID;
                                            msg["value"] = v;
                                            msg["time"] = now;
                                            OnObjectDie(msg);
                                            //Logger.Info("Obj die 1: " + o.ID + " by " + b.PlayerId + " v " + v + " bid " + b.ID + " " + b.Pos.ToString() + " " + o.Pos.ToString());
                                        }

                                        o.Remove();
                                        o.SetRefundOnRevive();

                                        if (WorldState != State.WaitingForNewWave && WorldState != State.NewWave)
                                        {
                                            if (bigFish.Contains(o.Type))
                                            {
                                                bigFishCount--;
                                                if (bigFishCount < 0)
                                                    bigFishCount = 0;
                                                //Logger.Info("Dead fish 1 " + o.Type);
                                            }

                                            deadFish.AddLast(o);
                                        }
                                    }
                                }

                                freeBullet(b, o.Type, i);
                                n--; // as list reduce 1 element
                                i--;
                                break;
                            }
                        }
                    }

                    if (!soloMode)
                    {
                        for (int i = 0, n = activeBullets.Count; i < n; i++)
                        {
                            var b = activeBullets[i];
                            for (int j = 0, m = allSpecialObjects.Count; j < m; j++)
                            {
                                var o = allSpecialObjects[j];
                                if (b.TestHit(o))
                                {
                                    b.Hit.Set(b.Pos);
                                    o.OnHit(b);
                                    o.Health = o.MaxHealth; // special fish is immortal
                                    BanCa.Sql.SqlLogger.LogHitFish(b.Type, b.EpicId, b.CashAtShoot, b.CashChangeAtShoot,
                                        b.TargetId, b.RapidFire, b.IsAuto, o.Type, b.Start, DateTime.UtcNow, o.ID);
                                    //if (o.Type == Config.FishType.GoldenFrog) // handle GoldenFrog
                                    {
                                        var p = getPlayer(b.PlayerId);
                                        if (p != null)
                                        {
                                            // find which rate to apply
                                            var check = 100f * (float) Random.NextDouble();
                                            var sum = 0f;
                                            var index = -1;
                                            var rate = Config.GOLDEN_FROG_WIN_RATE;
                                            var mul = Config.GOLDEN_FROG_WIN_MULTIPLE;
                                            for (int k = 0; k < rate.Count; k++)
                                            {
                                                sum += rate[k];
                                                if (check <= sum)
                                                {
                                                    index = k;
                                                    break;
                                                }
                                            }

                                            if (index != -1 && index < mul.Count)
                                            {
                                                var winRate = mul[index];
                                                var winValue = (long) (b.Value * winRate);
#if NetCore
                                                if (b.IsBot || FundManager.IncFund(TableBlindIndex, b.Type, o.Type,
                                                        -winValue) > 0) // enough fund to pay
#endif
                                                {
                                                    var v = winValue;
                                                    var exp = (long) Math.Sqrt(v * 100);
                                                    p.Cash += v;
                                                    p.Profit += v;
                                                    p.PendingPushCash += v;
                                                    p.CashGain += v;
                                                    p.Exp += exp;
                                                    p.ExpGain += exp;
                                                    o.Value -= v;
                                                }

                                                if (p.TimeToPush <= 0f)
                                                {
                                                    p.TimeToPush = 3f;
                                                }
                                            }
                                        }
                                    }
                                    //else if (o.Type == Config.FishType.BombFish) // also handle BombFish
                                    {
                                        var p = getPlayer(b.PlayerId);
                                        if (p != null)
                                        {
#if SERVER
                                            bool jpUser =
                                                Redis.RedisManager.IsJpUser(p.Id); // admin user can force do jackpot
                                            var jpCheck =
                                                b.IsBot || FundManager.GetJackpot(this.TableBlindIndex, b.Type) <
                                                FundManager.GetBombFund(this.TableBlindIndex,
                                                    b.Type); // enough fund to pay
                                            // Random.NextDouble() * Config.BombThreshold <= 1
                                            if (jpCheck) // enough fund
                                            {
                                                if (!jpUser)
                                                {
                                                    int checkCount = Config.TypeToJpCheckCount[b.Type];
                                                    bool ok = false;
                                                    for (int u = 0; u < checkCount; u++)
                                                    {
                                                        if (Prime.GetNext() == 1) // 1 is lucky number
                                                        {
                                                            ok = true;
                                                        }
                                                    }

                                                    jpCheck = ok;
                                                }
                                            }
#else
                                    var jpCheck = false;
#endif
                                            if (jpCheck) // do jackpot
                                            {
                                                var v = 0L;
                                                var exp = (long) o.MaxHealth + (long) Math.Sqrt(v * 100);
                                                p.Exp += exp;
                                                p.ExpGain += exp;
                                                if (!soloMode && p.DoLevelUpIfApplicable())
                                                {
                                                    if (OnLevelUp != null)
                                                    {
                                                        OnLevelUp(p.GetLvJson());
                                                    }
                                                }

#if SERVER
                                                if (IsServer && !p.IsBot)
                                                {
                                                    BanCa.Sql.SqlLogger.LogBulletHitFish(Id, TableBlindIndex, p.Id,
                                                        p.Cash, 0, ServerId);
                                                    BanCa.Sql.SqlLogger.LogKillFish(Id, TableBlindIndex, b.Type, p.Id,
                                                        p.Cash, v, ServerId, p.Item, o.Type, p.CashGain);
                                                    BanCa.Sql.SqlLogger.LogItemActive(Id, TableBlindIndex, p.Id, p.Cash,
                                                        v, ServerId, Config.PowerUp.ClearStage);
                                                }
#endif
                                                if (OnUpdateCash != null)
                                                {
                                                    var msg = new JSONObject();
                                                    msg["playerId"] = b.PlayerId;
                                                    msg["cash"] = p.Cash;
                                                    msg["value"] = v;
                                                    msg["time"] = now;
                                                    msg["scr"] = (int) CashSource.KillFish;
                                                    msg["cashGain"] = p.CashGain;
                                                    msg["expGain"] = p.ExpGain;
                                                    OnUpdateCash(msg);
                                                }


                                                if (OnObjectDie != null)
                                                {
                                                    var msg = new JSONObject();
                                                    msg["playerId"] = b.PlayerId;
                                                    msg["id"] = o.ID;
                                                    msg["value"] = v;
                                                    msg["time"] = now;
                                                    OnObjectDie(msg);
                                                    //Logger.Info("Obj die 2: " + o.ID + " by " + b.PlayerId);
                                                }

                                                var type = b.Type;
                                                Tasks.Add(new PendingTask()
                                                {
                                                    Action = () => { OnBombUse(p.Id, type); },
                                                    ExecuteAtTime = TimeUtil.TimeStamp + 1000
                                                });

                                                o.Health = -1;
                                                o.Remove();
                                                //o.SetRefundOnRevive();
                                                deadFish.AddLast(o);
                                            }
                                        }
                                    }

                                    freeBullet(b, o.Type, i);
                                    n--; // as list reduce 1 element
                                    i--;
                                    break;
                                }
                            }
                        }
                    }

                    if (oldState != WorldState)
                    {
                        //Logger.Info("New state: " + WorldState);
                        if (OnNewState != null)
                        {
                            var msg = new JSONObject();
                            msg["state"] = (int) WorldState;
                            //getWorldStateOnChange(msg);
                            OnNewState(msg);
                        }
                    }

                    // #revive
                    if (WorldState != State.WaitingForNewWave && WorldState != State.NewWave && deadFish.Count > 0)
                    {
                        if (deadFish.Count > 0)
                        {
                            ReviveRateS -= delta;
                            if (ReviveRateS < 0)
                            {
                                ReviveRateS = (float) Random.NextDouble() * 0.5f + 0.5f;

                                BanCaObject o = null;
                                int tryCount = 0;
                                while (deadFish.Count > 0)
                                {
                                    var first = deadFish.First.Value;
                                    deadFish.RemoveFirst();

                                    if ((bigFishCount >= 3 && bigFish.Contains(first.Type)) ||
                                        (SoloMode && first.isSpecial)) // too many big fish
                                    {
                                        //Logger.Info("Too many big fish " + bigFishCount);
                                        deadFish.AddLast(first);
                                        tryCount++;
                                        if (tryCount > 5) // prevent infinity loop
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    if (first.Health <= 0f)
                                    {
                                        o = first;
                                        if (o.Type != o.OriginalType)
                                        {
                                            FishFactory.BuildFish(TableBlindIndex, o.OriginalType, o,
                                                Random); // restore type
                                            o.Health = -1;
                                            o.onHitBound = null;
                                            o.onDie = null;
                                        }

                                        break;
                                    }
                                }

                                if (o != null) // only revive if die
                                {
                                    o.Revive(false);
                                    o.GeneratePath(200);
                                    o.pushUpdate();
                                    //Logger.Info("Revive " + o.ID + " at " + o.Pos.ToString());
                                    bool isBig = bigFish.Contains(o.Type);
                                    if (isBig)
                                    {
                                        //Logger.Info("Revive fish " + o.Type);
                                        bigFishCount++;
                                    }

                                    if (!isBig)
                                    {
                                        BanCaObject o1 = null, o2 = null, o3 = null, o4 = null, o5 = null;
                                        int herdSize = Random.Next(2, 5);
                                        int size = 0;
                                        var first = deadFish.First;
                                        herdFish.Clear();
                                        while (first != null) // try revive its siblings
                                        {
                                            var df = first.Value;
                                            if (df.Health > 0)
                                            {
                                                var next = first.Next;
                                                deadFish.Remove(first);
                                                first = next;
                                                continue;
                                            }
                                            else
                                            {
                                                if (df.Type != df.OriginalType)
                                                {
                                                    FishFactory.BuildFish(TableBlindIndex, df.OriginalType, df,
                                                        Random); // restore type
                                                    df.Health = -1;
                                                    df.onHitBound = null;
                                                    df.onDie = null;
                                                }

                                                if (df.Type == o.Type)
                                                {
                                                    size++;
                                                    if (!herdFish.Contains(df.ID)) // make sure fish is unique
                                                    {
                                                        herdFish.Add(df.ID);
                                                        if (o1 == null) o1 = df;
                                                        else if (o2 == null) o2 = df;
                                                        else if (o3 == null) o3 = df;
                                                        else if (o4 == null) o4 = df;
                                                        else if (o5 == null) o5 = df;
                                                    }

                                                    if (size >= herdSize)
                                                    {
                                                        deadFish.Remove(first);
                                                        break;
                                                    }

                                                    var next = first.Next;
                                                    deadFish.Remove(first);
                                                    first = next;
                                                }
                                                else
                                                {
                                                    first = first.Next;
                                                }
                                            }
                                        }

                                        {
                                            if (o1 != null)
                                            {
                                                o1.Revive(false);
                                                o1.SetMimicTarget(o);
                                                o1.PlaceBehind(o);
                                                o1.SetMimicTarget(null);
                                                o1.GeneratePath(200);
                                                o1.pushUpdate();
                                                //Logger.Info("Revive herd of " + o.ID + " id " + o1.ID + " at " + o1.Pos.ToString());
                                            }

                                            if (o2 != null)
                                            {
                                                o2.Revive(false);
                                                o2.SetMimicTarget(o);
                                                o2.PlaceBehind(o1);
                                                o2.SetMimicTarget(null);
                                                o2.GeneratePath(200);
                                                o2.pushUpdate();
                                                //Logger.Info("Revive herd of " + o.ID + " id " + o2.ID + " at " + o2.Pos.ToString());
                                            }

                                            if (o3 != null)
                                            {
                                                o3.Revive(false);
                                                o3.SetMimicTarget(o);
                                                o3.PlaceBehind(o2);
                                                o3.SetMimicTarget(null);
                                                o3.GeneratePath(200);
                                                o3.pushUpdate();
                                                //Logger.Info("Revive herd of " + o.ID + " id " + o3.ID + " at " + o3.Pos.ToString());
                                            }

                                            if (o4 != null)
                                            {
                                                o4.Revive(false);
                                                o4.SetMimicTarget(o);
                                                o4.PlaceBehind(o3);
                                                o4.SetMimicTarget(null);
                                                o4.GeneratePath(200);
                                                o4.pushUpdate();
                                                //Logger.Info("Revive herd of " + o.ID + " id " + o4.ID + " at " + o4.Pos.ToString());
                                            }

                                            if (o5 != null)
                                            {
                                                o5.Revive(false);
                                                o5.SetMimicTarget(o);
                                                o5.PlaceBehind(o4);
                                                o5.SetMimicTarget(null);
                                                o5.GeneratePath(200);
                                                o5.pushUpdate();
                                                //Logger.Info("Revive herd of " + o.ID + " id " + o5.ID + " at " + o5.Pos.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0, n = allObjects.Count; i < n; i++)
                    {
                        var o = allObjects[i];
                        if (o.Health > 0)
                        {
                            o.Update(delta);

                            if (WorldState != State.WaitingForNewWave) // allow fish to move off screen
                                o.CheckBound(Config.WorldX, Config.WorldY, Config.WorldW, Config.WorldH);
                        }
                    }

                    for (int i = 0, n = allSpecialObjects.Count; i < n; i++)
                    {
                        var o = allSpecialObjects[i];
                        if (o.Health > 0)
                        {
                            o.Update(delta);

                            // unlike normal fish, always in bound
                            o.CheckBound(Config.OutWorldX, Config.OutWorldY, Config.OutWorldW, Config.OutWorldH);
                        }
                    }

                    for (int i = 0, n = activeBullets.Count; i < n; i++)
                    {
                        var b = activeBullets[i];
                        b.Update(delta);
                        b.CheckBound(Config.ScreenX, Config.ScreenY, Config.ScreenW, Config.ScreenH);
                        if (b.TargetId != -1)
                        {
                            var obj = getObj(b.TargetId);
                            if (obj != null && obj.Health <= 0)
                            {
                                b.TargetId = -1;
                            }
                        }

                        if (b.BoundTime <= 0)
                        {
                            freeBullet(b, Config.FishType.Basic, i);
                            n--; // as list reduce 1 element
                            i--;
                            continue;
                        }

                        for (int j = 0, m = allObjects.Count; j < m; j++)
                        {
                            var o = allObjects[j];
                            if (o.Health > 0)
                            {
                                if (b.TestHit(o))
                                {
                                    b.Hit.Set(b.Pos);
                                    o.OnHit(b);
                                    if (OnBulletHitClient != null)
                                    {
                                        OnBulletHitClient(b.ID, o.ID);
                                    }

                                    if (o.Health <= 0) // fish fate decide by server
                                    {
                                        o.Health = 1; // so we keep the fish alive
                                    }

                                    freeBullet(b, o.Type, i);
                                    n--; // as list reduce 1 element
                                    i--;
                                    break;
                                }
                            }
                        }
                    }

                    for (int i = 0, n = activeBullets.Count; i < n; i++)
                    {
                        var b = activeBullets[i];
                        for (int j = 0, m = allSpecialObjects.Count; j < m; j++)
                        {
                            var o = allSpecialObjects[j];
                            if (o.Health > 0)
                            {
                                if (b.TestHit(o))
                                {
                                    b.Hit.Set(b.Pos);
                                    o.OnHit(b);
                                    if (OnBulletHitClient != null)
                                    {
                                        OnBulletHitClient(b.ID, o.ID);
                                    }
#if SERVER
                                    FishFactory.RandomHealth(o, Random);
#else
                                o.Health = o.MaxHealth;
#endif
                                    freeBullet(b, o.Type, i);
                                    n--; // as list reduce 1 element
                                    i--;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal void startInit(int index, bool isSolo)
        {
            TableBlindIndex = index;
            TableBlind = Config.GetTableBlind(index);
            minBulletValue = Config.GetBulletValue(TableBlindIndex, Config.BulletType.Bullet1);
#if SERVER
            if (IsServer)
            {
                BanCa.Sql.SqlLogger.LogTableStart(Id, index, ServerId, isSolo);
            }
#endif
        }

        public Player RegisterPlayer(string playerId, long epicId, long cash, bool isBot = false)
        {
            var p = getPlayer(playerId);
            if (p != null)
                return p;

            var cashGain = 0L;
            if (soloMode)
            {
                cash = Config.TableSoloVirtualCash[TableBlindIndex];
                var cashToJoin = Config.TableSoloCashIn[TableBlindIndex];
                if (players[0] == null)
                {
                    if (SoloPlayerId1 != null)
                    {
                        if (SoloPlayerId1.Id != epicId) // new player?
                        {
                            return null;
                        }

                        cash = SoloPlayerId1.Cash;
                        cashGain = SoloPlayerId1.CashGain;
                    }
                    else
                    {
                        SoloPlayerId1 = new SoloPlayer()
                        {
                            Id = epicId,
                            PlayerId = playerId,
                            Cash = cash,
                            SnipeCount = Config.SoloSnipe,
                            FastFireCount = Config.SoloFastFire,
                            BombCount = Config.SoloBomb
                        };
                    }

                    var p2 = new Player();
                    p2.PlayerId = playerId;
                    p2.PosIndex = 0;
                    p2.Cash = cash;
                    p2.CashGain = cashGain;
                    players[0] = p2;

                    if (WorldState == State.Waiting)
                    {
                        initFish();
                        var soloRes = Redis.RedisManager.IncEpicCashCache(epicId, -cashToJoin, "server",
                            "Join solo " + TableBlindIndex + " " + Id, Redis.TransType.SOLO_FEE);
                        if (soloRes < 0)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                Logger.Info("Cannot pay solo fee for player " + epicId);
                                var msg = new SimpleJSON.JSONObject();
                                msg["change"] = -cashToJoin;
                                msg["current"] = await BanCa.Redis.RedisManager.GetUserCash(epicId);
                                msg["tableId"] = Id;
                                msg["code"] = cash;
                                BanCa.Redis.RedisManager.AddHacker(epicId,
                                    BanCa.Redis.RedisManager.HackType.CannotIncCash, msg.ToString());
                            });
                            players[0] = null;
                            return null;
                        }

                        if (players[0] != null && players[2] != null)
                        {
                            TaskRun.QueueAction(10000, () =>
                            {
                                //Logger.Info("Check start 1");
                                if (WorldState == State.Waiting && players[0] != null && players[2] != null)
                                {
                                    WorldState = State.WaitingForNewWave;
                                    waveLength = 1;
                                    //WorldState = StartState;
                                    soloStart = TimeUtil.TimeStamp;
                                    //Logger.Info("Old state 1 " + WorldState);
                                    //if (OnNewState != null)
                                    //{
                                    //    var msg = new JSONClass();
                                    //    msg["state"] = (int)WorldState;
                                    //    //getWorldStateOnChange(msg);
                                    //    OnNewState(msg);
                                    //}
                                }
                            });
                        }
                    }

                    NumberOfPlayer++;
                    p2.Id = epicId;
                    return p2;
                }
                else if (players[2] == null)
                {
                    if (SoloPlayerId2 != null)
                    {
                        if (SoloPlayerId2.Id != epicId) // new player?
                        {
                            return null;
                        }

                        cash = SoloPlayerId2.Cash;
                        cashGain = SoloPlayerId2.CashGain;
                    }
                    else
                    {
                        SoloPlayerId2 = new SoloPlayer()
                        {
                            Id = epicId,
                            PlayerId = playerId,
                            Cash = cash,
                            SnipeCount = Config.SoloSnipe,
                            FastFireCount = Config.SoloFastFire,
                            BombCount = Config.SoloBomb
                        };
                    }

                    var p2 = new Player();
                    p2.PlayerId = playerId;
                    p2.PosIndex = 2;
                    p2.Cash = cash;
                    p2.CashGain = cashGain;
                    players[2] = p2;

                    if (WorldState == State.Waiting)
                    {
                        initFish();
                        var soloRes = Redis.RedisManager.IncEpicCashCache(epicId, -cashToJoin, "server",
                            "Join solo " + TableBlindIndex + " " + Id, Redis.TransType.SOLO_FEE);
                        if (soloRes < 0)
                        {
                            TaskRunner.RunOnPool(async () =>
                            {
                                Logger.Info("Cannot pay solo fee for player " + epicId);
                                var msg = new SimpleJSON.JSONObject();
                                msg["change"] = -cashToJoin;
                                msg["current"] = await BanCa.Redis.RedisManager.GetUserCash(epicId);
                                msg["tableId"] = Id;
                                msg["code"] = cash;
                                BanCa.Redis.RedisManager.AddHacker(epicId,
                                    BanCa.Redis.RedisManager.HackType.CannotIncCash, msg.ToString());
                            });
                            players[2] = null;
                            return null;
                        }

                        if (players[0] != null && players[2] != null)
                        {
                            TaskRun.QueueAction(10000, () =>
                            {
                                //Logger.Info("Check start 2");
                                if (WorldState == State.Waiting && players[0] != null && players[2] != null)
                                {
                                    WorldState = State.WaitingForNewWave;
                                    waveLength = 1;
                                    //WorldState = StartState;
                                    soloStart = TimeUtil.TimeStamp;
                                    //Logger.Info("Old state 2 " + WorldState);
                                    //if (OnNewState != null)
                                    //{
                                    //    var msg = new JSONClass();
                                    //    msg["state"] = (int)WorldState;
                                    //    //getWorldStateOnChange(msg);
                                    //    OnNewState(msg);
                                    //}
                                }
                            });
                        }
                    }

                    NumberOfPlayer++;
                    p2.Id = epicId;
                    return p2;
                }

                return null; // full
            }
            else
            {
                for (int i = 0, n = players.Length; i < n; i++)
                {
                    if (players[i] == null)
                    {
                        var p2 = new Player();
                        p2.PlayerId = playerId;
                        p2.PosIndex = i;
                        p2.Cash = cash;
                        p2.IsBot = isBot;
                        players[i] = p2;

                        if (WorldState == State.Waiting)
                        {
                            initFish();
                            WorldState = StartState;
                            //Logger.Info("Set state to " + WorldState);
                            //WorldState = State.WaitingForNewWave;
                        }

                        NumberOfPlayer++;
                        p2.Id = epicId;

                        int numberOfHuman = 0;
                        for (int j = 0; j < n; j++)
                        {
                            var _p = players[j];
                            if (_p != null)
                            {
                                if (!_p.IsBot)
                                {
                                    numberOfHuman++;
                                }
                            }
                        }

                        if (numberOfHuman == 1 || numberOfHuman == 2)
                        {
                            var numberOfBot = NumberOfPlayer - numberOfHuman;
                            if (numberOfBot < Bots.BotConfig.MaxBotInRoom)
                            {
                                needBot = Bots.BotConfig.BotActive;
                                if (NumberOfPlayer == 1)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin1Ms,
                                        Bots.BotConfig.TimeToJoinMax1Ms);
                                else if (NumberOfPlayer == 2)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin2Ms,
                                        Bots.BotConfig.TimeToJoinMax2Ms);
                                else if (NumberOfPlayer == 3)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin3Ms,
                                        Bots.BotConfig.TimeToJoinMax3Ms);
                                else
                                    needBot = false;
                            }
                            else
                            {
                                needBot = false;
                            }

                            //Logger.Info("Register, number of human " + numberOfHuman + " table " + Id + " need bot " + needBot + " count down " + needBotCountDown);
                        }
                        else
                        {
                            //Logger.Info("Register, number of human > 1 " + numberOfHuman + " table " + Id + " need bot " + needBot + " count down " + needBotCountDown);
                            needBot = false;
                        }

                        return p2;
                    }
                }
            }

            NumberOfPlayer = 4;
            // this room is full
            return null;
        }

        // use in client
        public Player RegisterPlayer(string playerId, int posIndex, JSONNode data)
        {
            var p = getPlayer(playerId);
            if (p != null)
                return p;

            if (players[posIndex] == null)
            {
                var p2 = new Player();
                p2.ParseJson(data);
                p2.PlayerId = playerId;
                p2.PosIndex = posIndex;

                players[posIndex] = p2;

                if (WorldState == State.Waiting)
                {
                    initFish();
                    WorldState = StartState;
                }

                //if (OnEnterPlayer != null)
                //{
                //    var msg = new JSONClass();
                //    msg["playerId"] = playerId;
                //    msg["posIndex"] = posIndex;
                //    msg["cash"] = cash;
                //    OnEnterPlayer(msg);
                //}
                NumberOfPlayer++;
                return p2;
            }

            NumberOfPlayer = 4;
            // this room is full
            return null;
        }

        // use in client
        public Player RegisterPlayer(Player p)
        {
            if (players[p.PosIndex] == null)
            {
                var p2 = p;
                players[p.PosIndex] = p2;

                if (WorldState == State.Waiting)
                {
                    initFish();
                    WorldState = StartState;
                }

                //if (OnEnterPlayer != null)
                //{
                //    var msg = new JSONClass();
                //    msg["playerId"] = p.PlayerId;
                //    msg["posIndex"] = p.PosIndex;
                //    msg["cash"] = p.Cash;
                //    OnEnterPlayer(msg);
                //}
                NumberOfPlayer++;
                return p2;
            }
            else if (players[p.PosIndex].PlayerId == p.PlayerId) // over register, replace
            {
                var p2 = p;
                players[p.PosIndex] = p2;

                if (WorldState == State.Waiting)
                {
                    initFish();
                    WorldState = StartState;
                }

                return p2;
            }

            NumberOfPlayer = 4;
            // this room is full
            return null;
        }

        public Player UseItem(JSONNode data)
        {
            string playerId = data["playerId"];
            var p = getPlayer(playerId);
            if (p != null)
            {
                p.ParseItemJson(data);
            }

            return p;
        }

        public Player LevelUp(JSONNode data)
        {
            string playerId = data["playerId"];
            var p = getPlayer(playerId);
            if (p != null)
            {
                p.ParseLvJson(data);
            }

            return p;
        }

        public Player RemovePlayerByPeerId(string peerId, Config.QuitReason reason = Config.QuitReason.Normal)
        {
            var p = getPlayerByPeerId(peerId);
            if (p != null)
                return RemovePlayer(p.PlayerId, reason);
            return null;
        }

        public Player RemovePlayer(string playerId, Config.QuitReason reason = Config.QuitReason.Normal)
        {
            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] != null && players[i].PlayerId == playerId)
                {
                    if (OnLeavePlayer != null)
                    {
                        var msg = new JSONObject();
                        msg["playerId"] = playerId;
                        msg["reason"] = (int) reason;
                        OnLeavePlayer(players[i].PeerId, msg);

                        /// bot
                        int numberOfHuman = 0;
                        for (int j = 0; j < n; j++)
                        {
                            var _p = players[j];
                            if (_p != null)
                            {
                                if (!_p.IsBot && _p.PlayerId != playerId)
                                {
                                    numberOfHuman++;
                                }
                            }
                        }

                        OnLeavePlayer(players[i].PeerId, msg);

                        if (numberOfHuman == 0)
                        {
                            needBot = false;
                            //Logger.Info("Leave, number of human = 0 " + numberOfHuman + " table " + Id + " need bot " + needBot + " count down " + needBotCountDown);
                            //TaskRun.QueueAction(() =>
                            //{
                            //    for (int j = 0; j < n; j++)
                            //    {
                            //        var _p = players[j];
                            //        if (_p != null && _p.IsBot)
                            //        {
                            //            RemovePlayer(_p.PlayerId, Config.QuitReason.Kick);
                            //        }
                            //    }
                            //});
                        }
                        else if (numberOfHuman == 1 || numberOfHuman == 2)
                        {
                            var numberOfBot = NumberOfPlayer - numberOfHuman - 1;
                            if (numberOfBot < Bots.BotConfig.MaxBotInRoom)
                            {
                                needBot = Bots.BotConfig.BotActive;
                                if (NumberOfPlayer == 2)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin1Ms,
                                        Bots.BotConfig.TimeToJoinMax1Ms);
                                else if (NumberOfPlayer == 3)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin2Ms,
                                        Bots.BotConfig.TimeToJoinMax2Ms);
                                else if (NumberOfPlayer == 4)
                                    needBotCountDown = Random.Next(Bots.BotConfig.TimeToJoinMin3Ms,
                                        Bots.BotConfig.TimeToJoinMax3Ms);
                                else
                                    needBot = false;
                            }
                            else
                            {
                                needBot = false;
                            }

                            //Logger.Info("Leave, number of human " + numberOfHuman + " table " + Id + " need bot " + needBot + " count down " + needBotCountDown);
                        }
                        else
                        {
                            //Logger.Info("Leave, number of human > 1 " + numberOfHuman + " table " + Id + " need bot " + needBot + " count down " + needBotCountDown);
                            needBot = false;
                        }
                    }

                    var p = players[i];
                    players[i] = null;

                    NumberOfPlayer--;
                    if (IsEmpty())
                    {
                        if (soloMode && (WorldState == State.Playing || WorldState == State.NewWave))
                        {
                            if (SoloPlayerId1 != null && p.PlayerId == SoloPlayerId1.PlayerId)
                            {
                                SoloPlayerId1.Cash = p.Cash;
                                SoloPlayerId1.CashGain = p.CashGain;
                            }
                            else if (SoloPlayerId2 != null && p.PlayerId == SoloPlayerId2.PlayerId)
                            {
                                SoloPlayerId2.Cash = p.Cash;
                                SoloPlayerId2.CashGain = p.CashGain;
                            }

                            EndGame();
                        }

                        //Logger.Info("Set back state to " + WorldState);
                        WorldState = State.Waiting;
                        NumberOfPlayer = 0;
                    }

#if SERVER
                    if (IsServer && p != null && !p.IsBot)
                    {
                        p.Item = Config.PowerUp.None;
                        lastShootTime.Remove(p.Id);
                        BanCa.Redis.RedisManager.SetNotPlaying(p.Id);
                        //BanCa.Redis.RedisManager.UpdateCash(p.PlayerId, p.Cash);
                        var cash = p.Cash;
                        var profit = p.Profit;
                        long id = p.Id;
                        TaskRunner.RunOnPool(async () =>
                        {
                            if (!soloMode)
                            {
                                cash = await BanCa.Redis.RedisManager.IncEpicCash(id, profit, "banca", "QUIT_BC: " + Id,
                                    BanCa.Redis.TransType.BAN_CA);
                                if (cash < 0)
                                {
                                    Logger.Info("Cannot inc cash on quit bc " + id);
                                    var msg = new SimpleJSON.JSONObject();
                                    msg["change"] = profit;
                                    msg["current"] = await BanCa.Redis.RedisManager.GetUserCash(id);
                                    msg["tableId"] = Id;
                                    msg["code"] = cash;
                                    BanCa.Redis.RedisManager.AddHacker(id,
                                        BanCa.Redis.RedisManager.HackType.CannotIncCash, msg.ToString());
                                }
                                else
                                {
                                    BanCa.Redis.RedisManager.IncTop(p.PlayerId, p.CashGain);
                                    if (cash > -1) MySqlProcess.Genneral.MySqlUser.SaveCashToDb(id, cash);
                                    BanCa.Redis.RedisManager.SavePlayer(p);
                                }
                            }

                            BanCa.Redis.RedisManager.SetItemTimestamp(p.PlayerId, p.TimeUseSnipe, p.TimeUseRapidFire);
                        });
                        BanCa.Sql.SqlLogger.LogBulletHitFish(Id, TableBlindIndex, p.Id, p.Cash, 0, ServerId, true);
                        BanCa.Sql.SqlLogger.LogPlayerLeave(Id, TableBlindIndex, p.Id, p.Cash, ServerId, reason);

                        if (soloMode) // save cash
                        {
                            if (!soloEnded)
                            {
                                if (WorldState == State.Waiting) // game has not started
                                {
                                    var cashToJoin = Config.TableSoloCashIn[TableBlindIndex];
                                    var _cash = BanCa.Redis.RedisManager.IncEpicCashCache(p.Id, cashToJoin, "banca",
                                        "REFUND_SOLO_BC: " + Id, BanCa.Redis.TransType.SOLO_FEE);
                                    if (_cash > -1) MySqlProcess.Genneral.MySqlUser.SaveCashToDb(p.Id, _cash);

                                    if (SoloPlayerId1 != null && p.PlayerId == SoloPlayerId1.PlayerId)
                                    {
                                        SoloPlayerId1 = null;
                                    }
                                    else if (SoloPlayerId2 != null && p.PlayerId == SoloPlayerId2.PlayerId)
                                    {
                                        SoloPlayerId2 = null;
                                    }
                                }
                                else
                                {
                                    if (SoloPlayerId1 != null && p.PlayerId == SoloPlayerId1.PlayerId)
                                    {
                                        SoloPlayerId1.Cash = p.Cash;
                                        SoloPlayerId1.CashGain = p.CashGain;
                                    }
                                    else if (SoloPlayerId2 != null && p.PlayerId == SoloPlayerId2.PlayerId)
                                    {
                                        SoloPlayerId2.Cash = p.Cash;
                                        SoloPlayerId2.CashGain = p.CashGain;
                                    }
                                }
                            }
                        }
                        else
                        {
                            BanCa.Sql.SqlLogger.LogBcHistory(p.Id, profit, Id, TableBlindIndex, 0);
                        }
                    }
#endif
                    return p;
                }
            }

            return null;
        }

        public bool IsFull(long userId = 0)
        {
            if (soloMode)
            {
                if (userId == 0)
                    return SoloPlayerId1 != null && SoloPlayerId2 != null;
                else
                    return SoloPlayerId1 != null && SoloPlayerId2 != null && SoloPlayerId1.Id != userId &&
                           SoloPlayerId2.Id != userId;
            }

            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsEmpty()
        {
            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] != null)
                {
                    return false;
                }
            }

            return true;
        }

        bool IsSoloPlayer1(Player p)
        {
            return SoloPlayerId1 != null && p.PlayerId == SoloPlayerId1.PlayerId;
        }

        bool IsSoloPlayer2(Player p)
        {
            return SoloPlayerId2 != null && p.PlayerId == SoloPlayerId2.PlayerId;
        }

        SoloPlayer GetSoloPlayer(Player p)
        {
            if (IsSoloPlayer1(p)) return SoloPlayerId1;
            if (IsSoloPlayer2(p)) return SoloPlayerId2;
            return null;
        }

        public void Shoot(Player p, float rad, Config.BulletType type, int targetId = -1, bool rapidFire = false,
            bool isAuto = false)
        {
            if (p != null && !Config.IsMaintain)
            {
                var now = TimeUtil.TimeStamp;
                var itemCost = 0;
                if (IsServer)
                {
                    if (!lastShootTime.ContainsKey(p.Id))
                    {
                        lastShootTime[p.Id] = now;
                    }
                    else
                    {
                        var delta = now - lastShootTime[p.Id];
                        if (delta < Config.MinTimeBetweenShooting) // shoot too fast
                        {
                            //Logger.Info("Shoot too fast: " + p.Id + " " + delta);
                            return;
                        }
                    }
                }

                var _value = Config.GetBulletValue(TableBlindIndex, type);
                p.IdleTimeS = 0;
                if (!isAuto && targetId != -1)
                {
                    var lastUse = (now - p.TimeUseSnipe) / 1000f; // s   
                    if (lastUse >= Config.SnipeDurationS + Config.SnipeCoolDownS)
                    {
                        if (soloMode)
                        {
                            var sp = GetSoloPlayer(p);
                            if (sp != null && --sp.SnipeCount >= 0)
                            {
                                p.TimeUseSnipe = now;
                            }
                            else
                            {
                                targetId = -1;
                            }
                        }
                        else
                        {
                            if (Redis.RedisManager.GetSnipeCount(p.Id) > 0)
                            {
                                TaskRunner.RunOnPool(async () => await Redis.RedisManager.AddSnipeItem(p.Id, -1));
                            }
                            else
                            {
                                itemCost += Config.SnipeCost;
                                BanCa.Sql.SqlLogger.LogBuyItem(Id, TableBlindIndex, p.Id, p.Cash, -Config.SnipeCost,
                                    ServerId, PowerUp.Snipe);
                            }

                            p.TimeUseSnipe = now;
                        }
                    }
                    else if (lastUse > Config.SnipeDurationS) // illegal
                    {
                        targetId = -1;
                    }
                }

                if (rapidFire)
                {
                    var lastUse = (now - p.TimeUseRapidFire) / 1000f; // s   
                    if (lastUse >= Config.FastFireDuration + Config.FastFireCoolDownS) // fully cooldown
                    {
                        if (soloMode)
                        {
                            var sp = GetSoloPlayer(p);
                            if (sp != null && --sp.FastFireCount >= 0)
                            {
                                p.TimeUseRapidFire = now;
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (Redis.RedisManager.GetRapidFireCount(p.Id) > 0)
                            {
                                TaskRunner.RunOnPool(async () => await Redis.RedisManager.AddRapidFireItem(p.Id, -1));
                            }
                            else
                            {
                                itemCost += Config.FastFireCost;
                                BanCa.Sql.SqlLogger.LogBuyItem(Id, TableBlindIndex, p.Id, p.Cash, -Config.FastFireCost,
                                    ServerId, PowerUp.FastShoot);
                            }

                            p.TimeUseRapidFire = now;
                        }
                    }
                    else if (lastUse > Config.FastFireDuration) // not fully cooldown or out of ammo
                    {
                        return;
                    }
                }

                _value += itemCost;
                if (p.Item != Config.PowerUp.FreeShot)
                {
                    if (p.Cash < _value) // not enough money
                    {
                        if (OnUpdateCash != null)
                        {
                            var msg = new JSONObject();
                            msg["playerId"] = p.PlayerId;
                            msg["cash"] = p.Cash;
                            msg["value"] = 0;
                            msg["time"] = now;
                            msg["scr"] = (int) CashSource.OutOfCash;
                            msg["cashGain"] = p.CashGain;
                            msg["expGain"] = p.ExpGain;
                            OnUpdateCash(msg);
                        }

                        return;
                    }
                }

                var b = obtainBullet();
                if (b != null)
                {
                    var pos = Config.playerPos[p.PosIndex];
                    b.V.Set(Config.GUN_LENGTH, 0);
                    b.V.Rotate(rad);
                    b.Pos.Set(pos).Add(b.V);
                    b.V.Normalize().Mul(Config.BulletSpeed);
                    b.Type = type;
                    b.PlayerId = p.PlayerId;
                    b.EpicId = p.Id;
                    b.CashAtShoot = p.Cash;
                    b.CashChangeAtShoot = _value;
                    b.IsAuto = isAuto;
                    b.RapidFire = rapidFire;
                    b.Power = Config.TypeToPower[type];
                    b.Value = _value;
                    b.TargetId = targetId;
                    b.Start = DateTime.UtcNow;
                    b.IsBot = p.IsBot;

                    if (p.Item != Config.PowerUp.FreeShot)
                    {
                        p.Profit -= _value;
                        p.Cash -= _value;
                    }

                    lastShootTime[p.Id] = now;
                    if (OnShoot != null)
                    {
                        var msg = new JSONObject();
                        msg["playerId"] = p.PlayerId;
                        msg["rad"] = rad;
                        msg["type"] = (int) type;
                        msg["cash"] = p.Cash;
                        msg["time"] = now;
                        msg["target"] = b.TargetId;
                        OnShoot(msg);
                    }
                }
            }
        }

        public void Shoot(string playerId, float rad, Config.BulletType type, int targetId = -1, bool rapidFire = false,
            bool isAuto = false)
        {
            var p = getPlayer(playerId);
            Shoot(p, rad, type, targetId, rapidFire, isAuto);
        }

        public void ShootByPeerId(string peerId, float rad, Config.BulletType type, int targetId = -1,
            bool rapidFire = false, bool isAuto = false)
        {
            var p = getPlayerByPeerId(peerId);
            Shoot(p, rad, type, targetId, rapidFire, isAuto);
        }

        private Vector tempV = new Vector();

        public Config.FishType AutoShoot(string playerId, int burstIndex)
        {
            var p = getPlayer(playerId);
            if (p != null)
            {
                var pos = Config.playerPos[p.PosIndex];
                BanCaObject nearest = null;
                float distance = float.MaxValue;
                var gunLength2 = Config.GUN_LENGTH * Config.GUN_LENGTH;
                var range2 = Config.ScreenH * Config.ScreenH / 4;
                tempV.Set(-pos.X, -pos.Y);
                var vary = 45 * 3.14f / 180;
                var centerAngle = tempV.GetAngle();
                var minAngle = centerAngle - vary;
                var maxAngle = centerAngle + vary;

                for (int i = 0, n = allSpecialObjects.Count; i < n; i++)
                {
                    var o = allObjects[i];
                    if (o != null && o.Health > 0)
                    {
                        if (nearest == null)
                        {
                            nearest = o;
                            distance = pos.SquareDistance(o.Pos);
                            tempV.X = o.Pos.X - pos.X;
                            tempV.Y = o.Pos.Y - pos.Y;
                            var angle = tempV.GetAngle();
                            if (distance < gunLength2 || distance > range2 || angle < minAngle || angle > maxAngle)
                            {
                                nearest = null;
                                distance = float.MaxValue;
                            }
                        }
                        else
                        {
                            tempV.X = o.Pos.X - pos.X;
                            tempV.Y = o.Pos.Y - pos.Y;
                            var angle = tempV.GetAngle();
                            var dis = pos.SquareDistance(o.Pos);
                            if (dis > gunLength2 && dis < range2 && distance > dis && angle >= minAngle &&
                                angle <= maxAngle)
                            {
                                distance = dis;
                                nearest = o;
                            }
                        }
                    }
                }

                for (int i = 0, n = allObjects.Count; i < n; i++)
                {
                    var o = allObjects[i];
                    if (o != null && o.Health > 0)
                    {
                        if (nearest == null)
                        {
                            nearest = o;
                            distance = pos.SquareDistance(o.Pos);
                            tempV.X = o.Pos.X - pos.X;
                            tempV.Y = o.Pos.Y - pos.Y;
                            var angle = tempV.GetAngle();
                            if (distance < gunLength2 || distance > range2 || angle < minAngle || angle > maxAngle)
                            {
                                nearest = null;
                                distance = float.MaxValue;
                            }
                        }
                        else
                        {
                            tempV.X = o.Pos.X - pos.X;
                            tempV.Y = o.Pos.Y - pos.Y;
                            var angle = tempV.GetAngle();
                            var dis = pos.SquareDistance(o.Pos);
                            if (dis > gunLength2 && dis < range2 && distance > dis && angle >= minAngle &&
                                angle <= maxAngle)
                            {
                                distance = dis;
                                nearest = o;
                            }
                        }
                    }
                }

                if (nearest != null)
                {
                    tempV.X = nearest.Pos.X - pos.X;
                    tempV.Y = nearest.Pos.Y - pos.Y;
                    var angle = tempV.GetAngle();
                    var bullet = Config.BulletType.Bullet1;
                    if (p.Cash >= Config.GetBulletValue(TableBlindIndex, Config.BulletType.Bullet2) &&
                        burstIndex < Bots.BotConfig.Bullet2UsagePercent)
                    {
                        bullet = Config.BulletType.Bullet2;
                    }
                    else if (p.Cash >= Config.GetBulletValue(TableBlindIndex, Config.BulletType.Bullet3) &&
                             burstIndex < Bots.BotConfig.Bullet2UsagePercent + Bots.BotConfig.Bullet3UsagePercent)
                    {
                        bullet = Config.BulletType.Bullet3;
                    }
                    else if (p.Cash >= Config.GetBulletValue(TableBlindIndex, Config.BulletType.Bullet4) && burstIndex <
                        Bots.BotConfig.Bullet2UsagePercent + Bots.BotConfig.Bullet3UsagePercent +
                        Bots.BotConfig.Bullet4UsagePercent)
                    {
                        bullet = Config.BulletType.Bullet4;
                    }

                    Shoot(p, angle, bullet);

                    return nearest.Type;
                }
            }

            return Config.FishType.Basic;
        }

        public void KillAllObject(long timeStamp)
        {
            for (int i = 0, n = allObjects.Count; i < n; i++)
            {
                var o = allObjects[i];
                if (o != null)
                {
                    if (o.lastTimeStamp < timeStamp)
                    {
                        o.lastTimeStamp = timeStamp;
                        o.Health = -1;
                        o.Value = 0;
                        o.Remove();
                    }
                }
            }
        }

        public void KillObject(int id, long timeStamp)
        {
            var o = getObj(id);
            if (o != null)
            {
                if (o.lastTimeStamp < timeStamp)
                {
                    o.lastTimeStamp = timeStamp;
                    o.Health = -1;
                    o.Remove();
                }
            }
        }

        public void UpdateObject(int id, JSONNode data)
        {
            var o = getObj(id);
            if (o != null)
            {
                forceUpdateObject(o, data);
            }
        }

        public void UpdateCash(string playerId, long cash, long addValue, long time)
        {
            var p = getPlayer(playerId);
            if (p != null)
            {
                p.Cash = cash;
            }
        }

        public int UseBombItem(string playerId, Config.BulletType type)
        {
            if (!soloMode)
                return 1;

            var p = getPlayer(playerId);
            if (p == null)
                return 2;

            var _value = Config.GetBulletValue(TableBlindIndex, type);
            var _power = Config.TypeToPower[type];
            var me = players[0] == p ? SoloPlayerId1 : SoloPlayerId2;
            if (me == null)
                return 3;
            me.BombCount--;
            if (me.BombCount < 0)
            {
                me.BombCount = 0;
                return 4;
            }

            for (int i = 0, n = allObjects.Count; i < n; i++)
            {
                var o = allObjects[i];
                if (o != null && o.BoundingBox.test(BoundingBox, collider) != null)
                {
                    if (o.Health > 0)
                    {
                        o.Health -= Config.SoloItemBombDamage;
                        if (o.Health <= 0)
                        {
                            var v = (long) (o.MaxHealth * _value / _power);
                            {
                                if (p != null)
                                {
                                    var exp = (long) o.MaxHealth + (long) Math.Sqrt(v * 100);
                                    p.Cash += v;
                                    p.Profit += v;
                                    p.CashGain += v;
                                    p.Exp += exp;
                                    p.ExpGain += exp;
                                    if (!soloMode && p.DoLevelUpIfApplicable())
                                    {
                                        if (OnLevelUp != null)
                                        {
                                            OnLevelUp(p.GetLvJson());
                                        }
                                    }

                                    BanCa.Sql.SqlLogger.LogKillFish(Id, TableBlindIndex, type, p.Id, p.Cash, v,
                                        ServerId, p.Item, o.Type, p.CashGain);

                                    if (OnUpdateCash != null)
                                    {
                                        var msg = new JSONObject();
                                        msg["playerId"] = p.PlayerId;
                                        msg["cash"] = p.Cash;
                                        msg["value"] = v;
                                        msg["time"] = TimeUtil.TimeStamp;
                                        msg["scr"] = (int) CashSource.KillAll;
                                        msg["cashGain"] = p.CashGain;
                                        msg["expGain"] = p.ExpGain;
                                        OnUpdateCash(msg);
                                    }
                                }

                                if (OnObjectDie != null)
                                {
                                    var msg = new JSONObject();
                                    msg["playerId"] = p.PlayerId;
                                    msg["id"] = o.ID;
                                    msg["value"] = v;
                                    msg["time"] = TimeUtil.TimeStamp;
                                    OnObjectDie(msg);
                                    //Logger.Info("Obj die 3: " + msg.ToString());
                                }

                                o.Remove();
                                o.SetRefundOnRevive();

                                deadFish.AddLast(o);
                            }
                        }
                    }
                }
            }

            return 0;
        }

        public JSONNode ToJson(bool hideHp = true, bool overrideCompress = true, long myUserId = -1)
        {
            var data = new JSONObject();
            var objs = new JSONArray();
            data["objects"] = objs;
            for (int i = 0, n = allObjects.Count; i < n; i++)
            {
                var o = allObjects[i];
                if (o.Health <= 0)
                {
                    o.Pos.Set(2000, 2000);
                }

                objs.Add(o.ToJson(hideHp, overrideCompress));
            }

            var sobjs = new JSONArray();
            data["sobjects"] = sobjs;
            for (int i = 0, n = allSpecialObjects.Count; i < n; i++)
            {
                var o = allSpecialObjects[i];
                if (o.Health <= 0)
                {
                    o.Pos.Set(2000, 2000);
                }

                sobjs.Add(o.ToJson(hideHp, overrideCompress));
            }

            var bls = new JSONArray();
            data["bullets"] = bls;
            for (int i = 0, n = activeBullets.Count; i < n; i++)
            {
                var b = activeBullets[i];
                bls.Add(b.ToJson());
            }

            var ps = new JSONArray();
            data["players"] = ps;
            for (int i = 0, n = players.Length; i < n; i++)
            {
                var p = players[i];
                if (p != null)
                {
                    ps.Add(p.ToJson());
                }
            }

            data["time"] = TimeUtil.TimeStamp;
            data["blind"] = TableBlind;
            data["blindIndex"] = TableBlindIndex;

            if (WorldState == State.NewWave)
            {
                var now = TimeUtil.TimeStamp;
                if (now - WaveStart > Config.NEW_WAVE_MAX_TIME * 1000)
                {
                    WorldState = State.Playing;
                    if (currentWave != null)
                    {
                        Logger.Info("Wave time out 2: " + currentWave.IsEnd() + " " + currentWave.IsEnding() + " " +
                                    currentWave.GetType().ToString() + " " + waveLength);
                        //try
                        //{
                        //    Logger.Info("Wave detail: " + currentWave.GetDetails());
                        //}
                        //catch (Exception ex)
                        //{
                        //    Logger.Info("Wave detail error: " + ex.ToString());
                        //}
                        currentWave.SetEnding();
                        currentWave = null;
                    }

                    waveLength = soloMode ? Config.SOLO_DURATION : Config.PLAYING_WAVE_DURATION;
                }
            }

            if (WorldState == State.Playing)
            {
                deadFish.Clear();
                for (int i = 0; i < allSpecialObjects.Count; i++) // re add bomb fish
                {
                    var o = allSpecialObjects[i];
                    if (o != null && o.Health <= 0)
                    {
                        deadFish.AddLast(o);
                    }
                }

                for (int i = 0, n = allObjects.Count; i < n; i++)
                {
                    var o = allObjects[i];
                    if (o != null && o.Health <= 0)
                    {
                        deadFish.AddLast(o);
                    }
                }
            }

            data["wstate"] = (int) WorldState;
            data["id"] = Id;
            data["solo"] = soloMode ? 1 : 0;
            data["remainTime"] =
                soloMode ? Config.SOLO_DURATION - (TimeUtil.TimeStamp - soloStart) / 1000f : waveLength;
            //if(soloMode)
            //{
            //    Logger.Info("remain time: " + (Config.SOLO_DURATION - (TimeUtil.TimeStamp - soloStart) / 1000f));
            //    Logger.Info("dutaion time: " + Config.SOLO_DURATION);
            //    Logger.Info("now time: " + TimeUtil.TimeStamp);
            //    Logger.Info("start time: " + soloStart);
            //    Logger.Info("waveLength time: " + waveLength);
            //}

            if (myUserId > -1)
            {
                if (soloMode)
                {
                    var me = players[0] != null && players[0].Id == myUserId ? SoloPlayerId1 : SoloPlayerId2;
                    if (me != null)
                    {
                        data["rf"] = me.FastFireCount;
                        data["sn"] = me.SnipeCount;
                        data["bomb"] = me.BombCount;
                    }
                }
                else
                {
                    data["rf"] = Redis.RedisManager.GetRapidFireCount(myUserId);
                    data["sn"] = Redis.RedisManager.GetSnipeCount(myUserId);
                    data["bomb"] = 0;
                }
            }

            //Logger.Info("Return state " + WorldState);
            return data;
        }

        //public JSONNode ToJsonExcludeBullets()
        //{
        //    var data = new JSONClass();
        //    var objs = new JSONArray();
        //    data["objects"] = objs;
        //    for (int i = 0, n = allObjects.Count; i < n; i++)
        //    {
        //        var o = allObjects[i];
        //        objs.Add(o.ToJson());
        //    }

        //    var sobjs = new JSONArray();
        //    data["sobjects"] = sobjs;
        //    for (int i = 0, n = allSpecialObjects.Count; i < n; i++)
        //    {
        //        var o = allSpecialObjects[i];
        //        sobjs.Add(o.ToJson());
        //    }

        //    var ps = new JSONArray();
        //    data["players"] = ps;
        //    for (int i = 0, n = players.Length; i < n; i++)
        //    {
        //        var p = players[i];
        //        if (p != null)
        //        {
        //            ps.Add(p.ToJson());
        //        }
        //    }

        //    data["time"] = TimeUtil.TimeStamp;
        //    data["wstate"] = (int)WorldState;
        //    data["blind"] = TableBlind;
        //    data["blindIndex"] = TableBlindIndex;
        //    return data;
        //}

        public void ParseJson(JSONNode data)
        {
            var time = data["time"].AsLong;
            TableBlind = data["blind"].AsLong;
            TableBlindIndex = data["blindIndex"].AsInt;
            WorldState = (State) data["wstate"].AsInt;
            Id = data["id"].AsInt;
            var timePass = (TimeUtil.TimeStamp + TimeUtil.ClientServerTimeDifferentMs - time) / 1000f;
            if (OnTimeDifferent != null)
            {
                if (OnTimeDifferent(timePass))
                {
                    return;
                }
            }

            var objs = data["objects"].AsArray;
            for (int i = 0, n = objs.Count; i < n; i++)
            {
                var o = objs[i];
                var obj = new BanCaObject(this);
                obj.isSpecial = false;
                obj.ParseJson(o);
                if (time > 0) BanCaObject.AdvanceTime(this, obj, timePass);
                allObjects.Add(obj);
                objectMap.Add(obj.ID, obj);
            }

            var sobjs = data["sobjects"].AsArray;
            for (int i = 0, n = sobjs.Count; i < n; i++)
            {
                var o = sobjs[i];
                var obj = new BanCaObject(this);
                obj.isSpecial = true;
                obj.ParseJson(o);
                if (time > 0) BanCaObject.AdvanceTime(this, obj, timePass);
                allSpecialObjects.Add(obj);
                objectMap.Add(obj.ID, obj);
            }

            var bls = data["bullets"].AsArray;
            for (int i = 0, n = bls.Count; i < n; i++)
            {
                var b = bls[i];
                var bul = obtainBullet();
                bul.ParseJson(b);
            }

            var ps = data["players"].AsArray;
            for (int i = 0, n = ps.Count; i < n; i++)
            {
                var p = ps[i];
                var player = new Player();
                player.ParseJson(p);
                RegisterPlayer(player);
            }
        }

        public void ParseUpdateJson(JSONNode data)
        {
            var objs = data["objects"].AsArray;
            for (int i = 0, n = objs.Count; i < n; i++)
            {
                var o = objs[i];
                var obj = getObj(o["id"].AsInt);
                if (obj != null)
                {
                    obj.ParseJson(o);
                }
            }

            var ps = data["players"].AsArray;
            for (int i = 0, n = ps.Count; i < n; i++)
            {
                var p = ps[i];
                var player = getPlayer(p["playerId"]);
                if (player != null)
                {
                    player.ParseJson(p);
                }
            }
        }

        private BanCaObject getObj(int id)
        {
            if (objectMap.ContainsKey(id))
            {
                return objectMap[id];
            }

            return null;
        }

        private void forceUpdateObject(BanCaObject fish, JSONNode data)
        {
            fish.ForceUpdate(data);
        }

        public long KillAllFishInScreen(Player p, Config.BulletType type)
        {
            long killAllProfit = 0;
            var Value = Config.GetBulletValue(TableBlindIndex, type);
            var Power = Config.TypeToPower[type];
            if (p != null)
            {
                killAllProfit = (long) (p.IsBot
                    ? FundManager.FlushJackpot(TableBlindIndex, type)
                    : FundManager.FlushJackpotAndBombBank(TableBlindIndex, type)); // get fund from jackpot
                var exp = (long) Math.Sqrt(killAllProfit * 10);
                p.Profit += killAllProfit;
                p.CashGain += killAllProfit;
                p.Cash += killAllProfit;
                p.Exp += exp;
                p.ExpGain += exp;
                var mylv = p.Level;
                for (int i = 0, n = allObjects.Count; i < n; i++)
                {
                    var o = allObjects[i];
                    if (o != null && o.BoundingBox.test(BoundingBox, collider) != null)
                    {
                        var v = (long) (o.MaxHealth * Value / Power);
                        if (p != null)
                        {
                            exp = (long) o.MaxHealth + (long) Math.Sqrt(v * 100);

                            //p.Profit += v;
                            //p.CashGain += v;
                            //p.Cash += v;
                            p.Exp += exp;
                            p.ExpGain += exp;
                            //killAllProfit += v;

                            if (!soloMode)
                            {
                                p.DoLevelUpIfApplicable();
                            }

                            //#if NetCore
                            //                            if (!p.IsBot)
                            //                            {
                            //                                BanCa.Sql.SqlLogger.LogBulletHitFish(Id, TableBlindIndex, p.Id, p.Cash, 0, ServerId);
                            //                                BanCa.Sql.SqlLogger.LogKillFish(Id, TableBlindIndex, type, p.Id, p.Cash, v, ServerId, p.Item, o.Type);
                            //                            }
                            //#endif
                        }

                        o.Health = -1;
                        if (OnObjectDie != null)
                        {
                            var msg = new JSONObject();
                            msg["playerId"] = p.PlayerId;
                            msg["id"] = o.ID;
                            msg["value"] = v;
                            msg["time"] = TimeUtil.TimeStamp;
                            OnObjectDie(msg);
                            //Logger.Info("Obj die 4: " + o.ID + " by " + playerId);
                        }

                        o.Remove();
                        o.SetRefundOnRevive();

                        if (WorldState != State.WaitingForNewWave && WorldState != State.NewWave)
                        {
                            if (bigFish.Contains(o.Type))
                            {
                                bigFishCount--;
                                if (bigFishCount < 0)
                                    bigFishCount = 0;
                                //Logger.Info("Dead fish 2 " + o.Type);
                            }

                            deadFish.AddLast(o);
                        }
                    }
                }

                if (!soloMode && mylv != p.Level)
                {
                    if (OnLevelUp != null)
                    {
                        OnLevelUp(p.GetLvJson());
                    }
                }

                if (OnUpdateCash != null)
                {
                    var msg = new JSONObject();
                    msg["playerId"] = p.PlayerId;
                    msg["cash"] = p.Cash;
                    msg["value"] = killAllProfit;
                    msg["time"] = TimeUtil.TimeStamp;
                    msg["scr"] = (int) CashSource.KillAll;
                    msg["cashGain"] = p.CashGain;
                    msg["expGain"] = p.ExpGain;
                    OnUpdateCash(msg);
                }
            }

            return killAllProfit;
        }

        public Player getPlayer(string playerId)
        {
            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] != null && players[i].PlayerId == playerId)
                {
                    return players[i];
                }
            }

            return null;
        }

        public Player getPlayerByPeerId(string peerId)
        {
            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] != null && players[i].PeerId == peerId)
                {
                    return players[i];
                }
            }

            return null;
        }

        public Player getPlayerByUserId(long userId)
        {
            for (int i = 0, n = players.Length; i < n; i++)
            {
                if (players[i] != null && players[i].Id == userId)
                {
                    return players[i];
                }
            }

            return null;
        }

        private BanCaBullet obtainBullet()
        {
            if (bulletPool.Count > 0)
            {
                var last = bulletPool.Count - 1;
                var b = bulletPool[last];
                bulletPool.RemoveAt(last);
                activeBullets.Add(b);
                b.Recycle();
                b.Active = true;
                return b;
            }

            return null;
        }

        private void freeBullet(BanCaBullet b, Config.FishType hitTarget, int activeIndex)
        {
#if SERVER
            if (IsServer && !soloMode)
            {
                var _value = b.Value;
                var type = b.Type;

                if (hitTarget == FishType.Basic)
                {
                    // refund
                    var p = getPlayer(b.PlayerId);
                    if (p != null)
                    {
                        p.Profit += b.Value;
                        p.Cash += b.Value;
                        if (OnRefundBullet != null)
                        {
                            var msg = new JSONObject();
                            msg["playerId"] = p.PlayerId;
                            msg["cash"] = p.Cash;
                            msg["profit"] = p.Profit;
                            msg["time"] = TimeUtil.TimeStamp;
                            OnRefundBullet(msg);
                        }
                    }
                }
                else
                {
                    var fee = _value * Config.FeeRate;
                    var jp = _value * Config.JackpotRate;
                    var remain = _value - fee - jp;
                    FundManager.IncJackpot(TableBlindIndex, b.Type, jp); // fund to show in client
                    if (!b.IsBot)
                    {
                        FundManager.IncFund(TableBlindIndex, b.Type, hitTarget, remain);
                        FundManager.IncBombFund(TableBlindIndex, b.Type, jp); // fund to pay jackpot
                        FundManager.IncProfit(TableBlindIndex, b.Type, hitTarget, fee); // our profit
                    }
                }
            }
#endif

            b.Pos.Set(-2000, -2000);
            if (activeIndex == -1)
            {
                activeBullets.Remove(b);
            }
            else
            {
                activeBullets[activeIndex] = activeBullets[activeBullets.Count - 1];
                activeBullets.RemoveAt(activeBullets.Count - 1);
            }

            bulletPool.Add(b);
            b.Active = false;
            if (b.OnRemove != null)
            {
                b.OnRemove(b.ID);
            }
        }

        internal void scheduleToRevive(BanCaObject fish, long reviveValue = 0, float reviveHealth = -1)
        {
            //Logger.Log("Schedule to revive " + fish.ID);
            fish.reviveValue = reviveValue;
            fish.reviveHealth = reviveHealth > 0 ? reviveHealth : -1f;
            if (bigFish.Contains(fish.Type))
            {
                bigFishCount--;
                if (bigFishCount < 0)
                    bigFishCount = 0;

                //Logger.Info("Dead fish 3 " + fish.Type);
            }

            deadFish.AddLast(fish);
        }

        private bool hasFishInScreen()
        {
            for (int i = 0, n = allObjects.Count; i < n; i++)
            {
                var o = allObjects[i];
                if (o.BoundingBox.test(this.WorldBoundingBox, collider) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void setupFishForNewWave()
        {
            deadFish.Clear();
            for (int i = 0; i < allSpecialObjects.Count; i++) // re add bomb fish
            {
                if (allSpecialObjects[i].Health <= 0)
                {
                    deadFish.AddFirst(allSpecialObjects[i]);
                }
            }

            bigFishCount = 0;
            //Logger.Info("Clear fish");
            if (currentWave == null)
            {
                var index = Random.Next() % waveLibrary.Length;
                currentWave = waveLibrary[index];
            }

            currentWave.Start(this, allObjects);
            waveLength = Config.NEW_WAVE_MAX_TIME;
        }

        #region On push event

        public Action<JSONNode> OnEnterPlayer;
        public Action<string, JSONNode> OnLeavePlayer; // peerId, data
        public Action<JSONNode> OnShoot; // player id, angle, power
        public Action<int, int> OnBulletHitClient; // bullet id, fish id
        public Action<JSONNode> OnItemUse;
        public Action<JSONNode> OnLevelUp;
        public Action<JSONNode> OnUpdateObject, OnObjectTeleport;
        public Action<JSONNode> OnUpdateObjectSequence;
        public Action<JSONNode> OnUpdateCash; // with peerid, -1 = all
        public Action<JSONNode> OnObjectDie; // object id and who kill the fish, value add to captor
        public Action<JSONNode> OnRemoveAllObject;
        public Action<JSONNode> OnNewState;
        public Action<JSONNode> OnEndSolo;
        public Action<JSONNode> OnRefundBullet;

        public Action OnBotRequest;

        public delegate bool TimeDiffCallback(float timeDiff);

        public TimeDiffCallback OnTimeDifferent; // return true will skip execution

        #endregion
    }
}