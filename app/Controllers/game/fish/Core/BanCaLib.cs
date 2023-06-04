using BanCa.Libs;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SimpleJSON;

namespace BanCa
{
    public class BanCaLib
    {
        public static ConcurrentQueue<SimpleJSON.JSONNode> PendingAlerts = new ConcurrentQueue<SimpleJSON.JSONNode>();

        public readonly static List<BanCaServer> bancaServerInstances = new List<BanCaServer>();

        protected readonly static object kickLock = new object();
        protected readonly static List<string> clientIdToKicks = new List<string>();
        protected readonly static Dictionary<int, List<long>> kickOnPorts = new Dictionary<int, List<long>>();

        protected readonly static ConcurrentQueue<Tuple<string, JSONNode>> pendingPushAll = new ConcurrentQueue<Tuple<string, JSONNode>>();

        public static void ClearPendingAlert()
        {
            PendingAlerts = new ConcurrentQueue<JSONNode>();
        }

        public static void AddPeerToKick(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                if (Monitor.TryEnter(kickLock, 1000))
                {
                    clientIdToKicks.Add(clientId);
                    Monitor.Exit(kickLock);
                }
                else
                {
                    Logger.Info("AddPeerToKick fail to lock");
                }
            }
        }

        public static void AddUserIdKickOnPort(long userId, int port)
        {
            if (Monitor.TryEnter(kickLock, 1000))
            {
                if (!kickOnPorts.ContainsKey(port))
                    kickOnPorts[port] = new List<long>();
                kickOnPorts[port].Add(userId);
                Monitor.Exit(kickLock);
            }
            else
            {
                Logger.Info("AddUserIdKickOnPort fail to lock");
            }
        }

        public static void PushAll(string route, JSONNode msg)
        {
            pendingPushAll.Enqueue(new Tuple<string, JSONNode>(route, msg));
        }
    }
}
