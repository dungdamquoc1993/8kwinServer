
using Entites.Cms;

namespace Entites.General
{
    public class Profile : response_base
    {
        public long UserID { get; set; }
        public string Nickname { get; set; }
        public int Level { get; set; }
        public string Levelname { get; set; }
        public long Cash { get; set; }
        public long Ruby { get; set; }
        public int CurrentVip { get; set; }
        public int MaxVip { get; set; }
        public string Avatar { get; set; }

    }
}
