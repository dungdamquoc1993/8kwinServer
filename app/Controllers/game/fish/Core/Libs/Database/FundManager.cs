using BanCa.Redis;
using System;
using System.Collections.Generic;

namespace BanCa.Libs
{
    public class FundManager
    {
        private static object lockFund = new object();

        private static Dictionary<int, double> profits = new Dictionary<int, double>(); // key, cash
        private static Dictionary<int, double> refundBank = new Dictionary<int, double>(); // key, cash
        private static Dictionary<int, double> bombBank = new Dictionary<int, double>(); // key, cash
        private static Dictionary<int, double> jackpot = new Dictionary<int, double>(); // jackpot show in client

        public static int GetKey(int blindIndex, Config.BulletType bulletType, Config.FishType fishType)
        {
            var key = 0;
            // eg: 3 (blind 3) 2 (bullet 2) 12 (fish 12)
            key += blindIndex * 1000;
            key += (int)bulletType * 100;
            key += (int)fishType;
            return key;
        }

        // return -1 = fail
        public static double IncProfit(int blindIndex, Config.BulletType bulletType, Config.FishType fishType, double val)
        {
            int key = GetKey(blindIndex, bulletType, fishType);
            lock (lockFund)
            {
                if (profits.ContainsKey(key))
                {
                    var _new = profits[key] + val;
                    if (_new < 0)
                    {
                        return -1;
                    }
                    profits[key] = _new;
                }
                else
                {
                    if (val < 0)
                    {
                        return -1;
                    }
                    profits[key] = val;
                }
                return profits[key];
            }
        }

        public static double GetProfit(int blindIndex, Config.BulletType bulletType, Config.FishType fishType)
        {
            int key = GetKey(blindIndex, bulletType, fishType);
            lock (lockFund)
            {
                if (profits.ContainsKey(key))
                {
                    return profits[key];
                }
            }

            return 0;
        }

        // return -1 = fail
        public static double IncFund(int blindIndex, Config.BulletType bulletType, Config.FishType fishType, double val)
        {
            int key = GetKey(blindIndex, bulletType, fishType);
            lock (lockFund)
            {
                if (refundBank.ContainsKey(key))
                {
                    var _new = refundBank[key] + val;
                    if (_new < 0)
                    {
                        //if (val < 0)
                        //    Logger.Info("not enough fund " + key + " " + val + " " + refundBank[key]);
                        return -1;
                    }
                    refundBank[key] = _new;
                }
                else
                {
                    if (val < 0)
                    {
                        return -1;
                    }
                    refundBank[key] = val;
                }
                return refundBank[key];
            }
        }

        public static double GetFund(int blindIndex, Config.BulletType bulletType, Config.FishType fishType)
        {
            int key = GetKey(blindIndex, bulletType, fishType);
            lock (lockFund)
            {
                if (refundBank.ContainsKey(key))
                {
                    return refundBank[key];
                }
            }

            return 0;
        }

        public static int GetKeyJackpot(int blindIndex, Config.BulletType bulletType)
        {
            var key = 0;
            // eg: 3 (blind 3) 2 (bullet 2) 12 (fish 12)
            key += blindIndex * 10;
            key += (int)bulletType;
            return key;
        }

