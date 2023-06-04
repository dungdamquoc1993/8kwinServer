using SimpleJSON;

namespace BanCa.Libs
{
    public class Player
    {
        public long Id { get; set; }
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string Avatar { get; set; }
        public long Cash { get; set; }
        public long CardIn { get; set; }
        public long Exp { get; set; }
        public int Level { get; set; }

        internal string PeerId { get; set; }
        internal int PosIndex { get; set; }
        internal long nextExp { get; set; }
        internal long Profit { get; set; }
        internal float PlayTimeS { get; set; }
        internal float LogCashTimeS { get; set; }
        internal Config.PowerUp Item { get; set; }
        internal float ItemDuration { get; set; }
        internal long CashGain { get; set; }
        internal long ExpGain { get; set; }
        internal float IdleTimeS { get; set; }
        internal long PendingPushCash { get; set; }
        internal float TimeToPush { get; set; }

        internal long TimeUseSnipe { get; set; }
        internal long TimeUseRapidFire { get; set; }
        internal bool IsBot { get; set; } = false;

        public Player()
        {
            SetLevel(1);
            Item = Config.PowerUp.None;
            PlayerId = Nickname = Avatar = string.Empty;
            PendingPushCash = 0;
            TimeToPush = -1;
        }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["id"] = Id;
            data["playerId"] = PlayerId;
            data["nickname"] = Nickname;
            data["avatar"] = Avatar;
            data["posIndex"] = PosIndex;
            data["cash"] = Cash;
            data["level"] = Level;
            data["exp"] = Exp;
            data["cashGain"] = CashGain;
            data["expGain"] = ExpGain;
            data["rfire"] = TimeUseRapidFire;
            data["snipe"] = TimeUseSnipe;

            if (Item != Config.PowerUp.None)
            {
                data["item"] = (int)Item;
                data["duration"] = ItemDuration;
            }
            return data;
        }

        public void ParseJson(JSONNode data)
        {
            Id = data["id"].AsLong;
            PlayerId = data["playerId"];
            Nickname = data["nickname"];
            Avatar = data["avatar"];
            PosIndex = data["posIndex"].AsInt;
            Cash = data["cash"].AsLong;
            Level = data["level"].AsInt;
            Exp = data["exp"].AsLong;
            CashGain = data["cashGain"].AsLong;
            ExpGain = data["expGain"].AsLong;
            TimeUseRapidFire = data["rfire"].AsLong;
            TimeUseSnipe = data["snipe"].AsLong;

            if (data.HasKey("item"))
            {
                Item = (Config.PowerUp)data["item"].AsInt;
                ItemDuration = data["duration"].AsFloat;
            }
        }

        public JSONNode GetItemJson()
        {
            var data = new JSONObject();
            data["playerId"] = PlayerId;

            if (Item != Config.PowerUp.None)
            {
                data["item"] = (int)Item;
                data["duration"] = ItemDuration;
            }
            return data;
        }

        public void ParseItemJson(JSONNode data)
        {
            if (data.HasKey("item"))
            {
                Item = (Config.PowerUp)data["item"].AsInt;
                ItemDuration = data["duration"].AsFloat;
            }
        }

        public JSONNode GetLvJson()
        {
            var data = new JSONObject();
            data["playerId"] = PlayerId;
            data["exp"] = Exp;
            data["lv"] = Level;
            return data;
        }

        public void ParseLvJson(JSONNode data)
        {
            Exp = data["exp"].AsLong;
            Level = data["lv"].AsInt;
            nextExp = Config.GetExpToNextLevel(Level);
        }

        public void SetLevel(int lv)
        {
            Level = lv;
            nextExp = Config.GetExpToNextLevel(Level);
        }

        public bool DoLevelUpIfApplicable()
        {
            if(Exp < 0) // out of bound?
            {
                Exp = 0;
            }
            else if (Exp >= nextExp)
            {
                Exp -= nextExp;
                Level += 1;
                nextExp = Config.GetExpToNextLevel(Level);
                return true;
            }
            return false;
        }
    }
}
