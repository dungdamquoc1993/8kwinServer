using SimpleJSON;
using System;
using System.Collections.Generic;

namespace BanCa.Libs
{
    public class Config
    {
        static Config()
        {
            var json = JSON.Parse("{\r\n    \"TableStartingBlinds\": [\r\n      1,\r\n      10000,\r\n      100000,\r\n      1000000\r\n    ],\r\n    \"TableBulletValueRate\": [\r\n      1,\r\n      1,\r\n      10,\r\n      100\r\n    ],\r\n    \"TableRequireCardIn\": [\r\n      0,\r\n      0,\r\n      0,\r\n      0\r\n    ],\r\n    \"NumberOfAccountPerDevice\": 1000,\r\n    \"NumberOfAccountPerDay\": 3,\r\n    \"RequestSampleRateMs\": 2000,\r\n    \"MaxRequestPerSecond\": 15,\r\n    \"LogShooting\": 1,\r\n    \"MinTimeBetweenShooting\": 125,\r\n    \"TimeBetweenVideoAdsMs0\": 10000,\r\n    \"TimeBetweenVideoAdsMs1\": 10000,\r\n    \"VideoAdsRewardCount0\": 3,\r\n    \"VideoAdsRewardCount1\": 5,\r\n    \"VideoAdsRewardType0\": 100,\r\n    \"VideoAdsRewardType1\": 50,\r\n    \"KickDuplicateUsers\": 1,\r\n    \"HideFishHp\": 1,\r\n    \"PowerUpIntervalS\": 300,\r\n    \"PowerUpMinPlayTimeS\": 120,\r\n    \"PowerUpDurationS\": 15,\r\n    \"BombRate\": 0,\r\n    \"BonusRate\": 45,\r\n    \"FastFireCoolDownS\": 1,\r\n    \"FastFireRate\": 2,\r\n    \"FastFireDuration\": 10,\r\n    \"FastFireCost\": 20,\r\n    \"SnipeCoolDownS\": 1,\r\n    \"SnipeDurationS\": 10,\r\n    \"SnipeCost\": 20,\r\n    \"FeeRate\": 0.02,\r\n    \"BombThreshold\": 5000,\r\n    \"JackpotInitial\": 7300,\r\n    \"JackpotRate\": 0.02,\r\n    \"FIRE_RATE\": 4,\r\n    \"TURN_TIME\": 0.25,\r\n    \"MIN_SPEED_2\": 0.01,\r\n    \"MAX_JUMP_SPEED\": 150,\r\n    \"MAX_JUMP_SPEED_2\": 22500,\r\n    \"MAX_SHADOW_SPEED_2\": 25600,\r\n    \"TELEPORT_SHADOW_SPEED_2\": 250000,\r\n    \"JUMP_ACCELERATION_OCTOPUS\": 150,\r\n    \"JUMP_DECCELERATION_OCTOPUS\": 150,\r\n    \"JUMP_ACCELERATION_CUTTLE\": 150,\r\n    \"JUMP_DECCELERATION_CUTTLE\": 150,\r\n    \"JUMP_ACCELERATION_SEA_TURTLE\": 150,\r\n    \"JUMP_DECCELERATION_SEA_TURTLE\": 150,\r\n    \"MIN_SPEED\": 40,\r\n    \"MAX_VARY_SPEED\": 80,\r\n    \"TURN_ANGLE_RAD\": 0.01744444,\r\n    \"JUMP_TURN_ANGLE_RAD\": 0.01744444,\r\n    \"MAX_BOUND_TIME\": 4,\r\n    \"BulletSpeed\": 1400,\r\n    \"BulletRadius\": 22,\r\n    \"MaxIdleTimeS\": 120,\r\n    \"a\": 8,\r\n    \"b\": 32,\r\n    \"c\": 1024,\r\n    \"d\": 2048,\r\n    \"MinScale\": 1,\r\n    \"MaxScale\": 1.05,\r\n    \"MaxFillScale\": 1.2,\r\n    \"NUMBER_OF_BULLETS\": 100,\r\n    \"NUMBER_OF_OBJECTS\": 60,\r\n    \"PLAYING_WAVE_DURATION\": 600,\r\n    \"NEW_WAVE_MAX_TIME\": 120,\r\n    \"SOLO_DURATION\": 120,\r\n    \"WAITING_WAVE_DURATION\": 30,\r\n    \"GUN_LENGTH\": 120,\r\n    \"GOLDEN_FROG_WIN_RATE\": [\r\n      1,\r\n      5,\r\n      12,\r\n      16,\r\n      10\r\n    ],\r\n    \"GOLDEN_FROG_WIN_MULTIPLE\": [\r\n      5,\r\n      4,\r\n      3,\r\n      2,\r\n      0.5\r\n    ],\r\n    \"ScreenW\": 1280,\r\n    \"ScreenH\": 720,\r\n    \"ScreenX\": -640,\r\n    \"ScreenY\": -360,\r\n    \"WorldW\": 2400,\r\n    \"WorldH\": 1700,\r\n    \"WorldX\": -1200,\r\n    \"WorldY\": -850,\r\n    \"OutWorldW\": 5000,\r\n    \"OutWorldH\": 4000,\r\n    \"OutWorldX\": -2500,\r\n    \"OutWorldY\": -2000,\r\n    \"SpawnW\": 2000,\r\n    \"SpawnH\": 1600,\r\n    \"SpawnX\": -1000,\r\n    \"SpawnY\": -800,\r\n    \"SERVER_UPDATE_LOOP_MS\": 17,\r\n    \"CLIENT_UPDATE_S\": 0.033,\r\n    \"playerPos\": [\r\n      {\r\n        \"x\": -320,\r\n        \"y\": -320\r\n      },\r\n      {\r\n        \"x\": 320,\r\n        \"y\": -320\r\n      },\r\n      {\r\n        \"x\": 320,\r\n        \"y\": 320\r\n      },\r\n      {\r\n        \"x\": -320,\r\n        \"y\": 320\r\n      }\r\n    ],\r\n    \"TypeToPower\": {\r\n      \"Basic\": 1,\r\n      \"Bullet1\": 100,\r\n      \"Bullet2\": 100,\r\n      \"Bullet3\": 100,\r\n      \"Bullet4\": 100,\r\n      \"Bullet5\": 100,\r\n      \"Bullet6\": 100\r\n    },\r\n    \"TypeToValue\": {\r\n      \"Basic\": 0,\r\n      \"Bullet1\": 10,\r\n      \"Bullet2\": 20,\r\n      \"Bullet3\": 50,\r\n      \"Bullet4\": 100,\r\n      \"Bullet5\": 200,\r\n      \"Bullet6\": 500\r\n    },\r\n    \"FishPhysicalData\": {\r\n      \"Basic\": {\r\n        \"Width\": 2,\r\n        \"Height\": 2,\r\n        \"Health\": 0,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.2\r\n      },\r\n      \"Cuttle\": {\r\n        \"Width\": 22,\r\n        \"Height\": 46,\r\n        \"Health\": 200,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"GoldFish\": {\r\n        \"Width\": 29,\r\n        \"Height\": 49,\r\n        \"Health\": 300,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"LightenFish\": {\r\n        \"Width\": 22,\r\n        \"Height\": 46,\r\n        \"Health\": 400,\r\n        \"HealthRate\": 0.9,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Mermaid\": {\r\n        \"Width\": 33,\r\n        \"Height\": 45,\r\n        \"Health\": 500,\r\n        \"HealthRate\": 0.95,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Octopus\": {\r\n        \"Width\": 32,\r\n        \"Height\": 61,\r\n        \"Health\": 600,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"PufferFish\": {\r\n        \"Width\": 27,\r\n        \"Height\": 41,\r\n        \"Health\": 700,\r\n        \"HealthRate\": 0.8,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"SeaFish\": {\r\n        \"Width\": 30,\r\n        \"Height\": 58,\r\n        \"Health\": 800,\r\n        \"HealthRate\": 0.8,\r\n        \"HealthScale\": 0.2\r\n      },\r\n      \"Shark\": {\r\n        \"Width\": 46,\r\n        \"Height\": 61,\r\n        \"Health\": 900,\r\n        \"HealthRate\": 0.9,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Stringray\": {\r\n        \"Width\": 37,\r\n        \"Height\": 82,\r\n        \"Health\": 1000,\r\n        \"HealthRate\": 0.95,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Turtle\": {\r\n        \"Width\": 26,\r\n        \"Height\": 66,\r\n        \"Health\": 1100,\r\n        \"HealthRate\": 0.7,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"CaThanTai\": {\r\n        \"Width\": 28,\r\n        \"Height\": 64,\r\n        \"Health\": 1200,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.2\r\n      },\r\n      \"FlyingFish\": {\r\n        \"Width\": 35,\r\n        \"Height\": 72,\r\n        \"Health\": 1300,\r\n        \"HealthRate\": 0.9,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"GoldenFrog\": {\r\n        \"Width\": 100,\r\n        \"Height\": 436,\r\n        \"Health\": 6600,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"SeaTurtle\": {\r\n        \"Width\": 100,\r\n        \"Height\": 50,\r\n        \"Health\": 1400,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"MerMan\": {\r\n        \"Width\": 46,\r\n        \"Height\": 62,\r\n        \"Health\": 1500,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Phoenix\": {\r\n        \"Width\": 61,\r\n        \"Height\": 63,\r\n        \"Health\": 1600,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"MermaidBig\": {\r\n        \"Width\": 100,\r\n        \"Height\": 88,\r\n        \"Health\": 1700,\r\n        \"HealthRate\": 0.98,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"MermaidSmall\": {\r\n        \"Width\": 40,\r\n        \"Height\": 143,\r\n        \"Health\": 1800,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"BombFish\": {\r\n        \"Width\": 61,\r\n        \"Height\": 175,\r\n        \"Health\": 1900,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish19\": {\r\n        \"Width\": 46,\r\n        \"Height\": 175,\r\n        \"Health\": 2000,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish20\": {\r\n        \"Width\": 50,\r\n        \"Height\": 145,\r\n        \"Health\": 2100,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish21\": {\r\n        \"Width\": 56,\r\n        \"Height\": 175,\r\n        \"Health\": 2200,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish22\": {\r\n        \"Width\": 63,\r\n        \"Height\": 187,\r\n        \"Health\": 2300,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish23\": {\r\n        \"Width\": 100,\r\n        \"Height\": 261,\r\n        \"Health\": 2400,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish24\": {\r\n        \"Width\": 100,\r\n        \"Height\": 301,\r\n        \"Health\": 2500,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      },\r\n      \"Fish25\": {\r\n        \"Width\": 100,\r\n        \"Height\": 301,\r\n        \"Health\": 0,\r\n        \"HealthRate\": 1,\r\n        \"HealthScale\": 0.1\r\n      }\r\n    },\r\n    \"FakeJpMinTimeMS\": {\r\n      \"11\": 300000,\r\n      \"12\": 300000,\r\n      \"13\": 300000,\r\n      \"14\": 300000,\r\n      \"21\": 600000,\r\n      \"22\": 600000,\r\n      \"23\": 600000,\r\n      \"24\": 600000,\r\n      \"31\": 3600000,\r\n      \"32\": 3600000,\r\n      \"33\": 3600000,\r\n      \"34\": 3600000\r\n    },\r\n    \"FakeJpMaxTimeMS\": {\r\n      \"11\": 1800000,\r\n      \"12\": 1800000,\r\n      \"13\": 1800000,\r\n      \"14\": 1800000,\r\n      \"21\": 3600000,\r\n      \"22\": 3600000,\r\n      \"23\": 3600000,\r\n      \"24\": 3600000,\r\n      \"31\": 21600000,\r\n      \"32\": 21600000,\r\n      \"33\": 21600000,\r\n      \"34\": 21600000\r\n    },\r\n    \"MinAddFakeJp\": {\r\n      \"11\": 100,\r\n      \"12\": 100,\r\n      \"13\": 100,\r\n      \"14\": 100,\r\n      \"21\": 300,\r\n      \"22\": 300,\r\n      \"23\": 300,\r\n      \"24\": 300,\r\n      \"31\": 200,\r\n      \"32\": 200,\r\n      \"33\": 200,\r\n      \"34\": 200\r\n    },\r\n    \"MaxAddFakeJp\": {\r\n      \"11\": 500,\r\n      \"12\": 500,\r\n      \"13\": 500,\r\n      \"14\": 500,\r\n      \"21\": 1500,\r\n      \"22\": 1500,\r\n      \"23\": 1500,\r\n      \"24\": 1500,\r\n      \"31\": 1000,\r\n      \"32\": 1000,\r\n      \"33\": 1000,\r\n      \"34\": 1000\r\n    },\r\n    \"SoloFee\": 0.05,\r\n    \"SoloSnipe\": 3,\r\n    \"SoloFastFire\": 3,\r\n    \"SoloBomb\": 3,\r\n    \"SoloItemBombDamage\": 300,\r\n    \"TableSoloCashIn\": [\r\n      1,\r\n      5000,\r\n      50000,\r\n      500000\r\n    ],\r\n    \"TableSoloVirtualCash\": [\r\n      1,\r\n      5000,\r\n      50000,\r\n      500000\r\n    ],\r\n    \"UserCardInOnOff\": 1,\r\n    \"UserHighCash\": 20000,\r\n    \"UserMidCash\": 10000,\r\n    \"UserCardInHighCash\": 20000,\r\n    \"UserCardInMidCash\": 10000,\r\n    \"MinimumRefundUserBankRate\": [\r\n      1.3,\r\n      1.2,\r\n      1.1\r\n    ],\r\n    \"MinimumRefundUserCardInBankRate\": [\r\n      1.2,\r\n      1.1,\r\n      1\r\n    ]\r\n  }");
            Config.ParseJson(json);
        }

