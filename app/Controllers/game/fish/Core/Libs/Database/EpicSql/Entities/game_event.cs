using System.Collections.Generic;

namespace Entites.Cms
{
    public class game_event : response_base
    {
        public int event_id;
        public string title;
        public byte is_new;
        public byte is_show;
        public int event_type;
        public string url_icon;
        public string url_image;
        public string url_lobby;
        public string top_event;
        public string event_info;
        public string join_event;
        public string create_time;
        public string start_time;
        public string end_time;
    }

    public class game_events : response_base
    {
        public List<game_event> data { get; set; }
        public game_events()
        {
            data = new List<game_event>();
        }
    }
}
