using BanCa.Libs;
using BanCa.Libs.Bots;
using BanCa.Redis;
using BanCa.Sql;
using Database;
using Entites.General;
using JWT;
using JWT.Serializers;
using libCoinPaymentsNET;
using Microsoft.Diagnostics.Runtime;
using MySqlProcess.Genneral;
using Nancy;
using Nancy.Extensions;
using PAYHandler;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BanCa.WebService
{
    public class BanCaService : NancyModule
    {
        private const string secret = "cgame";
        private static HashSet<string> allowIps = new HashSet<string> { };

        static BanCaService()
        {
            ConfigJson.OnConfigChange += configChange;
            configChange();
        }

        private static void configChange()
        {
            var whiteList = ConfigJson.Config["allow-ip-service"].AsArray;
            allowIps.Clear();
            for (int i = 0, n = whiteList.Count; i < n; i++)
            {
                string ip = whiteList[i];
                if (!string.IsNullOrEmpty(ip))
                {
                    Logger.Info("GameService add white list: " + ip);
                    allowIps.Add(ip);
                }
            }
        }

        public BanCaService()
        {
            After.AddItemToEndOfPipeline(ctx =>
            {
                string access = "*";
                if (ConfigJson.Config.HasKey("Access-Control-Allow-Origin"))
                    access = ConfigJson.Config["Access-Control-Allow-Origin"].Value;

                ctx.Response.WithHeader("Access-Control-Allow-Origin", access)
                    .WithHeader("Access-Control-Allow-Methods", "*")
                    .WithHeader("Access-Control-Allow-Headers", "*");
            }
           );

            Get("/", parameters =>
            {
                var ip = this.Request.UserHostAddress;
                if (allowIps.Count != 0 && !allowIps.Contains(ip))
                {
                    Logger.Info("Invalid ip request: " + ip);
                }
                return ip;
            });

            Get("/bancaapi/reloadevent/{secret}/{lang}", async parameters =>
            {
                string _secret = parameters.secret;
                string lang = parameters.lang;
                if (string.IsNullOrEmpty(lang))
                {
                    lang = "vi";
                }
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    Logger.Info("Reloading event");
                    try
                    {
                        await MySqlEvent.Reload();
                        Logger.Info("Reload event done");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Reload event fail: " + ex.ToString());
                    }
                    return "{\"code\":200,\"data\":" + (await MySqlEvent.GetList(lang)).ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettopprize/{secret}/{isweek}", async parameters =>
            {
                string _secret = parameters.secret;
                int isweek = parameters.isweek;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + (await RedisManager.GetTopPrize(isweek != 0)).ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/settopprize/{secret}/{isweek}", parameters =>
            {
                string _secret = parameters.secret;
                int isweek = parameters.isweek;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var prizes = JSON.Parse(body).AsArray;
                        RedisManager.SetTopPrize(isweek != 0, prizes);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to settopprize from post: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + Config.ToJsonString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    var bak = Config.ToJson();
                    bool useBak = false;
                    try
                    {
                        var postConfig = JSON.Parse(body);
                        useBak = true;
                        Config.ParseJson(postConfig);
                        RedisManager.SaveConfig();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to set config from post: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        if (useBak)
                        {
                            Config.ParseJson(bak);
                        }
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/reloadevent/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    MySqlEvent.ClearCache();
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettop/{secret}/{isweek}/{limit}/{index}", async parameters =>
            {
                string _secret = parameters.secret;
                int limit = parameters.limit;
                int isweek = parameters.isweek;
                int index = parameters.index;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    if (index >= 0)
                    {
                        return "{\"code\":200,\"data\":" + (isweek != 0 ? (await RedisManager.GetTopWeek(limit)).ToString() : (await RedisManager.GetTopMonth(limit)).ToString()) + "}";
                    }
                    else
                    {
                        var lbItems = await SqlLogger.GetLeaderboardMonthOrWeek(isweek != 0, index);
                        var _data = new JSONArray();
                        for (int i = 0, n = lbItems.Count > limit ? limit : lbItems.Count; i < n; i++)
                        {
                            var _it = lbItems[i];
                            var item = new JSONObject();
                            item["userId"] = _it.UserId;
                            item["username"] = _it.Username;
                            item["avatar"] = _it.Avatar;
                            item["cash"] = _it.CashGain;
                            item["level"] = _it.Level;
                            item["nickname"] = _it.Nickname;
                            item["prize"] = _it.Prize;
                            _data.Add(item);
                        }
                        return "{\"code\":200,\"data\":" + _data.ToString() + "}";
                    }
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/inctop/{secret}/{username}/{changeCash}", async parameters =>
            {
                string _secret = parameters.secret;
                string username = parameters.username;
                long changeCash = parameters.changeCash;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        RedisManager.IncTop(username, changeCash);
                        await RedisManager.SetTopWeek();
                        await RedisManager.SetTopMonth();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to inctop from post: " + ex.ToString());
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getlobbyconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + LobbyConfig.ToJsonString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setlobbyconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    var bak = LobbyConfig.ToJson();
                    bool useBak = false;
                    try
                    {
                        var postConfig = JSON.Parse(body);
                        useBak = true;
                        LobbyConfig.ParseJson(postConfig);
                        RedisManager.SaveLobbyConfig();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to set lbconfig from post: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        if (useBak)
                        {
                            LobbyConfig.ParseJson(bak);
                        }
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            // TODO: test
            Get("/bancaapi/addCash/{secret}/{userid}/{changeCash}/{reasonText}", async parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                long newCash = 0L;
                long changeCash = parameters.changeCash;
                string reasonText = parameters.reasonText;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        newCash = await RedisManager.IncEpicCash(userid, changeCash, "webapi", reasonText, TransType.CMS);
                        if (newCash < 0) // error
                        {
                            return "{\"code\":" + (-newCash) + "}";
                        }

                        var json = new SimpleJSON.JSONObject();
                        json["userid"] = userid;
                        json["newCash"] = newCash;
                        json["changeCash"] = changeCash;
                        json["reason"] = reasonText;

                        var redis = RedisManager.GetRedis();
                        await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userid, json.SaveToCompressedBase64()));

                        MySqlUser.SaveCashToDb(userid, newCash);
                        SqlLogger.LogCashChangeByCms(userid, newCash, changeCash, reasonText);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to updateCash from post: " + ex.ToString());
                        return "{\"code\":405}";
                    }

                    return "{\"code\":200,\"cash\":" + newCash + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/updateCash/{secret}/{userid}/{changeCash}/{reasonText}", async parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                long newCash = 0L;
                long changeCash = parameters.changeCash;
                string reasonText = parameters.reasonText;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        newCash = await RedisManager.IncEpicCash(userid, changeCash, "webapi", reasonText, TransType.CMS);
                        if (newCash < 0) // error
                        {
                            return "{\"code\":" + (-newCash) + "}";
                        }

                        var json = new SimpleJSON.JSONObject();
                        json["userid"] = userid;
                        json["newCash"] = newCash;
                        json["changeCash"] = changeCash;
                        json["reason"] = reasonText;

                        var redis = RedisManager.GetRedis();
                        await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userid, json.SaveToCompressedBase64()));

                        MySqlUser.SaveCashToDb(userid, newCash);
                        SqlLogger.LogCashChangeByCms(userid, newCash, changeCash, reasonText);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to updateCash from post: " + ex.ToString());
                        return "{\"code\":405}";
                    }

                    return "{\"code\":200,\"cash\":" + newCash + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/register/{secret}/{username}/{password}/{platform}/{ip}/{language}", async parameters =>
            {
                string _secret = parameters.secret;
                string username = parameters.username;
                string password = parameters.password;
                string platform = parameters.platform;
                string language = parameters.language;
                string ip = parameters.ip;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    if (!MySqlUser.ValidString(username) || !MySqlUser.ValidString(password) || username.Equals(password))
                    {
                        return "{\"code\":400}";
                    }

                    var res = await MySqlUser.Register(username, password, platform, "", ip, language);
                    var msg2 = new JSONObject();
                    msg2["ok"] = false;
                    if (res.error == 0) // success
                    {
                        return "{\"code\":200}";
                    }
                    else
                    {
                        var code = 300 + res.error;
                        return "{\"code\":" + code + "}";
                    }
                }
                else
                {
                    return "{\"code\":404}";
                }
            });

            Post("/bancaapi/login/{secret}/{username}/{password}", async parameters =>
            {
                string _secret = parameters.secret;
                string username = parameters.username;
                string password = parameters.password;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    if (!MySqlUser.ValidString(username) || !MySqlUser.ValidString(password) || username.Equals(password))
                    {
                        return "{\"code\":400}";
                    }

                    var res = await MySqlUser.Login(username, password);
                    var msg2 = new JSONObject();
                    msg2["ok"] = false;
                    if (res.error == 0) // success
                    {
                        var redisCash = await BanCa.Redis.RedisManager.GetUserCash(res.UserId);
                        res.Cash = redisCash >= 0 ? redisCash : res.Cash;
                        if (redisCash < 0)
                        {
                            RedisManager.SetUserCash(res.UserId, res.Cash);
                        }
                        var player = new Player();
                        player.Id = res.UserId;
                        await SqlLogger.LoadImportantPlayerProperties(res.UserId, player);
                        res.BcLevel = player.Level;
                        res.BcExp = player.Exp;
                        msg2["ok"] = true;
                        msg2["username"] = username;
                        msg2["password"] = password;
                        msg2["userId"] = res.UserId;
                        msg2["avatar"] = res.Avatar;
                        msg2["nickname"] = res.Nickname;
                        msg2["vipId"] = res.VipId;
                        msg2["vipPoint"] = res.VipPoint;
                        msg2["level"] = res.BcLevel;
                        msg2["exp"] = res.BcExp;
                        msg2["phone"] = res.PhoneNumber;
                        msg2["cash"] = res.Cash;
                        return "{\"code\":200,\"data\":" + msg2.ToString() + "}";
                    }
                    else
                    {
                        var code = 300 + res.error;
                        return "{\"code\":" + code + "}";
                    }
                }
                else
                {
                    return "{\"code\":404}";
                }
            });

            Post("/bancaapi/giftcode/{secret}/{username}/{userid}/{giftcode}", parameters =>
            {
                string _secret = parameters.secret;
                string username = parameters.username;
                long userid = parameters.userid;
                string giftcode = parameters.giftcode;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var user = new User();
                    user.UserId = userid;
                    user.Platform = "web";
                    user.DeviceId = "";
                    var msg2 = GiftCodeHandle.GiftCode(user, giftcode);
                    return "{\"code\":200,\"data\":" + msg2.ToString() + "}";
                }
                else
                {
                    return "{\"code\":404}";
                }
            });

            Get("/bancaapi/getbanks/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                //Logger.Info("Getting banks...");
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        return "{\"code\":200,\"data\":" + FundManager.ToJson().ToString() + "}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting banks: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/incbanks/{secret}/{type}/{blind}/{bullet}/{fish}/{value}", parameters =>
            {
                string _secret = parameters.secret;
                int type = parameters.type;
                int blind = parameters.blind;
                var bullet = (Config.BulletType)(int)parameters.bullet;
                var fish = (Config.FishType)(int)parameters.fish;
                double value = parameters.value;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var res = -1.0;
                    if (type == 0)
                    {
                        res = FundManager.IncProfit(blind, bullet, fish, value);
                    }
                    else if (type == 1)
                    {
                        res = FundManager.IncBombFund(blind, bullet, value);
                    }
                    else
                    {
                        res = FundManager.IncFund(blind, bullet, fish, value);
                    }
                    if (res < 0)
                    {
                        return "{\"code\":400}";
                    }

                    return "{\"code\":200,\"cash\":" + res + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setbanks/{secret}/{type}/{key}/{value}", parameters =>
            {
                string _secret = parameters.secret;
                int type = parameters.type;
                int key = parameters.key;
                long value = parameters.value;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    if (type == 0)
                    {
                        FundManager.SetProfit(key, value);
                    }
                    else if (type == 1)
                    {
                        FundManager.SetBombFund(key, value);
                    }
                    else if (type == 2)
                    {
                        FundManager.SetRefundBank(key, value);
                    }
                    else
                    {
                        FundManager.SetJackpot(key, value);
                    }

                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getprofile/{secret}/{userid}", async parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        var user = await MySqlUser.GetUserInfo(userid);
                        if (user == null)
                        {
                            return "{\"code\":401}";
                        }
                        var msg2 = new JSONObject();
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
                        return "{\"code\":200,\"data\":" + msg2.ToString() + "}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting banks: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getplayer/{secret}/{username}", async parameters =>
            {
                string _secret = parameters.secret;
                string username = parameters.username;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        var user = await RedisManager.GetPlayer(username);
                        if (user == null)
                        {
                            return "{\"code\":401}";
                        }
                        return "{\"code\":200,\"data\":" + user.ToJson().ToString() + "}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getplayer: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getjphistory/{secret}/{limit}", async parameters =>
            {
                string _secret = parameters.secret;
                int limit = parameters.limit;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        return "{\"code\":200,\"data\":" + (await SqlLogger.GetJackpotHistory(limit)).ToString() + "}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting jp history: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/loghomemessage/{secret}/{userid}/{nickname}/{cashgain}/{fishtype}/{tableIndex}/{bulletType}/{gameId}", parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                string nickname = parameters.nickname;
                long cashgain = parameters.cashgain;
                int fishtype = parameters.fishtype;
                int tableIndex = parameters.tableIndex;
                Config.BulletType type = (Config.BulletType)((int)parameters.bulletType);
                string gameId = parameters.gameId;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.LogHomeMessages(userid, nickname, cashgain, (Config.FishType)fishtype, tableIndex, type, gameId);

                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            // {userId, title, content}
            Post("/bancaapi/alert/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var json = JSON.Parse(body);
                        //Logger.Info("Alert: " + json.ToString());
                        BanCaLib.PendingAlerts.Enqueue(json);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to show alert: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/alertall/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var json = JSON.Parse(body); // {"content":"msg"}
                        BanCaLib.PushAll("onAlert", json);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to show alert all: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setjpusers/{secret}/{gameId}/{name}", parameters =>
                    {
                        string _secret = parameters.secret;
                        string gameId = parameters.gameId;
                        string name = parameters.name;
                        if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                        {
                            string body = this.Request.Body.AsString();
                            try
                            {
                                var json = JSON.Parse(body);
                                var data = json["data"].AsArray;
                                Logger.Info("Jp users: " + json.ToString());
                                    RedisManager.SetJpUsers(data);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Fail to set jp users: " + ex.ToString());
                                Logger.Error("Body: " + body);
                                return "{\"code\":405}";
                            }
                            return "{\"code\":200}";
                        }
                        return "{\"code\":404}";
                    });

            Get("/bancaapi/getjpusers/{secret}/{gameId}", parameters =>
            {
                string _secret = parameters.secret;
                string gameId = parameters.gameId;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        return "{\"code\":200,\"data\":" + (RedisManager.GetJpUsers().ToString()) + "}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting jpusers: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/reloadblacklist/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        RedisManager.LoadBlackList();
                        return "{\"code\":200}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error reloadblacklist: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/addblackip/{secret}/{ip}", parameters =>
            {
                string _secret = parameters.secret;
                string ip = parameters.ip;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.AddBlackIp(ip);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/addblackdevice/{secret}/{did}", parameters =>
            {
                string _secret = parameters.secret;
                string did = parameters.did;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.AddBlackDeviceId(did);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/removeblackip/{secret}/{ip}", parameters =>
            {
                string _secret = parameters.secret;
                string ip = parameters.ip;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.RemoveBlackIp(ip);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/removeblackdevice/{secret}/{did}", parameters =>
            {
                string _secret = parameters.secret;
                string did = parameters.did;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.RemoveBlackDeviceId(did);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getblackips/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var data = RedisManager.GetBlackIp();
                    return "{\"code\":200, \"data\":" + data.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getblackdevices/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var data = RedisManager.GetBlackDeviceIds();
                    return "{\"code\":200, \"data\":" + data.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/kick/{secret}/{userid}", async parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        var redis = RedisManager.GetRedis();
                        await redis.PublishAsync("bc_cmd", string.Format("kick {0}", userid));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to kick from post: " + ex.ToString());
                        return "{\"code\":405}";
                    }

                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/listtable/{secret}", async (parameters, ct) =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var servers = BanCaLib.bancaServerInstances;
                    if (servers != null)
                    {
                        int count = 0;
                        Dictionary<int, List<int>> saved = new Dictionary<int, List<int>>();
                        //Monitor.Enter(servers);
                        for (int i = 0; i < servers.Count; i++)
                        {
                            var server = servers[i];
                            count++;
                            await Task.Run(() =>
                            {
                                server.GetListTable((serverId, list) =>
                                {
                                    count--;
                                    lock (saved)
                                    {
                                        saved[server.Port] = list;
                                    }
                                });
                            });
                        }
                        //Monitor.Exit(servers);

                        int time = 0;
                        while (count != 0)
                        {
                            Thread.Sleep(100);
                            time += 100;

                            if (time > 10000)
                            {
                                return "{\"code\":401}";
                            }
                        }

                        var jsonarr = new JSONArray();
                        foreach (var pair in saved)
                        {
                            foreach (var id in pair.Value)
                            {
                                var json = new JSONObject();
                                json["server"] = pair.Key;
                                json["id"] = id;
                                jsonarr.Add(json);
                            }
                        }
                        return "{\"code\":200, \"data\":" + jsonarr.ToString() + "}";
                    }

                    return "{\"code\":400}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettable/{secret}/{tableid}", async (parameters, ct) =>
            {
                string _secret = parameters.secret;
                int tableid = parameters.tableid;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var servers = BanCaLib.bancaServerInstances;
                    if (servers != null)
                    {
                        int count = 0;
                        JSONNode result = null;
                        //Monitor.Enter(servers);
                        for (int i = 0; i < servers.Count; i++)
                        {
                            var server = servers[i];
                            count++;
                            await Task.Run(() =>
                            {
                                server.GetTable(tableid, (json) =>
                                {
                                    count--;
                                    if (json != null)
                                    {
                                        result = json;
                                    }
                                });
                            });
                        }
                        //Monitor.Exit(servers);

                        int time = 0;
                        while (count != 0)
                        {
                            Thread.Sleep(100);
                            time += 100;

                            if (time > 10000)
                            {
                                return "{\"code\":401}";
                            }
                        }

                        if (result == null)
                        {
                            return "{\"code\":403}";
                        }

                        return "{\"code\":200, \"data\":" + result.ToString() + "}";
                    }

                    return "{\"code\":400}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getonlines/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var servers = BanCaLib.bancaServerInstances;
                    if (servers != null)
                    {
                        JSONNode result = new JSONObject();
                        for (int i = 0; i < servers.Count; i++)
                        {
                            var server = servers[i];
                            result[server.Port.ToString()] = server.GetOnlines();
                        }

                        return "{\"code\":200, \"data\":" + result.ToString() + "}";
                    }

                    return "{\"code\":400}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gethackers/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret)
                {
                    var hackers = await RedisManager.GetHackers();
                    return "{\"code\":200, \"data\":" + hackers.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getstat/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret)
                {
                    var json = new JSONObject();
                    {
                        System.Diagnostics.Process procces = System.Diagnostics.Process.GetCurrentProcess();
                        System.Diagnostics.ProcessThreadCollection threadCollection = procces.Threads;

                        var strBuilder = new StringBuilder();
                        strBuilder.Append("Number of threads: ");
                        strBuilder.Append(threadCollection.Count);
                        strBuilder.AppendLine(" - stats:");
                        foreach (System.Diagnostics.ProcessThread proccessThread in threadCollection)
                        {
                            strBuilder.AppendLine(string.Format("Id: {0}, State: {1}, Start: {2}, Total: {3}, User: {4}, Address: {5}, Wait: \"{6}\"",
                                proccessThread.Id, proccessThread.ThreadState, proccessThread.StartTime,
                                proccessThread.TotalProcessorTime, proccessThread.UserProcessorTime, proccessThread.StartAddress,
                                proccessThread.ThreadState == System.Diagnostics.ThreadState.Wait ? proccessThread.WaitReason.ToString() : ""));
                        }

                        json["threads"] = strBuilder.ToString();
                    }
                    using (DataTarget target = DataTarget.AttachToProcess(System.Diagnostics.Process.GetCurrentProcess().Id, 5000, AttachFlag.Passive))
                    {
                        var strBuilder = new StringBuilder();
                        strBuilder.AppendLine("Thread traces: ");
                        ClrRuntime runtime = target.ClrVersions.First().CreateRuntime();
                        foreach (ClrAppDomain domain in runtime.AppDomains)
                        {
                            strBuilder.AppendLine(string.Format("ID:      {0}", domain.Id));
                            strBuilder.AppendLine(string.Format("Name:    {0}", domain.Name));
                            strBuilder.AppendLine(string.Format("Address: {0}", domain.Address));
                        }

                        foreach (ClrThread thread in runtime.Threads)
                        {
                            if (!thread.IsAlive)
                                continue;

                            strBuilder.AppendLine(string.Format("Thread {0}:", thread.OSThreadId));

                            foreach (ClrStackFrame frame in thread.StackTrace)
                                strBuilder.AppendLine(string.Format("{0,12:X} {1,12:X} {2}", frame.StackPointer, frame.InstructionPointer, frame.ToString()));
                        }

                        json["traces"] = strBuilder.ToString();
                    }
                    return "{\"code\":200, \"data\":" + json.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/incwrongcard/{secret}/{userid}", async parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var count = await RedisManager.IncWrongCard(userid);
                    return "{\"code\":200, \"count\":" + count + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/addcoinevent/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var json = JSON.Parse(body);
                        RedisManager.AddCointEvent(json["amount"].AsLong, json["min"].AsLong, json["durationMs"].AsInt, json["message"]);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to add coin event: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getcoinevent/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var json = await RedisManager.GetCoinEvent();
                    return "{\"code\":200, \"data\":" + json.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/addblockcashout/{secret}/{userId}", parameters =>
            {
                string _secret = parameters.secret;
                long userId = parameters.userId;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.AddBlockCashOut(userId);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/removeblockcashout/{secret}/{userId}", parameters =>
            {
                string _secret = parameters.secret;
                long userId = parameters.userId;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.RemoveBlockCashOut(userId);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getblockcashout/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var json = await RedisManager.GetBlockCashOutList();
                    return "{\"code\":200, \"data\":" + json.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/addreloginevent/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var json = JSON.Parse(body);
                        RedisManager.SetReloginEvent(json["days"].AsInt, json["amount"].AsLong, json["cashIn"].AsInt, json["message"]);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to set relogin event: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getreloginevent/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var json = await RedisManager.GetReloginEvent();
                    return "{\"code\":200, \"data\":" + json.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/switchcheckduplogin/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var check = !RedisManager.CheckDupplicateLogin;
                    RedisManager.CheckDupplicateLogin = check;
                    return "{\"code\":200, \"check\":" + check + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getplayerserver/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var data = RedisManager.GetPlayerServerJson();
                    return "{\"code\":200, \"data\":" + data + "}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/clearplayerserver/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.ClearPlayerServer();
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/removeplayerserver/{secret}/{userId}", parameters =>
            {
                string _secret = parameters.secret;
                long userId = parameters.userId;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.RemovePlayerServer(userId);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/smscallback", async parameters =>
            {
                try
                {
                    string data = this.Request.Body.AsString();
                    Logger.Info("Receive smscallback data\n" + data);
                    var jdata = JSON.Parse(data);
                    var transactionNo = jdata["transactionNo"].Value; // Mã giao dịch duy nhất với từng giao dịch
                    var contentId = jdata["contentId"].Value; // Mã nội dung trừ tiền (Ví dụ NAP1, NAP10)
                    var contentCode = jdata["contentCode"].Value; // Tên game, nội dung ddược đăng ký trên cổng thanh toán
                    var totalAmount = jdata["totalAmount"].AsLong; // Số tiền trừ
                    var userId = jdata["account"].AsLong; // Tài khoản trong game của CP
                    var isdn = jdata["isdn"].Value; // Số thuê bao trừ tiền
                    var result = jdata["result"].AsInt; // Kết quả trừ tiền (1: thành công, 0: thất bại)
                    var mode = jdata["mode"].Value; // Loại giao dịch gồm 2 loại: -check: giao dịch kiểm tra xem tài khoản useringame có tồn tại không(nếu có mới có giao dịch real); - real: Giao dịch ghi sản lượng thật, đã trừ tiền người dùng
                    var syncUrl = jdata["syncUrl"].Value; // API URL mà Đối tác đã cung cấp cho VNNPLUS
                    var telco = jdata["telco"].Value; // Nhà mạng: VTT,VMS,VNP

                    try
                    {
                        var config = LobbyConfig.GetDefaultConfig();
                        if ("real" == mode)
                        {
                            var smsItem = config.FindSmsItem(totalAmount, telco);
                            if (smsItem != null)
                            {
                                var reasonText = "smscallback:" + telco + "/" + smsItem.Cash;
                                var newCash = await RedisManager.IncEpicCash(userId, smsItem.Cash, "system",
                                    reasonText, TransType.SMS_IN, realmoney: totalAmount, rate: smsItem.Cash);
                                if (newCash < 0) // error
                                {
                                    Logger.Info("Sms callback inc cash fail, result {0}", newCash);
                                    return "{\"Status\":1, \"Description\":\"Failure\"}";
                                }
                                MySqlUser.SaveCashToDb(userId, newCash);
                                SqlLogger.LogCashChangeByCms(userId, newCash, smsItem.Cash, reasonText);
                                await RedisManager.CardInInc(userId, totalAmount); // not need to update user as client will relogin
                                var json2 = new SimpleJSON.JSONObject();
                                json2["userid"] = userId;
                                json2["newCash"] = newCash;
                                json2["changeCash"] = smsItem.Cash;
                                json2["reason"] = "Nạp sms thành công";

                                var redis = RedisManager.GetRedis();
                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                                return "{\"Status\":0, \"Description\":\"Success\"}";
                            }
                            else
                            {
                                Logger.Info("Sms item not found: amount {0}, telco {1}", totalAmount, telco);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to updateCash from post smscallback: " + ex.ToString());
                        return "{\"Status\":1, \"Description\":\"Failure\"}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in smscallback " + ex.ToString());
                }
                return "{\"Status\":1, \"Description\":\"Failure\"}";
            });

            Get("/bancaapi/momocallback", async parameters =>
            {
                try
                {
                    string SecretKey = EpicApi.MoMoSecretKey;
                    string requestTime = this.Request.Query["requestTime"]; // yyyyMMddHHmmss
                    string momo_transId = this.Request.Query["momo_transId"];
                    string message = this.Request.Query["message"]; // syntax "scv" + uid
                    string money = this.Request.Query["money"];
                    string phone = this.Request.Query["phone"];
                    string type = this.Request.Query["type"]; // 2 - momo
                    string authKey = this.Request.Query["authKey"]; // MD5(requestTime + "|" + message + "|" + money + "|" + phone + "|" + SecretKey)
                    // response { errorCode (0-success), errorDescription (string) }

                    Logger.Info("Receive momocallback requestTime " + requestTime + " momo_transId " + momo_transId + " message " + message
                        + "\n money " + money + " phone " + phone + " type " + type + "\n authKey " + authKey);
                    var md5 = EpicApi.Md5(requestTime + "|" + message + "|" + money + "|" + phone + "|" + SecretKey);

                    //if (Md5(apiKey + tranid).Equals(signature))
                    if (authKey != null && md5.Equals(authKey) && message != null && message.StartsWith("scv", StringComparison.OrdinalIgnoreCase))
                    {
                        //Update trạng thái thẻ
                        try
                        {
                            var config = LobbyConfig.GetDefaultConfig();
                            var userId = long.Parse(message.Substring(3));
                            var rm = long.Parse(money);
                            var add = (long)Math.Round(rm * config.MomoCashInRate);
                            var reasonText = "momo-charge";
                            var newCash = await RedisManager.IncEpicCash(userId, add, "momo " + momo_transId + " " + message + " " + phone, reasonText, TransType.MOMO_IN, realmoney: rm, rate: config.MomoCashInRate);
                            if (newCash < 0) // error
                            {
                                return "{\"errorCode\":1, \"errorDescription\":\"fail, code " + newCash + "\"}";
                            }
                            var json2 = new SimpleJSON.JSONObject();
                            json2["userid"] = userId;
                            json2["newCash"] = newCash;
                            json2["changeCash"] = add;
                            json2["reason"] = "Nạp MoMo thành công";
                            await RedisManager.MoMoInc(userId, add); // not need to update user as client will relogin
                            var redis = RedisManager.GetRedis();
                            await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                            MySqlUser.SaveCashToDb(userId, newCash);
                            SqlLogger.LogCashChangeByCms(userId, newCash, add, reasonText);
                            return "{\"errorCode\":0, \"errorDescription\":\"ok\"}";
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post momocallback: " + ex.ToString());
                            return "{\"errorCode\":2, \"errorDescription\":\"Fail ex\"}";
                        }
                    }
                    else
                    {
                        //"sai chữ kí";
                        return "{\"errorCode\":3, \"errorDescription\":\"Fail sign\"}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in momocallback " + ex.ToString());
                }
                return "{\"errorCode\":4, \"errorDescription\":\"Fail\"}";
            });

            Get("/bancaapi/momoinfoupdate", parameters =>
            {
                try
                {
                    string SecretKey = EpicApi.MoMoSecretKey;
                    string updateTime = this.Request.Query["updateTime"]; // yyyyMMddHHmmss
                    string type = this.Request.Query["type"]; // 2 - momo
                    string info = this.Request.Query["info"]; // base 64
                    string authKey = this.Request.Query["authKey"]; // MD5(updateTime +"|"+type+"|"+SecretKey)
                    // response { errorCode (0-success), errorDescription (string) }

                    Logger.Info("Receive momoinfoupdate updateTime " + updateTime + " info " + info
                        + "\n type " + type + "\n authKey " + authKey);
                    var md5 = EpicApi.Md5(updateTime + "|" + type + "|" + SecretKey);

                    //if (Md5(apiKey + tranid).Equals(signature))
                    if (authKey != null && md5.Equals(authKey))
                    {
                        RedisManager.ClearMomoInfoCache();

                        //Update trạng thái thẻ
                        try
                        {
                            var data = EpicApi.Base64Decode(info);
                            BanCaLib.PushAll("onUpdateMomoInfo", JSON.Parse(data));
                            return "{\"errorCode\":0, \"errorDescription\":\"ok\"}";
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post momocallback: " + ex.ToString());
                            return "{\"errorCode\":1, \"errorDescription\":\"Fail ex\"}";
                        }
                    }
                    else
                    {
                        //"sai chữ kí";
                        return "{\"errorCode\":2, \"errorDescription\":\"Fail sign\"}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in momoinfoupdate " + ex.ToString());
                }
                return "{\"errorCode\":3, \"errorDescription\":\"Fail\"}";
            });

            Get("/bancaapi/muacardcallback", async parameters =>
            {
                try
                {
                    string tranid = this.Request.Query["trans_id"];
                    long menhGiaThat = this.Request.Query["amount"];
                    int status = this.Request.Query["status"]; // status: Trạng thái thẻ (3: Nạp thành công, 4: Thẻ sai, 5: Thẻ bị thu hồi do sai mệnh giá)
                    string signature = this.Request.Query["signature"];
                    string description = this.Request.Query["description"];

                    Logger.Info("Receive muacardcallback transid " + tranid + " amount " + menhGiaThat + " status " + status + " sign " + signature + " des " + description);

                    string apiKey = "543f51e72b0c0979ca18736b19042633";
                    if (EpicApi.Md5(apiKey + tranid).Equals(signature))
                    {
                        //Update trạng thái thẻ
                        try
                        {
                            var config = LobbyConfig.GetDefaultConfig();
                            if (status == 3)
                            {
                                SqlLogger.UpdateRequestCard(tranid, true, status.ToString(), menhGiaThat);
                                var rc = await SqlLogger.GetRequestCard(tranid);
                                var userId = rc["user_id"].AsLong;
                                var card_type = rc["card_type"].Value;
                                var totalCardIn = await RedisManager.GetCardInCount(userId);
                                var bonus = config.GetCashInBonus(card_type, menhGiaThat, totalCardIn == 0);
                                var add = (long)Math.Round(menhGiaThat * (config.CashInRate * bonus));
                                var reasonText = "muacardcallback:" + config.CashInRate + " + " + bonus;
                                var newCash = await RedisManager.IncEpicCash(userId, add, "system",
                                    reasonText, TransType.CARD_IN, realmoney: menhGiaThat, rate: config.CashInRate + bonus);
                                if (newCash < 0) // error
                                {
                                    return "{\"status\":0, \"message\":\"fail, code " + newCash + "\"}";
                                }
                                await RedisManager.CardInInc(userId, add); // not need to update user as client will relogin
                                var json2 = new SimpleJSON.JSONObject();
                                json2["userid"] = userId;
                                json2["newCash"] = newCash;
                                json2["changeCash"] = add;
                                json2["reason"] = "Nạp thẻ thành công";

                                var redis = RedisManager.GetRedis();
                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                                MySqlUser.SaveCashToDb(userId, newCash);
                                SqlLogger.LogCashChangeByCms(userId, newCash, add, reasonText);
                                return "{\"status\":1, \"message\":\"ok\"}";
                            }
                            else
                            {
                                SqlLogger.UpdateRequestCard(tranid, false, status.ToString(), menhGiaThat);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post muacardcallback: " + ex.ToString());
                            return "{\"status\":0, \"message\":\"Fail ex\"}";
                        }
                    }
                    else
                    {
                        //"sai chữ kí";
                        SqlLogger.UpdateRequestCard(tranid, false, status.ToString(), menhGiaThat);
                        return "{\"status\":1, \"message\":\"Fail sign\"}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in muacardcallback " + ex.ToString());
                }
                return "{\"status\":1, \"message\":\"Fail\"}";
            });

            Post("/bancaapi/push247callback", async parameters =>
            {
                try
                {
                    string data = this.Request.Body.AsString();
                    Logger.Info("Receive push247callback data\n" + data);
                    var jdata = JSON.Parse(data);
                    var data2 = jdata["data"].AsObject;
                    string tranid = data2["tranid"];
                    string cardSerial = data2["cardSerial"];
                    string cardType = data2["cardType"];
                    long cardPrice = data2["cardPrice"];
                    long menhGiaNhapVao = data2["menhGiaNhapVao"];
                    long menhGiaThat = data2["menhGiaThat"];

                    //00: thành công
                    //24: thẻ đã dùng rồi
                    //07: thẻ sai
                    //08: hệ thống bận
                    //99: hệ thống đang xử lý
                    string statuscode = data2["statuscode"];

                    string apiKey = "ec8eed35d7434e541842c22b723ae378";
                    string sign = jdata["sign"];
                    if (EpicApi.Md5(apiKey + tranid + cardSerial + cardType + menhGiaNhapVao + statuscode).Equals(sign))
                    {
                        //Update trạng thái thẻ
                        try
                        {
                            var config = LobbyConfig.GetDefaultConfig();
                            if (statuscode == "00")
                            {
                                SqlLogger.UpdateRequestCard(tranid, true, statuscode, menhGiaThat);
                                var rc = await SqlLogger.GetRequestCard(tranid);
                                var userId = rc["user_id"].AsLong;
                                var card_type = rc["card_type"].Value;
                                var totalCardIn = await RedisManager.GetCardInCount(userId);
                                var bonus = config.GetCashInBonus(card_type, menhGiaThat, totalCardIn == 0);
                                var add = (long)Math.Round(menhGiaThat * (config.CashInRate * bonus));
                                var reasonText = "push247callback:" + config.CashInRate + " + " + bonus;
                                var newCash = await RedisManager.IncEpicCash(userId, add, "system",
                                    reasonText, TransType.CARD_IN, realmoney: menhGiaThat, rate: config.CashInRate + bonus);
                                if (newCash < 0) // error
                                {
                                    return "{\"status\":0, \"message\":\"fail, code " + newCash + "\"}";
                                }
                                await RedisManager.CardInInc(userId, add); // not need to update user as client will relogin
                                var json2 = new SimpleJSON.JSONObject();
                                json2["userid"] = userId;
                                json2["newCash"] = newCash;
                                json2["changeCash"] = add;
                                json2["reason"] = "Nạp thẻ thành công";

                                var redis = RedisManager.GetRedis();
                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                                MySqlUser.SaveCashToDb(userId, newCash);
                                SqlLogger.LogCashChangeByCms(userId, newCash, add, reasonText);
                                return "{\"status\":1, \"message\":\"ok\"}";
                            }
                            else
                            {
                                SqlLogger.UpdateRequestCard(tranid, false, statuscode, menhGiaThat);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post push247callback: " + ex.ToString());
                            return "{\"status\":0, \"message\":\"Fail ex\"}";
                        }
                    }
                    else
                    {
                        //"sai chữ kí";
                        SqlLogger.UpdateRequestCard(tranid, false, statuscode, menhGiaThat);
                        return "{\"status\":1, \"message\":\"Fail sign\"}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in push247callback " + ex.ToString());
                }
                return "{\"status\":1, \"message\":\"Fail\"}";
            });

            Get("/bancaapi/card-charge-callback", async parameters =>
            {
                //string data = parameters.data;
                string data = this.Request.Query["data"];
                Logger.Info("card-charge-callback data: " + data);
                if (true)
                //if ((allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string token = data;
                    string secret = "kD9g5MrGqExhPQnRZBvFsPDtVEmVJLDx";

                    try
                    {
                        IJsonSerializer serializer = new JsonNetSerializer();
                        IDateTimeProvider provider = new UtcDateTimeProvider();
                        IJwtValidator validator = new JwtValidator(serializer, provider);
                        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                        IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                        var json = JSON.Parse(decoder.Decode(token, secret, verify: true));
                        var status = json["status"].AsInt;
                        long userId = json["user"].AsLong;
                        long add = json["gold"].AsLong;
                        string msg = json["msg"];
                        json["userid"] = userId;
                        Logger.Info("card-charge-callback decode data: " + json.ToString());
                        // {userId, title, content}
                        if (json["status"].AsInt == 1)
                        {
                            try
                            {
                                var reasonText = "card-charge";
                                var newCash = await RedisManager.IncEpicCash(userId, add, "webapi", reasonText, TransType.CARD_IN);
                                if (newCash < 0) // error
                                {
                                    return "{\"status\":0, \"message\":\"fail, code " + newCash + "\"}";
                                }
                                await RedisManager.CardInInc(userId, add); // not need to update user as client will relogin
                                var json2 = new SimpleJSON.JSONObject();
                                json2["userid"] = userId;
                                json2["newCash"] = newCash;
                                json2["changeCash"] = add;
                                json2["reason"] = msg;

                                var redis = RedisManager.GetRedis();
                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                                MySqlUser.SaveCashToDb(userId, newCash);
                                SqlLogger.LogCashChangeByCms(userId, newCash, add, reasonText);
                                return "{\"status\":1, \"message\":\"ok\"}";
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Fail to updateCash from post card-charge-callback: " + ex.ToString());
                                return "{\"status\":0, \"message\":\"Fail ex\"}";
                            }
                        }
                    }
                    catch (TokenExpiredException tee)
                    {
                        Logger.Error("Token has expired: " + data + "\n" + tee.ToString());
                        return "{\"status\":0, \"message\":\"Token has expired\"}";
                    }
                    catch (SignatureVerificationException sve)
                    {
                        Logger.Error("Token has invalid signature: " + data + "\n" + sve.ToString());
                        return "{\"status\":0, \"message\":\"Token has invalid signature\"}";
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("card-charge-callback error: " + data + "\n" + ex.ToString());
                        return "{\"status\":0, \"message\":\"Fail ex2\"}";
                    }
                }
                return "{\"status\":0, \"message\":\"fail\"}";
            });

            Post("/yoyoapi/vin-pay-callback", async parameters =>
            {
                if (true)
                //if ((allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        //return Response.AsJson(responseThing);
                        Logger.Info("vin-pay-callback data code {0}, message {1}, id {2}, seri {3}, telco {4}, amount {5}",
                            Request.Form["i_errorcode"], Request.Form["i_message"], Request.Form["i_idref"], Request.Form["i_serial"], Request.Form["i_telco"], Request.Form["i_amount"]);
                        int statuscode = Request.Form["i_errorcode"];
                        string msg = Request.Form["i_message"];
                        string tranid = Request.Form["i_idref"];
                        string cardSerial = Request.Form["i_serial"];
                        string cardType = Request.Form["i_telco"];
                        long menhGiaNhapVao, menhGiaThat;
                        long cardPrice = menhGiaNhapVao = menhGiaThat = long.Parse((string)Request.Form["i_amount"]);

                        //Update trạng thái thẻ
                        try
                        {
                            var config = LobbyConfig.GetDefaultConfig();
                            if (statuscode == 0)
                            {
                                SqlLogger.UpdateRequestCard(tranid, true, statuscode.ToString(), menhGiaThat);
                                var rc = await SqlLogger.GetRequestCard(tranid);
                                var userId = rc["user_id"].AsLong;
                                var card_type = rc["card_type"].Value;
                                var totalCardIn = await RedisManager.GetCardInCount(userId);
                                var bonus = config.GetCashInBonus(card_type, menhGiaThat, totalCardIn == 0);
                                var add = (long)Math.Round(menhGiaThat * (config.CashInRate * bonus));
                                var reasonText = "vin-pay-callback:" + config.CashInRate + " + " + bonus;
                                var newCash = await RedisManager.IncEpicCash(userId, add, "system",
                                    reasonText, TransType.CARD_IN, realmoney: menhGiaThat, rate: config.CashInRate + bonus);
                                if (newCash < 0) // error
                                {
                                    return "{\"status\":0, \"message\":\"fail, code " + newCash + "\"}";
                                }
                                await RedisManager.CardInInc(userId, add); // not need to update user as client will relogin
                                var json2 = new SimpleJSON.JSONObject();
                                json2["userid"] = userId;
                                json2["newCash"] = newCash;
                                json2["changeCash"] = add;
                                json2["reason"] = "Nạp thẻ thành công";

                                var redis = RedisManager.GetRedis();
                                await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userId, json2.SaveToCompressedBase64()));

                                MySqlUser.SaveCashToDb(userId, newCash);
                                SqlLogger.LogCashChangeByCms(userId, newCash, add, reasonText);
                                return "{\"status\":1, \"message\":\"ok\"}";
                            }
                            else
                            {
                                SqlLogger.UpdateRequestCard(tranid, false, statuscode.ToString(), menhGiaThat);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post push247callback: " + ex.ToString());
                            return "{\"status\":0, \"message\":\"Fail ex\"}";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("vin-pay-callback error: \n" + ex.ToString());
                        return "{\"status\":0, \"message\":\"Fail ex2\"}";
                    }
                }
                return "{\"status\":0, \"message\":\"fail\"}";
            });

            Get("/bancaapi/getdailygifts/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var data = await RedisManager.GetDailyFreeGoldList();
                    return "{\"code\":200, \"data\":" + data.ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setdailygifts/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var gifts = JSONArray.Parse(body).AsArray;
                        RedisManager.SetDailyFreeGoldList(gifts);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to setdailygifts: " + ex.ToString());
                        return "{\"code\":403}";
                    }

                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getbotconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + BotConfig.ToJson().ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setbotconfig/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    var bak = BotConfig.ToJson();
                    bool useBak = false;
                    try
                    {
                        var postConfig = JSON.Parse(body);
                        useBak = true;
                        BotConfig.ParseJson(postConfig);
                        RedisManager.SaveBotConfig();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to set bot config from post: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        if (useBak)
                        {
                            BotConfig.ParseJson(bak);
                        }
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/reloadnicknames/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                int type = parameters.type;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    BotNickname.ReloadNicknames();
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/executesql/{sqlfile}", parameters =>
            {
                string sqlfile = parameters.sqlfile;
                int type = parameters.type;
                var add = this.Request.UserHostAddress;
                if ("localhost".Equals(add) || "127.0.0.1".Equals(add) || "::1".Equals(add))
                {
                    try
                    {
                        SqlLogger.executeNonQuery(System.IO.File.ReadAllText(sqlfile));
                        return "200";
                    }
                    catch (Exception ex)
                    {
                        return "Error: " + ex.ToString();
                    }
                }
                return "404";
            });

            Post("/bancaapi/seteventcashintime/{secret}/{start}/{end}", parameters =>
            {
                string _secret = parameters.secret;
                string start = parameters.start;
                string end = parameters.end;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.SetEventCashinTime(start, end);

                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/geteventcashintime/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    var res = new JSONObject();
                    res["code"] = 200;
                    res["start"] = await RedisManager.EventCashinStart();
                    res["end"] = await RedisManager.EventCashinEnd();
                    return res.ToString();
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettopcashinprize/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + (await RedisManager.GetTopCashInPrize()).ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/settopcashinprize/{secret}", parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    string body = this.Request.Body.AsString();
                    try
                    {
                        var prizes = JSON.Parse(body).AsArray;
                        RedisManager.SetTopCashInPrize(prizes);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to settopcashinprize from post: " + ex.ToString());
                        Logger.Error("Body: " + body);
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettopcashin/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + (await SqlLogger.GetTopEventCashIn()).ToString() + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/changepassword/{secret}/{userid}/{password}", parameters =>
            {
                string _secret = parameters.secret;
                long userid = parameters.userid;
                string password = parameters.password;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    bool ok = MySqlUser.ChangePassword(userid, password);
                    return ok ? "{\"code\":200}" : "{\"code\":400}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/terminate", async parameters =>
            {
                if (this.Request.IsLocal())
                {
                    try
                    {
                        var redis = RedisManager.GetRedis();
                        await redis.PublishAsync("bc_cmd", "terminate");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Fail to publish terminate: " + ex.ToString());
                        return "{\"code\":405}";
                    }
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/isautocardout/{secret}", async parameters =>
            {
                string _secret = parameters.secret;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    return "{\"code\":200,\"data\":" + (await RedisManager.IsCardOutAuto()) + "}";
                }
                return "{\"code\":404}";
            });

            Post("/bancaapi/setautocardout/{secret}/{isauto01}", parameters =>
            {
                string _secret = parameters.secret;
                int isauto01 = parameters.isauto01;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    RedisManager.SetCardOutAuto(isauto01 != 0);
                    return "{\"code\":200}";
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/gettranshistory/{secret}/{nickname}", async parameters =>
            {
                string _secret = parameters.secret;
                string nickname = parameters.nickname;
                if (secret == _secret && (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress))))
                {
                    try
                    {
                        var res = new JSONObject();
                        res["code"] = 200;
                        res["data"] = await MySqlUser.GetTranHistories(nickname, 100);
                        return res.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error gettranshistory: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });

            Get("/bancaapi/getltctaddress", async parameters =>
            {
                if (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress)))
                {
                    try
                    {
                        var res = new JSONObject();
                        res["code"] = 200;
                        string ipn = "";
                        var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                        if (ipHostInfo.AddressList.Length > 0)
                        {
                            var ip = "149.28.138.254"; // TODO
                            var port = ConfigJson.Config["webservice-port"].Value;
                            ipn = string.Format("http://{0}:{1}/bancaapi/coinpaymentipn", ip, port);
                            res["ipn"] = ipn;
                        }
                        res["data"] = await CoinPayments.GetCallbackAddress(ipn, "LTCT");
                        var rep = res.ToString();
                        Logger.Info("getbtcaddress rep: " + rep);
                        return rep;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getbtcaddress: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });
            Get("/bancaapi/getbtcaddress/{userid}/{coin}", async parameters =>
            {
                string userid = parameters.userid;
                string coin = parameters.coin; // BTC LTC ETH TUSD USDC USDT.ERC20
                if (allowIps.Count == 0 || (allowIps.Count > 0 && allowIps.Contains(this.Request.UserHostAddress)))
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
                        res["data"] = await CoinPayments.GetCallbackAddress(ipn, coin);
                        var rep = res.ToString();
                        Logger.Info("getbtcaddress rep: " + rep);
                        return rep;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getbtcaddress: " + ex.ToString());
                        return "{\"code\":400}";
                    }
                }
                return "{\"code\":404}";
            });
            Post("/bancaapi/coinpaymentipn/{userid}", async parameters =>
            {
                long userid = parameters.userid;
                string body = this.Request.Body.AsString();
                Logger.Info("coinpaymentipn body: " + body);
                //body = "address=33wFFFgVjnk1RKagsGU7cwxWSShbKBVWMs&amount=0.00021812&amounti=21812&confirms=2&currency=BTC&deposit_id=CDEFK5001O2Q5EZPG5J9GF1238&fee=0.00000109&feei=109&fiat_amount=1.99708568&fiat_amounti=199708568&fiat_coin=USD&fiat_fee=0.00997993&fiat_feei=997993&ipn_id=8f0fc23ccd174290a9bcf58a007be5d5&ipn_mode=hmac&ipn_type=deposit&ipn_version=1.0&merchant=828b72432670da1a67dc7e6a913a43e5&status=100&status_text=Deposit+confirmed&txn_id=87956139d4276479c0f23a180e36884a2d1e8a6f7d99ecfe8c5e1e7a3e5c1a38";
                var pairs = body.Split('&');
                var data = new JSONObject();
                foreach (var pair in pairs)
                {
                    var keyValues = pair.Split('=');
                    if (keyValues.Length > 1)
                    {
                        data[keyValues[0].Trim()] = keyValues[1].Trim();
                    }
                }
                Logger.Info("Result: " + data.ToString());

                double usdPrice = ConfigJson.Config["usd-price"];
                if (data.HasKey("status"))
                {
                    var status = data["status"].AsInt;
                    if (status == 2 || status >= 100) // ok
                    {
                        try
                        {
                            var changeCash = (long)Math.Round(data["fiat_amount"].AsDouble * usdPrice);
                            string reasonText = "coinpaymentipn";
                            var newCash = await RedisManager.IncEpicCash(userid, changeCash, "coin,add:" + data["address"].Value + ",amount:" + data["amount"].Value,
                                reasonText, TransType.COIN_PAYMENT_IPN);
                            if (newCash < 0) // error
                            {
                                return "{\"code\":" + (-newCash) + "}";
                            }

                            var json = new JSONObject();
                            json["userid"] = userid;
                            json["newCash"] = newCash;
                            json["changeCash"] = changeCash;
                            json["reason"] = reasonText;

                            var redis = RedisManager.GetRedis();
                            await redis.PublishAsync("bc_cmd", string.Format("reloadCash {0} {1}", userid, json.SaveToCompressedBase64()));

                            MySqlUser.SaveCashToDb(userid, newCash);
                            SqlLogger.LogCashChangeByCms(userid, newCash, changeCash, reasonText);
                            return "{\"code\":200}";
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to updateCash from post: " + ex.ToString());
                            return "{\"code\":405}";
                        }
                    }
                }

                return "{\"code\":401}";
            });
        }
    }
}