        public static Action OnConfigChange;

        public static volatile bool IsMaintain = false;

        public static volatile int NumberOfAccountPerDevice = 5;
        public static volatile int NumberOfAccountPerDay = 3;

        public static volatile int RequestSampleRateMs = 2000;
        public static volatile float MaxRequestPerSecond = 20f;
        public static volatile int LogShooting = 1;
        public static volatile int MinTimeBetweenShooting = 125;

        public static volatile int TimeBetweenVideoAdsMs0 = 10 * 1000;
        public static volatile int TimeBetweenVideoAdsMs1 = 10 * 1000;
        public static volatile int VideoAdsRewardType0 = 50;
        public static volatile int VideoAdsRewardCount0 = 3;
        public static volatile int VideoAdsRewardType1 = 10;
        public static volatile int VideoAdsRewardCount1 = 5;
        public static volatile int KickDuplicateUsers = 1;
        public static volatile int HideFishHp = 1;

        #region Table
        public static volatile long[] TableStartingBlinds = new long[] { 1, 1000, 10000, 100000 };
        public static volatile int[] TableBulletValueRate = new int[] { 1, 1, 10, 100 };
        public static volatile long[] TableRequireCardIn = new long[] { 0, 0, 10000, 20000 };

        // World
        public volatile static float NUMBER_OF_BULLETS = 100;
        public volatile static float NUMBER_OF_OBJECTS = 60; //60
        public volatile static float NEW_WAVE_MAX_TIME = 120f;
        public volatile static float SOLO_DURATION = 120f;
        public volatile static float PLAYING_WAVE_DURATION = 600f; //10f * 60; // 2592000; // a month :) // 15f * 60; // time between wave
        public volatile static float WAITING_WAVE_DURATION = 30; // //30;
        public volatile static float GUN_LENGTH = 120;
        public volatile static Vector[] playerPos = new Vector[] { new Vector(-320, -320), new Vector(320, -320), new Vector(320, 320), new Vector(-320, 320) };

        public volatile static float ScreenW = 1280, ScreenH = 720, ScreenX = -ScreenW / 2, ScreenY = -ScreenH / 2;
        public volatile static float WorldW = 2400, WorldH = 1700, WorldX = -WorldW / 2, WorldY = -WorldH / 2;
        public volatile static float OutWorldW = 5000, OutWorldH = 4000, OutWorldX = -OutWorldW / 2, OutWorldY = -OutWorldH / 2;
        public volatile static float SpawnW = 2000, SpawnH = 1600, SpawnX = -SpawnW / 2, SpawnY = -SpawnH / 2;

        public volatile static int SERVER_UPDATE_LOOP_MS = 17;
        public volatile static float CLIENT_UPDATE_S = 0.033f;

