using Entites.Cms;
using System.Collections.Generic;

namespace Entites.General
{

    public class Trusts
    {
        public long UserId { get; set; }
        public string Avatar { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string Language { get; set; }
        public long Trust { get; set; }
    }
    public class TrustList : response_base
    {
        public List<Trusts> Histories { get; set; }
        
        public TrustList()
        {
            Histories = new List<Trusts>();
        }
        
    }
}