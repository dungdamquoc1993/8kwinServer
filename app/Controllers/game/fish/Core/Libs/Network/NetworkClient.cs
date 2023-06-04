using SimpleJSON;
using System;

namespace BanCa.Libs
{
    public abstract class NetworkClient
    {
        public Action<string, JSONNode> OnPush; // (string route, JSONNode msg)
        public Action OnDisconnect;

        public virtual bool Connected
        {
            get
            {
                return false;
            }
        }

        public abstract void Dispose();

        public abstract void Update();

        public abstract void Request(string route, JSONNode msg, Action<JSONNode> cb);

        public abstract void Notify(string route, JSONNode msg);
    }
}
