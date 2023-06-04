using System.Collections.Generic;

namespace BanCa.Libs // define how a wave work
{
    public interface IWave
    {
        void Start(GameBanCa world, List<BanCaObject> fish); // start and setup a new wave
        void Update(float delta); // on going wave
        bool IsEnd(); // end a wave
        bool IsEnding(); // end a wave
        void SetEnding();
        string GetDetails();
    }

    public abstract class AbstractWave : IWave
    {
        protected List<BanCaObject> allFish;
        protected GameBanCa world;
        public float TotalTime = 0;

        public abstract bool IsEnd();
        public abstract bool IsEnding();
        public abstract string GetDetails();

        public virtual void Start(GameBanCa world, List<BanCaObject> fish)
        {
            this.world = world;
            allFish = fish;
            TotalTime = 0;
        }

        public virtual void Update(float delta)
        {
            TotalTime += delta;
        }

        public abstract void SetEnding();
    }
}
