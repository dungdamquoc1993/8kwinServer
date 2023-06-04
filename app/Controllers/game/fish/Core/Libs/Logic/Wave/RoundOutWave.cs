using SimpleJSON;
using System.Collections.Generic;

namespace BanCa.Libs
{
    public class RoundOutWave : AbstractWave
    {
        private static Config.FishType[] PlaceFish = new Config.FishType[] { Config.FishType.Stringray, Config.FishType.PufferFish, Config.FishType.SeaFish,
            Config.FishType.LightenFish, Config.FishType.SeaFish, Config.FishType.PufferFish, Config.FishType.Stringray };

        private LinkedList<BanCaObject> pool = new LinkedList<BanCaObject>();
        private List<BanCaObject> actives = new List<BanCaObject>();
        private Dictionary<int, Config.FishType> oldTypes = new Dictionary<int, Config.FishType>();
        private Dictionary<int, float> oldHealths = new Dictionary<int, float>();

        private bool end = false;
        private bool ending = false;
        private int placeCount = 0;

        private const float PlaceInterval = 2.5f;
        private float PlaceIntervalCount = 0;

        public override string GetDetails()
        {
            var json = new JSONObject();
            json["end"] = end;
            json["ending"] = ending;
            json["placeCount"] = placeCount;
            json["PlaceIntervalCount"] = PlaceIntervalCount;

            {
                var poolJson = new JSONArray();
                foreach (var fish in pool)
                {
                    poolJson.Add(fish.ToJson(false, false));
                }
                json["pool"] = poolJson;
            }

            {
                var activeJson = new JSONArray();
                foreach (var fish in actives)
                {
                    activeJson.Add(fish.ToJson(false, false));
                }
                json["actives"] = activeJson;
            }

            return json.ToString();
        }

        public override void Start(GameBanCa world, List<BanCaObject> fish)
        {
            base.Start(world, fish);

            ending = false;
            end = false;
            placeCount = 0;
            PlaceIntervalCount = 0;

            pool.Clear();
            for (int i = 0, n = fish.Count; i < n; i++)
            {
                var _f = fish[i];
                pool.AddLast(_f);
                oldTypes[_f.ID] = _f.Type;
                oldHealths[_f.ID] = _f.Health;
                _f.Health = -1;
                _f.Remove();
            }
            if (world.OnRemoveAllObject != null)
            {
                var msg = new JSONObject();
                msg["time"] = TimeUtil.TimeStamp;
                world.OnRemoveAllObject(msg);
            }

            actives.Clear();

            placeFish();
        }

        private void placeFish()
        {
            int fishToPlace = 10;
            float step = (360 * 3.1416f / 180) / fishToPlace;
            float cRad = 0;
            
            for (int i = 0; i < fishToPlace && pool.Count > 0; i++)
            {
                var fish = pool.First.Value;
                pool.RemoveFirst();
                actives.Add(fish);

                FishFactory.BuildFish(world.TableBlindIndex, PlaceFish[placeCount], fish, world.Random); // change type
                fish.Pos.Set(0, 0);
                fish.V.Set(Config.MIN_SPEED * 2, 0);
                fish.V.Rotate(cRad);
                fish.ForceSetDirection();
                fish.SetMoveType();
                FishFactory.RandomHealth(fish, world.Random);
                fish.Value = 0; // (i >= fishToPlace - 2) ? (world.Profit > 0 ? world.Profit / 100 : 0) : 0; // refund 1% total profit for mermaid
                fish.MoveCount = 100;
                fish.TurnTimeS = Config.TURN_TIME;
                fish.UpdateBound();
                fish.onHitBound = OnFishHitBound;
                fish.onDie = OnFishDie;
                fish.GeneratePath(100);
                fish.pushUpdate();

                cRad += step;
            }

            placeCount++;
        }

        private void OnFishHitBound(BanCaObject fish)
        {
            if (ending)
            {
                FishFactory.BuildFish(world.TableBlindIndex, oldTypes[fish.ID], fish, world.Random); // restore type
                fish.Health = -1;
                fish.onHitBound = null;
                fish.onDie = null;
                pool.AddLast(fish);
                actives.Remove(fish);
                fish.pushRemove();
                world.scheduleToRevive(fish, 0, oldHealths[fish.ID]);
            }
            else
            {
                fish.Health = -1;
                fish.onHitBound = null;
                fish.onDie = null;
                pool.AddLast(fish);
                actives.Remove(fish);
                fish.pushRemove();
            }
        }

        private void OnFishDie(BanCaObject fish)
        {
            if (ending)
            {
                FishFactory.BuildFish(world.TableBlindIndex, oldTypes[fish.ID], fish, world.Random); // restore type
                fish.Health = -1;
                fish.onHitBound = null;
                fish.onDie = null;
                pool.AddLast(fish);
                actives.Remove(fish);
                world.scheduleToRevive(fish, 0, oldHealths[fish.ID]);
            }
            else
            {
                fish.Health = -1;
                fish.onHitBound = null;
                fish.onDie = null;
                pool.AddLast(fish);
                actives.Remove(fish);
            }
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            if (!end && !ending)
            {
                PlaceIntervalCount += delta;
                if (PlaceIntervalCount > PlaceInterval)
                {
                    PlaceIntervalCount = 0;
                    placeFish();
                    if (placeCount >= PlaceFish.Length)
                    {
                        doEnding();
                    }
                }
            }
            else if (ending)
            {
                if (actives.Count == 0)
                {
                    doEnd();
                }
                else
                {
                    for (int i = 0, n = actives.Count; i < n; i++)
                    {
                        var fish = actives[i];
                        if (fish.Health < 0) // already dead?
                        {
                            actives[i] = actives[n - 1];
                            actives.RemoveAt(n - 1);
                            i--;
                            n--;
                        }
                    }
                }
            }
        }

        private void doEnding()
        {
            ending = true;
        }

        private void doEnd()
        {
            end = true;

            for (int i = 0, n = actives.Count; i < n; i++)
            {
                var fish = actives[i];
                fish.onHitBound = null;
                fish.onDie = null;
            }

            foreach (var fish in pool)
            {
                fish.onHitBound = null;
                fish.onDie = null;
                if (fish.Health <= 0)
                {
                    FishFactory.BuildFish(world.TableBlindIndex, oldTypes[fish.ID], fish, world.Random); // restore type, beware also restore health
                    fish.Health = -1;
                    world.scheduleToRevive(fish, 0, oldHealths[fish.ID]);
                }
            }

            actives.Clear();
            pool.Clear();
        }

        public override bool IsEnd()
        {
            return end;
        }

        public override void SetEnding()
        {
            ending = true;
        }

        public override bool IsEnding()
        {
            return ending;
        }
    }
}
