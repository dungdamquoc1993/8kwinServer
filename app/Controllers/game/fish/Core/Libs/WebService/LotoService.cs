using BanCa;
using Database;
using Loto;
using Nancy;
using Nancy.Extensions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace LotoService
{
    public class LotoService : NancyModule
    {
        public LotoService()
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

            Post("/lotoapi/playrequest", async parameters =>
            {
                string body = this.Request.Body.AsString();
                var data = JSON.Parse(body);
                var number = data["number"];
                var mode = (LotoGameMode)data["mode"].AsInt;
                if (LotoSql.NeedArrayOfNumbers(mode) && !(number is JSONArray))
                {
                    
                }

                if (LotoSql.NeedArrayOfNumbers(mode))
                {
                    if (!(number is JSONArray))
                    {
                        var res = new JSONObject();
                        res["code"] = 1;
                        res["msg"] = "Need array of number";
                        return res.ToString();
                    }
                }
                else
                {
                    if (number is JSONArray)
                    {
                        var res = new JSONObject();
                        res["code"] = 1;
                        res["msg"] = "Need number";
                        return res.ToString();
                    }
                }
                var newNumber = number is JSONArray ? number.ToString() : number.Value;
                if (!LotoSql.checkInputValid(mode, number))
                {
                    var res = new JSONObject();
                    res["code"] = 2;
                    res["msg"] = "Invalid input";
                    return res.ToString();
                }
                var cost = await LotoSql.AddPlayRequest(data["appId"], data["username"], data["session"],
                    mode, newNumber, (LotoChannel)data["channel"].AsInt, data["pay"]);
                {
                    var res = new JSONObject();
                    res["code"] = cost > 0 ? 0 : 1;
                    res["msg"] = cost > 0 ? "Success" : "Fail";
                    res["cost"] = cost;
                    return res.ToString();
                }
            });

            Post("/lotoapi/addresult", async parameters =>
            {
                string body = this.Request.Body.AsString();
                var data = JSON.Parse(body);
                var result = await LotoSql.AddResult(data["session"], (LotoChannel)data["channel"].AsInt, data["results"].AsArray, data["timeResult"]);
                var res = new JSONObject();
                res["code"] = result.Item1 != 0 ? 0 : 1;
                res["msg"] = result.Item1 != 0 ? "Success" : "Fail";
                return res.ToString();
            });

            Post("/lotoapi/calculateresult", async parameters =>
            {
                var result = await LotoSql.CalculateResult();
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["count"] = result.Count;
                return res.ToString();
            });

            Post("/lotoapi/calculateresultbysession/{session}", async parameters =>
            {
                int session = parameters.session;
                var result = await LotoSql.CalculateResult(session);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["count"] = result.Count;

                BanCaLib.PushAll("OnCalculatedLoto", JSON.ListObjToJson(result));

                return res.ToString();
            });

            Post("/lotoapi/clearcache", parameters =>
            {
                LotoSql.ClearCache();
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                return res.ToString();
            });

            Get("/lotoapi/getpayrate/{gameMode}/{channel}", async parameters =>
            {
                int gameMode = parameters.gameMode;
                int channel = parameters.channel;

                var rate = await LotoSql.GetPayRate((LotoGameMode)gameMode, (LotoChannel)channel);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["rate"] = rate;
                return res.ToString();
            });

            Post("/lotoapi/setpayrate/{gameMode}/{channel}/{rate}", parameters =>
            {
                int gameMode = parameters.gameMode;
                int channel = parameters.channel;
                float rate = parameters.rate;

                LotoSql.UpdatePayRate((LotoGameMode)gameMode, (LotoChannel)channel, rate);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                return res.ToString();
            });

            Get("/lotoapi/getwinrate/{gameMode}/{channel}", async parameters =>
            {
                int gameMode = parameters.gameMode;
                int channel = parameters.channel;

                var rate = await LotoSql.GetWinRate((LotoGameMode)gameMode, (LotoChannel)channel);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["rate"] = rate;
                return res.ToString();
            });

            Post("/lotoapi/setwinrate/{gameMode}/{channel}/{rate}", parameters =>
            {
                int gameMode = parameters.gameMode;
                int channel = parameters.channel;
                float rate = parameters.rate;

                LotoSql.UpdateWinRate((LotoGameMode)gameMode, (LotoChannel)channel, rate);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                return res.ToString();
            });

            Get("/lotoapi/getcalculateresult/{session}/{appId}", async parameters =>
            {
                int session = parameters.session;
                string appId = parameters.appId;
                var result = await LotoSql.GetCalculateResult(session, appId, string.Empty);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["data"] = JSON.ListObjToJson(result);
                return res.ToString();
            });

            Get("/lotoapi/getcalculateresult/{session}/{appId}/{username}", async parameters =>
            {
                int session = parameters.session;
                string appId = parameters.appId;
                string username = parameters.username;
                var result = await LotoSql.GetCalculateResult(session, appId, username);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["data"] = JSON.ListObjToJson(result);
                return res.ToString();
            });

            Get("/lotoapi/getplayrequest/{appId}/{username}", async parameters =>
            {
                string appId = parameters.appId;
                string username = parameters.username;
                var result = await LotoSql.GetPlayRequest(appId, username);
                if (result != null)
                {
                    var res = new JSONObject();
                    res["code"] = 0;
                    res["msg"] = "Success";
                    res["data"] = JSON.ListObjToJson(result);
                    return res.ToString();
                }
                else
                {
                    var res = new JSONObject();
                    res["code"] = 1;
                    res["msg"] = "Fail, too many request";
                    return res.ToString();
                }
            });

            Get("/lotoapi/getlotoresult/{session}/{channel}", async parameters =>
            {
                int session = parameters.session;
                int channel = parameters.channel;
                var result = await LotoSql.GetLotoResult((LotoChannel)channel, session);
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["data"] = result.ToJson();
                return res.ToString();
            });

            Get("/lotoapi/getlotohelp", async parameters =>
            {
                var result = await LotoSql.GetGameModes();
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["data"] = result;
                return res.ToString();
            });

            Post("/lotoapi/setallows", parameters =>
            {
                string body = this.Request.Body.AsString();
                var data = JSON.Parse(body);
                // { channels, modes }
                LotoGame.Instance.SetAllowsData(data);
                return "{\"code\":0,\"msg\":\"Success\"}";
            });

            Get("/lotoapi/getallows", parameters =>
            {
                var res = new JSONObject();
                res["code"] = 0;
                res["msg"] = "Success";
                res["data"] = LotoGame.Instance.GetAllowsData();
                return res.ToString();
            });
        }
    }
}
