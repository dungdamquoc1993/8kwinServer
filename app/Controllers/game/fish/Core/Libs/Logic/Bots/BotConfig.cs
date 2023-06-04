using SimpleJSON;

namespace BanCa.Libs.Bots
{
    public class BotConfig
    {
        public static int MinCash1 = 200;
        public static int MaxCash1 = 10000;
        public static int MinCash2 = 10000;
        public static int MaxCash2 = 45000;
        public static int MinCash3 = 50000;
        public static int MaxCash3 = 1000000;

        public static int TimeToJoinMin1Ms = 3000;
        public static int TimeToJoinMax1Ms = 10000;
        public static int TimeToJoinMin2Ms = 8000;
        public static int TimeToJoinMax2Ms = 20000;
        public static int TimeToJoinMin3Ms = 30000;
        public static int TimeToJoinMax3Ms = 60000;

        public static int PlayTimeMinMs = 60000;
        public static int PlayTimeMaxMs = 300000;

        public static int ShootWaitMinMs = 500;
        public static int ShootWaitMaxMs = 3000;

        public static int ShootTimeMinMs = 15000;
        public static int ShootTimeMaxMs = 30000;

        public static int QuitTimeByTimeOutMinMs = 2000;
        public static int QuitTimeByTimeOutMaxMs = 5000;
        public static int QuitTimeByOutOfCashMinMs = 5000;
        public static int QuitTimeByOutOfCashMaxMs = 15000;

        public static int Bullet2UsagePercent = 15;
        public static int Bullet3UsagePercent = 15;
        public static int Bullet4UsagePercent = 40;

        public static int MaxBotInRoom = 2;
        public static int MaxAllowBot = 50;
        public static bool BotActive = true;

        public static JSONNode ToJson()
        {
            var data = new JSONObject();
            data["MinCash1"] = MinCash1;
            data["MaxCash1"] = MaxCash1;
            data["MinCash2"] = MinCash2;
            data["MaxCash2"] = MaxCash2;
            data["MinCash3"] = MinCash3;
            data["MaxCash3"] = MaxCash3;

            data["TimeToJoinMin1Ms"] = TimeToJoinMin1Ms;
            data["TimeToJoinMax1Ms"] = TimeToJoinMax1Ms;
            data["TimeToJoinMin2Ms"] = TimeToJoinMin2Ms;
            data["TimeToJoinMax2Ms"] = TimeToJoinMax2Ms;
            data["TimeToJoinMin3Ms"] = TimeToJoinMin3Ms;
            data["TimeToJoinMax3Ms"] = TimeToJoinMax3Ms;

            data["PlayTimeMinMs"] = PlayTimeMinMs;
            data["PlayTimeMaxMs"] = PlayTimeMaxMs;

            data["ShootWaitMinMs"] = ShootWaitMinMs;
            data["ShootWaitMaxMs"] = ShootWaitMaxMs;

            data["ShootTimeMinMs"] = ShootTimeMinMs;
            data["ShootTimeMaxMs"] = ShootTimeMaxMs;

            data["QuitTimeByTimeOutMinMs"] = QuitTimeByTimeOutMinMs;
            data["QuitTimeByTimeOutMaxMs"] = QuitTimeByTimeOutMaxMs;
            data["QuitTimeByOutOfCashMinMs"] = QuitTimeByOutOfCashMinMs;
            data["QuitTimeByOutOfCashMaxMs"] = QuitTimeByOutOfCashMaxMs;

            data["Bullet2UsagePercent"] = Bullet2UsagePercent;
            data["Bullet3UsagePercent"] = Bullet3UsagePercent;
            data["Bullet4UsagePercent"] = Bullet4UsagePercent;
            data["MaxAllowBot"] = MaxAllowBot;
            data["MaxBotInRoom"] = MaxBotInRoom;

            data["BotActive"] = BotActive ? 1 : 0;
            return data;
        }

