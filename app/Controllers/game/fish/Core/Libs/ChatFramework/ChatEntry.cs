using SimpleJSON;

namespace ChatFramework
{
    public class ChatEntry
    {
        public long UserId;
        public string Nickname;
        public string Message;
        public long TimeStamp;
        internal JSONNode cache;

        public JSONNode ToJson()
        {
            if (cache != null)
            {
                return cache;
            }
            else
            {
                JSONNode data = new JSONObject();
                data["userId"] = UserId;
                data["nickname"] = Nickname;
                data["msg"] = Message;
                data["time"] = TimeStamp;
                cache = data;
                return data;
            }
        }

        public void ParseJson(JSONNode data)
        {
            UserId = data["userId"];
            Nickname = data["nickname"];
            Message = data["msg"];
            TimeStamp = data["time"];
            cache = data;
        }
    }
}
