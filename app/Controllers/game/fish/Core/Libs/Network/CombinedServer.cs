using System;
using System.Collections.Generic;
using Network;
using SimpleJSON;

namespace BanCa.Libs
{
    public class CombinedServer : NetworkServer
    {
        private LiteNetServer lnServer;
        private WebsocketServer wsServer;

        public override int Count
        {
            get
            {
                return lnServer.Count + wsServer.Count;
            }
        }

        public readonly int wsPort;
        public CombinedServer(int port, int ws_port, bool ssl = false) : base(port)
        {
            wsPort = ws_port;
            lnServer = new LiteNetServer(port);
            wsServer = new WebsocketServer(ws_port, ssl);
        }

        public override void SetOnClientNotify(Action<string, string, JSONNode> OnClientNotify)
        {
            lnServer.SetOnClientNotify(OnClientNotify);
            wsServer.SetOnClientNotify(OnClientNotify);
        }

        public override void SetOnClientRequest(Action<string, int, string, JSONNode> OnClientRequest)
        {
            lnServer.SetOnClientRequest(OnClientRequest);
            wsServer.SetOnClientRequest(OnClientRequest);
        }

        public override void SetOnRemovePeer(Action<string> OnRemovePeer)
        {
            lnServer.SetOnRemovePeer(OnRemovePeer);
            wsServer.SetOnRemovePeer(OnRemovePeer);
        }

        public override void Dispose()
        {
            var ln = lnServer;
            if (ln != null)
            {
                ln.Dispose();
                lnServer = null;
            }

            var ws = wsServer;
            if (ws != null)
            {
                ws.Dispose();
                wsServer = null;
            }
        }

        private static bool fromLiteNet(string id)
        {
            return id.StartsWith(LiteNetServer.LiteNetIdPrefix);
        }

        public override void PushToClient(string clientId, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            if (string.IsNullOrEmpty(clientId)) // push all
            {
                var ln = lnServer;
                ln?.PushToClient(clientId, route, msg, option);
                var ws = wsServer;
                ws?.PushToClient(clientId, route, msg, option);
            }
            else
            {
                if (fromLiteNet(clientId))
                {
                    var ln = lnServer;
                    ln?.PushToClient(clientId, route, msg, option);
                }
                else
                {
                    var ws = wsServer;
                    ws?.PushToClient(clientId, route, msg, option);
                }
            }
        }

        public override void PushToClientsInWorld(GameBanCa world, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            var package = new JSONObject();
            package["data"] = msg;
            package["msgId"] = 0;
            package["route"] = route;
            var content = default(SimpleJSON.Buffer); //= package.SaveToCompressedBuffer();
            var content2 = default(SimpleJSON.Buffer); //= package.SaveToOriginalLz4();

            for (int i = 0, n = world.players.Length; i < n; i++)
            {
                if (world.players[i] == null)
                    continue;
                var clientId = world.players[i].PeerId;
                if (fromLiteNet(clientId))
                {
                    if (content == null)
                    {
                        content = package.SaveToQuickLzBuffer();
                    }
                    var ln = lnServer;
                    ln?.PushToPeer(clientId, content, option);
                }
                else
                {
                    if (content2 == null)
                    {
                        content2 = package.SaveToMsgPackBuffer();
                    }
                    var ws = wsServer;
                    ws?.PushToPeer(clientId, content2, option);
                }
            }

            if (content != null)
                content.Free();

            if (content2 != null)
                content2.Free();
        }

        public override void PushToClients(IEnumerable<string> peers, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            var package = new JSONObject();
            package["data"] = msg;
            package["msgId"] = 0;
            package["route"] = route;
            var content = default(SimpleJSON.Buffer); //= package.SaveToCompressedBuffer();
            var content2 = default(SimpleJSON.Buffer); //= package.SaveToOriginalLz4();

            foreach (var clientId in peers)
            {
                if (fromLiteNet(clientId))
                {
                    if (content == null)
                    {
                        content = package.SaveToQuickLzBuffer();
                    }
                    var ln = lnServer;
                    ln?.PushToPeer(clientId, content, option);
                }
                else
                {
                    if (content2 == null)
                    {
                        content2 = package.SaveToMsgPackBuffer();
                    }
                    var ws = wsServer;
                    ws?.PushToPeer(clientId, content2, option);
                }
            }

            if (content != null)
                content.Free();

            if (content2 != null)
                content2.Free();
        }

        public override void PushToPeer(string clientId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered)
        {
            if (string.IsNullOrEmpty(clientId)) // push all
            {
                var ln = lnServer;
                ln?.PushToPeer(clientId, msg, option);
                var ws = wsServer;
                ws?.PushToPeer(clientId, msg, option);
            }
            else
            {
                if (fromLiteNet(clientId))
                {
                    var ln = lnServer;
                    ln?.PushToPeer(clientId, msg, option);
                }
                else
                {
                    var ws = wsServer;
                    ws?.PushToPeer(clientId, msg, option);
                }
            }
        }

        public override void RemoveAllPeers()
        {
            var ln = lnServer;
            ln?.RemoveAllPeers();
            var ws = wsServer;
            ws?.RemoveAllPeers();
        }

        public override void ResponseToClient(string clientId, int msgId, JSONNode msg)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return;
            }

            if (fromLiteNet(clientId))
            {
                var ln = lnServer;
                ln?.ResponseToClient(clientId, msgId, msg);
            }
            else
            {
                var ws = wsServer;
                ws?.ResponseToClient(clientId, msgId, msg);
            }
        }

        public override void Update()
        {
            var ln = lnServer;
            ln?.Update();
            var ws = wsServer;
            ws?.Update();
        }

        public override string GetEndPoint(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return "::";
            }

            if (fromLiteNet(clientId))
            {
                var ln = lnServer;
                return ln != null ? ln.GetEndPoint(clientId) : "::";
            }
            else
            {
                var ws = wsServer;
                return ws != null ? ws.GetEndPoint(clientId) : "::";
            }
        }

        public override void Kick(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return;
            }

            if (fromLiteNet(clientId))
            {
                var ln = lnServer;
                ln?.Kick(clientId);
            }
            else
            {
                var ws = wsServer;
                ws?.Kick(clientId);
            }
        }

        public override bool IsRunning()
        {
            var ln = lnServer;
            var ws = wsServer;
            return ln != null && ln.IsRunning() &&
                ws != null && ws.IsRunning();
        }

        public override bool HasPeer(string clientId)
        {
            var ln = lnServer;
            var ws = wsServer;
            return (ln != null && ln.HasPeer(clientId)) ||
                (ws != null && ws.HasPeer(clientId));
        }
    }
}