        public volatile static Dictionary<int, int> FakeJpMinTimeMS = new Dictionary<int, int>
            { {11, 5 * 60 * 1000}, {12, 5 * 60 * 1000}, {13, 5 * 60 * 1000}, {14, 5 * 60 * 1000},
              {21, 10 * 60 * 1000}, {22, 10 * 60 * 1000}, {23, 10 * 60 * 1000}, {24, 10 * 60 * 1000},
              {31, 60 * 60 * 1000},{32, 60 * 60 * 1000},{33, 60 * 60 * 1000},{34, 60 * 60 * 1000},
        };
        public volatile static Dictionary<int, int> FakeJpMaxTimeMS = new Dictionary<int, int>
            { {11, 30 * 60 * 1000},{12, 30 * 60 * 1000},{13, 30 * 60 * 1000},{14, 30 * 60 * 1000},
              {21, 60 * 60 * 1000},{22, 60 * 60 * 1000},{23, 60 * 60 * 1000},{24, 60 * 60 * 1000},
              {31, 6 * 60 * 60 * 1000},{32, 6 * 60 * 60 * 1000},{33, 6 * 60 * 60 * 1000},{34, 6 * 60 * 60 * 1000},
        };
        public volatile static Dictionary<int, int> MinAddFakeJp = new Dictionary<int, int>
            { {11, 100},{12, 100},{13, 100},{14, 100},
              {21, 300},{22, 300},{23, 300},{24, 300},
              {31, 200},{32, 200},{33, 200},{34, 200},
        };
        public volatile static Dictionary<int, int> MaxAddFakeJp = new Dictionary<int, int>
            { {11, 500},{12, 500},{13, 500},{14, 500},
              {21, 1500},{22, 1500},{23, 1500},{24, 1500},
              {31, 1000},{32, 1000},{33, 1000},{34, 1000},
        };

        /// starting gun sprite, -2 (out of money), -1 (too much money)
        public static int GetStartingBulletType(int index)
        {
            if (index == -1 || index >= 12)
            {
                return 5;
            }

            if (index < TableBulletValueRate.Length && index >= 0)
            {
                return (index / 2) % 6;
            }

            return 0;
        }

        /// = -2 (out of money), -1 (too much money)
        public static int GetTableBlindIndexForPlayer(long startingCash)
        {
            if (startingCash == 0)
                return -2;

            for (int i = 1; i < TableStartingBlinds.Length; i++)
            {
                if (startingCash <= TableStartingBlinds[i] && startingCash > TableStartingBlinds[i - 1])
                    return i - 1;
            }

            //return -1;
            return TableStartingBlinds.Length - 1;
        }

        /// = -2 (out of money), -1 (too much money)
        public static long GetTableBlind(int index)
        {
            if (index == -1)
            {
                return TableStartingBlinds[TableStartingBlinds.Length - 1];
            }

            if (index >= 0 && index < TableStartingBlinds.Length)
            {
                return TableStartingBlinds[index];
            }

            return 0;
        }

        /// index = -2 return 0, index = -1 return max
        public static long GetBulletValue(int index, BulletType type)
        {
            var val = TypeToValue[type];
            if (index == -1)
            {
                var rate = TableBulletValueRate[TableBulletValueRate.Length - 1];
                return (long)rate * val;
            }

            if (index >= 0 && index < TableBulletValueRate.Length)
            {
                var rate = TableBulletValueRate[index];
                return (long)rate * val;
            }

            return 0;
        }
        #endregion

        #region Items
        public static volatile float PowerUpIntervalS = 5 * 60; // power trigger per PowerUpIntervalS second on player with lowest below zero profit with play time > PowerUpMinPlayTimeS
        public static volatile float PowerUpMinPlayTimeS = 2 * 60;
        public static volatile float PowerUpDurationS = 15;
        public static volatile int BombRate = 0;
        public static volatile int BonusRate = 45;

        public static volatile float FastFireCoolDownS = 1;
        public static volatile float FastFireRate = 2;
        public static volatile float FastFireDuration = 10;
        public static volatile int FastFireCost = 20;
        public static volatile float SnipeCoolDownS = 1;
        public static volatile float SnipeDurationS = 10;
        public static volatile int SnipeCost = 20;

        public enum PowerUp : int
        {
            None = -1,
            FreeShot = 0,
            ClearStage = 1,
            Bonus25 = 2,
            FastShoot = 3,
            Auto = 4,
            Snipe = 5
        }
        #endregion

        #region Fish
        public enum MoveType : int
        {
            None = -1, Straight = 0, TurnLeft = 1, TurnRight = 2, Jump = 3, JumpLeft = 4, JumpRight = 5, All = 6
        }

        public enum FishType : int
        {
            Basic = -1,
            Cuttle = 0,
            GoldFish, //1
            LightenFish, //2
            Mermaid, //3
            Octopus, //4
            PufferFish, //5
            SeaFish, //6
            Shark, //7
            Stringray, //8
            Turtle, //9
            CaThanTai, //10
            FlyingFish, //11
            GoldenFrog, //12
            SeaTurtle, //13
            MerMan, //14
            Phoenix, //15
            MermaidBig, //16
            MermaidSmall, //17
            BombFish, //18
            Fish19, //19
            Fish20, //20
            Fish21, //21
            Fish22, //22
            Fish23, //23
            Fish24, //24
            Fish25, //25
            All
        }

        public static volatile float JackpotRate = 0.03f; // amount accumulate to jackpot
        public static volatile float FeeRate = 0.02f;

        public static volatile float BombThreshold = 5000;
        public static volatile float JackpotInitial = 2500;

        public static volatile float FIRE_RATE = 4f;
        public static volatile float TURN_TIME = 0.25f;
        public static volatile float MIN_SPEED_2 = 0.01f;
        public static volatile float MAX_JUMP_SPEED = 150f;
        public static volatile float MAX_JUMP_SPEED_2 = 150f * 150f;
        public static volatile float MAX_SHADOW_SPEED_2 = 160f * 160f;
        public static volatile float TELEPORT_SHADOW_SPEED_2 = 500f * 500f;
        public static volatile float JUMP_ACCELERATION_OCTOPUS = 150f;
        public static volatile float JUMP_DECCELERATION_OCTOPUS = 150f;
        public static volatile float JUMP_ACCELERATION_CUTTLE = 150f; //120
        public static volatile float JUMP_DECCELERATION_CUTTLE = 150f;
        public static volatile float JUMP_ACCELERATION_SEA_TURTLE = 150f; //100
        public static volatile float JUMP_DECCELERATION_SEA_TURTLE = 150f; //200
        public static volatile float MIN_SPEED = 40f;
        public static volatile float MAX_VARY_SPEED = 80f;
        public static volatile float TURN_ANGLE_RAD = 1f * 3.14f / 180;
        public static volatile float JUMP_TURN_ANGLE_RAD = 1f * 3.14f / 180;

        public static volatile List<float> GOLDEN_FROG_WIN_RATE = new List<float> { 1, 5, 12, 16, 10 };
        public static volatile List<float> GOLDEN_FROG_WIN_MULTIPLE = new List<float> { 5, 4, 3, 2, 0.5f };

        public class FishPhysicalInfo
        {
            public volatile float Width, Height, Health;
            public volatile float HealthRate = 1.03f; //3%
            public volatile float HealthScale = 0.4f; //+-40%

            public FishPhysicalInfo(float Width, float Height, float Health, float HealthRate = 1.03f, float HealthScale = 0.4f)
            {
                this.Width = Width;
                this.Height = Height;
                this.Health = Health;
                this.HealthRate = HealthRate;
                this.HealthScale = HealthScale;
            }

            public JSONNode ToJson()
            {
                var data = new JSONObject();
                data["Width"] = Width;
                data["Height"] = Height;
                data["Health"] = Health;
                data["HealthRate"] = HealthRate;
                data["HealthScale"] = HealthScale;
                return data;
            }

            public void ParseJson(JSONNode data)
            {
                Width = data["Width"].AsFloat;
                Height = data["Height"].AsFloat;
                Health = data["Health"].AsFloat;
                HealthRate = data["HealthRate"].AsFloat;
                HealthScale = data["HealthScale"].AsFloat;
            }
        }

