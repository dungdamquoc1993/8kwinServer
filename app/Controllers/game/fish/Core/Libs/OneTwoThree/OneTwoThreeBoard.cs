using BanCa.Libs;
using BanCa.Redis;
using Entites.General;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace OneTwoThree
{
    public class OneTwoThreeBoard
    {
        public static int MATCHING_TIME_MS = 10000;
        public static int MATCHING_TIME_VARY_MS = 3000;
        public static int PLAYING_TIME_MS = 10000;

        public readonly static List<int> AllowBlinds = new List<int>() { 0, 1000, 5000, 10000, 50000, 100000 }; // 0 = quick play, select blind by user cash

        private static OneTwoThreeBoard instance;
        public static OneTwoThreeBoard Instance
        {
            get
            {
                if (instance == null) instance = new OneTwoThreeBoard();
                return instance;
            }
        }

        private List<PlayRequest> playRequests = new List<PlayRequest>();
        private List<Match> matches = new List<Match>();
        private Random random;
        private TaskRunner runner;

        public Action<Match> OnMatching;
        private LobbyService Lobby;
        private NetworkServer NetworkServer;

        public OneTwoThreeBoard()
        {
            random = new Random();
            runner = new TaskRunner();
            runner.SelfOperate(250);
            runner.SetInterval(1000, perSecond);

            OnMatching = match =>
            {
                runner.QueueAction(() =>
                {
                    if (NetworkServer != null && Lobby != null)
                    {
                        var msg = new JSONObject();
                        msg["blind"] = match.Blind;
                        msg["nickname1"] = match.Nickname1;
                        msg["nickname2"] = match.Nickname2;
                        msg["userId1"] = match.UserId1;
                        msg["userId2"] = match.UserId2;
                        msg["avatar1"] = match.Avatar1;
                        msg["avatar2"] = match.Avatar2;

                        if (match.UserId1 != 0)
                        {
                            var user1 = Lobby.IsLogin(match.UserId1);
                            if (user1 != null && user1.ClientId != null)
                                NetworkServer.PushToClient(user1.ClientId, "OttOnMatching", msg);
                        }

                        if (match.UserId2 != 0)
                        {
                            var user2 = Lobby.IsLogin(match.UserId2);
                            if (user2 != null && user2.ClientId != null)
                                NetworkServer.PushToClient(user2.ClientId, "OttOnMatching", msg);
                        }
                    }
                });
            };
        }

        void perSecond()
        {
            var now = TimeUtil.TimeStamp;

            for (int i = 0, n = playRequests.Count; i < n; i++)
            {
                for (int j = i + 1, m = playRequests.Count; j < m; j++)
                {
                    if (playRequests[i].Blind == playRequests[j].Blind) // matching
                    {
                        var blind = playRequests[i].Blind; // TODO: blind 0
                        var match = new Match(runner, random, blind, playRequests[i].UserId, playRequests[i].Nickname, playRequests[i].Avatar,
                            playRequests[j].UserId, playRequests[j].Nickname, playRequests[j].Avatar);
                        match.OnStart = onMatchStart;
                        match.OnSolved = onMatchSolved;
                        match.OnEndGame = onMatchEnd;
                        matches.Add(match);
                        if (OnMatching != null) OnMatching(match);
                        match.Start();

                        // because j > i
                        playRequests[j] = playRequests[playRequests.Count - 1];
                        playRequests.RemoveAt(playRequests.Count - 1);

                        playRequests[i] = playRequests[playRequests.Count - 1];
                        playRequests.RemoveAt(playRequests.Count - 1);

                        i -= 1;
                        n -= 2;
                        break;
                    }
                }
            }

            for (int i = 0, n = playRequests.Count; i < n; i++)
            {
                if (now >= playRequests[i].TimeOutStamp) // timeout
                {
                    var blind = playRequests[i].Blind; // TODO: blind 0
                    var match = new Match(runner, random, blind, playRequests[i].UserId, playRequests[i].Nickname, playRequests[i].Avatar,
                        0, nextBotName(), "");
                    match.OnStart = onMatchStart;
                    match.OnSolved = onMatchSolved;
                    match.OnEndGame = onMatchEnd;
                    match.Start();
                    matches.Add(match);
                    if (OnMatching != null) OnMatching(match);

                    playRequests[i] = playRequests[playRequests.Count - 1];
                    playRequests.RemoveAt(playRequests.Count - 1);

                    i -= 1;
                    n -= 1;
                }
            }
        }
        private string nextBotName()
        {
            return BotNickname.RandomName(random);
        }

        private void onMatchStart(Match match)
        {
            if (NetworkServer != null && Lobby != null)
            {
                var msg = new JSONObject();
                msg["blind"] = match.Blind;
                msg["nickname1"] = match.Nickname1;
                msg["nickname2"] = match.Nickname2;
                msg["userId1"] = match.UserId1;
                msg["userId2"] = match.UserId2;
                msg["avatar1"] = match.Avatar1;
                msg["avatar2"] = match.Avatar2;

                if (match.UserId1 != 0)
                {
                    var user1 = Lobby.IsLogin(match.UserId1);
                    if (user1 != null && user1.ClientId != null)
                        NetworkServer.PushToClient(user1.ClientId, "OttOnMatchStart", msg);
                }

                if (match.UserId2 != 0)
                {
                    var user2 = Lobby.IsLogin(match.UserId2);
                    if (user2 != null && user2.ClientId != null)
                        NetworkServer.PushToClient(user2.ClientId, "OttOnMatchStart", msg);
                }
            }
        }
        private void onMatchSolved(Match match, History history)
        {
            if (NetworkServer != null && Lobby != null)
            {
                var msg = new JSONObject();
                msg["blind"] = match.Blind;
                msg["nickname1"] = match.Nickname1;
                msg["nickname2"] = match.Nickname2;
                msg["avatar1"] = match.Avatar1;
                msg["avatar2"] = match.Avatar2;
                msg["userId1"] = match.UserId1;
                msg["userId2"] = match.UserId2;
                msg["choice1"] = (int)match.BetChoice1;
                msg["choice2"] = (int)match.BetChoice2;
                msg["result"] = (int)match.MatchResult;

                if (match.UserId1 != 0)
                {
                    var user1 = Lobby.IsLogin(match.UserId1);
                    if (user1 != null && user1.ClientId != null)
                        NetworkServer.PushToClient(user1.ClientId, "OttOnMatchSolved", msg);
                }

                if (match.UserId2 != 0)
                {
                    var user2 = Lobby.IsLogin(match.UserId2);
                    if (user2 != null && user2.ClientId != null)
                        NetworkServer.PushToClient(user2.ClientId, "OttOnMatchSolved", msg);
                }
            }
        }
        private void onMatchEnd(Match match, long changeCash1, long cash1, long changeCash2, long cash2)
        {
            if (NetworkServer != null && Lobby != null)
            {
                var msg = new JSONObject();
                msg["blind"] = match.Blind;
                msg["nickname1"] = match.Nickname1;
                msg["nickname2"] = match.Nickname2;
                msg["avatar1"] = match.Avatar1;
                msg["avatar2"] = match.Avatar2;
                msg["userId1"] = match.UserId1;
                msg["userId2"] = match.UserId2;
                msg["choice1"] = (int)match.BetChoice1;
                msg["choice2"] = (int)match.BetChoice2;
                msg["result"] = (int)match.MatchResult;
                msg["changeCash1"] = changeCash1;
                msg["cash1"] = cash1;
                msg["changeCash2"] = changeCash2;
                msg["cash2"] = cash2;

                if (match.UserId1 != 0)
                {
                    var user1 = Lobby.IsLogin(match.UserId1);
                    if (user1 != null && user1.ClientId != null)
                        NetworkServer.PushToClient(user1.ClientId, "OttOnMatchEnd", msg);
                }

                if (match.UserId2 != 0)
                {
                    var user2 = Lobby.IsLogin(match.UserId2);
                    if (user2 != null && user2.ClientId != null)
                        NetworkServer.PushToClient(user2.ClientId, "OttOnMatchEnd", msg);
                }
            }

            matches.Remove(match);
        }

        public void Dispose()
        {
            instance = null;
        }

        public void onRemovePeer(string clientId)
        {
            runner.QueueAction(async () =>
            {
                try
                {
                    for (int i = 0, n = playRequests.Count; i < n; i++)
                    {
                        var pr = playRequests[i];
                        if (pr.ClientId == clientId)
                        {
                            await RedisManager.IncEpicCash(pr.UserId, pr.Blind, "server", "onetwothree refund", TransType.ONE_TWO_THREE_PAY);
                            playRequests[i] = playRequests[playRequests.Count - 1];
                            playRequests.RemoveAt(playRequests.Count - 1);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in ott onRemovePeer: " + ex.ToString());
                }
            });
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
                case "OTT1": // play request
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        long userId = user.UserId;
                        string nickname = msg["nickname"];
                        int blind = msg["blind"];
                        PlayRequest(userId, nickname, user.Avatar, blind, clientId, (res) =>
                        {
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "OTT11": // cancel play request
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        CancelPlayRequest(user.UserId, (res) =>
                        {
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "OTT2": // choice request
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        Choice choice = (Choice)msg["choice"].AsInt;
                        ChoiceRequest(user.UserId, choice, (res) =>
                        {
                            NetworkServer.ResponseToClient(clientId, msgId, res);
                        });
                    }
                    return true;
                case "OTT3": // history
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        long userId = user.UserId;
                        TaskRunner.RunOnPool(async () =>
                        {
                            try
                            {
                                var data = await Match.GetGameHistories(userId);
                                var msg2 = new JSONObject();
                                msg2["code"] = 200;
                                msg2["data"] = data;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error in ott3: " + ex.ToString());
                            }
                        });
                    }
                    return true;
                case "OTT4": // glory
                    {
                        User user = Lobby.CheckLogin(clientId, msgId);
                        if (user == null)
                            return true;

                        TaskRunner.RunOnPool(async () =>
                        {
                            try
                            {
                                var data = await Match.GetGameGlories();
                                var msg2 = new JSONObject();
                                msg2["code"] = 200;
                                msg2["data"] = data;
                                NetworkServer.ResponseToClient(clientId, msgId, msg2);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error in ott4: " + ex.ToString());
                            }
                        });
                    }
                    return true;
            }
            return false;
        }

        public void PlayRequest(long userId, string nickname, string avatar, int blind, string clientId, Action<JSONNode> cb)
        {
            runner.QueueAction(async () =>
            {
                try
                {
                    if (userId <= 0 || string.IsNullOrEmpty(nickname) || !AllowBlinds.Contains(blind))
                    {
                        var msg = new JSONObject();
                        msg["code"] = 300; // tham so ko hop le
                        if (cb != null) cb(msg);
                        return;
                    }

                    if (blind == 0)
                    {
                        var myCash = await RedisManager.GetUserCash(userId);
                        for (int i = AllowBlinds.Count - 1; i >= 0; i--)
                        {
                            if (AllowBlinds[i] < myCash)
                            {
                                blind = AllowBlinds[i];
                                break;
                            }
                        }
                    }
                    if (blind == 0)
                    {
                        var msg = new JSONObject(); // not enough cash
                        msg["code"] = 302;
                        if (cb != null) cb(msg);
                        return;
                    }

                    for (int i = 0, n = playRequests.Count; i < n; i++)
                    {
                        if (playRequests[i].UserId == userId) // already enqueue
                        {
                            var msg = new JSONObject();
                            msg["code"] = 301;
                            msg["blind"] = playRequests[i].Blind;
                            if (cb != null) cb(msg);
                            return;
                        }
                    }

                    var cash = await RedisManager.IncEpicCash(userId, -blind, "server", "onetwothree", TransType.ONE_TWO_THREE_PAY);
                    if (cash >= 0)
                    {
                        var req = new OneTwoThree.PlayRequest()
                        {
                            Blind = blind,
                            UserId = userId,
                            Nickname = nickname,
                            Avatar = avatar,
                            Timestamp = TimeUtil.TimeStamp,
                            TimeOutStamp = TimeUtil.TimeStamp + MATCHING_TIME_MS + random.Next(2 * MATCHING_TIME_VARY_MS) - MATCHING_TIME_VARY_MS,
                            ClientId = clientId
                        };
                        playRequests.Add(req);
                        Logger.Info("id {0} name {1} blind {2} time out after {3}", userId, nickname, blind, req.TimeOutStamp - req.Timestamp);
                        var msg = new JSONObject();
                        msg["code"] = 200;
                        msg["blind"] = req.Blind;
                        msg["cash"] = cash;
                        if (cb != null) cb(msg);
                    }
                    else
                    {
                        var msg = new JSONObject(); // not enough cash
                        msg["code"] = 302;
                        if (cb != null) cb(msg);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in ott PlayRequest: " + ex.ToString());
                }
            });
        }

        public void CancelPlayRequest(long userId, Action<JSONNode> cb)
        {
            runner.QueueAction(async () =>
            {
                try
                {
                    if (userId <= 0)
                    {
                        var msg = new JSONObject();
                        msg["code"] = 300; // tham so ko hop le
                        if (cb != null) cb(msg);
                        return;
                    }

                    for (int i = 0, n = playRequests.Count; i < n; i++)
                    {
                        if (playRequests[i].UserId == userId) // found
                        {
                            await RedisManager.IncEpicCash(userId, playRequests[i].Blind, "server", "onetwothree refund", TransType.ONE_TWO_THREE_PAY);
                            playRequests[i] = playRequests[playRequests.Count - 1];
                            playRequests.RemoveAt(playRequests.Count - 1);
                            break;
                        }
                    }

                    {
                        var msg = new JSONObject();
                        msg["code"] = 200;
                        if (cb != null) cb(msg);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in ott CancelPlayRequest: " + ex.ToString());
                }
            });
        }

        public void ChoiceRequest(long userId, Choice choice, Action<JSONNode> cb)
        {
            runner.QueueAction(() =>
            {
                for (int i = 0, n = matches.Count; i < n; i++)
                {
                    var match = matches[i];
                    if (match.IsEnd) continue;
                    if (match.UserId1 == userId) // found
                    {
                        match.BetChoice1 = choice;

                        var msg = new JSONObject();
                        msg["code"] = 200;
                        if (cb != null) cb(msg);
                        return;
                    }
                    if (match.UserId2 == userId) // found
                    {
                        match.BetChoice2 = choice;

                        var msg = new JSONObject();
                        msg["code"] = 200;
                        if (cb != null) cb(msg);
                        return;
                    }
                }

                {
                    var msg = new JSONObject();
                    msg["code"] = 300; // not playing
                    if (cb != null) cb(msg);
                    return;
                }
            });
        }
    }
}
