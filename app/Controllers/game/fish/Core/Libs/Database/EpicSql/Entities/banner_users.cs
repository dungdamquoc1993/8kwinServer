using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entites.Cms
{
    public class banner_users : response_base
    {
        public List<long> users { get; set; }
        public banner_users()
        {
            users = new List<long>();
        }
    }
}
