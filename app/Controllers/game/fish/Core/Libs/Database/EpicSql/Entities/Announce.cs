using Entites.Cms;
using System;
using System.Collections.Generic;

namespace Entites.General
{
    public class Announce
    {
        public int AnnouneId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } 
        public short Type { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();
            data["AnnouneId"] = AnnouneId;
            data["Title"] = Title;
            data["Content"] = Content;
            data["Type"] = Type;
            data["StartTime"] = StartTime;
            data["EndTime"] = EndTime;
            return data;
        }
    }

    public class Anns : response_base
    {
        public List<Announce> Announces { get; set; }
        public Anns()
        {
            Announces = new List<Announce>();
        }

        public SimpleJSON.JSONArray ToJson()
        {
            var data = new SimpleJSON.JSONArray();
            for (int i = 0; i < Announces.Count; i++)
            {
                data.Add(Announces[i].ToJson());
            }
            return data;
        }
    }
}
