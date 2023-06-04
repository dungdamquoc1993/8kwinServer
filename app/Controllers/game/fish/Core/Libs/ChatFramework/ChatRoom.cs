using BanCa.Libs;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
using Utils;

namespace ChatFramework
{
    /// <summary>
    /// Filter bad word or reject message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public delegate string ChatFilter(string message);

    public class ChatRoom
    {
        private int maxChatHistory = 128;
        private LinkedList<ChatEntry> chatHistory = new LinkedList<ChatEntry>();
        private JSONArray chatHistoryCache;

        public ChatFilter Filter;
        public Action<ChatEntry> OnChat;

        public ChatRoom(int maxHistory = 128)
        {
            maxChatHistory = maxHistory;
        }

        private ChatEntry addChatEntry(long userId, string nickname, string message)
        {
            chatHistoryCache = null;
            if (chatHistory.Count >= maxChatHistory)
            {
                var item = chatHistory.First.Value;
                chatHistory.RemoveFirst();
                item.Message = message;
                item.Nickname = nickname;
                item.TimeStamp = TimeUtil.TimeStamp;
                item.UserId = userId;
                item.cache = null;
                chatHistory.AddLast(item);
                return item;
            }
            else
            {
                var item = new ChatEntry()
                {
                    Message = message,
                    Nickname = nickname,
                    TimeStamp = TimeUtil.TimeStamp,
                    UserId = userId
                };
                chatHistory.AddLast(item);
                return item;
            }
        }

        public bool Chat(long userId, string nickname, string message)
        {
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(nickname))
            {
                message = Filter != null ? Filter(message) : message;
                if (!string.IsNullOrEmpty(message))
                {
                    var entry = addChatEntry(userId, nickname, message);
                    if (OnChat != null) OnChat(entry);
                    return true;
                }
            }

            return false;
        }

        public JSONArray GetChatHistory()
        {
            if (chatHistoryCache == null)
            {
                var arr = new JSONArray();
                foreach (var item in chatHistory)
                {
                    arr.Add(item.ToJson());
                }
                chatHistoryCache = arr;
            }
            return chatHistoryCache;
        }

        public void LoadChatHistory(JSONArray data)
        {
            chatHistory.Clear();
            for (int i = 0, n = data.Count; i < n; i++)
            {
                var item = new ChatEntry();
                item.ParseJson(data[i]);
                chatHistory.AddLast(item);
            }
            chatHistoryCache = data;
        }
    }
}
