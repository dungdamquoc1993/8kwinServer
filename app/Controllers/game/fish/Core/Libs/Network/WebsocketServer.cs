// TODO: Debug
//#define Statistic
//#define SerializeOnPool

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BanCa.Libs;
using Fleck;
using SimpleJSON;
using Utils;

namespace Network
{
    class WsPeer
    {
        public int Id;
        public string ClientId;
        public IWebSocketConnection Socket;
        public long LastMessageTimestamp = -1;
    }

    public class WebsocketServer : NetworkServer
    {
        public static int idCount = 0;

        private WebSocketServer server;

        private object peerLock = new object();
        private Dictionary<int, WsPeer> peerMap = new Dictionary<int, WsPeer>();
        private Dictionary<string, int> peerMapIdStrToIdInt = new Dictionary<string, int>();
        private Dictionary<IWebSocketConnection, WsPeer> socketToPeerMap = new Dictionary<IWebSocketConnection, WsPeer>();
        private List<WsPeer> allPeers = new List<WsPeer>();

        private ConcurrentQueue<IWebSocketConnection> disconnectedPeers = new ConcurrentQueue<IWebSocketConnection>();

        private TaskRunner taskRunner = new TaskRunner();

        public int TimeOutMs = 5000; // if last message exceed 5s => disconnected
        private HashSet<int> ignoreMsgIdLog = new HashSet<int>();

        // cache
        private static List<JSONArray> poolPackages = new List<JSONArray>();
        private SimplePool<List<string>> peersToPush = new SimplePool<List<string>>(() => new List<string>());

        private readonly string PeerPrefix;

        public override int Count
        {
            get
            {
                lock (peerLock) return allPeers.Count;
            }
        }

        public WebsocketServer(int port, bool ssl = false, string peerPrefix = "ws") : base(port)
        {
            this.PeerPrefix = peerPrefix;

            int retryCount = 0;
            while (!startWs(port, ssl) && retryCount < 12)
            {
                retryCount++;
                Logger.Info("Start WebsocketServer fail, retry {0}", retryCount);
                Thread.Sleep(5000);
            }
        }

