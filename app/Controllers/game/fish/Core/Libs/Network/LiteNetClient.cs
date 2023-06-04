using LiteNetLib;
using LiteNetLib.Utils;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace BanCa.Libs
{
    public class LiteNetClient : NetworkClient
    {
        private Dictionary<int, Action<JSONNode>> cbMap = new Dictionary<int, Action<JSONNode>>();

        private static int msgIdCount = 1;
        private NetManager _netClient;

        public LiteNetClient(string ip, int port)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkErrorEvent += OnNetworkError;
            listener.NetworkReceiveEvent += OnNetworkReceive;
                       
            _netClient = new NetManager(listener);
            _netClient.Start();
            _netClient.Connect(ip, port, "cgame");
            _netClient.UpdateTime = 15;
            _netClient.PingInterval = 2000;
            _netClient.Deserializer = (data, pos, length) =>
            {
                JSONNode package = null;
                try
                {
                    package = JSON.LoadFromQuickLzBytes(data, pos, length);
                }
                catch (Exception ex)
                {
                    Logger.Info("Client receive invalid data:\n" + ex.ToString());
                }
                return package;
            };
        }

        public override bool Connected
        {
            get
            {
                if (_netClient != null)
                {
                    var peer = _netClient.FirstPeer;
                    if (peer != null && peer.ConnectionState == ConnectionState.Connected)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Dispose()
        {
            if (_netClient != null)
            {
                _netClient.Stop();
                _netClient = null;
            }
        }

        public override void Update()
        {
            if (_netClient != null)
            {
                _netClient.PollEvents();
            }
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Logger.Info("[CLIENT] We connected to " + peer.EndPoint);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Logger.Info("[CLIENT] We received error " + socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod, object deserializedData)
        {
            if (deserializedData != null)
            {
                var data = deserializedData as JSONNode;
                if (data != null)
                {
                    processPackage(data);
                    return;
                }
            }

            JSONNode package = null;
            try
            {
                package = JSON.LoadFromQuickLzBytes(reader.RawData, reader.Position, reader.AvailableBytes);
            }
            catch (Exception ex)
            {
                Logger.Info("Client receive invalid data:\n" + ex.ToString());
                return;
            }

            processPackage(package);
        }

        private void processPackage(JSONNode package)
        {
            //Logger.Info("process pkg: " + package.ToString());
            var msgId = package["msgId"].AsInt;
            //UnityEngine.Debug.Log("Got Package: " + package["route"] + " " + package.ToString());
            if (msgId == 0) // push
            {
                OnPush(package["route"], package["data"].AsObject);
            }
            else // response
            {
                Action<JSONNode> cb = cbMap.ContainsKey(msgId) ? cbMap[msgId] : null;
                cbMap.Remove(msgId);
                if (cb != null) cb(package["data"].AsObject);
            }
        }

        public int cLatency;
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            cLatency = latency;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info("[CLIENT] We disconnected because " + disconnectInfo.Reason);
            if (OnDisconnect != null) OnDisconnect();
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

        private JSONNode empty = new JSONObject();
        public override void Request(string route, JSONNode msg, Action<JSONNode> cb)
        {
            if (_netClient != null)
            {
                var peer = _netClient.FirstPeer;
                if (peer != null && peer.ConnectionState == ConnectionState.Connected)
                {
                    var package = new JSONObject();
                    package["data"] = msg == null ? empty : msg;
                    package["route"] = route;
                    cbMap[msgIdCount] = cb;
                    package["msgId"] = msgIdCount++;
                    var b = package.SaveToQuickLzBuffer();
                    //peer.Send(b.ByteBuffer, b.Start, b.Length, SendOptions.ReliableUnordered);
                    peer.Send(b.ByteBuffer, b.Start, b.Length, DeliveryMethod.ReliableOrdered);
                    b.Free();
                }
            }
        }

        public override void Notify(string route, JSONNode msg)
        {
            if (_netClient != null)
            {
                var peer = _netClient.FirstPeer;
                if (peer != null && peer.ConnectionState == ConnectionState.Connected)
                {
                    var package = new JSONObject();
                    package["data"] = msg == null ? empty : msg;
                    package["route"] = route;
                    package["msgId"] = 0;
                    var b = package.SaveToQuickLzBuffer();
                    //                    peer.Send(b.ByteBuffer, b.Start, b.Length, SendOptions.Unreliable);
                    peer.Send(b.ByteBuffer, b.Start, b.Length, DeliveryMethod.ReliableUnordered);
                    b.Free();
                }
            }
        }
    }
}
