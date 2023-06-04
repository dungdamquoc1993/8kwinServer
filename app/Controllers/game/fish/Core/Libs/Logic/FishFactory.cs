using System;
using System.Collections.Generic;

namespace BanCa.Libs
{
    class FishFactory
    {
        public readonly static Config.MoveType[] NORMAL = new Config.MoveType[] { Config.MoveType.Straight, Config.MoveType.TurnLeft, Config.MoveType.TurnRight };
        public readonly static Config.MoveType[] JUMP = new Config.MoveType[] { Config.MoveType.Jump, Config.MoveType.JumpLeft, Config.MoveType.JumpRight };
        public readonly static HashSet<Config.FishType> IgnoreScaleGroup = new HashSet<Config.FishType>() { Config.FishType.CaThanTai, Config.FishType.GoldenFrog, Config.FishType.Phoenix };

        public static long CalculateMaxValue(float maxHealth, int blindIndex)
        {
            var gunPower = Config.TypeToPower[Config.BulletType.Bullet4];
            return (long)Math.Ceiling(maxHealth / gunPower);
        }
        public static void RandomHealth(BanCaObject fish, Random random)
        {
            var gunPower = Config.TypeToPower[Config.BulletType.Bullet4];
            var pd = Config.FishPhysicalData[fish.Type];
            fish.Health = fish.MaxHealth * pd.HealthRate * (1f + (float)(random.NextDouble() * 2 * pd.HealthScale - pd.HealthScale));

            // round
            var roundValue = (float)Math.Round(fish.Health / gunPower);
            if (roundValue < 0.5f) roundValue = 1f;
            fish.Health = gunPower * roundValue - 1f;
        }
        public static BanCaObject BuildFish(int blindIndex, Config.FishType type, BanCaObject fish, Random random)
        {
            var sizeVary = IgnoreScaleGroup.Contains(type) ? 1f : (float)(random.NextDouble() * (Config.MaxScale - Config.MinScale) + Config.MinScale);

            fish.Type = type;
            var pd = Config.FishPhysicalData[type];
            fish.Width = pd.Width * sizeVary;
            fish.Height = pd.Height * sizeVary;
            fish.MaxHealth = pd.Health;
            RandomHealth(fish, random);
            fish.MaxValue = CalculateMaxValue(pd.Health, blindIndex);

            switch (type)
            {
                case Config.FishType.Basic:
                    //fish.MaxHealth = fish.Health = 1000;
                    break;
                case Config.FishType.Cuttle:
                    //fish.Width *= sizeVary;
                    //fish.Height *= sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 400;
                    break;
                case Config.FishType.GoldFish:
                    //fish.Width = 56 * sizeVary;
                    //fish.Height = 127 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 200;
                    break;
                case Config.FishType.LightenFish:
                    //fish.Width = 170 * sizeVary;
                    //fish.Height = 177 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 600;
                    break;
                case Config.FishType.Mermaid:
                    //fish.Width = 106 * sizeVary;
                    //fish.Height = 378 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2500;
                    break;
                case Config.FishType.Octopus:
                    //fish.Width = 200 * sizeVary;
                    //fish.Height = 200 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 1000;
                    break;
                case Config.FishType.PufferFish:
                    //fish.Width = 85 * sizeVary;
                    //fish.Height = 115 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 300;
                    break;
                case Config.FishType.SeaFish:
                    //fish.Width = 76 * sizeVary;
                    //fish.Height = 83 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 500;
                    break;
                case Config.FishType.Shark:
                    //fish.Width = 251 * sizeVary;
                    //fish.Height = 340 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 4000;
                    break;
                case Config.FishType.Stringray:
                    //fish.Width = 209 * sizeVary;
                    //fish.Height = 160 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 1500;
                    break;
                case Config.FishType.Turtle:
                    //fish.Width = 56 * sizeVary;
                    //fish.Height = 127 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 800;
                    break;
                case Config.FishType.CaThanTai:
                    //fish.Width = 156 * sizeVary;
                    //fish.Height = 285 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 20000;
                    break;
                case Config.FishType.FlyingFish:
                    //fish.Width = 32 * sizeVary;
                    //fish.Height = 120 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 500;
                    break;
                case Config.FishType.GoldenFrog:
                    //fish.Width = 411 * sizeVary;
                    //fish.Height = 425 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 6600;
                    break;
                case Config.FishType.SeaTurtle:
                    //fish.Width = 123 * sizeVary;
                    //fish.Height = 178 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 800;
                    break;
                case Config.FishType.MerMan:
                    //fish.Width = 100 * sizeVary;
                    //fish.Height = 493 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 20000;
                    break;
                case Config.FishType.Phoenix:
                    //fish.Width = 193 * sizeVary;
                    //fish.Height = 510 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 5000;
                    break;
                case Config.FishType.MermaidBig:
                    //fish.Width = 178 * sizeVary;
                    //fish.Height = 454 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 3000;
                    break;
                case Config.FishType.MermaidSmall:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.BombFish:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish19:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish20:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish21:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish22:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish23:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish24:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
                case Config.FishType.Fish25:
                    //fish.Width = 92 * sizeVary;
                    //fish.Height = 310 * sizeVary;
                    fish.allowMoveTypes = NORMAL;
                    //fish.MaxHealth = fish.Health = 2000;
                    break;
            }

            fish.UpdateBound();
            fish.SetMoveType();
            return fish;
        }

        public static BanCaObject RandomFish(int blindIndex, BanCaObject bco, Random random, List<Config.FishType> common = null // freely random in this list
            , List<Config.FishType> constrain = null // fish type in this will be limited
            )
        {
            var r = Config.FishType.Basic;
            if (constrain != null && constrain.Count > 0)
            {
                r = constrain[constrain.Count - 1];
                constrain.RemoveAt(constrain.Count - 1);
            }
            else if (common != null && common.Count > 0)
            {
                int i = random.Next() % common.Count;
                r = common[i];
            }
            else
            {
                r = (Config.FishType)(random.Next() % (int)Config.FishType.All); // random all
            }

            //r = Config.FishType.GoldFish;
            //bco.SetMimicTarget(lastFish);
            //lastFish = bco;

            return BuildFish(blindIndex, r, bco, random);
        }
    }
}