        public static volatile Dictionary<FishType, FishPhysicalInfo> FishPhysicalData = new Dictionary<FishType, FishPhysicalInfo>{
            {FishType.Basic, new FishPhysicalInfo(1,1,0) },
            {FishType.Cuttle, new FishPhysicalInfo(46,117,0) },
            {FishType.GoldFish, new FishPhysicalInfo(25,93,0) },
            {FishType.LightenFish, new FishPhysicalInfo(53,96,2500) }, //
            {FishType.Mermaid, new FishPhysicalInfo(53,91,1000) }, //
            {FishType.Octopus, new FishPhysicalInfo(53,43,0) }, //
            {FishType.PufferFish, new FishPhysicalInfo(40,40,200) }, //
            {FishType.SeaFish, new FishPhysicalInfo(53,60,400) }, //
            {FishType.Shark, new FishPhysicalInfo(40,60,600) }, //
            {FishType.Stringray, new FishPhysicalInfo(85,92,1500) },//
            {FishType.Turtle, new FishPhysicalInfo(53,43,300) },
            {FishType.CaThanTai, new FishPhysicalInfo(100,270,20000) }, //
            {FishType.FlyingFish, new FishPhysicalInfo(53,60,500) },
            {FishType.GoldenFrog, new FishPhysicalInfo(233,233,6600) }, //
            {FishType.SeaTurtle, new FishPhysicalInfo(53,60,0) }, //
            {FishType.MerMan, new FishPhysicalInfo(60,180,5000) }, //
            {FishType.Phoenix, new FishPhysicalInfo(135,257,0) },
            {FishType.MermaidBig, new FishPhysicalInfo(53,280,10000) }, //
            {FishType.MermaidSmall, new FishPhysicalInfo(80,200,7000) }, //
            {FishType.BombFish, new FishPhysicalInfo(100,420,6000000) }, //
            {FishType.Fish19, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish20, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish21, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish22, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish23, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish24, new FishPhysicalInfo(100,420,0) }, //
            {FishType.Fish25, new FishPhysicalInfo(100,420,0) }, //
        };
        public static volatile float MinScale = 1f;
        public static volatile float MaxScale = 1.05f;
        public static volatile float MaxFillScale = 1.2f;
        #endregion

        #region Bullet
        public enum BulletType : int
        {
            Basic = 0,
            Bullet1 = 1,
            Bullet2 = 2,
            Bullet3 = 3,
            Bullet4 = 4,
            Bullet5 = 5,
            Bullet6 = 6
        }

        public static volatile Dictionary<BulletType, int> TypeToPower = new Dictionary<BulletType, int> {
            {BulletType.Basic, 1 },
            {BulletType.Bullet1, 100 },
            {BulletType.Bullet2, 100 },
            {BulletType.Bullet3, 100 },
            {BulletType.Bullet4, 100 },
            {BulletType.Bullet5, 100 },
            {BulletType.Bullet6, 100 }
        };

        public static volatile Dictionary<BulletType, int> TypeToValue = new Dictionary<BulletType, int> {
            {BulletType.Basic, 0 },
            {BulletType.Bullet1, 10 },
            {BulletType.Bullet2, 20 },
            {BulletType.Bullet3, 50 },
            {BulletType.Bullet4, 100 },
            {BulletType.Bullet5, 200 },
            {BulletType.Bullet6, 500 }
        };

        public static volatile Dictionary<BulletType, int> TypeToJpCheckCount = new Dictionary<BulletType, int> {
            {BulletType.Basic, 0 },
            {BulletType.Bullet1, 1 },
            {BulletType.Bullet2, 2 },
            {BulletType.Bullet3, 5 },
            {BulletType.Bullet4, 10 },
            {BulletType.Bullet5, 20 },
            {BulletType.Bullet6, 50 }
        };

        public static volatile int MAX_BOUND_TIME = 4;
        public static volatile float BulletSpeed = 1400;
        public static volatile float BulletRadius = 22;
        #endregion

        #region Player
        public enum QuitReason : int
        {
            Unknown = -1,
            Normal = 0,
            TimeOut = 1,
            Kick = 2,
            Disconnect = 3
        }

        public static volatile float MaxIdleTimeS = 2f * 60;
        // new lv = a * lv3 + b * lv2 + c * lv + d
        private static volatile float a = 8;
        private static volatile float b = 32;
        private static volatile float c = 1024;
        private static volatile float d = 2048;
        public static long GetExpToNextLevel(long lv)
        {
            long lv2 = lv * lv;
            long lv3 = lv2 * lv;
            return (long)(a * lv3 + b * lv2 + c * lv + d);
        }

        // divide user
        private static volatile bool UserCardInOnOff = true;
        private static volatile int UserHighCash = 20000;
        private static volatile int UserMidCash = 10000;

        private static volatile int UserCardInHighCash = 20000;
        private static volatile int UserCardInMidCash = 10000;

        public static volatile float[] MinimumRefundUserBankRate = new float[] { 1.3f, 1.2f, 1.1f }; // low, mid, high
        public static volatile float[] MinimumRefundUserCardInBankRate = new float[] { 1.2f, 1.1f, 1f }; // low, mid, high

        public static float GetMinimumBankRate(long currentCash, long cardIn)
        {
            if(UserCardInOnOff && cardIn > 0) // is user cardin
            {
                var rates = MinimumRefundUserCardInBankRate;
                if (currentCash >= UserCardInHighCash && rates.Length > 2)
                {
                    //Logger.Info("User bc is card in high");
                    return rates[2];
                }
                else if(currentCash >= UserCardInMidCash && rates.Length > 1)
                {
                    //Logger.Info("User bc is card in mid");
                    return rates[1];
                }
                else if (rates.Length > 0)
                {
                    //Logger.Info("User bc is card in low");
                    return rates[0];
                }
                return 1f;
            }
            else
            {
                var rates = MinimumRefundUserBankRate;
                if (currentCash >= UserHighCash && rates.Length > 2)
                {
                    //Logger.Info("User bc is high");
                    return rates[2];
                }
                else if (currentCash >= UserMidCash && rates.Length > 1)
                {
                    //Logger.Info("User bc is mid");
                    return rates[1];
                }
                else if (rates.Length > 0)
                {
                    //Logger.Info("User bc is low");
                    return rates[0];
                }
                return 1f;
            }
        }
        #endregion

        #region json
        public volatile static bool ConfigLoaded = false; // did config load from external

        public static JSONArray NumberArrayToJson<T>(T[] a)
        {
            var arr = new JSONArray();
            for (int i = 0, n = a.Length; i < n; i++)
            {
                arr.Add(a[i].ToString());
            }
            return arr;
        }

        public static int[] JsonToIntArray(JSONArray a)
        {
            var arr = new int[a.Count];
            for (int i = 0, n = a.Count; i < n; i++)
            {
                arr[i] = a[i].AsInt;
            }
            return arr;
        }

        public static float[] JsonToFloatArray(JSONArray a)
        {
            var arr = new float[a.Count];
            for (int i = 0, n = a.Count; i < n; i++)
            {
                arr[i] = a[i].AsFloat;
            }
            return arr;
        }

        public static long[] JsonToLongArray(JSONArray a)
        {
            try
            {
                var arr = new long[a.Count];
                for (int i = 0, n = a.Count; i < n; i++)
                {
                    arr[i] = a[i].AsLong;
                }
                return arr;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw ex;
            }
        }

        public static string ToJsonString()
        {
            return ToJson().ToString();
        }

