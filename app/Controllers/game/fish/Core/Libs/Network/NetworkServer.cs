using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BanCa.Libs
{
    public enum SendMode : int
    {
        /// <summary>
        /// Packets can be dropped, can be duplicated, can arrive without order.
        /// </summary>
        Unreliable,

        /// <summary>
        /// Packets won't be dropped, won't be duplicated, guaranteed to be delivered, can arrive without order.
        /// </summary>
        ReliableUnordered,

        /// <summary>
        /// Packets can be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        Sequenced,

        /// <summary>
        /// Packets won't be dropped, won't be duplicated, guaranteed to be delivered, will arrive in order.
        /// </summary>
        ReliableOrdered,

        /// <summary>
        /// Packets can be dropped (except the last one), won't be duplicated, guaranteed to be delivered, will arrive in order.
        /// </summary>
        ReliableSequenced
    }

    public abstract class NetworkServer
    {
        public readonly int Port;

        protected Action<string, string, JSONNode> OnClientNotify; // (long clientId, string route, JSONNode msg)
        protected Action<string, int, string, JSONNode> OnClientRequest; // (long clientId, int msgId, string route, JSONNode msg)
        protected Action<string> OnRemovePeer; // long clientId

        public virtual int Count
        {
            get
            {
                return 0;
            }
        }

        public NetworkServer(int port)
        {
            this.Port = port;
        }

        public virtual void SetOnClientNotify(Action<string, string, JSONNode> OnClientNotify)
        {
            this.OnClientNotify = OnClientNotify;
        }

        public virtual void SetOnClientRequest(Action<string, int, string, JSONNode> OnClientRequest)
        {
            this.OnClientRequest = OnClientRequest;
        }

        public virtual void SetOnRemovePeer(Action<string> OnRemovePeer)
        {
            this.OnRemovePeer = OnRemovePeer;
        }

        public abstract void Kick(string clientId);

        public abstract void Update();

        public abstract void Dispose();

        public abstract void PushToClientsInWorld(GameBanCa world, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered);

        public abstract void PushToClients(IEnumerable<string> peers, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered);

        public abstract void PushToPeer(string clientId, SimpleJSON.Buffer msg, SendMode option = SendMode.ReliableUnordered);

        public abstract void PushToClient(string clientId, string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered);

        public void PushAll(string route, JSONNode msg, SendMode option = SendMode.ReliableUnordered)
        {
            this.PushToClient(null, route, msg, option);
        }

        public abstract void ResponseToClient(string clientId, int msgId, JSONNode msg);

        public abstract bool HasPeer(string clientId);
        /// <summary>
        /// Use in maintain only
        /// </summary>
        public abstract void RemoveAllPeers();

        public abstract bool IsRunning();

        public abstract string GetEndPoint(string peerId);
    }
}
