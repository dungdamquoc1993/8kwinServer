using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using SimpleJSON;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace BanCa.Libs
{
    class WebSocketHandler : WebSocketBehavior
    {
        public BcWebSocketServer Server;

        protected override void OnOpen()
        {
            //Logger.Log("ws connected " + ID);
            Server.queueEvent(new WsEvent() { Event = WsEventType.OnOpen, Socket = this });
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            //Logger.Log("ws message " + ID + " " + e.Data);
            //Server.queueEvent(new WsEvent() { Event = WsEventType.OnMessage, Socket = this, Data = e.RawData });
            Server.OnNetworkReceive(this, e.RawData);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            //Logger.Log("ws close " + ID + " " + e.Reason);
            Server.queueEvent(new WsEvent() { Event = WsEventType.OnClose, Socket = this });
        }

        public void SendData(byte[] data, int index, int length)
        {
            
            if (this.ConnectionState == WebSocketState.Open)
            {
                try
                {
                    Send(data, index, length);
                }
                catch (Exception ex)
                {
                    Logger.Error("Ws send data fail " + ex.ToString());
                }
            }
        }

        public void SendData(Stream data)
        {
            if (this.ConnectionState == WebSocketState.Open)
            {
                try
                {
                    Send(data, (int)data.Length);
                }
                catch (Exception ex)
                {
                    Logger.Error("Ws send data fail " + ex.ToString());
                }
            }
        }
    }

    enum WsEventType
    {
        OnOpen, OnMessage, OnClose
    }
    struct WsEvent
    {
        public WsEventType Event;
        public WebSocketHandler Socket;
        public byte[] Data;
    }

    public class BcWebSocketServer : NetworkServer
    {
        private Dictionary<string, WebSocketHandler> peerMap = new Dictionary<string, WebSocketHandler>();
        private List<WebSocketHandler> allPeers = new List<WebSocketHandler>();
        private object _lock = new object();
        private HttpServer server;
        private List<WsEvent> queues = new List<WsEvent>();
        private TaskRunner taskRunner = new TaskRunner();

        public override int Count
        {
            get
            {
                return allPeers.Count;
            }
        }

        public BcWebSocketServer(int port, bool ssl = false) : base(port)
        {
            var httpsv = new HttpServer(port, ssl);
            if (ssl)
                httpsv.SslConfiguration.ServerCertificate = new X509Certificate2("./cer.pfx", "coconde");
            httpsv.AddWebSocketService<WebSocketHandler>("/", (ws) =>
            {
                ws.Server = this;
                ws.IgnoreExtensions = true;
            });
            httpsv.AddWebSocketService<WebSocketHandler>("/bc", (ws) =>
            {
                ws.Server = this;
                ws.Protocol = "bc";
                ws.IgnoreExtensions = true;
            });
            httpsv.Start();
            if (httpsv.IsListening)
            {
                Logger.Info(Port + " ws started");
            }
            else
            {
                Logger.Info(Port + " ws started fail");
            }
            server = httpsv;
            server.Log.Level = WebSocketSharp.LogLevel.Info;
        }

        internal void queueEvent(WsEvent e)
        {
            lock (queues) queues.Add(e);
        }

        public override void Dispose()
        {
            lock (queues) queues.Clear();

            lock (_lock)
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                peerMap.Clear();
                allPeers.Clear();
            }
        }

        public override void Update()
        {
            taskRunner.Update();
            lock (queues)
            {
                for (int i = 0; i < queues.Count; i++)
                {
                    var e = queues[i];
                    switch (e.Event)
                    {
                        case WsEventType.OnOpen:
                            OnPeerConnected(e.Socket);
                            break;
                        case WsEventType.OnMessage:
                            OnNetworkReceive(e.Socket, e.Data);
                            break;
                        case WsEventType.OnClose:
                            OnPeerDisconnected(e.Socket);
                            break;
                        default:
                            break;
                    }
                }

                queues.Clear();
            }
        }

        internal void OnPeerConnected(WebSocketHandler peer)
        {
            lock (_lock)
            {
                if (peerMap.ContainsKey(peer.ID))
                {
                    for (int i = 0; i < allPeers.Count; i++)
                    {
                        if (allPeers[i].ID == peer.ID)
                        {
                            allPeers[i] = allPeers[allPeers.Count - 1];
                            allPeers.RemoveAt(allPeers.Count - 1);
                            break;
                        }
                    }
                }

                peerMap[peer.ID] = peer;
                allPeers.Add(peer);
            }
        }

        internal void OnPeerDisconnected(WebSocketHandler peer)
        {
            lock (_lock)
            {
                removePeer(peer);
            }
        }

        internal void OnNetworkReceive(WebSocketHandler peer, byte[] data)
        {
            taskRunner.QueueAction(() =>
            {
                try
                {
                    JSONNode package = JSON.LoadFromMsgPackBytes(data, 0, data.Length);
                    return package;
                }
                catch (Exception ex)//: someone send invalid json
                {
                    Logger.Error("Server receive invalid message:\n" + ex.ToString());
                    return null;
                }
            }, (o) =>
            {
                if (o != null)
                {
                    lock (_lock)
                    {
                        try
                        {
                            JSONNode package = (JSONNode)o;
                            var msgId = package["msgId"].AsInt;

                            if (msgId == 0)
                            {
                                if (OnClientNotify != null)
                                    OnClientNotify(peer.ID, package["route"], package["data"].AsObject);
                            }
                            else
                            {
                                if (OnClientRequest != null)
                                    OnClientRequest(peer.ID, msgId, package["route"], package["data"].AsObject);
                            }
                        }
                        catch (Exception ex)//: someone send invalid json
                        {
                            Logger.Error("Server receive invalid message:\n" + ex.ToString());
                            return;
                        }
                    }
                }
            });
        }

        private void removePeer(WebSocketHandler peer)
        {
            string peerId = peer.ID;
            if (peerMap.ContainsKey(peerId))
            {
                for (int i = 0; i < allPeers.Count; i++)
                {
                    if (allPeers[i].ID == peer.ID)
                    {
                        allPeers[i] = allPeers[allPeers.Count - 1];
                        allPeers.RemoveAt(allPeers.Count - 1);
                        break;
                    }
                }
                peerMap.Remove(peer.ID);
            }

            if (OnRemovePeer != null)
            {
                OnRemovePeer(peer.ID);
            }
        }

        public override void PushToClientsInWorld(GameBanCa world, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            lock (_lock)
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = 0;
                package["route"] = route;
                var content = package.SaveToMsgPackBuffer();

                var pushToIds = new List<string>();
                for (int i = 0, n = world.players.Length; i < n; i++)
                {
                    if (world.players[i] == null)
                        continue;
                    var clientId = world.players[i].PeerId;
                    pushToIds.Add(clientId);
                }

                ThreadPool.QueueUserWorkItem(o =>
                {
                    lock (_lock)
                    {
                        //var data = content.ToByteArray();
                        for (int i = 0, n = pushToIds.Count; i < n; i++)
                        {
                            var clientId = pushToIds[i];
                            //PushToPeer(clientId, data, 0, data.Length);
                            PushToPeer(clientId, content);
                        }
                    }
                    content.Free();
                });
            }
        }

        public override void PushToClients(IEnumerable<string> pushToIds, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            lock (_lock)
            {
                var package = new JSONObject();
                package["data"] = msg;
                package["msgId"] = 0;
                package["route"] = route;
                var content = package.SaveToMsgPackBuffer();

                ThreadPool.QueueUserWorkItem(o =>
                {
                    lock (_lock)
                    {
                        foreach (var clientId in pushToIds)
                        {
                            PushToPeer(clientId, content);
                        }
                    }
                    content.Free();
                });
            }
        }

        public override void PushToPeer(string peerId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered)
        {
            var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            if (peer != null)
            {
                peer.SendData(msg.ByteBuffer, msg.Start, msg.Length);
            }
        }

        internal void PushToPeer(string peerId, byte[] data, int index, int length)
        {
            var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            if (peer != null)
            {
                peer.SendData(data, index, length);
            }
        }

        public override void PushToClient(string peerId, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(peerId))
                {
                    var pushToIds = new List<string>();
                    for (int i = 0; i < allPeers.Count; i++)
                    {
                        //PushToPeer(allPeers[i].ID, content);
                        pushToIds.Add(allPeers[i].ID);
                    }

                    if (pushToIds.Count > 0)
                    {
                        var package = new JSONObject();
                        package["data"] = msg;
                        package["msgId"] = 0;
                        package["route"] = route;
                        var content = package.SaveToMsgPackBuffer();
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            //var data = content.ToByteArray();
                            lock (_lock)
                            {
                                for (int i = 0, n = pushToIds.Count; i < n; i++)
                                {
                                    var clientId = pushToIds[i];
                                    //PushToPeer(clientId, data, 0, data.Length);
                                    PushToPeer(clientId, content);
                                }
                            }
                            content.Free();
                        });
                    }
                }
                else
                {
                    var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                    if (peer != null)
                    {
                        var clientId = peer.ID;
                        var package = new JSONObject();
                        package["data"] = msg;
                        package["msgId"] = 0;
                        package["route"] = route;
                        var content = package.SaveToMsgPackBuffer();
                        //PushToPeer(peer.ID, content);
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            //var data = content.ToByteArray();
                            lock (_lock)
                            {
                                //PushToPeer(clientId, data, 0, data.Length);
                                PushToPeer(clientId, content);
                            }
                            content.Free();
                        });
                    }
                }
            }
        }

        public override void ResponseToClient(string clientId, int msgId, JSONNode msg)
        {
            if (HasPeer(clientId))
                lock (_lock)
                {
                    var package = new JSONObject();
                    package["data"] = msg;
                    package["msgId"] = msgId;
                    var content = package.SaveToMsgPackBuffer();
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        lock (_lock)
                        {
                            PushToPeer(clientId, content);
                        }
                        content.Free();
                    });
                }
        }

        /// <summary>
        /// Use in maintain only
        /// </summary>
        public override void RemoveAllPeers()
        {
            lock (_lock)
            {
                for (int i = 0, n = allPeers.Count; i < n; i++)
                {
                    var peer = allPeers[i];
                    string peerId = peer.ID;
                    if (peerId != null && peerMap.ContainsKey(peerId))
                    {
                        peerMap.Remove(peerId);
                    }

                    allPeers[i] = allPeers[allPeers.Count - 1];
                    allPeers.RemoveAt(allPeers.Count - 1);
                    i--;
                    n--;

                    if (OnRemovePeer != null)
                    {
                        OnRemovePeer(peer.ID);
                    }
                }
            }
        }

        public override string GetEndPoint(string peerId)
        {
            lock (_lock)
            {
                var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                if (peer != null)
                {
                    peer.Context.UserEndPoint.Address.ToString();
                }

                return "::";
            }
        }

        public override void Kick(string peerId)
        {
            lock (_lock)
            {
                var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                if (peer != null)
                {
                    peer.Context.WebSocket.Close();
                }
            }
        }

        public override bool IsRunning()
        {
            return server != null && server.IsListening;
        }

        public override bool HasPeer(string peerId)
        {
            if (string.IsNullOrEmpty(peerId))
            {
                return false;
            }
            lock (_lock)
            {
                var peer = peerId != null && peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                return peer != null;
            }
        }
    }
}