        public static JSONNode ToJson()
        {
            var data = new JSONObject();
            data["TableStartingBlinds"] = NumberArrayToJson(TableStartingBlinds);
            data["TableBulletValueRate"] = NumberArrayToJson(TableBulletValueRate);
            data["TableRequireCardIn"] = NumberArrayToJson(TableRequireCardIn);

            data["NumberOfAccountPerDevice"] = NumberOfAccountPerDevice;
            data["NumberOfAccountPerDay"] = NumberOfAccountPerDay;

            data["RequestSampleRateMs"] = RequestSampleRateMs;
            data["MaxRequestPerSecond"] = MaxRequestPerSecond;
            data["LogShooting"] = LogShooting;
            data["MinTimeBetweenShooting"] = MinTimeBetweenShooting;
            data["TimeBetweenVideoAdsMs0"] = TimeBetweenVideoAdsMs0;
            data["TimeBetweenVideoAdsMs1"] = TimeBetweenVideoAdsMs1;
            data["VideoAdsRewardCount0"] = VideoAdsRewardCount0;
            data["VideoAdsRewardCount1"] = VideoAdsRewardCount1;
            data["VideoAdsRewardType0"] = VideoAdsRewardType0;
            data["VideoAdsRewardType1"] = VideoAdsRewardType1;
            data["KickDuplicateUsers"] = KickDuplicateUsers;
            data["HideFishHp"] = HideFishHp;

            data["PowerUpIntervalS"] = PowerUpIntervalS;
            data["PowerUpMinPlayTimeS"] = PowerUpMinPlayTimeS;
            data["PowerUpDurationS"] = PowerUpDurationS;
            data["BombRate"] = BombRate;
            data["BonusRate"] = BonusRate;

            data["FastFireCoolDownS"] = FastFireCoolDownS;
            data["FastFireRate"] = FastFireRate;
            data["FastFireDuration"] = FastFireDuration;
            data["FastFireCost"] = FastFireCost;
            data["SnipeCoolDownS"] = SnipeCoolDownS;
            data["SnipeDurationS"] = SnipeDurationS;
            data["SnipeCost"] = SnipeCost;

            data["FeeRate"] = FeeRate;
            data["BombThreshold"] = BombThreshold;
            data["JackpotInitial"] = JackpotInitial;
            data["JackpotRate"] = JackpotRate;

            data["FIRE_RATE"] = FIRE_RATE;
            data["TURN_TIME"] = TURN_TIME;
            data["MIN_SPEED_2"] = MIN_SPEED_2;
            data["MAX_JUMP_SPEED"] = MAX_JUMP_SPEED;
            data["MAX_JUMP_SPEED_2"] = MAX_JUMP_SPEED_2;
            data["MAX_SHADOW_SPEED_2"] = MAX_SHADOW_SPEED_2;
            data["TELEPORT_SHADOW_SPEED_2"] = TELEPORT_SHADOW_SPEED_2;
            data["JUMP_ACCELERATION_OCTOPUS"] = JUMP_ACCELERATION_OCTOPUS;
            data["JUMP_DECCELERATION_OCTOPUS"] = JUMP_DECCELERATION_OCTOPUS;
            data["JUMP_ACCELERATION_CUTTLE"] = JUMP_ACCELERATION_CUTTLE;
            data["JUMP_DECCELERATION_CUTTLE"] = JUMP_DECCELERATION_CUTTLE;
            data["JUMP_ACCELERATION_SEA_TURTLE"] = JUMP_ACCELERATION_SEA_TURTLE;
            data["JUMP_DECCELERATION_SEA_TURTLE"] = JUMP_DECCELERATION_SEA_TURTLE;
            data["MIN_SPEED"] = MIN_SPEED;
            data["MAX_VARY_SPEED"] = MAX_VARY_SPEED;
            data["TURN_ANGLE_RAD"] = TURN_ANGLE_RAD;
            data["JUMP_TURN_ANGLE_RAD"] = JUMP_TURN_ANGLE_RAD;

            data["MAX_BOUND_TIME"] = MAX_BOUND_TIME;
            data["BulletSpeed"] = BulletSpeed;
            data["BulletRadius"] = BulletRadius;

            data["MaxIdleTimeS"] = MaxIdleTimeS;
            data["a"] = a;
            data["b"] = b;
            data["c"] = c;
            data["d"] = d;

            data["MinScale"] = MinScale;
            data["MaxScale"] = MaxScale;
            data["MaxFillScale"] = MaxFillScale;

            data["NUMBER_OF_BULLETS"] = NUMBER_OF_BULLETS;
            data["NUMBER_OF_OBJECTS"] = NUMBER_OF_OBJECTS;
            data["PLAYING_WAVE_DURATION"] = PLAYING_WAVE_DURATION;
            data["NEW_WAVE_MAX_TIME"] = NEW_WAVE_MAX_TIME;
            data["SOLO_DURATION"] = SOLO_DURATION;
            data["WAITING_WAVE_DURATION"] = WAITING_WAVE_DURATION;
            data["GUN_LENGTH"] = GUN_LENGTH;

            var gfWinRate = new JSONArray();
            for (int i = 0, n = GOLDEN_FROG_WIN_RATE.Count; i < n; i++)
            {
                gfWinRate.Add(GOLDEN_FROG_WIN_RATE[i]);
            }
            data["GOLDEN_FROG_WIN_RATE"] = gfWinRate;

            var gfWinMul = new JSONArray();
            for (int i = 0, n = GOLDEN_FROG_WIN_MULTIPLE.Count; i < n; i++)
            {
                gfWinMul.Add(GOLDEN_FROG_WIN_MULTIPLE[i]);
            }
            data["GOLDEN_FROG_WIN_MULTIPLE"] = gfWinMul;

            data["ScreenW"] = ScreenW;
            data["ScreenH"] = ScreenH;
            data["ScreenX"] = ScreenX;
            data["ScreenY"] = ScreenY;
            data["WorldW"] = WorldW;
            data["WorldH"] = WorldH;
            data["WorldX"] = WorldX;
            data["WorldY"] = WorldY;
            data["OutWorldW"] = OutWorldW;
            data["OutWorldH"] = OutWorldH;
            data["OutWorldX"] = OutWorldX;
            data["OutWorldY"] = OutWorldY;
            data["SpawnW"] = SpawnW;
            data["SpawnH"] = SpawnH;
            data["SpawnX"] = SpawnX;
            data["SpawnY"] = SpawnY;

            data["SERVER_UPDATE_LOOP_MS"] = SERVER_UPDATE_LOOP_MS;
            data["CLIENT_UPDATE_S"] = CLIENT_UPDATE_S;

            var playerPosA = new JSONArray();
            for (int i = 0, n = playerPos.Length; i < n; i++)
            {
                playerPosA.Add(playerPos[i].ToJson());
            }
            data["playerPos"] = playerPosA;

            var bp = new JSONObject();
            bp[BulletType.Basic.ToString()] = TypeToPower[BulletType.Basic];
            bp[BulletType.Bullet1.ToString()] = TypeToPower[BulletType.Bullet1];
            bp[BulletType.Bullet2.ToString()] = TypeToPower[BulletType.Bullet2];
            bp[BulletType.Bullet3.ToString()] = TypeToPower[BulletType.Bullet3];
            bp[BulletType.Bullet4.ToString()] = TypeToPower[BulletType.Bullet4];
            bp[BulletType.Bullet5.ToString()] = TypeToPower[BulletType.Bullet5];
            bp[BulletType.Bullet6.ToString()] = TypeToPower[BulletType.Bullet6];
            data["TypeToPower"] = bp;

            var bp2 = new JSONObject();
            bp2[BulletType.Basic.ToString()] = TypeToValue[BulletType.Basic];
            bp2[BulletType.Bullet1.ToString()] = TypeToValue[BulletType.Bullet1];
            bp2[BulletType.Bullet2.ToString()] = TypeToValue[BulletType.Bullet2];
            bp2[BulletType.Bullet3.ToString()] = TypeToValue[BulletType.Bullet3];
            bp2[BulletType.Bullet4.ToString()] = TypeToValue[BulletType.Bullet4];
            bp2[BulletType.Bullet5.ToString()] = TypeToValue[BulletType.Bullet5];
            bp2[BulletType.Bullet6.ToString()] = TypeToValue[BulletType.Bullet6];
            data["TypeToValue"] = bp2;

            var fd = new JSONObject();
            fd[FishType.Basic.ToString()] = FishPhysicalData[FishType.Basic].ToJson();
            fd[FishType.Cuttle.ToString()] = FishPhysicalData[FishType.Cuttle].ToJson();
            fd[FishType.GoldFish.ToString()] = FishPhysicalData[FishType.GoldFish].ToJson();
            fd[FishType.LightenFish.ToString()] = FishPhysicalData[FishType.LightenFish].ToJson();
            fd[FishType.Mermaid.ToString()] = FishPhysicalData[FishType.Mermaid].ToJson();
            fd[FishType.Octopus.ToString()] = FishPhysicalData[FishType.Octopus].ToJson();
            fd[FishType.PufferFish.ToString()] = FishPhysicalData[FishType.PufferFish].ToJson();
            fd[FishType.SeaFish.ToString()] = FishPhysicalData[FishType.SeaFish].ToJson();
            fd[FishType.Shark.ToString()] = FishPhysicalData[FishType.Shark].ToJson();
            fd[FishType.Stringray.ToString()] = FishPhysicalData[FishType.Stringray].ToJson();
            fd[FishType.Turtle.ToString()] = FishPhysicalData[FishType.Turtle].ToJson();
            fd[FishType.CaThanTai.ToString()] = FishPhysicalData[FishType.CaThanTai].ToJson();
            fd[FishType.FlyingFish.ToString()] = FishPhysicalData[FishType.FlyingFish].ToJson();
            fd[FishType.GoldenFrog.ToString()] = FishPhysicalData[FishType.GoldenFrog].ToJson();
            fd[FishType.SeaTurtle.ToString()] = FishPhysicalData[FishType.SeaTurtle].ToJson();
            fd[FishType.MerMan.ToString()] = FishPhysicalData[FishType.MerMan].ToJson();
            fd[FishType.Phoenix.ToString()] = FishPhysicalData[FishType.Phoenix].ToJson();
            fd[FishType.MermaidBig.ToString()] = FishPhysicalData[FishType.MermaidBig].ToJson();
            fd[FishType.MermaidSmall.ToString()] = FishPhysicalData[FishType.MermaidSmall].ToJson();
            fd[FishType.BombFish.ToString()] = FishPhysicalData[FishType.BombFish].ToJson();
            fd[FishType.Fish19.ToString()] = FishPhysicalData[FishType.Fish19].ToJson();
            fd[FishType.Fish20.ToString()] = FishPhysicalData[FishType.Fish20].ToJson();
            fd[FishType.Fish21.ToString()] = FishPhysicalData[FishType.Fish21].ToJson();
            fd[FishType.Fish22.ToString()] = FishPhysicalData[FishType.Fish22].ToJson();
            fd[FishType.Fish23.ToString()] = FishPhysicalData[FishType.Fish23].ToJson();
            fd[FishType.Fish24.ToString()] = FishPhysicalData[FishType.Fish24].ToJson();
            fd[FishType.Fish25.ToString()] = FishPhysicalData[FishType.Fish25].ToJson();
            data["FishPhysicalData"] = fd;

            var fakeJpMin = new JSONObject();
            data["FakeJpMinTimeMS"] = fakeJpMin;
            foreach (var item in FakeJpMinTimeMS)
            {
                fakeJpMin[item.Key.ToString()] = item.Value;
            }

            var fakeJpMax = new JSONObject();
            data["FakeJpMaxTimeMS"] = fakeJpMax;
            foreach (var item in FakeJpMaxTimeMS)
            {
                fakeJpMax[item.Key.ToString()] = item.Value;
            }

            var fakeMinAddJp = new JSONObject();
            data["MinAddFakeJp"] = fakeMinAddJp;
            foreach (var item in MinAddFakeJp)
            {
                fakeMinAddJp[item.Key.ToString()] = item.Value;
            }

            var fakeMaxAddJp = new JSONObject();
            data["MaxAddFakeJp"] = fakeMaxAddJp;
            foreach (var item in MaxAddFakeJp)
            {
                fakeMaxAddJp[item.Key.ToString()] = item.Value;
            }

            data["SoloFee"] = SoloFee;
            data["SoloSnipe"] = SoloSnipe;
            data["SoloFastFire"] = SoloFastFire;
            data["SoloBomb"] = SoloBomb;
            data["SoloItemBombDamage"] = SoloItemBombDamage;

            data["TableSoloCashIn"] = NumberArrayToJson(TableSoloCashIn);
            data["TableSoloVirtualCash"] = NumberArrayToJson(TableSoloVirtualCash);

            data["UserCardInOnOff"] = UserCardInOnOff ? 1 : 0;
            data["UserHighCash"] = UserHighCash;
            data["UserMidCash"] = UserMidCash;
            data["UserCardInHighCash"] = UserCardInHighCash;
            data["UserCardInMidCash"] = UserCardInMidCash;

            data["MinimumRefundUserBankRate"] = NumberArrayToJson(MinimumRefundUserBankRate);
            data["MinimumRefundUserCardInBankRate"] = NumberArrayToJson(MinimumRefundUserCardInBankRate);

            return data;
        }