        public static double IncBombFund(int blindIndex, Config.BulletType bulletType, double val)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (bombBank.ContainsKey(key))
                {
                    var _new = bombBank[key] + val;
                    if (_new < 0)
                    {
                        return -1;
                    }
                    bombBank[key] = _new;
                }
                else
                {
                    if (val < 0)
                    {
                        return -1;
                    }
                    bombBank[key] = val;
                }
                return bombBank[key];
            }
        }

        public static double GetBombFund(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (bombBank.ContainsKey(key))
                {
                    return bombBank[key];
                }
            }

            return 0;
        }

        public static double FlushBombFund(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (bombBank.ContainsKey(key))
                {
                    var all = bombBank[key];
                    bombBank[key] = 0;
                    return all;
                }
            }

            return 0;
        }

        public static void SaveToRedis()
        {
            lock (lockFund)
            {
                try
                {
                    RedisManager.SaveFund(profits, refundBank, bombBank, jackpot);
                }
                catch (Exception ex)
                {
                    Logger.Error("Fail to save fund to redis: " + ex.ToString());
                }
            }
        }

        public static void LoadFromRedis()
        {
            lock (lockFund)
            {
                profits.Clear();
                refundBank.Clear();
                bombBank.Clear();
                jackpot.Clear();
                try
                {
                    RedisManager.LoadFund(profits, refundBank, bombBank, jackpot);
                }
                catch (Exception ex)
                {
                    Logger.Error("Fail to load fund from redis: " + ex.ToString());
                }
            }
        }

        public static SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();

            var profitJ = new SimpleJSON.JSONObject();
            lock (lockFund)
                foreach (var pair in profits)
                {
                    profitJ[pair.Key.ToString()] = pair.Value;
                }
            data["profit"] = profitJ;

            var fundJ = new SimpleJSON.JSONObject();
            lock (lockFund)
                foreach (var pair in refundBank)
                {
                    fundJ[pair.Key.ToString()] = pair.Value;
                }
            data["fund"] = fundJ;

            var bombJ = new SimpleJSON.JSONObject();
            lock (lockFund)
                foreach (var pair in bombBank)
                {
                    bombJ[pair.Key.ToString()] = pair.Value;
                }
            data["bomb"] = bombJ;

            var jackpotJ = new SimpleJSON.JSONObject();
            lock (lockFund)
                foreach (var pair in jackpot)
                {
                    jackpotJ[pair.Key.ToString()] = pair.Value;
                }
            data["jackpot"] = jackpotJ;

            return data;
        }

        public static SimpleJSON.JSONNode GetJackpotJson()
        {
            var data = new SimpleJSON.JSONObject();

            lock (lockFund)
            {
                foreach (var pair in jackpot)
                {
                    data[pair.Key.ToString()] = pair.Value;
                }
            }
            return data;
        }

        public static double IncJackpot(int blindIndex, Config.BulletType bulletType, double val)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (jackpot.ContainsKey(key))
                {
                    var _new = jackpot[key] + val;
                    if (_new < 0)
                    {
                        return -1;
                    }
                    jackpot[key] = _new;
                }
                else
                {
                    if (val < 0)
                    {
                        return -1;
                    }
                    jackpot[key] = val;
                }
                return jackpot[key];
            }
        }

        private static Random random = new Random();
        private static List<int> keys = new List<int>();
        public static void IncJackpotFake()
        {
            lock (lockFund)
            {
                keys.Clear();
                foreach (var key in jackpot.Keys)
                {
                    keys.Add(key);
                }
                foreach (var key in keys)
                {
                    if (!Config.MinAddFakeJp.ContainsKey(key) || !Config.MaxAddFakeJp.ContainsKey(key))
                    {
                        continue;
                    }

                    int index = key / 10;
                    Config.BulletType type = (Config.BulletType)(key % 10);
                    var min = Config.MinAddFakeJp[key];
                    var max = Config.MaxAddFakeJp[key];
                    var value = Config.GetBulletValue(index, type);
                    jackpot[key] += random.Next(min, max);
                }
            }
        }

        public static double GetJackpot(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (jackpot.ContainsKey(key))
                {
                    return jackpot[key];
                }
            }

            return 0;
        }

        public static double FlushJackpotAndBombBank(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (jackpot.ContainsKey(key))
                {
                    var all = jackpot[key];
                    jackpot[key] = (double)(Config.JackpotInitial * Config.GetBulletValue(blindIndex, bulletType));

                    if (bombBank.ContainsKey(key))
                    {
                        if (all > bombBank[key])
                        {
                            all = bombBank[key];
                            bombBank[key] = 0;
                        }
                        else
                        {
                            Logger.Info("BCJackpot: " + key + " " + all + " vs " + bombBank[key]);
                            bombBank[key] -= all;
                        }
                    }
                    else
                    {
                        all = 0;
                        Logger.Info("BCJackpot key not found " + key);
                    }
                    return all;
                }
            }

            // restart fake timer
            lock (locktimers)
            {
                if (timers.ContainsKey(key))
                {
                    var timer = timers[key];
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    timers.Remove(key);
                }
            }
            StartFakeFlush(key);

            return 0;
        }

        public static double FlushJackpot(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            lock (lockFund)
            {
                if (jackpot.ContainsKey(key))
                {
                    var all = jackpot[key];
                    jackpot[key] = (double)(Config.JackpotInitial * Config.GetBulletValue(blindIndex, bulletType));
                    return all;
                }
            }

            return 0;
        }

        public static void StartFakeFlushAll()
        {
            StartFakeFlush(1, Config.BulletType.Bullet1);
            StartFakeFlush(1, Config.BulletType.Bullet2);
            StartFakeFlush(1, Config.BulletType.Bullet3);
            StartFakeFlush(1, Config.BulletType.Bullet4);

            StartFakeFlush(2, Config.BulletType.Bullet1);
            StartFakeFlush(2, Config.BulletType.Bullet2);
            StartFakeFlush(2, Config.BulletType.Bullet3);
            StartFakeFlush(2, Config.BulletType.Bullet4);

            StartFakeFlush(3, Config.BulletType.Bullet1);
            StartFakeFlush(3, Config.BulletType.Bullet2);
            StartFakeFlush(3, Config.BulletType.Bullet3);
            StartFakeFlush(3, Config.BulletType.Bullet4);
        }

        private static object locktimers = new object();
        private static Dictionary<int, System.Timers.Timer> timers = new Dictionary<int, System.Timers.Timer>();
        public static void StartFakeFlush(int blindIndex, Config.BulletType bulletType)
        {
            int key = GetKeyJackpot(blindIndex, bulletType);
            StartFakeFlush(key);
        }
        public static void StartFakeFlush(int key)
        {
            int blindIndex = key / 10;
            if (!Config.FakeJpMinTimeMS.ContainsKey(key) || !Config.FakeJpMaxTimeMS.ContainsKey(key))
            {
                Logger.Info("Min max time does not exist for key " + key);
                return;
            }
            var min = Config.FakeJpMinTimeMS[key];
            var max = Config.FakeJpMaxTimeMS[key];
            var waitMilis = 0L;
            lock (lockFund) waitMilis = random.Next(min, max);
            var timer = new System.Timers.Timer((double)waitMilis);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, a) =>
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                    lock (locktimers) timers.Remove(key);
                    FakeFlushJackpot(key);
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in StartFakeFlush: " + ex.ToString());
                    StartFakeFlush(key);
                }
            });
            timer.Start();
            //Logger.Info("Start fake jp " + blindIndex + " type " + type + " after " + waitMilis);
            lock (locktimers) timers[key] = timer;
        }

        private static double FakeFlushJackpot(int key)
        {
            var all = 0L;
            string nn = string.Empty;
            int blindIndex = key / 10;
            lock (lockFund)
            {
                if (jackpot.ContainsKey(key))
                {
                    all = (long)jackpot[key];
                    jackpot[key] = (double)(Config.JackpotInitial * Config.GetBulletValue(blindIndex, (Config.BulletType)(key % 10)));
                    nn = BotNickname.RandomName(random);
                }
            }

            if (all > 0 && !string.IsNullOrEmpty(nn))
            {
                var msg = new SimpleJSON.JSONObject();
                msg["nickname"] = nn;
                msg["value"] = all;
                msg["blind"] = blindIndex;
                BanCaLib.PushAll("onBcJackpot", msg);
                Redis.RedisManager.LogHomeMessages(0, nn, all, Config.FishType.BombFish, blindIndex, Config.BulletType.Bullet1);
                Redis.RedisManager.IncTop(nn, (long)(all * Math.Round(random.NextDouble() * 1.5 + 1.1)));
                BanCa.Sql.SqlLogger.LogJackpotEvent(-1, blindIndex, Config.BulletType.Bullet1, 0, all, all, -1, nn);
                //Logger.Info("Fake jackpot " + blindIndex + " type " + type);
                //Logger.Info("Fake jackpot for " + nn + " amount " + all);
            }

            lock (locktimers)
            {
                if (timers.ContainsKey(key))
                {
                    var timer = timers[key];
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    timers.Remove(key);
                }
            }
            StartFakeFlush(key);

            return 0;
        }

        public static void SetBombFund(int key, double val)
        {
            lock (lockFund)
            {
                bombBank[key] = val;
            }
        }

        public static void SetProfit(int key, double val)
        {
            lock (lockFund)
            {
                profits[key] = val;
            }
        }

        public static void SetRefundBank(int key, double val)
        {
            lock (lockFund)
            {
                refundBank[key] = val;
            }
        }

        public static void SetJackpot(int key, double val)
        {
            lock (lockFund)
            {
                jackpot[key] = val;
            }
        }
    }
}

