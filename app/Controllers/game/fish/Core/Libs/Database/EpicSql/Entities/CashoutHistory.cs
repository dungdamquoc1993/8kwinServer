using Entites.Cms;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace Entites.Payment
{
    public class CashoutHistory
    {
        public long TransId { get; set; }
        public string ItemId { get; set; }
        public long Price { get; set; }
        public string TimeCashout { get; set; }
        public string Seri { get; set; }
        public string NumberCard { get; set; }
        public int Status { get; set; }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["TransId"] = TransId;
            data["ItemId"] = ItemId;
            data["Price"] = Price;
            data["TimeCashout"] = TimeCashout;
            data["Seri"] = Seri;
            data["NumberCard"] = NumberCard;
            data["Status"] = Status;
            return data;
        }
    }
    public class CashoutHistories : response_base
    {
        public List<CashoutHistory> Histories;
        public CashoutHistories()
        {
            Histories = new List<CashoutHistory>();
        }

        public JSONNode ToJson()
        {
            var data = new JSONArray();
            foreach (var item in Histories)
            {
                data.Add(item.ToJson());
            }
            return data;
        }
    }

    public class CashoutHistoryCms
    {
        public long TransId { get; set; }
        public string ItemId { get; set; }
        public long Price { get; set; }
        public string TimeCashout { get; set; }
        public string Seri { get; set; }
        public string NumberCard { get; set; }
        public byte Status { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
    }
    public class CashoutHistoriesCms : response_base
    {
        public List<CashoutHistoryCms> Histories;
        public short current_page;
        public short total_pages;
        public long total_cashout;
        public CashoutHistoriesCms()
        {
            Histories = new List<CashoutHistoryCms>();
        }
    }
}