        public static void ParseJson(JSONNode data)
        {
            if (data.HasKey("TableStartingBlinds")) TableStartingBlinds = JsonToLongArray(data["TableStartingBlinds"].AsArray);
            if (data.HasKey("TableBulletValueRate")) TableBulletValueRate = JsonToIntArray(data["TableBulletValueRate"].AsArray);
            if (data.HasKey("TableRequireCardIn")) TableRequireCardIn = JsonToLongArray(data["TableRequireCardIn"].AsArray);

            if (data.HasKey("NumberOfAccountPerDevice")) NumberOfAccountPerDevice = data["NumberOfAccountPerDevice"].AsInt;
            if (data.HasKey("NumberOfAccountPerDay")) NumberOfAccountPerDay = data["NumberOfAccountPerDay"].AsInt;

            if (data.HasKey("RequestSampleRateMs")) RequestSampleRateMs = data["RequestSampleRateMs"].AsInt;
            if (data.HasKey("MaxRequestPerSecond")) MaxRequestPerSecond = data["MaxRequestPerSecond"].AsFloat;
            if (data.HasKey("LogShooting")) LogShooting = data["LogShooting"].AsInt;
            if (data.HasKey("MinTimeBetweenShooting")) MinTimeBetweenShooting = data["MinTimeBetweenShooting"].AsInt;

            if (data.HasKey("TimeBetweenVideoAdsMs0")) TimeBetweenVideoAdsMs0 = data["TimeBetweenVideoAdsMs0"].AsInt;
            if (data.HasKey("TimeBetweenVideoAdsMs1")) TimeBetweenVideoAdsMs1 = data["TimeBetweenVideoAdsMs1"].AsInt;
            if (data.HasKey("VideoAdsRewardCount0")) VideoAdsRewardCount0 = data["VideoAdsRewardCount0"].AsInt;
            if (data.HasKey("VideoAdsRewardCount1")) VideoAdsRewardCount1 = data["VideoAdsRewardCount1"].AsInt;
            if (data.HasKey("VideoAdsRewardType0")) VideoAdsRewardType0 = data["VideoAdsRewardType0"].AsInt;
            if (data.HasKey("VideoAdsRewardType1")) VideoAdsRewardType1 = data["VideoAdsRewardType1"].AsInt;
            if (data.HasKey("KickDuplicateUsers")) KickDuplicateUsers = data["KickDuplicateUsers"].AsInt;
            if (data.HasKey("HideFishHp")) HideFishHp = data["HideFishHp"].AsInt;

            if (data.HasKey("PowerUpIntervalS")) PowerUpIntervalS = data["PowerUpIntervalS"].AsFloat;
            if (data.HasKey("PowerUpMinPlayTimeS")) PowerUpMinPlayTimeS = data["PowerUpMinPlayTimeS"].AsFloat;
            if (data.HasKey("PowerUpDurationS")) PowerUpDurationS = data["PowerUpDurationS"].AsFloat;
            if (data.HasKey("BombRate")) BombRate = data["BombRate"].AsInt;
            if (data.HasKey("BonusRate")) BonusRate = data["BonusRate"].AsInt;

            if (data.HasKey("FastFireCoolDownS")) FastFireCoolDownS = data["FastFireCoolDownS"].AsFloat;
            if (data.HasKey("FastFireRate")) FastFireRate = data["FastFireRate"].AsFloat;
            if (data.HasKey("FastFireDuration")) FastFireDuration = data["FastFireDuration"].AsFloat;
            if (data.HasKey("FastFireCost")) FastFireCost = data["FastFireCost"].AsInt;
            if (data.HasKey("SnipeCoolDownS")) SnipeCoolDownS = data["SnipeCoolDownS"].AsFloat;
            if (data.HasKey("SnipeDurationS")) SnipeDurationS = data["SnipeDurationS"].AsFloat;
            if (data.HasKey("SnipeCost")) SnipeCost = data["SnipeCost"].AsInt;

            if (data.HasKey("FeeRate")) FeeRate = data["FeeRate"].AsFloat;
            if (data.HasKey("BombThreshold")) BombThreshold = data["BombThreshold"].AsFloat;
            if (data.HasKey("JackpotInitial")) JackpotInitial = data["JackpotInitial"].AsFloat;
            if (data.HasKey("JackpotRate")) JackpotRate = data["JackpotRate"].AsFloat;

            if (data.HasKey("FIRE_RATE")) FIRE_RATE = data["FIRE_RATE"].AsFloat;
            if (data.HasKey("TURN_TIME")) TURN_TIME = data["TURN_TIME"].AsFloat;
            if (data.HasKey("MIN_SPEED_2")) MIN_SPEED_2 = data["MIN_SPEED_2"].AsFloat;
            if (data.HasKey("MAX_JUMP_SPEED")) MAX_JUMP_SPEED = data["MAX_JUMP_SPEED"].AsFloat;
            if (data.HasKey("MAX_JUMP_SPEED_2")) MAX_JUMP_SPEED_2 = data["MAX_JUMP_SPEED_2"].AsFloat;
            if (data.HasKey("MAX_SHADOW_SPEED_2")) MAX_SHADOW_SPEED_2 = data["MAX_SHADOW_SPEED_2"].AsFloat;
            if (data.HasKey("TELEPORT_SHADOW_SPEED_2")) TELEPORT_SHADOW_SPEED_2 = data["TELEPORT_SHADOW_SPEED_2"].AsFloat;
            if (data.HasKey("JUMP_ACCELERATION_OCTOPUS")) JUMP_ACCELERATION_OCTOPUS = data["JUMP_ACCELERATION_OCTOPUS"].AsFloat;
            if (data.HasKey("JUMP_DECCELERATION_OCTOPUS")) JUMP_DECCELERATION_OCTOPUS = data["JUMP_DECCELERATION_OCTOPUS"].AsFloat;
            if (data.HasKey("JUMP_ACCELERATION_CUTTLE")) JUMP_ACCELERATION_CUTTLE = data["JUMP_ACCELERATION_CUTTLE"].AsFloat;
            if (data.HasKey("JUMP_DECCELERATION_CUTTLE")) JUMP_DECCELERATION_CUTTLE = data["JUMP_DECCELERATION_CUTTLE"].AsFloat;
            if (data.HasKey("JUMP_ACCELERATION_SEA_TURTLE")) JUMP_ACCELERATION_SEA_TURTLE = data["JUMP_ACCELERATION_SEA_TURTLE"].AsFloat;
            if (data.HasKey("JUMP_DECCELERATION_SEA_TURTLE")) JUMP_DECCELERATION_SEA_TURTLE = data["JUMP_DECCELERATION_SEA_TURTLE"].AsFloat;
            if (data.HasKey("MIN_SPEED")) MIN_SPEED = data["MIN_SPEED"].AsFloat;
            if (data.HasKey("MAX_VARY_SPEED")) MAX_VARY_SPEED = data["MAX_VARY_SPEED"].AsFloat;
            if (data.HasKey("TURN_ANGLE_RAD")) TURN_ANGLE_RAD = data["TURN_ANGLE_RAD"].AsFloat;
            if (data.HasKey("JUMP_TURN_ANGLE_RAD")) JUMP_TURN_ANGLE_RAD = data["JUMP_TURN_ANGLE_RAD"].AsFloat;

            if (data.HasKey("MAX_BOUND_TIME")) MAX_BOUND_TIME = data["MAX_BOUND_TIME"].AsInt;
            if (data.HasKey("BulletSpeed")) BulletSpeed = data["BulletSpeed"].AsFloat;
            if (data.HasKey("BulletRadius")) BulletRadius = data["BulletRadius"].AsFloat;
            if (data.HasKey("MaxIdleTimeS")) MaxIdleTimeS = data["MaxIdleTimeS"].AsFloat;

            if (data.HasKey("a")) a = data["a"].AsFloat;
            if (data.HasKey("b")) b = data["b"].AsFloat;
            if (data.HasKey("c")) c = data["c"].AsFloat;
            if (data.HasKey("d")) d = data["d"].AsFloat;

            if (data.HasKey("MinScale")) MinScale = data["MinScale"].AsFloat;
            if (data.HasKey("MaxScale")) MaxScale = data["MaxScale"].AsFloat;
            if (data.HasKey("MaxFillScale")) MaxFillScale = data["MaxFillScale"].AsFloat;

            if (data.HasKey("NUMBER_OF_BULLETS")) NUMBER_OF_BULLETS = data["NUMBER_OF_BULLETS"].AsFloat;
            if (data.HasKey("NUMBER_OF_OBJECTS")) NUMBER_OF_OBJECTS = data["NUMBER_OF_OBJECTS"].AsFloat;
            if (data.HasKey("PLAYING_WAVE_DURATION")) PLAYING_WAVE_DURATION = data["PLAYING_WAVE_DURATION"].AsFloat;
            if (data.HasKey("NEW_WAVE_MAX_TIME")) NEW_WAVE_MAX_TIME = data["NEW_WAVE_MAX_TIME"].AsFloat;
            if (data.HasKey("SOLO_DURATION")) SOLO_DURATION = data["SOLO_DURATION"].AsFloat;
            if (data.HasKey("WAITING_WAVE_DURATION")) WAITING_WAVE_DURATION = data["WAITING_WAVE_DURATION"].AsFloat;
            if (data.HasKey("GUN_LENGTH")) GUN_LENGTH = data["GUN_LENGTH"].AsFloat;

            if (data.HasKey("GOLDEN_FROG_WIN_RATE"))
            {
                var gfWinRate = data["GOLDEN_FROG_WIN_RATE"].AsArray;
                GOLDEN_FROG_WIN_RATE.Clear();
                for (int i = 0, n = gfWinRate.Count; i < n; i++)
                {
                    GOLDEN_FROG_WIN_RATE.Add(gfWinRate[i].AsFloat);
                }
            }

            if (data.HasKey("GOLDEN_FROG_WIN_MULTIPLE"))
            {
                var gfWinMul = data["GOLDEN_FROG_WIN_MULTIPLE"].AsArray;
                GOLDEN_FROG_WIN_MULTIPLE.Clear();
                for (int i = 0, n = gfWinMul.Count; i < n; i++)
                {
                    GOLDEN_FROG_WIN_MULTIPLE.Add(gfWinMul[i].AsFloat);
                }
            }

            if (data.HasKey("ScreenW")) ScreenW = data["ScreenW"].AsFloat;
            if (data.HasKey("ScreenH")) ScreenH = data["ScreenH"].AsFloat;
            if (data.HasKey("ScreenX")) ScreenX = data["ScreenX"].AsFloat;
            if (data.HasKey("ScreenY")) ScreenY = data["ScreenY"].AsFloat;
            if (data.HasKey("WorldW")) WorldW = data["WorldW"].AsFloat;
            if (data.HasKey("WorldH")) WorldH = data["WorldH"].AsFloat;
            if (data.HasKey("WorldX")) WorldX = data["WorldX"].AsFloat;
            if (data.HasKey("WorldY")) WorldY = data["WorldY"].AsFloat;
            if (data.HasKey("OutWorldW")) OutWorldW = data["OutWorldW"].AsFloat;
            if (data.HasKey("OutWorldH")) OutWorldH = data["OutWorldH"].AsFloat;
            if (data.HasKey("OutWorldX")) OutWorldX = data["OutWorldX"].AsFloat;
            if (data.HasKey("OutWorldY")) OutWorldY = data["OutWorldY"].AsFloat;
            if (data.HasKey("SpawnW")) SpawnW = data["SpawnW"].AsFloat;
            if (data.HasKey("SpawnH")) SpawnH = data["SpawnH"].AsFloat;
            if (data.HasKey("SpawnX")) SpawnX = data["SpawnX"].AsFloat;
            if (data.HasKey("SpawnY")) SpawnY = data["SpawnY"].AsFloat;

            if (data.HasKey("SERVER_UPDATE_LOOP_MS")) SERVER_UPDATE_LOOP_MS = data["SERVER_UPDATE_LOOP_MS"].AsInt;
            if (data.HasKey("CLIENT_UPDATE_S")) CLIENT_UPDATE_S = data["CLIENT_UPDATE_S"].AsFloat;

            if (data.HasKey("playerPos"))
            {
                var playerPosA = data["playerPos"].AsArray;
                for (int i = 0, n = playerPosA.Count, m = playerPos.Length; i < n && i < m; i++)
                {
                    playerPos[i].ParseJson(playerPosA[i].AsObject);
                }
            }

            if (data.HasKey("TypeToPower"))
            {
                var bp = data["TypeToPower"].AsObject;
                TypeToPower[BulletType.Basic] = bp[BulletType.Basic.ToString()].AsInt;
                TypeToPower[BulletType.Bullet1] = bp[BulletType.Bullet1.ToString()].AsInt;
                TypeToPower[BulletType.Bullet2] = bp[BulletType.Bullet2.ToString()].AsInt;
                TypeToPower[BulletType.Bullet3] = bp[BulletType.Bullet3.ToString()].AsInt;
                TypeToPower[BulletType.Bullet4] = bp[BulletType.Bullet4.ToString()].AsInt;
                TypeToPower[BulletType.Bullet5] = bp[BulletType.Bullet5.ToString()].AsInt;
                TypeToPower[BulletType.Bullet6] = bp[BulletType.Bullet6.ToString()].AsInt;
            }

            if (data.HasKey("TypeToValue"))
            {
                var bp2 = data["TypeToValue"].AsObject;
                TypeToValue[BulletType.Basic] = bp2[BulletType.Basic.ToString()].AsInt;
                TypeToValue[BulletType.Bullet1] = bp2[BulletType.Bullet1.ToString()].AsInt;
                TypeToValue[BulletType.Bullet2] = bp2[BulletType.Bullet2.ToString()].AsInt;
                TypeToValue[BulletType.Bullet3] = bp2[BulletType.Bullet3.ToString()].AsInt;
                TypeToValue[BulletType.Bullet4] = bp2[BulletType.Bullet4.ToString()].AsInt;
                TypeToValue[BulletType.Bullet5] = bp2[BulletType.Bullet5.ToString()].AsInt;
                TypeToValue[BulletType.Bullet6] = bp2[BulletType.Bullet6.ToString()].AsInt;

                int _base = TypeToValue[BulletType.Bullet1] = bp2[BulletType.Bullet1.ToString()].AsInt;
                TypeToJpCheckCount[BulletType.Bullet1] = 1; // always 1
                TypeToJpCheckCount[BulletType.Bullet2] = TypeToValue[BulletType.Bullet2] / _base;
                TypeToJpCheckCount[BulletType.Bullet3] = TypeToValue[BulletType.Bullet3] / _base;
                TypeToJpCheckCount[BulletType.Bullet4] = TypeToValue[BulletType.Bullet4] / _base;
                TypeToJpCheckCount[BulletType.Bullet5] = TypeToValue[BulletType.Bullet5] / _base;
                TypeToJpCheckCount[BulletType.Bullet6] = TypeToValue[BulletType.Bullet6] / _base;
            }

            if (data.HasKey("FishPhysicalData"))
            {
                var fd = data["FishPhysicalData"].AsObject;
                FishPhysicalData[FishType.Basic].ParseJson(fd[FishType.Basic.ToString()]);
                FishPhysicalData[FishType.Cuttle].ParseJson(fd[FishType.Cuttle.ToString()]);
                FishPhysicalData[FishType.GoldFish].ParseJson(fd[FishType.GoldFish.ToString()]);
                FishPhysicalData[FishType.LightenFish].ParseJson(fd[FishType.LightenFish.ToString()]);
                FishPhysicalData[FishType.Mermaid].ParseJson(fd[FishType.Mermaid.ToString()]);
                FishPhysicalData[FishType.Octopus].ParseJson(fd[FishType.Octopus.ToString()]);
                FishPhysicalData[FishType.PufferFish].ParseJson(fd[FishType.PufferFish.ToString()]);
                FishPhysicalData[FishType.SeaFish].ParseJson(fd[FishType.SeaFish.ToString()]);
                FishPhysicalData[FishType.Shark].ParseJson(fd[FishType.Shark.ToString()]);
                FishPhysicalData[FishType.Stringray].ParseJson(fd[FishType.Stringray.ToString()]);
                FishPhysicalData[FishType.Turtle].ParseJson(fd[FishType.Turtle.ToString()]);
                FishPhysicalData[FishType.CaThanTai].ParseJson(fd[FishType.CaThanTai.ToString()]);
                FishPhysicalData[FishType.FlyingFish].ParseJson(fd[FishType.FlyingFish.ToString()]);
                FishPhysicalData[FishType.GoldenFrog].ParseJson(fd[FishType.GoldenFrog.ToString()]);
                FishPhysicalData[FishType.SeaTurtle].ParseJson(fd[FishType.SeaTurtle.ToString()]);
                FishPhysicalData[FishType.MerMan].ParseJson(fd[FishType.MerMan.ToString()]);
                FishPhysicalData[FishType.Phoenix].ParseJson(fd[FishType.Phoenix.ToString()]);
                FishPhysicalData[FishType.MermaidBig].ParseJson(fd[FishType.MermaidBig.ToString()]);
                FishPhysicalData[FishType.MermaidSmall].ParseJson(fd[FishType.MermaidSmall.ToString()]);
                FishPhysicalData[FishType.BombFish].ParseJson(fd[FishType.BombFish.ToString()]);
                FishPhysicalData[FishType.Fish19].ParseJson(fd[FishType.Fish19.ToString()]);
                FishPhysicalData[FishType.Fish20].ParseJson(fd[FishType.Fish20.ToString()]);
                FishPhysicalData[FishType.Fish21].ParseJson(fd[FishType.Fish21.ToString()]);
                FishPhysicalData[FishType.Fish22].ParseJson(fd[FishType.Fish22.ToString()]);
                FishPhysicalData[FishType.Fish23].ParseJson(fd[FishType.Fish23.ToString()]);
                FishPhysicalData[FishType.Fish24].ParseJson(fd[FishType.Fish24.ToString()]);
                FishPhysicalData[FishType.Fish25].ParseJson(fd[FishType.Fish25.ToString()]);
            }

            if (data.HasKey("FakeJpMinTimeMS"))
            {
                var fakeJpMin = data["FakeJpMinTimeMS"].AsObject;
                foreach (var item in fakeJpMin.Keys)
                {
                    FakeJpMinTimeMS[int.Parse(item)] = fakeJpMin[item].AsInt;
                }
            }

            if (data.HasKey("FakeJpMaxTimeMS"))
            {
                var fakeJpMax = data["FakeJpMaxTimeMS"].AsObject;
                foreach (var item in fakeJpMax.Keys)
                {
                    FakeJpMaxTimeMS[int.Parse(item)] = fakeJpMax[item].AsInt;
                }
            }

            if (data.HasKey("MinAddFakeJp"))
            {
                var fakeAddJpMin = data["MinAddFakeJp"].AsObject;
                foreach (var item in fakeAddJpMin.Keys)
                {
                    MinAddFakeJp[int.Parse(item)] = fakeAddJpMin[item].AsInt;
                }
            }
            if (data.HasKey("MaxAddFakeJp"))
            {
                var fakeAddJpMax = data["MaxAddFakeJp"].AsObject;
                foreach (var item in fakeAddJpMax.Keys)
                {
                    MaxAddFakeJp[int.Parse(item)] = fakeAddJpMax[item].AsInt;
                }
            }

            if (data.HasKey("SoloFee")) SoloFee = data["SoloFee"].AsFloat;
            if (data.HasKey("SoloSnipe")) SoloSnipe = data["SoloSnipe"].AsInt;
            if (data.HasKey("SoloFastFire")) SoloFastFire = data["SoloFastFire"].AsInt;
            if (data.HasKey("SoloBomb")) SoloBomb = data["SoloBomb"].AsInt;
            if (data.HasKey("SoloItemBombDamage")) SoloItemBombDamage = data["SoloItemBombDamage"].AsInt;

            if (data.HasKey("TableSoloCashIn")) TableSoloCashIn = JsonToIntArray(data["TableSoloCashIn"].AsArray);
            if (data.HasKey("TableSoloVirtualCash")) TableSoloVirtualCash = JsonToIntArray(data["TableSoloVirtualCash"].AsArray);


            if (data.HasKey("UserCardInOnOff")) UserCardInOnOff = data["UserCardInOnOff"].AsInt == 1 ? true : false;
            if (data.HasKey("UserHighCash")) UserHighCash = data["UserHighCash"].AsInt;
            if (data.HasKey("UserMidCash")) UserMidCash = data["UserMidCash"].AsInt;
            if (data.HasKey("UserCardInHighCash")) UserCardInHighCash = data["UserCardInHighCash"].AsInt;
            if (data.HasKey("UserCardInMidCash")) UserCardInMidCash = data["UserCardInMidCash"].AsInt;

            if (data.HasKey("MinimumRefundUserBankRate")) MinimumRefundUserBankRate = JsonToFloatArray(data["MinimumRefundUserBankRate"].AsArray);
            if (data.HasKey("MinimumRefundUserCardInBankRate")) MinimumRefundUserCardInBankRate = JsonToFloatArray(data["MinimumRefundUserCardInBankRate"].AsArray);

            if (OnConfigChange != null)
            {
                OnConfigChange();
            }
        }
        #endregion

        #region Solo
        public static volatile int[] TableSoloCashIn = new int[] { 1, 500, 5000, 50000 };
        public static volatile int[] TableSoloVirtualCash = new int[] { 1, 5000, 50000, 500000 };
        public static volatile float SoloFee = 0.05f;
        public static volatile int SoloSnipe = 3;
        public static volatile int SoloFastFire = 3;
        public static volatile int SoloBomb = 3;
        public static volatile int SoloItemBombDamage = 300;
        #endregion
    }
}
