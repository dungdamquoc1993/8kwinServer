using System;
using differ.shapes;
using SimpleJSON;

namespace BanCa.Libs
{
    public class BanCaBullet : MovingObject
    {
        public long EpicId; // who shoot
        public long CashAtShoot;
        public long CashChangeAtShoot;
        public bool RapidFire = false;
        public bool IsAuto = false;
        public DateTime Start;

        public string PlayerId; // who shoot
        public float Power; // damage to fish when hit
        public long Value; // value add to fish on hit
        public int BoundTime = Config.MAX_BOUND_TIME;
        public Config.BulletType Type;

        private GameBanCa world;

        public Circle BoundingBox;
        public float R = Config.BulletRadius;

        public bool Active = false;
        public Vector Hit;

        public Action<int> OnRecycle, OnRemove;

        // -1 = no target
        public int TargetId = -1;

        public bool IsBot = false;

        public BanCaBullet(GameBanCa world)
        {
            this.world = world;

            Pos.Set(-2000, -2000);
            Hit = new Vector(Pos.X, Pos.Y);
            BoundingBox = new Circle(Pos.X, Pos.Y, R);
        }

        public void Update(float delta)
        {
            this.Move(delta);

            BoundingBox.set_x(Pos.X);
            BoundingBox.set_y(Pos.Y);
        }

        public void CheckBound(float x, float y, float w, float h)
        {
            if (Pos.X < x)
            {
                if (V.X < 0)
                {
                    V.X = -V.X;
                    BoundTime--;
                }
                TargetId = -1;
            }
            else if (Pos.X > x + w)
            {
                if (V.X > 0)
                {
                    V.X = -V.X;
                    BoundTime--;
                }
                TargetId = -1;
            }

            if (Pos.Y < y)
            {
                if (V.Y < 0)
                {
                    V.Y = -V.Y;
                    BoundTime--;
                }
                TargetId = -1;
            }
            else if (Pos.Y > y + h)
            {
                if (V.Y > 0)
                {
                    V.Y = -V.Y;
                    BoundTime--;
                }
                TargetId = -1;
            }
        }

        public bool TestHit(BanCaObject fish)
        {
            if (TargetId == -1 || TargetId == fish.ID)
            {
                return this.BoundingBox.testPolygon(fish.BoundingBox, world.collider, false) != null;
            }
            return false;
        }

        public void Recycle()
        {
            BoundTime = Config.MAX_BOUND_TIME;
            TargetId = -1;
            if (OnRecycle != null)
            {
                OnRecycle(ID);
            }
        }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["pos"] = Pos.ToJson();
            data["v"] = V.ToJson();
            data["playerId"] = PlayerId;
            data["type"] = (int)Type;
            data["r"] = R;
            return data;
        }

        public void ParseJson(JSONNode data)
        {
            Pos.ParseJson(data["pos"].AsObject);
            V.ParseJson(data["v"].AsObject);
            PlayerId = data["playerId"];
            try
            {
                Type = (Config.BulletType)data["type"].AsInt;
            }
            catch
            {
                Type = Config.BulletType.Basic;
            }
            Value = Config.GetBulletValue(world.TableBlindIndex, Type);
            Power = Config.TypeToPower[Type];
            R = data["r"].AsFloat;
        }
    }
}
