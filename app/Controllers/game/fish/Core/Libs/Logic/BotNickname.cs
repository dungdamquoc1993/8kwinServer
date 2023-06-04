using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanCa.Libs
{
    public class BotNickname
    {
        static BotNickname()
        {
            ReloadNicknames();
        }

        static readonly List<string> Nicknames = new List<string>();

        public static string RandomName(Random r)
        {
            return Nicknames[r.Next(Nicknames.Count)];
        }

        public static void ReloadNicknames()
        {
            try
            {
                var jt = System.IO.File.ReadAllText("./data.json");
                var ja = JSON.Parse(jt).AsArray;
                Nicknames.Clear();
                for (int i = 0, n = ja.Count; i < n; i++)
                {
                    string name = ja[i];
                    Nicknames.Add(name);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Load nicknames fail: " + ex.ToString());
            }
        }
    }
}
