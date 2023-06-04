using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MySqlProcess.Genneral
{
    public enum Countrys
    {
        vi,
        th,
        en,
        id,
        my,
        sg
    }

    public class MySqlEvent
    {
        private static object lockEvent = new object();
        private static Dictionary<string, SimpleJSON.JSONArray> ListEvent = new Dictionary<string, SimpleJSON.JSONArray>();

        // TODO: switch event db
        private const string EventTable = "bc_events";
        //private const string EventDetailTable = "bc_events_detail";
        //private const string EventTable = "events";
        //private const string EventDetailTable = "events_detail";

        public static List<Countrys> GetCountrys()
        {
            return new List<Countrys>() {
                Countrys.vi,
                Countrys.th,
                Countrys.en,
                Countrys.id,
                Countrys.my,
                Countrys.sg
            };
        }

        public static void ClearCache()
        {
            lock (lockEvent)
            {
                ListEvent.Clear();
            }
        }

        public static async Task Reload()
        {
            List<Countrys> countrys = GetCountrys();
            string start = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-dd HH:mm:ss");
            string query = "SELECT * FROM {0} WHERE start_time <= '{1}' AND is_show=1 order by create_time desc;";
            query = string.Format(query, EventTable, start);
            Dictionary<string, List<EventDetail>> _events = new Dictionary<string, List<EventDetail>>();
            foreach (Countrys country in countrys)
            {
                _events.Add(country.ToString(), new List<EventDetail>());
            }
            using (MySqlConnection con = MySqlConnect.Connection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    await con.OpenAsync();
                    List<EventDetail> _listEvent = new List<EventDetail>();
                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            EventDetail _event = new EventDetail();
                            _event.EventId = int.Parse(reader["event_id"].ToString());
                            _event.Title = reader["title"].ToString();
                            _event.IsNew = byte.Parse(reader["is_new"].ToString()) != 0 ? true : false;
                            var hot = reader["is_hot"];
                            _event.IsHot = false;
                            if (hot != null)
                            {
                                _event.IsHot = Convert.ToByte(hot) != 0 ? true : false;
                            }
                            _event.IsShow = byte.Parse(reader["is_show"].ToString()) != 0 ? true : false;
                            _event.EventType = byte.Parse(reader["event_type"].ToString());
                            _event.UrlIcon = reader["url_icon"].ToString();
                            _event.UrlImage = reader["url_image"].ToString();
                            _event.ImageLobby = reader["url_lobby"].ToString();
                            _event.TopEvent = reader["top_event"].ToString();
                            _event.EventInfo = reader["event_info"].ToString();
                            _event.JoinEvent = reader["join_event"].ToString();
                            _event.Language = reader["language"].ToString();
                            _event.dateStart = DateTime.Parse(reader["start_time"].ToString());
                            _event.dateEnd = DateTime.Parse(reader["end_time"].ToString());
                            DateTime datenow = DateTime.UtcNow.ToLocalTime();
                            //Logger.Info("Add event: " + _event.ToJson().ToString());
                            // TODO: fake events
                            if (datenow >= _event.dateStart && _event.dateEnd >= datenow)
                            {
                                _listEvent.Add(_event);
                            }
                        }
                        reader.NextResult();
                        reader.Dispose();
                    }
                    foreach (EventDetail ev in _listEvent)
                    {
                        //string query1 = "SELECT * FROM {0} WHERE event_id = {1};";
                        //query1 = string.Format(query1, EventDetailTable, ev.EventId);
                        //using (MySqlCommand cmd1 = new MySqlCommand(query1, con))
                        //{
                        //    using (MySqlDataReader reader1 = cmd1.ExecuteReader())
                        //    {
                        //        while (reader1.Read())
                        //        {
                        //            List<EventDetail> _eventLang;
                        //            _events.TryGetValue(reader1["lang_code"].ToString(),
                        //                out _eventLang);
                        //            cmd1.CommandType = CommandType.Text;
                        //            string event_info = reader1["event_info"].ToString();
                        //            if (string.IsNullOrEmpty(event_info))
                        //            {
                        //                event_info = ev.EventInfo;
                        //            }
                        //            string join_event = reader1["join_event"].ToString();
                        //            if (string.IsNullOrEmpty(join_event))
                        //            {
                        //                join_event = ev.JoinEvent;
                        //            }

                        //            string title = reader1["title"].ToString();
                        //            if (string.IsNullOrEmpty(title))
                        //            {
                        //                title = ev.Title;
                        //            }
                        //            string UrlImage = reader1["url_image"].ToString();
                        //            if (string.IsNullOrEmpty(UrlImage))
                        //            {
                        //                UrlImage = ev.UrlImage;
                        //            }
                        //            string ImageLobby = reader1["url_lobby"].ToString();
                        //            if (string.IsNullOrEmpty(ImageLobby))
                        //            {
                        //                ImageLobby = ev.ImageLobby;
                        //            }
                        //            EventDetail _event = new EventDetail();
                        //            _event.EventId = ev.EventId;
                        //            _event.Title = title;
                        //            _event.IsNew = ev.IsNew;
                        //            _event.IsShow = ev.IsShow;
                        //            _event.EventType = ev.EventType;
                        //            _event.UrlIcon = ev.UrlIcon;
                        //            _event.UrlImage = UrlImage;
                        //            _event.ImageLobby = ImageLobby;
                        //            _event.TopEvent = ev.TopEvent;
                        //            _event.EventInfo = event_info;
                        //            _event.JoinEvent = join_event;
                        //            _event.dateStart = ev.dateStart;
                        //            _event.dateEnd = ev.dateEnd;
                        //            //Logger.Info("Add event detail: " + _event.ToJson().ToString());
                        //            _eventLang.Add(_event);
                        //        }
                        //        reader1.NextResult();
                        //    }
                        //    cmd1.Dispose();
                        //}

                        List<EventDetail> _eventLang;
                        _events.TryGetValue(ev.Language, out _eventLang);
                        if (_eventLang != null)
                        {
                            _eventLang.Add(ev);
                        }
                    }
                    con.Close();
                }
            }
            lock (lockEvent)
            {
                ListEvent.Clear();
                foreach (var key in _events.Keys)
                {
                    var le = _events[key];
                    var jle = ListEvent[key] = new SimpleJSON.JSONArray();
                    for (int i = 0, m = le.Count; i < m; i++)
                    {
                        jle.Add(le[i].ToJson());
                    }
                }
            }
        }
        public static async Task<SimpleJSON.JSONArray> GetList(string lang)
        {
            var count = 0;
            lock (lockEvent)
            {
                count = ListEvent.Count;
            }
            if (count == 0)
            {
                await Reload();
            }
            lock (lockEvent)
            {
                SimpleJSON.JSONArray _event;
                if (lang != null && ListEvent.ContainsKey(lang))
                {
                    _event = ListEvent[lang];
                }
                else
                {
                    _event = ListEvent.ContainsKey("vi") ? ListEvent["vi"] : new SimpleJSON.JSONArray();
                }

                for (int i = 0, n = _event.Count; i < n; i++)
                {
                    DateTime datenow = DateTime.UtcNow.ToLocalTime();
                    var ev = _event[i];
                    var end = DateTime.Parse(ev["dateEnd"]);
                    // TODO: fake events
                    if (end < datenow)
                    {
                        _event[i] = _event[n - 1];
                        _event.Remove(n - 1);
                        n--;
                        i--;
                    }
                }

                return _event;
            }
        }
    }

    public class EventDetail
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public bool IsNew { get; set; }
        public bool IsHot { get; set; }
        public bool IsShow { get; set; }
        public byte EventType { get; set; }
        public string UrlIcon { get; set; }
        public string UrlImage { get; set; }
        public string ImageLobby { get; set; }
        public string EventInfo { get; set; }
        public string JoinEvent { get; set; }
        public string Language { get; set; }
        public string TopEvent { get; set; }
        public DateTime dateStart;
        public DateTime dateEnd;

        public SimpleJSON.JSONNode ToJson()
        {
            var data = new SimpleJSON.JSONObject();
            data["EventId"] = EventId;
            data["Title"] = Title;
            data["IsNew"] = IsNew;
            data["IsHot"] = IsHot;
            data["IsShow"] = IsShow;
            data["EventType"] = EventType;
            data["UrlIcon"] = UrlIcon;
            data["UrlImage"] = UrlImage;
            data["ImageLobby"] = ImageLobby;
            data["EventInfo"] = EventInfo;
            data["JoinEvent"] = JoinEvent;
            data["TopEvent"] = TopEvent;
            data["dateStart"] = dateStart.ToString("yyyy-MM-dd HH:mm:ss");
            data["dateEnd"] = dateEnd.ToString("yyyy-MM-dd HH:mm:ss");
            return data;
        }
    }
}
