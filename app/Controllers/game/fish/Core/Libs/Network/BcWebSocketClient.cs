using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;

namespace BanCa.Libs
{
    public class BcWebSocketClient : NetworkClient
    {
        private Dictionary<int, Action<JSONNode>> cbMap = new Dictionary<int, Action<JSONNode>>();

        private static int msgIdCount = 1;
        private WebSocket socket;
        private List<byte[]> queues = new List<byte[]>();
        private bool connected = false;

        private volatile bool disconnecting = false;

        public BcWebSocketClient(string ip, int port, bool wss)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                var url = wss ? string.Format("wss://{0}:{1}/", ip, port) : string.Format("ws://{0}:{1}/", ip, port);
                Logger.Log("Connecting to " + url);
                var ws = new WebSocket(url);
                ws.OnMessage += (sender, e) =>
                {
                    if (e.IsBinary && e.RawData != null)
                        lock (queues) queues.Add(e.RawData);
                };
                ws.OnError += (sender, e) =>
                {
                    Logger.Error("Ws error: " + e.Message);
                };
                ws.OnClose += (sender, e) =>
                {
                    connected = false;
                    disconnecting = true;
                };
                ws.Connect();
                socket = ws;
                connected = true;
            });
        }

        public override bool Connected
        {
            get
            {
                if (socket != null)
                {
                    return connected;
                }

                return false;
            }
        }

        public override void Dispose()
        {
            if (socket != null)
            {
                lock (socket)
                {
                    socket.Close();
                    socket = null;
                }
                connected = false;
            }
        }

        public override void Update()
        {
            lock (queues)
            {
                for (int i = 0; i < queues.Count; i++)
                {
                    var data = queues[i];
                    onNetworkReceive(data);
                }
                queues.Clear();
            }

            if (disconnecting)
            {
                disconnecting = false;
                if (OnDisconnect != null) OnDisconnect();
            }
        }

        private void onNetworkReceive(byte[] data)
        {
            JSONNode package = null;
            try
            {
                package = JSON.LoadFromMsgPackBytes(data, 0, data.Length);
                processPackage(package);
            }
            catch (Exception ex)
            {
                Logger.Log("Client receive invalid data:\n" + ex.ToString());
                return;
            }
        }

        private void processPackage(JSONNode package)
        {
            //Logger.Info("process pkg: " + package.ToString());
            var msgId = package["msgId"].AsInt;
            if (msgId == 0) // push
            {
                OnPush(package["route"], package["data"].AsObject);
            }
            else // response
            {
                Action<JSONNode> cb = null;
                lock (cbMap)
                {
                    cb = cbMap.ContainsKey(msgId) ? cbMap[msgId] : null;
                    cbMap.Remove(msgId);
                }
                if (cb != null) cb(package["data"].AsObject);
            }
        }

        private JSONNode empty = new JSONObject();
        public override void Request(string route, JSONNode msg, Action<JSONNode> cb)
        {
            if (Connected)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    var package = new JSONObject();
                    package["data"] = msg == null ? empty : msg;
                    package["route"] = route;
                    lock (cbMap) cbMap[msgIdCount] = cb;
                    package["msgId"] = msgIdCount++;
                    var b = package.SaveToMsgPackBuffer();
                    lock (socket) socket.Send(b.ByteBuffer, b.Start, b.Length);
                    b.Free();
                });
            }
        }

        public override void Notify(string route, JSONNode msg)
        {
            if (Connected)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    var package = new JSONObject();
                    package["data"] = msg == null ? empty : msg;
                    package["route"] = route;
                    package["msgId"] = 0;
                    var b = package.SaveToMsgPackBuffer();
                    lock (socket) socket.Send(b.ByteBuffer, b.Start, b.Length);
                    b.Free();
                });
            }
        }
    }
}
