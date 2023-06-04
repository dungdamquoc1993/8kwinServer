using System.Collections.Generic;

namespace BanCa.Libs
{
    public class SimplePool<T>
    {
        public delegate T HowToCreateT();

        private object _lock;
        private List<T> pools;
        private HowToCreateT creator;

        public SimplePool(HowToCreateT creator)
        {
            _lock = new object();
            pools = new List<T>();
            this.creator = creator;
        }

        public T Obtain()
        {
            lock (_lock)
            {
                var lastI = pools.Count - 1;
                if (lastI >= 0)
                {
                    var item = pools[lastI];
                    pools.RemoveAt(lastI);
                    return item;
                }
            }

            return creator();
        }

        public void Free(T item)
        {
            lock (_lock)
            {
                pools.Add(item);
            }
        }
    }
}
