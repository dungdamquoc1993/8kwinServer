using Entites.General;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BanCa.Libs.Bots
{
    public enum BotState : int
    {
        Free, WaitingToEnter, Playing, WaitingToQuit
    }

    public class BcBot
    {
        private HashSet<Config.FishType> bigFish = new HashSet<Config.FishType> { Config.FishType.CaThanTai, Config.FishType.Mermaid, Config.FishType.MermaidBig, Config.FishType.MermaidSmall, Config.FishType.MerMan };

        private static long BOT_COUNT = int.MaxValue;
        private Random random;
        private BanCaServer caServer;
        public readonly User UserBot;

        public long Id { get { return UserBot.UserId; } }
        public BotState State { get; private set; }

        public GameBanCa currentWorld;

        private long fireRate = 250;

        private int playRemainTimeS;
        private long quitTimestamp;
        private long lastFastShootTimestamp;

        public BcBot(BanCaServer caServer)
        {
            this.caServer = caServer;
            random = new Random();
            UserBot = new User();
            UserBot.UserId = Interlocked.Decrement(ref BOT_COUNT);
            UserBot.ClientId = UserBot.Username = "#BOT_" + UserBot.UserId;
            UserBot.Platform = "BOT";
            UserBot.Avatar = "";
            UserBot.Nickname = BotNickname.RandomName(random);

            SetState(BotState.Free);
            fireRate = (long)(1000 / Config.FIRE_RATE);
        }

        public void SetState(BotState state)
        {
            State = state;
            changeStateTime = TimeUtil.TimeStamp;
        }

        public void JoinTable(int tableId, int tableIndex)
        {
            //if (UserBot.Cash < 100000)
            var minCash = 200;
            var maxCash = 10000;
            if (tableIndex == 1)
            {
                minCash = BotConfig.MinCash1;
                maxCash = BotConfig.MaxCash1;
            }
            else if (tableIndex == 2)
            {
                minCash = BotConfig.MinCash2;
                maxCash = BotConfig.MaxCash2;
            }
            else if (tableIndex == 3)
            {
                minCash = BotConfig.MinCash3;
                maxCash = BotConfig.MaxCash3;
            }
            UserBot.Cash = (random.Next(minCash, maxCash) / 10) * 10;

            UserBot.Nickname = BotNickname.RandomName(random);
            currentWorld = caServer.botJoinGame(this, tableId);
            playRemainTimeS = random.Next(BotConfig.PlayTimeMinMs, BotConfig.PlayTimeMaxMs);
            if (currentWorld != null)
            {
                SetState(BotState.WaitingToEnter);
            }
            else
            {
                SetState(BotState.Free);
            }
        }

        public void OnPush(string route, JSONNode msg)
        {
            if (route == "OnLeavePlayer" && msg["playerId"] == UserBot.Username)
            {
                if (State != BotState.Free)
                {
                    currentWorld = null;
                    SetState(BotState.Free);
                }
            }
        }

        private void quitGame()
        {
            caServer.quitGame(this);
            currentWorld = null;
            SetState(BotState.Free);
        }

        long changeStateTime = 0;
        long lastShoot = 0;
        long waitTo, shootTo;
        public void Update(int delta)
        {
            if (State == BotState.Free)
            {
                return;
            }

            if (State == BotState.Playing)
            {
                if (currentWorld != null)
                {
                    playRemainTimeS -= delta;
                    var now = TimeUtil.TimeStamp;
                    if (!BotConfig.BotActive)
                    {
                        //quitGame();
                        SetState(BotState.WaitingToQuit);
                        quitTimestamp = now + random.Next(BotConfig.QuitTimeByTimeOutMinMs, BotConfig.QuitTimeByTimeOutMaxMs);
                        return;
                    }

                    if (playRemainTimeS <= 0)
                    {
                        //quitGame();
                        SetState(BotState.WaitingToQuit);
                        quitTimestamp = now + random.Next(BotConfig.QuitTimeByTimeOutMinMs, BotConfig.QuitTimeByTimeOutMaxMs);
                        return;
                    }

                    if (currentWorld.WorldState == Libs.State.Playing || currentWorld.WorldState == Libs.State.NewWave)
                    {
                        if (shootTo <= now)
                        {
                            waitTo = now + random.Next(BotConfig.ShootWaitMinMs, BotConfig.ShootWaitMaxMs);
                            shootTo = waitTo + random.Next(BotConfig.ShootTimeMinMs, BotConfig.ShootTimeMaxMs);
                            fireRate = (long)(1000 / Config.FIRE_RATE);
                        }
                        else if (waitTo >= now) // do nothing
                        { }
                        else
                        {
                            if (now - lastShoot > fireRate)
                            {
                                lastShoot = now;
                                var fish = currentWorld.AutoShoot(UserBot.Username, (int)(shootTo % 100));
                                if (bigFish.Contains(fish))
                                {
                                    if (now - lastFastShootTimestamp > Config.FastFireCoolDownS * 1000)
                                    {
                                        fireRate = (long)(1000 / (Config.FIRE_RATE * Config.FastFireRate));
                                        lastFastShootTimestamp = now;
                                        if (shootTo - now > Config.FastFireDuration * 1000)
                                        {
                                            shootTo = now + (long)(Config.FastFireDuration * 1000);
                                        }
                                    }
                                }
                                else
                                {
                                    fireRate = (long)(1000 / Config.FIRE_RATE);
                                }

                                var me = currentWorld.getPlayer(UserBot.Username);
                                if (me == null || me.Cash < currentWorld.TableBlind)
                                {
                                    //quitGame();
                                    SetState(BotState.WaitingToQuit);
                                    quitTimestamp = now + random.Next(BotConfig.QuitTimeByOutOfCashMinMs, BotConfig.QuitTimeByOutOfCashMaxMs);
                                }
                            }
                        }
                    }
                }
                else
                {
                    SetState(BotState.WaitingToQuit);
                    quitTimestamp = TimeUtil.TimeStamp + random.Next(BotConfig.QuitTimeByTimeOutMinMs, BotConfig.QuitTimeByTimeOutMaxMs);
                }
            }
            else if (State == BotState.WaitingToEnter)
            {
                SetState(BotState.Playing);
            }
            else if (State == BotState.WaitingToQuit)
            {
                if (TimeUtil.TimeStamp > quitTimestamp)
                {
                    quitGame();
                }
            }
        }
    }
}
