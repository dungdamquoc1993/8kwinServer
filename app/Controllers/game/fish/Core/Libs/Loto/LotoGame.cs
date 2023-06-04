using BanCa.Libs;
using BanCa.Redis;
using Entites.General;
using LotoService;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Loto
{
    public class LotoGame
    {
        public const string APP_ID = "xxeng";

        private static LotoGame instance;
        public static LotoGame Instance
        {
            get
            {
                if (instance == null) instance = new LotoGame();
                return instance;
            }
        }

        private LobbyService Lobby;
        private NetworkServer NetworkServer;

        private List<LotoChannel> allowChannels = new List<LotoChannel>(); // empty = all
        private List<LotoGameMode> allowModes = new List<LotoGameMode>(); // empty = all

        private JSONArray playHistories = new JSONArray();

        public LotoGame()
        {
        }

        public void Dispose()
        {
            instance = null;
        }

        public void onRemovePeer(string clientId)
        {
        }

        public bool onClientNotify(BanCaServer server, string clientId, string route, JSONNode msg)
        {
            return false;
        }

        public bool onClientRequest(BanCaServer server, string clientId, int msgId, string route, JSONNode msg)
        {
            NetworkServer = server.NetworkServer;
            Lobby = server.Lobby;
            switch (route)
            {
                case "LOTO1": // play request
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        var number = msg["number"];
                        var mode = (LotoGameMode)msg["mode"].AsInt;
                        if (LotoSql.NeedArrayOfNumbers(mode))
                        {
                            if (!(number is JSONArray))
                            {
                                var res = new JSONObject();
                                res["code"] = 301;
                                res["msg"] = "Need array of number";
                                NetworkServer.ResponseToClient(clientId, msgId, res);
                            }
                        }
                        else
                        {
                            if (number is JSONArray)
                            {
                                var res = new JSONObject();
                                res["code"] = 301;
                                res["msg"] = "Need number";
                                NetworkServer.ResponseToClient(clientId, msgId, res);
                            }
                        }
                        var newNumber = number is JSONArray ? number.ToString() : number.Value;
                        if (!LotoSql.checkInputValid(mode, number))
                        {
                            var res = new JSONObject();
                            res["code"] = 302;
                            res["msg"] = "Input invalid";
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        }
                        long pay = msg["pay"];
                        var channel = (LotoChannel)msg["channel"].AsInt;
                        var nn = user.Nickname;
                        server.TaskRun.QueueAction(async () =>
                        {
                            long cost = (long)Math.Round(pay * await LotoSql.GetPayRate(mode, channel));
                            var cash = await RedisManager.IncEpicCash(user.UserId, -cost, user.Platform, "lotopay:" + mode.ToString(), TransType.LOTO_PAY);
                            if (cash < 0)
                            {
                                var res = new JSONObject();
                                res["code"] = 303;
                                res["msg"] = "Not enough cash";
                                res["cost"] = cost;
                                NetworkServer.ResponseToClient(clientId, msgId, res);
                            }
                            else
                            {
                                cost = await LotoSql.AddPlayRequest(APP_ID, user.UserId.ToString(), msg["session"],
                                    mode, newNumber, channel, pay);

                                var res = new JSONObject();
                                res["code"] = cost > 0 ? 200 : 302;
                                res["msg"] = cost > 0 ? "Success" : "Fail";
                                res["cash"] = cash;
                                res["cost"] = cost;
                                NetworkServer.ResponseToClient(clientId, msgId, res);

                                var response = new JSONObject();
                                response["nickname"] = nn;
                                response["mode"] = (int)mode;
                                response["channel"] = (int)channel;
                                response["number"] = number;
                                response["cost"] = cost;
                                response["time"] = TimeUtil.TimeStamp;
                                NetworkServer.PushAll("onLOTO1", response, SendMode.ReliableOrdered);

                                lock (playHistories)
                                {
                                    playHistories.Add(response);
                                    if (playHistories.Count > 50)
                                    {
                                        playHistories.Remove(0);
                                    }
                                }
                            }
                        });
                    }
                    return true;
                case "LOTO2": // get pay/win rate
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        var gameMode = (LotoGameMode)msg["mode"].AsInt;
                        var channel = (LotoChannel)msg["channel"].AsInt;

                        server.TaskRun.QueueAction(async () =>
                        {
                            var payRate = await LotoSql.GetPayRate(gameMode, channel);
                            var winRate = await LotoSql.GetWinRate(gameMode, channel);
                            var res = new JSONObject();
                            res["code"] = 200;
                            res["msg"] = "Success";
                            res["payRate"] = payRate;
                            res["winRate"] = winRate;
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "LOTO3": // getcalculateresult
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        int session = msg["session"].AsInt;
                        server.TaskRun.QueueAction(async () =>
                        {
                            var result = await LotoSql.GetCalculateResult(session, APP_ID, user.UserId.ToString());
                            var res = new JSONObject();
                            res["code"] = 200;
                            res["msg"] = "Success";
                            res["data"] = JSON.ListObjToJson(result);
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "LOTO4": // getplayrequest
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        server.TaskRun.QueueAction(async () =>
                        {
                            var result = await LotoSql.GetPlayRequest(APP_ID, user.UserId.ToString());
                            var res = new JSONObject();
                            if (result != null)
                            {
                                res["code"] = 200;
                                res["msg"] = "Success";
                                res["data"] = JSON.ListObjToJson(result);
                            }
                            else
                            {
                                res["code"] = 301;
                                res["msg"] = "Fail, too many request";
                            }
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "LOTO5": // getlotoresult
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        int session = msg["session"].AsInt;
                        var channel = (LotoChannel)msg["channel"].AsInt;
                        server.TaskRun.QueueAction(async () =>
                        {
                            var result = await LotoSql.GetLotoResult((LotoChannel)channel, session);
                            var res = new JSONObject();
                            res["code"] = 200;
                            res["msg"] = "Success";
                            res["data"] = result.ToJson();
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "LOTO6": // help
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        server.TaskRun.QueueAction(async () =>
                        {
                            var result = await LotoSql.GetGameModes();
                            var res = new JSONObject();
                            res["code"] = 200;
                            res["msg"] = "Success";
                            res["data"] = result;
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "LOTO7": // chat
                    {
                        var user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(() =>
                        {
                            string message = msg["msg"];
                            Chat(user.Nickname, message);
                            var response = new JSONObject();
                            response["code"] = 200;
                            response["msg"] = "Success";
                            NetworkServer.ResponseToClient(clientId, msgId, response);
                        });
                    }
                    return true;
                case "LOTO8": // chat history
                    {
                        var user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(() =>
                        {
                            var response = new JSONObject();
                            response["code"] = 200;
                            response["msg"] = "Success";
                            response["data"] = ChatHistory();
                            NetworkServer.ResponseToClient(clientId, msgId, response);
                        });
                    }
                    return true;
                case "LOTO9": // allow mode and channel
                    {
                        NetworkServer.ResponseToClient(clientId, msgId, GetAllowsData());
                    }
                    return true;
                case "LOTO10": // recent play history
                    {
                        var response = new JSONObject();
                        response["code"] = 200;
                        response["msg"] = "Success";
                        lock (playHistories)
                        {
                            response["data"] = playHistories;
                            NetworkServer.ResponseToClient(clientId, msgId, response);
                        }
                    }
                    return true;

            }
            return false;
        }

        public JSONNode GetAllowsData()
        {
            var response = new JSONObject();
            var modes = new JSONArray();
            lock (allowModes)
                foreach (var item in allowModes)
                {
                    modes.Add((int)item);
                }
            var channels = new JSONArray();
            lock (allowChannels)
                foreach (var item in allowChannels)
                {
                    channels.Add((int)item);
                }
            response["code"] = 200;
            response["msg"] = "Success";
            response["channels"] = channels;
            response["modes"] = modes;
            return response;
        }

        public void SetAllowsData(JSONNode data)
        {
            if (data.HasKey("channels"))
            {
                var channels = data["channels"].AsArray;
                lock (allowChannels)
                {
                    allowChannels.Clear();
                    for (int i = 0, n = channels.Count; i < n; i++)
                    {
                        allowChannels.Add((LotoChannel)channels[i].AsInt);
                    }
                }
            }

            if (data.HasKey("modes"))
            {
                var modes = data["modes"].AsArray;
                lock (allowModes)
                {
                    allowModes.Clear();
                    for (int i = 0, n = modes.Count; i < n; i++)
                    {
                        allowModes.Add((LotoGameMode)modes[i].AsInt);
                    }
                }
            }
        }

        private class ChatMessage
        {
            public string Nickname, Message;
        }
        private LinkedList<ChatMessage> ChatHistories = new LinkedList<ChatMessage>();
        public void Chat(string nickname, string message)
        {
            lock (ChatHistories)
            {
                if (ChatHistories.Count > 50)
                {
                    var item = ChatHistories.First.Value;
                    ChatHistories.RemoveFirst();
                    item.Nickname = nickname;
                    item.Message = message;
                    ChatHistories.AddLast(item);
                }
                else
                {
                    var item = new ChatMessage();
                    item.Nickname = nickname;
                    item.Message = message;
                    ChatHistories.AddLast(item);
                }
            }

            {
                var response = new JSONObject();
                response["nickname"] = nickname;
                response["msg"] = message;
                NetworkServer.PushAll("onLOTO7", response, SendMode.ReliableOrdered);
            }
        }

        public JSONArray ChatHistory()
        {
            lock (ChatHistories)
            {
                var arr = new JSONArray();
                foreach (var item in ChatHistories)
                {
                    var json = new JSONObject();
                    json["nickname"] = item.Nickname;
                    json["msg"] = item.Message;
                    arr.Add(json);
                }
                return arr;
            }
        }
    }
}
