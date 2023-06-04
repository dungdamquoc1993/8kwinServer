using Entites.Cms;
using System.Collections.Generic;

namespace Entites.Payment
{
    public class ChargingHistory
    {
        public string CreateTime { get; set; }
        public long Amount { get; set; }
        public string Seri { get; set; }
        public string NumberCard { get; set; }
        public long CurrentCash { get; set; }
        public string Telco { get; set; }
        public int Status { get; set; }

        public SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();
            data["CreateTime"] = CreateTime;
            data["Amount"] = Amount;
            data["Seri"] = Seri;
            data["NumberCard"] = NumberCard;
            data["CurrentCash"] = CurrentCash;
            data["Telco"] = Telco;
            data["Status"] = Status;
            return data;
        }
    }
    public class ChargingHistories : response_base
    {
        public List<ChargingHistory> Histories { get; set; }
        public ChargingHistories()
        {
            Histories = new List<ChargingHistory>();
        }

        public SimpleJSON.JSONArray ToJson()
        {
            var data = new SimpleJSON.JSONArray();
            for (int i = 0; i < Histories.Count; i++)
            {
                data.Add(Histories[i].ToJson());
            }
            return data;
        }
    }

    public class ChargingHistoryCms
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Amount { get; set; }
        public string Seri { get; set; }
        public string NumberCard { get; set; }
        public string DeviceId { get; set; }
        public string Telco { get; set; }
        public string CreateTime { get; set; }
    }
    public class ChargingHistoriesCms : response_base
    {
        public List<ChargingHistoryCms> Histories { get; set; }
        public short current_page { get; set; }
        public short total_pages { get; set; }
        public long total_charging { get; set; }
        public ChargingHistoriesCms()
        {
            Histories = new List<ChargingHistoryCms>();
        }
    }
    public class SmsChargingHistoryCms
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Amount { get; set; }
        public string PhoneNumber { get; set; }
        public string DeviceId { get; set; }
        public string CreateTime { get; set; }
    }
    public class SmsChargingHistoriesCms : response_base
    {
        public List<SmsChargingHistoryCms> Histories { get; set; }
        public short current_page { get; set; }
        public short total_pages { get; set; }
        public long total_charging { get; set; }
        public SmsChargingHistoriesCms()
        {
            Histories = new List<SmsChargingHistoryCms>();
        }
    }

    public class GiveCashHistory
    {
        public string CreateTime { get; set; }
        public long Transaction { get; set; }
        public long Amount { get; set; }
        public long CurrentCash { get; set; }
        public string Receiver { get; set; }
    }

    public class GiveCashHistories : response_base
    {
        public List<GiveCashHistory> Histories { get; set; }
        public GiveCashHistories()
        {
            Histories = new List<GiveCashHistory>();
        }
    }
}
