using System.IO;

namespace Entites.General
{
    public class WhoTrusts
    {
        public WhoTrusts()
        {
        }

        public long UserId { get; set; }
                
        public string Avatar { get; set; }

        public string Username { get; set; }

        public string Nickname { get; set; }

        public string Language { get; set; }
        
        public long Trust { get; set; }

        public long VipPoint { get; set; }
        public string VipName = "";
        public int Vip { get; set; }
        public int totalMsg { get; set; }
        public static byte RegisterType
        {
            get { return (byte)'U'; }
        }
        public static byte[] Serialize(object data)
        { 
            WhoTrusts ene = (WhoTrusts)data;
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(ene.UserId);
                    writer.Write(ene.Avatar);
                    writer.Write(ene.Username);
                    writer.Write(ene.Nickname);
                    writer.Write(ene.Language);
                    writer.Write(ene.Trust);
                    writer.Write(ene.VipPoint);
                    writer.Write(ene.VipName);
                    writer.Write(ene.Vip);
                    writer.Write(ene.totalMsg);
                }
                return m.ToArray();
            }
        }

        public static WhoTrusts Desserialize(byte[] data)
        {
            WhoTrusts result = new WhoTrusts();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.UserId = reader.ReadInt64();
                    result.Avatar = reader.ReadString();
                    result.Username = reader.ReadString();

                    result.Nickname = reader.ReadString();
                    result.Language = reader.ReadString();
                    result.Trust = reader.ReadInt64();
                    result.VipPoint = reader.ReadInt64();
                    result.VipName = reader.ReadString();
                    result.Vip = reader.ReadInt32();
                    result.totalMsg = reader.ReadInt32();
                }
            }
            return result;
        }
    }
}
