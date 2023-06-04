using Entites.Cms;
using System.Collections.Generic;
using System.IO;

namespace Entites.Payment
{
    public class ItemCashout
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public long Price { get; set; }
        public string IconUrl { get; set; }
    }
    public class CashoutItems : response_base
    {
        public List<ItemCashout> Items;
        public CashoutItems()
        {
            Items = new List<ItemCashout>();
        }
    }
}
