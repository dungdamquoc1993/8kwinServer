using System.Collections.Generic;

namespace Entites.Cms
{
    public class giftcode_campaign : response_base
    {
        public int campaign_id { get; set; }
        public string campain_name { get; set; }
        public long cash { get; set; }
        public int quantity { get; set; }
        public byte status { get; set; }
        public string create_time { get; set; }
    }

    public class giftcode_campaigns : response_base
    {
        public List<giftcode_campaign> data { get; set; }
        public short current_page { get; set; }
        public short total_pages { get; set; }
        public giftcode_campaigns()
        {
            data = new List<giftcode_campaign>();
        }
    }

    public class giftcode : response_base
    {
        public string gift_code { get; set; }
        public int campaign_id { get; set; }
        public long cash { get; set; }
        public byte status { get; set; }
        public long receiver_id { get; set; }
        public string receiver { get; set; }
        public string use_time { get; set; }
    }

    public class giftcodes : response_base
    {
        public List<giftcode> data { get; set; }
        public short current_page { get; set; }
        public short total_pages { get; set; }
        public short total_code { get; set; }
        public short total_unused { get; set; }
        
        public giftcodes()
        {
            data = new List<giftcode>();
        }
    }

    
}
