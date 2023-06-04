using SimpleJSON;
using System.Collections.Generic;

namespace BanCa.Libs
{
    // Simply kill all fish and then respawn via revive mechanic
    public class RespawnWave : AbstractWave
    {
        private Dictionary<int, Config.FishType> oldTypes = new Dictionary<int, Config.FishType>();
        private Dictionary<int, float> oldHealths = new Dictionary<int, float>();
        private bool end = false;

        public override string GetDetails()
        {
            var json = new JSONObject();
            json["end"] = end;

            return json.ToString();
        }

        public override void Start(GameBanCa world, List<BanCaObject> fish)
        {
            base.Start(world, fish);
            
            end = false;

            for (int i = 0, n = fish.Count; i < n; i++)
            {
                var _f = fish[i];
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
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            if(!end)
            {
                doEnd();
            }
        }

        private void doEnd()
        {
            end = true;

            foreach (var fish in allFish)
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
        }

        public override bool IsEnd()
        {
            return end;
        }

        public override void SetEnding()
        {
        }

        public override bool IsEnding()
        {
            return false;
        }
    }
}
