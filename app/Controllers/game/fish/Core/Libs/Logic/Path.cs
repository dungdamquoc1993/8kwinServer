using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanCa.Libs
{
    public struct PathNode
    {
        public long TimeStamp;
        public float X, Y;

        public PathNode(long timeStamp, float x, float y)
        {
            TimeStamp = timeStamp;
            X = x;
            Y = y;
        }

        public JSONNode ToJson()
        {
            var res = new JSONObject();
            res["x"] = X;
            res["y"] = Y;
            res["t"] = TimeStamp;
            return res;
        }
    }

    /// <summary>
    /// Move fish by path
    /// </summary>
    public class Path
    {
        public List<PathNode> Nodes = new List<PathNode>();

        public int Count
        {
            get { return Nodes.Count; }
        }

        public bool IsEmpty
        {
            get
            {
                return Nodes.Count == 0;
            }
        }

        public void Clear()
        {
            Nodes.Clear();
        }

        public void AddNode(long timeStamp, float x, float y)
        {
            this.Nodes.Add(new PathNode(timeStamp, x, y));
        }

        public PathNode LastNode
        {
            get
            {
                if (Nodes.Count > 0) return Nodes[Nodes.Count - 1];
                return default(PathNode);
            }
        }

        public void GetPositionByTime(long timeStamp, ref float x, ref float y, ref Vector direction, out bool outOfRange)
        {
            outOfRange = true;
            if (Nodes[0].TimeStamp > timeStamp) return;
            for (int i = 0, n = Nodes.Count - 1; i < n; i++)
            {
                if (Nodes[i].TimeStamp <= timeStamp && Nodes[i + 1].TimeStamp > timeStamp)
                {
                    var start = Nodes[i];
                    var end = Nodes[i + 1];
                    var rate = (float)(timeStamp - start.TimeStamp) / (end.TimeStamp - start.TimeStamp);
                    outOfRange = false;
                    direction.Set(end.X - start.X, end.Y - start.Y);
                    x = start.X + (direction.X) * rate;
                    y = start.Y + (direction.Y) * rate;
                    direction.Normalize();
                    break;
                }
            }
        }

        public JSONArray ToJson()
        {
            var res = new JSONArray();
            for (int i = 0, n = Nodes.Count; i < n; i++)
            {
                res.Add(Nodes[i].ToJson());
            }
            return res;
        }
    }
}
