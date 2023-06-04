using System.Collections.Generic;

namespace Entites.Cms
{
    public class ccu
    {
        public int total_user { get; set; }
        public string platform { get; set; }
        public string create_time { get; set; }
    }
    public class ccus : response_base
    {
        public List<ccu> data { get; set; }
        public ccus()
        {
            data = new List<ccu>();
        }
    }
}
