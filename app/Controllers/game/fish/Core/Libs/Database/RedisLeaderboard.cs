using BanCa.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BanCa.Redis
{
    /// <summary>
    /// Utialize redis for leaderboard like top
    /// </summary>
    public class RedisLeaderboard
    {
        public readonly string Name;
        public RedisLeaderboard(string name)
        {
            Name = name;
        }

        public async void PostScore(string player, double score)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                await redis.SortedSetAddAsync(Name, player, score);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
        }

        public async void PostScore(string player, double score, bool ifScoreBigger)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                var currentScore = await redis.SortedSetScoreAsync(Name, player);
                if (currentScore.HasValue)
                {
                    var v = currentScore.Value;
                    if ((ifScoreBigger && score > v) || (!ifScoreBigger && score < v))
                    {
                        await redis.SortedSetAddAsync(Name, player, score);
                    }
                }
                else
                {
                    await redis.SortedSetAddAsync(Name, player, score);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
        }

        public async Task<double> IncScore(string player, long add)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetIncrementAsync(Name, player, add);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }

            return 0.0;
        }

        /// <summary>
        /// Null if player is not exist
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public async Task<double?> GetScore(string player)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetScoreAsync(Name, player);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<long?> GetRank(string player, bool asc)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetRankAsync(Name, player, asc ? Order.Ascending : Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<RedisValue[]> GetTop(bool asc, int skip = 0, int limit = 30)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetRangeByScoreAsync(Name, skip: skip, take: limit, order: asc ? Order.Ascending : Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<SortedSetEntry[]> GetTopWithScore(bool asc, int skip = 0, int limit = 30)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetRangeByScoreWithScoresAsync(Name, skip: skip, take: limit, order: asc ? Order.Ascending : Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<RedisValue[]> GetRanks(bool asc)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetRangeByRankAsync(Name, order: asc ? Order.Ascending : Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<SortedSetEntry[]> GetTopByRanksWithScore(int fromRank, int toRank, bool asc)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                return await redis.SortedSetRangeByRankWithScoresAsync(Name, start: fromRank, stop: toRank, order: asc ? Order.Ascending : Order.Descending);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return null;
        }

        public async Task<long> TrimLeaderboard(int max, bool asc)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                if (asc)
                {
                    return await redis.SortedSetRemoveRangeByRankAsync(Name, start: max, stop: -1);
                }
                else
                {
                    return await redis.SortedSetRemoveRangeByRankAsync(Name, start: 0, stop: -1 - max);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return 0;
        }

        public async Task<long> TrimLeaderboardByScore(double score, bool asc)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                if (asc)
                {
                    return await redis.SortedSetRemoveRangeByScoreAsync(Name, start: score, stop: double.PositiveInfinity);
                }
                else
                {
                    return await redis.SortedSetRemoveRangeByScoreAsync(Name, start: double.NegativeInfinity, stop: score);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
            return 0;
        }

        public void Clear(bool backup = false)
        {
            try
            {
                var redis = RedisManager.GetRedis();
                if (backup)
                {
                    redis.KeyRename(Name, Name + "@" + DateTime.Now.ToString("yyyy-MM-dd"), flags: CommandFlags.FireAndForget);
                }
                else
                {
                    redis.KeyDelete(Name, CommandFlags.FireAndForget);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RedisLeaderboard error: " + ex.ToString());
            }
        }
    }
}
