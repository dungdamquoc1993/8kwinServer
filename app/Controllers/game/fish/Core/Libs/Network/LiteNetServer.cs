using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using SimpleJSON;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace BanCa.Libs
{
    public class LiteNetServer : NetworkServer
    {
        public const string LiteNetIdPrefix = "litenet";

        private NetManager _netServer;

        private object peerLock = new object();
        private Dictionary<long, NetPeer> peerMap = new Dictionary<long, NetPeer>();
        private Dictionary<string, long> peerMapIdStrToIdLong = new Dictionary<string, long>();
        private List<NetPeer> allPeers = new List<NetPeer>();

        private ConcurrentQueue<NetPeer> disconnectedPeers = new ConcurrentQueue<NetPeer>();

        private TaskRunner taskRunner = new TaskRunner();

        public override int Count
        {
            get
            {
                lock (peerLock) return allPeers.Count;
            }
        }

        public LiteNetServer(int port) : base(port)
        {
            var listener = new EventBasedNetListener();
            listener.ConnectionRequestEvent += request =>
            {
                if (_netServer.PeersCount < 512 /* max connections */)
                    request.AcceptIfKey("cgame");
                else
                    request.Reject();
            };
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkErrorEvent += OnNetworkError;
            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;

            _netServer = new NetManager(listener);
            _netServer.BroadcastReceiveEnabled = false;
            _netServer.UnconnectedMessagesEnabled = false;
            _netServer.UpdateTime = 15;
            _netServer.PingInterval = 2000;
            _netServer.DisconnectTimeout = 5000;
            //_netServer.AutoRecycle = true;
            //_netServer.SimulateLatency = true;
            //_netServer.SimulationMinLatency = 50;
            //_netServer.SimulationMaxLatency = 100;
            
            _netServer.Deserializer = (data, pos, length) =>
            {
                try
                {
                    return JSON.LoadFromQuickLzBytes(data, pos, length);
                }
                catch (Exception ex)
                {
                    Logger.Error("ltnetServer.Deserializer fail: " + ex.ToString());
                }
                return null;
            };
            if (_netServer.Start(port))
            {
                Logger.Info("Litenet server started at " + port);
            }
            else
            {
                Logger.Info("Litenet server start fail at " + port);
            }
        }

        public override void Update()
        {
            if (_netServer.IsRunning)
            {
                _netServer.PollEvents();
            }

            while (disconnectedPeers.TryDequeue(out var peer))
            {
                removePeer(peer);
            }

            taskRunner.Update();
        }

        public override void Dispose()
        {
            if (_netServer != null)
            {
                _netServer.Stop();
                _netServer = null;
            }

            lock (peerLock)
            {
                peerMap.Clear();
                peerMapIdStrToIdLong.Clear();
                allPeers.Clear();
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
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

                var key = LiteNetIdPrefix + peer.Id;
                peerMap[peer.Id] = peer;
                peer.Tag = key;
                peerMapIdStrToIdLong[key] = peer.Id;
                allPeers.Add(peer);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectReason reason, int socketErrorCode)
        {
            disconnectedPeers.Enqueue(peer);
        }

        private void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Logger.Error("LiteNet OnNetworkError " + endPoint.ToString() + " code " + socketError);
        }

        private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Logger.Info(string.Format("Unconnected msg from {0}, data length {1}, type {2}", remoteEndPoint.ToString(), reader.RawDataSize, messageType));
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            //Debug.Log("[SERVER] peer disconnected " + peer.EndPoint + ", info: " + disconnectInfo.Reason);
            disconnectedPeers.Enqueue(peer);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod, object deserializedData)
        {
            var data = reader.RawData;
            var pos = reader.Position;
            var length = reader.AvailableBytes;
            taskRunner.QueueAction(() =>
            {
                JSONNode package = null;
                try
                {
                    package = JSON.LoadFromQuickLzBytes(data, pos, length);
                }
                catch (Exception ex)//: someone send invalid json
                {
                    Logger.Log("Server receive invalid message:\n" + ex.ToString());
                }
                return package;
            }, (o) =>
            {
                if (peer.ConnectionState != ConnectionState.Connected || o == null)
                {
                    return;
                }
                var package = (JSONNode)o;
                var msgId = package["msgId"].AsInt;
                if (msgId == 0)
                {
                    if (OnClientNotify != null)
                        OnClientNotify((string)peer.Tag, package["route"], package["data"].AsObject);
                }
                else
                {
                    if (OnClientRequest != null)
                        OnClientRequest((string)peer.Tag, msgId, package["route"], package["data"].AsObject);
                }
            });
        }

        private void removePeer(NetPeer peer)
        {
            long peerId = peer.Id;
            string key = peer.Tag as string;
            lock (peerLock)
            {
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

                if (key != null && peerMapIdStrToIdLong.ContainsKey(key))
                {
                    peerMapIdStrToIdLong.Remove(key);
                }
            }
            if (OnRemovePeer != null)
            {
                OnRemovePeer((string)peer.Tag);
            }
        }

        public override void PushToClientsInWorld(GameBanCa world, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            taskRunner.QueueAction(() =>
            {
                try
                {
                    var package = new JSONObject();
                    package["data"] = msg;
                    package["msgId"] = 0;
                    package["route"] = route;
                    return package.SaveToQuickLzBuffer();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in thread pool PushToClientsInWorld " + ex.ToString());
                    return null;
                }
            }, (o) =>
            {
                if (o == null)
                {
                    return;
                }
                SimpleJSON.Buffer content = (SimpleJSON.Buffer)o;
                for (int i = 0, n = world.players.Length; i < n; i++)
                {
                    if (world.players[i] == null)
                        continue;
                    var clientId = world.players[i].PeerId;
                    PushToPeer(clientId, content, option);
                }
                content.Free();
            });
        }

        public override void PushToClients(IEnumerable<string> _peers, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            taskRunner.QueueAction(() =>
            {
                try
                {
                    var package = new JSONObject();
                    package["data"] = msg;
                    package["msgId"] = 0;
                    package["route"] = route;
                    return package.SaveToQuickLzBuffer();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in thread pool PushToClientsInWorld " + ex.ToString());
                    return null;
                }
            }, (o) =>
            {
                if (o == null)
                {
                    return;
                }
                SimpleJSON.Buffer content = (SimpleJSON.Buffer)o;
                foreach (var pid in _peers)
                {
                    PushToPeer(pid, content, option);
                }
                content.Free();
            });
        }

        private DeliveryMethod SendModeToDeliveryMethod(SendMode option)
        {
            switch (option)
            {
                case SendMode.Unreliable:
                    return DeliveryMethod.Unreliable;
                case SendMode.ReliableUnordered:
                    return DeliveryMethod.ReliableUnordered;
                case SendMode.Sequenced:
                    return DeliveryMethod.Sequenced;
                case SendMode.ReliableOrdered:
                    return DeliveryMethod.ReliableOrdered;
                case SendMode.ReliableSequenced:
                    return DeliveryMethod.ReliableSequenced;
            }
            return DeliveryMethod.ReliableOrdered;
        }
        public override void PushToPeer(string clientId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return;
            }

            NetPeer peer = null;
            lock (peerLock)
            {
                var peerId = peerMapIdStrToIdLong.ContainsKey(clientId) ? peerMapIdStrToIdLong[clientId] : -1;
                peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            }
            if (peer != null)
            {
                var _option = SendModeToDeliveryMethod(peer.Mtu - 10 < msg.Length ? SendMode.ReliableOrdered : option);
                peer.Send(msg.ByteBuffer, msg.Start, msg.Length, _option); // force reliable if message is too big
            }
        }

        private void pushToPeer(long peerId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered)
        {
            NetPeer peer = null;
            lock (peerLock)
            {
                peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            }
            if (peer != null)
            {
                var _option = SendModeToDeliveryMethod(peer.Mtu - 10 < msg.Length ? SendMode.ReliableOrdered : option);
                peer.Send(msg.ByteBuffer, msg.Start, msg.Length, _option); // force reliable if message is too big
            }
        }

        public override void PushToClient(string clientId, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            if (string.IsNullOrEmpty(clientId)) // push all
            {
                if (allPeers.Count == 0) return; // but no peer
            }
            else if (!HasPeer(clientId)) // dont have this peer
            {
                return;
            }

            taskRunner.QueueAction(() =>
            {
                try
                {
                    var package = new JSONObject();
                    package["data"] = msg;
                    package["msgId"] = 0;
                    package["route"] = route;
                    return package.SaveToQuickLzBuffer();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in thread pool PushToClient " + ex.ToString());
                    return null;
                }
            }, (o) =>
            {
                if (o == null)
                {
                    return;
                }
                SimpleJSON.Buffer content = (SimpleJSON.Buffer)o;
                if (string.IsNullOrEmpty(clientId))
                {
                    for (int i = 0; i < allPeers.Count; i++)
                    {
                        pushToPeer(allPeers[i].Id, content);
                    }
                }
                else
                {
                    NetPeer peer = null;
                    lock (peerLock)
                    {
                        var peerId = peerMapIdStrToIdLong.ContainsKey(clientId) ? peerMapIdStrToIdLong[clientId] : -1;
                        peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                    }
                    if (peer != null)
                    {
                        pushToPeer(peer.Id, content);
                    }
                    else
                    {
                        Logger.Info("Peer not found " + clientId);
                    }
                }
                content.Free();
            });
        }

        public override void ResponseToClient(string clientId, int msgId, JSONNode msg)
        {
            if (!HasPeer(clientId)) return;
            taskRunner.QueueAction(() =>
            {
                try
                {
                    var package = new JSONObject();
                    package["data"] = msg;
                    package["msgId"] = msgId;
                    return package.SaveToQuickLzBuffer();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in thread pool ResponseToClient " + ex.ToString());
                    return null;
                }
            }, (o) =>
            {
                if (o == null)
                {
                    return;
                }
                SimpleJSON.Buffer content = (SimpleJSON.Buffer)o;
                PushToPeer(clientId, content);
                content.Free();
            });
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
                    long peerId = peer.Id;
                    string key = peer.Tag as string;
                    if (peerMap.ContainsKey(peerId))
                    {
                        peerMap.Remove(peer.Id);
                    }

                    if (key != null && peerMapIdStrToIdLong.ContainsKey(key))
                    {
                        peerMapIdStrToIdLong.Remove(key);
                    }

                    allPeers[i] = allPeers[allPeers.Count - 1];
                    allPeers.RemoveAt(allPeers.Count - 1);
                    n--;
                    i--;

                    if (OnRemovePeer != null)
                    {
                        OnRemovePeer((string)peer.Tag);
                    }
                }
            }
        }

        public override string GetEndPoint(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return "::";
            }
            NetPeer peer = null;
            lock (peerLock)
            {
                var peerId = peerMapIdStrToIdLong.ContainsKey(clientId) ? peerMapIdStrToIdLong[clientId] : -1;
                peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            }
            if (peer != null)
            {
                return peer.EndPoint.Address.ToString();
            }

            return "::";
        }

        public override void Kick(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return;
            }
            NetPeer peer = null;
            if (Monitor.TryEnter(peerLock, 1000))
            {
                try
                {
                    var peerId = peerMapIdStrToIdLong.ContainsKey(clientId) ? peerMapIdStrToIdLong[clientId] : -1;
                    peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
                }
                finally
                {
                    Monitor.Exit(peerLock);
                }
            }
            if (peer != null)
            {
                _netServer.DisconnectPeer(peer);
            }
        }

        public override bool IsRunning()
        {
            return _netServer != null && _netServer.IsRunning;
        }

        public override bool HasPeer(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }
            NetPeer peer = null;
            lock (peerLock)
            {
                var peerId = peerMapIdStrToIdLong.ContainsKey(clientId) ? peerMapIdStrToIdLong[clientId] : -1;
                peer = peerMap.ContainsKey(peerId) ? peerMap[peerId] : null;
            }
            return peer != null;
        }
    }
}