        private bool startWs(int port, bool ssl = false)
        {
            try
            {
                if (server != null)
                {
                    try
                    {
                        if (server != null)
                        {
                            server.Dispose();
                            server = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Info("Fail to dispose ws server: " + ex.ToString());
                        server = null;
                    }
                }

                FleckLog.LogAction = (level, message, ex) =>
                {
                    if (level >= FleckLog.Level)
                    {
                        switch (level)
                        {
                            case Fleck.LogLevel.Debug:
                                Logger.Log("{0}\n{1}", message, ex == null ? "no ex" : ex.ToString());
                                break;
                            case Fleck.LogLevel.Info:
                                Logger.Info("{0}\n{1}", message, ex == null ? "no ex" : ex.ToString());
                                break;
                            case Fleck.LogLevel.Warn:
                            case Fleck.LogLevel.Error:
                                Logger.Error("{0}\n{1}", message, ex == null ? "no ex" : ex.ToString());
                                break;
                        }
                    }
                };
                FleckLog.Level = Fleck.LogLevel.Debug;
                //var viprikHost = ConfigJson.Config["adress-ip"].Value;
                server = new WebSocketServer(ssl ? ("wss://0.0.0.0:" + port) : ("ws://0.0.0.0:" + port));
                if (ssl)
                {
                    server.Certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2("./cer.pfx", "coconde");
                }
                //server.SupportedSubProtocols = new[] { "sub" };
                server.ListenerSocket.NoDelay = true;
                server.RestartAfterListenError = false; // lol
                server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        int id = Interlocked.Increment(ref idCount);
                        var peer = new WsPeer() { Id = id, Socket = socket };
                        lock (peerLock)
                        {
                            if (peerMap.ContainsKey(peer.Id))
                            {
                                for (int i = 0; i < allPeers.Count; i++)
                                {
                                    if (allPeers[i].Id == peer.Id)
                                    {
                                        allPeers[i] = allPeers[allPeers.Count - 1];
                                        allPeers.RemoveAt(allPeers.Count - 1);
                                        break;
                                    }
                                }
                            }

                            var key = PeerPrefix + peer.Id;
                            peerMap[peer.Id] = peer;
                            peer.ClientId = key;
                            peerMapIdStrToIdInt[key] = peer.Id;
                            socketToPeerMap[socket] = peer;
                            allPeers.Add(peer);
                            peer.LastMessageTimestamp = TimeUtil.TimeStamp;
                        }
                    };
                    socket.OnClose = () =>
                    {
                        disconnectedPeers.Enqueue(socket);
                    };
                    socket.OnError = ex =>
                    {
                        if (ex is IOException) // maybe not importance
                        {
                            Logger.Info("Websocket OnError IOException {0}\n{1}", socket.ConnectionInfo.ClientIpAddress.ToString(), ex.ToString());
                        }
                        else
                        {
                            Logger.Error("Websocket OnError {0}\n{1}", socket.ConnectionInfo.ClientIpAddress.ToString(), ex.ToString());
                        }
                    };
                    socket.OnPing = data =>
                    {
                        try
                        {
                            socket.SendPong(data);
                        }
                        catch (Exception ex)
                        {
                            Logger.Info("Fail to send pong: " + ex.ToString());
                        }
                    };
                    socket.OnMessage = message =>
                    {
                        try
                        {
                            var bs = Convert.FromBase64String(message);
                            lock (peerLock)
                            {
                                if (socketToPeerMap.TryGetValue(socket, out var peer))
                                {
                                    peer.LastMessageTimestamp = TimeUtil.TimeStamp;
                                    OnNetworkReceive(peer, bs);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Websocket error in OnMessage " + socket.ConnectionInfo.ClientIpAddress.ToString() + " ex\n " + ex.ToString());
                        }
                    };
                    socket.OnBinary = bytes =>
                    {
                        try
                        {
                            lock (peerLock)
                            {
                                if (socketToPeerMap.TryGetValue(socket, out var peer))
                                {
                                    peer.LastMessageTimestamp = TimeUtil.TimeStamp;
                                    OnNetworkReceive(peer, bytes);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Websocket error in OnBinary " + socket.ConnectionInfo.ClientIpAddress.ToString() + " ex\n " + ex.ToString());
                        }
                    };
                });
                Logger.Info("Websocket server started at " + port);
                return true;
            }
            catch (SocketException se)
            {
                // SocketException (98): Address already in use
                if (se.ErrorCode == 98)
                {
                    Logger.Info("Websocket server start fail at " + port + " ex:\n" + se.ToString());
                    return false;
                }
                else
                    throw se;
            }
            catch (Exception ex)
            {
                Logger.Info("Websocket server start fail at " + port + " ex:\n" + ex.ToString());
                throw ex;
            }
        }

        public override void Update()
        {
            while (disconnectedPeers.TryDequeue(out var peer))
            {
                removePeer(peer);
            }
            taskRunner.Update();

            var now = TimeUtil.TimeStamp;
            lock (peerLock)
            {
                for (int i = 0, n = allPeers.Count; i < n; i++)
                {
                    var p = allPeers[i];
                    if (p.LastMessageTimestamp != -1 && now - p.LastMessageTimestamp > TimeOutMs)
                    {
                        disconnectedPeers.Enqueue(p.Socket);
                    }
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                if (server != null)
                {
                    server.Dispose();
                    server = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Fail to dispose ws server: " + ex.ToString());
                server = null;
            }

            lock (peerLock)
            {
                peerMap.Clear();
                peerMapIdStrToIdInt.Clear();
                allPeers.Clear();
            }
        }

        private void OnNetworkReceive(WsPeer peer, byte[] data)
        {
            var pos = 0;
            var length = data.Length;
            JSONNode package = null;
            try
            {
                package = JSON.LoadFromMsgPackBytes(data, pos, length);
            }
            catch (Exception ex)//: someone send invalid json
            {
                Logger.Error("Server receive invalid message:\n" + ex.ToString());
            }

            if (package == null)
            {
                return;
            }

            try
            {
                var msgId = package["msgId"].AsInt;
                if (msgId == 0)
                {
                    if (OnClientNotify != null)
                        OnClientNotify((string)peer.ClientId, package["route"], package["data"].AsObject);
                }
                else
                {
                    if (OnClientRequest != null)
                        OnClientRequest((string)peer.ClientId, msgId, package["route"], package["data"].AsObject);
                }
            }
            catch (Exception ex)//: someone send invalid json
            {
                Logger.Error("Server receive invalid message:\n" + ex.ToString());
                return;
            }
        }

        private void removePeer(IWebSocketConnection socket)
        {
            lock (peerLock)
            {
                if (socketToPeerMap.TryGetValue(socket, out var peer))
                {
                    int peerId = peer.Id;
                    string key = peer.ClientId as string;
                    if (peerMap.ContainsKey(peerId))
                    {
                        for (int i = 0; i < allPeers.Count; i++)
                        {
                            if (allPeers[i].Id == peer.Id)
                            {
                                allPeers[i] = allPeers[allPeers.Count - 1];
                                allPeers.RemoveAt(allPeers.Count - 1);
                                break;
                            }
                        }
                        peerMap.Remove(peer.Id);
                    }

                    if (key != null)
                    {
                        peerMapIdStrToIdInt.Remove(key);
                    }

                    try
                    {
                        socket.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.Info("Fail to close socket: " + ex.ToString());
                    }
                }
                if (OnRemovePeer != null)
                {
                    OnRemovePeer((string)peer.ClientId);
                }
            }
        }

        public override void PushToClientsInWorld(GameBanCa world, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            var listPeers = peersToPush.Obtain();
            listPeers.Clear();
            for (int i = 0, n = world.players.Length; i < n; i++)
            {
                if (world.players[i] == null)
                    continue;
                var peerId = world.players[i].PeerId;
                if (!string.IsNullOrEmpty(peerId) && HasPeer(peerId))
                {
                    listPeers.Add(peerId);
                }
            }
            if (listPeers.Count == 0)
            {
                peersToPush.Free(listPeers);
                return;
            }

            SimpleJSON.Buffer b = null;
            try
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = 0;
                package["route"] = route;
                b = package.SaveToMsgPackBuffer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error in thread pool PushToClientsInWorld " + ex.ToString());
                peersToPush.Free(listPeers);
                return;
            }
            if (b != null)
            {
                var content = (SimpleJSON.Buffer)b;
                for (int i = 0, n = listPeers.Count; i < n; i++)
                {
                    var peerId = listPeers[i];
                    if (!string.IsNullOrEmpty(peerId))
                    {
                        pushToPeer(peerId, content, option);
                    }
                }
                content.Free();
            }
            peersToPush.Free(listPeers);
        }

        public override void PushToClients(IEnumerable<string> peers, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            var listPeers = peersToPush.Obtain();
            listPeers.Clear();
            foreach (var peerId in peers)
            {
                if (!string.IsNullOrEmpty(peerId) && HasPeer(peerId))
                {
                    listPeers.Add(peerId);
                }
            }
            if (listPeers.Count == 0)
            {
                peersToPush.Free(listPeers);
                return;
            }

            SimpleJSON.Buffer b = null;
            try
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = 0;
                package["route"] = route;
                b = package.SaveToMsgPackBuffer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error in thread pool PushToClients " + ex.ToString());
                peersToPush.Free(listPeers);
                return;
            }

            if (b != null)
            {
                var content = (SimpleJSON.Buffer)b;
                for (int i = 0, n = listPeers.Count; i < n; i++)
                {
                    var peerId = listPeers[i];
                    if (!string.IsNullOrEmpty(peerId))
                    {
                        pushToPeer(peerId, content, option);
                    }
                }
                content.Free();
            }
            peersToPush.Free(listPeers);
        }

        private void pushToPeer(string peerId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableOrdered)
        {
            if (string.IsNullOrEmpty(peerId))
            {
                return;
            }

            WsPeer peer = null;
            lock (peerLock)
            {
                var _peerId = peerMapIdStrToIdInt.ContainsKey(peerId) ? peerMapIdStrToIdInt[peerId] : -1;
                peer = peerMap.ContainsKey(_peerId) ? peerMap[_peerId] : null;
            }

            try
            {
                if (peer != null)
                {
                    peer.Socket.Send(msg.ByteBuffer, msg.Start, msg.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Fail to send by ws socket: " + ex.ToString());
            }
        }
        private void pushToPeer(int peerId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableOrdered)
        {
            WsPeer peer = null;
            lock (peerLock)
            {
                peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            }
            try
            {
                if (peer != null)
                {
                    peer.Socket.Send(msg.ByteBuffer, msg.Start, msg.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Fail to send by ws socket: " + ex.ToString());
            }
        }

        public override void PushToClient(string peerId, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            if (string.IsNullOrEmpty(peerId)) // push all
            {
                if (allPeers.Count == 0) return; // but no peer
            }
            else if (!HasPeer(peerId)) // dont have this peer
            {
                return;
            }

            SimpleJSON.Buffer b = null;
            try
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = 0;
                package["route"] = route;
                b = package.SaveToMsgPackBuffer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error in PushToClient " + ex.ToString());
                return;
            }

            if (b == null)
            {
                return;
            }

            var content = (SimpleJSON.Buffer)b;
            if (string.IsNullOrEmpty(peerId))
            {
                lock (peerLock)
                {
                    for (int i = 0; i < allPeers.Count; i++)
                    {
                        pushToPeer(allPeers[i].ClientId, content);
                    }
                }
            }
            else
            {
                pushToPeer(peerId, content);
            }
            content.Free();
        }

        public override void PushToPeer(string clientId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered)
        {
            pushToPeer(clientId, msg, option);
        }

        public override void ResponseToClient(string peerId, int msgId, JSONNode msg)
        {
            SimpleJSON.Buffer b = null;
            try
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = msgId;
                b = package.SaveToMsgPackBuffer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error in thread pool ResponseToClient " + ex.ToString());
                return;
            }

            if (b == null)
            {
                return;
            }
            var content = (SimpleJSON.Buffer)b;

            pushToPeer(peerId, content);
            content.Free();
        }

        /// <summary>
        /// Use in maintain only
        /// </summary>
        public override void RemoveAllPeers()
        {
            lock (peerLock)
            {
                for (int i = 0, n = allPeers.Count; i < n; i++)
                {
                    var peer = allPeers[i];
                    int peerId = peer.Id;
                    string key = peer.ClientId as string;
                    if (peerMap.ContainsKey(peerId))
                    {
                        peerMap.Remove(peer.Id);
                    }

                    if (key != null && peerMapIdStrToIdInt.ContainsKey(key))
                    {
                        peerMapIdStrToIdInt.Remove(key);
                    }

                    allPeers[i] = allPeers[allPeers.Count - 1];
                    allPeers.RemoveAt(allPeers.Count - 1);
                    n--;
                    i--;

                    if (OnRemovePeer != null)
                    {
                        OnRemovePeer((string)peer.ClientId);
                    }
                }
            }
        }

        public override string GetEndPoint(string peerId)
        {
            if (string.IsNullOrEmpty(peerId))
            {
                return "::";
            }
            WsPeer peer = null;
            lock (peerLock)
            {
                var _peerId = peerMapIdStrToIdInt.ContainsKey(peerId) ? peerMapIdStrToIdInt[peerId] : -1;
                peer = peerMap.ContainsKey(_peerId) ? peerMap[_peerId] : null;
            }
            if (peer != null)
            {
                return peer.Socket.ConnectionInfo.ClientIpAddress.ToString();
            }

            return "::";
        }

        public override void Kick(string peerId)
        {
            if (string.IsNullOrEmpty(peerId))
            {
                return;
            }
            WsPeer peer = null;
            if (Monitor.TryEnter(peerLock, 1000))
            {
                try
                {
                    var _peerId = peerMapIdStrToIdInt.ContainsKey(peerId) ? peerMapIdStrToIdInt[peerId] : -1;
                    peer = peerMap.ContainsKey(_peerId) ? peerMap[_peerId] : null;
                }
                finally
                {
                    Monitor.Exit(peerLock);
                }
            }

            try
            {
                if (peer != null)
                {
                    disconnectedPeers.Enqueue(peer.Socket);
                    peer.Socket.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Fail to close ws socket: " + ex.ToString());
            }
        }

        public override bool IsRunning()
        {
            return server != null;
        }

        public override bool HasPeer(string peerId)
        {
            if (string.IsNullOrEmpty(peerId))
            {
                return false;
            }
            WsPeer peer = null;
            lock (peerLock)
            {
                var _peerId = peerMapIdStrToIdInt.ContainsKey(peerId) ? peerMapIdStrToIdInt[peerId] : -1;
                peer = peerMap.ContainsKey(_peerId) ? peerMap[_peerId] : null;
            }
            return peer != null;
        }
    }
}
