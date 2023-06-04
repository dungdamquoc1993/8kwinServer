
using Entites.Cms;

namespace Entites.General
{
    public class LoginFbResponse : response_base
    {
        public string Username { get; set; }
        public bool NewAcc = false;
    }
}
