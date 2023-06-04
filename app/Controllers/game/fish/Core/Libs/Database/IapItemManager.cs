using BanCa.Redis;
using BanCa.Sql;
using SimpleJSON;
using System;
using System.Threading.Tasks;

namespace BanCa.Libs
{
    public class IapItemManager
    {
        public static async Task<JSONArray> GetIapItems(string appId, long versionNumber)
        {
            var cacheKey = "bc_iap_" + appId + "_" + versionNumber; // bc_iap_com.slotsmachine.bancaclub_1
            var redis = RedisManager.GetRedis();
            var cache = await redis.StringGetAsync(cacheKey);
            var res = default(JSONArray);

            if(!string.IsNullOrEmpty(cache))
            {
                try
                {
                    res = JSON.Parse(cache).AsArray;
                }
                catch(Exception ex)
                {
                    Logger.Error("Error parsing iap cache appId: " + appId + " version " + versionNumber);
                    Logger.Error("Error parsing iap cache: " + ex.ToString());
                }
            }

            if(res == null)
            {
                res = await SqlLogger.GetIapItem(appId, versionNumber);
                if (res.Count > 0)
                {
                    await redis.StringSetAsync(cacheKey, res.ToString());
                    await redis.KeyExpireAsync(cacheKey, DateTime.Now.AddDays(1));
                }
            }

            return res;
        }
    }
}
