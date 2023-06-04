using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entites.Cms
{
    public class transaction_history
    {
        public int trans_id { get; set; }
        public int user_id { get; set; }
        public string username { get; set; }
        public long cash { get; set; }
        public long current_cash { get; set; }
        public long current_cash_safe { get; set; }
        public long game_session { get; set; }
        public string cnid { get; set; }
        public string platform { get; set; }
        public string trans_time { get; set; }
    }

    public class transaction_histories : response_base
    {
        public List<transaction_history> data { get; set; }
        public int current_page { get; set; }
        public int total_pages { get; set; }
        public transaction_histories()
        {
            data = new List<transaction_history>();
        }
    }
}