        public static void ParseJson(JSONNode data)
        {
            if (data.HasKey("MinCash1")) MinCash1 = data["MinCash1"].AsInt;
            if (data.HasKey("MaxCash1")) MaxCash1 = data["MaxCash1"].AsInt;
            if (data.HasKey("MinCash2")) MinCash2 = data["MinCash2"].AsInt;
            if (data.HasKey("MaxCash2")) MaxCash2 = data["MaxCash2"].AsInt;
            if (data.HasKey("MinCash3")) MinCash3 = data["MinCash3"].AsInt;
            if (data.HasKey("MaxCash3")) MaxCash3 = data["MaxCash3"].AsInt;

            if (data.HasKey("TimeToJoinMin1Ms")) TimeToJoinMin1Ms = data["TimeToJoinMin1Ms"].AsInt;
            if (data.HasKey("TimeToJoinMax1Ms")) TimeToJoinMax1Ms = data["TimeToJoinMax1Ms"].AsInt;
            if (data.HasKey("TimeToJoinMin2Ms")) TimeToJoinMin2Ms = data["TimeToJoinMin2Ms"].AsInt;
            if (data.HasKey("TimeToJoinMax2Ms")) TimeToJoinMax2Ms = data["TimeToJoinMax2Ms"].AsInt;
            if (data.HasKey("TimeToJoinMin3Ms")) TimeToJoinMin3Ms = data["TimeToJoinMin3Ms"].AsInt;
            if (data.HasKey("TimeToJoinMax3Ms")) TimeToJoinMax3Ms = data["TimeToJoinMax3Ms"].AsInt;

            if (data.HasKey("PlayTimeMinMs")) PlayTimeMinMs = data["PlayTimeMinMs"].AsInt;
            if (data.HasKey("PlayTimeMaxMs")) PlayTimeMaxMs = data["PlayTimeMaxMs"].AsInt;

            if (data.HasKey("ShootWaitMinMs")) ShootWaitMinMs =data["ShootWaitMinMs"].AsInt;
            if (data.HasKey("ShootWaitMaxMs")) ShootWaitMaxMs= data["ShootWaitMaxMs"].AsInt;

            if (data.HasKey("ShootTimeMinMs")) ShootTimeMinMs= data["ShootTimeMinMs"].AsInt;
            if (data.HasKey("ShootTimeMaxMs")) ShootTimeMaxMs =data["ShootTimeMaxMs"].AsInt;

            if (data.HasKey("QuitTimeByTimeOutMinMs")) QuitTimeByTimeOutMinMs= data["QuitTimeByTimeOutMinMs"].AsInt;
            if (data.HasKey("QuitTimeByTimeOutMaxMs")) QuitTimeByTimeOutMaxMs= data["QuitTimeByTimeOutMaxMs"].AsInt;
            if (data.HasKey("QuitTimeByOutOfCashMinMs")) QuitTimeByOutOfCashMinMs= data["QuitTimeByOutOfCashMinMs"].AsInt;
            if (data.HasKey("QuitTimeByOutOfCashMaxMs")) QuitTimeByOutOfCashMaxMs= data["QuitTimeByOutOfCashMaxMs"].AsInt;

            if (data.HasKey("Bullet2UsagePercent")) Bullet2UsagePercent= data["Bullet2UsagePercent"].AsInt;
            if (data.HasKey("Bullet3UsagePercent")) Bullet3UsagePercent =data["Bullet3UsagePercent"].AsInt;
            if (data.HasKey("Bullet4UsagePercent")) Bullet4UsagePercent=data["Bullet4UsagePercent"].AsInt;
            if (data.HasKey("MaxAllowBot")) MaxAllowBot = data["MaxAllowBot"].AsInt;
            if (data.HasKey("MaxBotInRoom")) MaxBotInRoom = data["MaxBotInRoom"].AsInt;

            if (data.HasKey("BotActive")) BotActive = data["BotActive"].AsInt != 0;
        }
    }
}
