using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Utils;

namespace Messaging
{
    /// <summary>
    /// Send message internal in request response manner
    /// </summary>
    public class ClientServer
    {
        public delegate void ClientServerHandle(string route, JSONNode msg, Action<JSONNode> cb);

        private static ConcurrentDictionary<string, ClientServerHandle> handles = new ConcurrentDictionary<string, ClientServerHandle>();

        public static void RegisterHandle(string route, ClientServerHandle handle)
        {
            if (!string.IsNullOrEmpty(route))
                handles[route] = handle;
        }

        public static void RemoveHandle(string route)
        {
            if (!string.IsNullOrEmpty(route))
                handles.TryRemove(route, out var handle);
        }

        public static void Request(string route, JSONNode msg, Action<JSONNode> responseCb)
        {
            if (!string.IsNullOrEmpty(route) && handles.TryGetValue(route, out var handle) && handle != null)
            {
                handle(route, msg, responseCb);
            }
        }

        public static async Task<JSONNode> RequestAsync(string route, JSONNode msg)
        {
            if (!string.IsNullOrEmpty(route) && handles.TryGetValue(route, out var handle) && handle != null)
            {
                var t = new TaskCompletionSource<JSONNode>();
                handle(route, msg, s => t.TrySetResult(s));
                return await t.Task;
            }
            return null;
        }
    }
}
