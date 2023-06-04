using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    class Pair
    {

        public object k;
        public object v;

        public Pair(object k, object v)
        {
            this.k = k;
            this.v = v;
        }
    }
    public class MsgPackDecoder
    {
        public static JSONNode Decode(System.IO.BinaryReader i)
        {
            try
            {
                var b = i.ReadByte();
                switch (b)
                {
                    // null
                    case 0xc0: return JSONData.NULL;

                    // boolean
                    case 0xc2: return false;
                    case 0xc3: return true;

                    // binary
                    case 0xc4:
                        {
                            var bs = i.ReadBytes(i.ReadByte());
                            return new JSONBinary(bs, 0, bs.Length);
                        }
                    case 0xc5:
                        {
                            var bs = i.ReadBytes(readUInt16(i));
                            return new JSONBinary(bs, 0, bs.Length);
                        }
                    case 0xc6:
                        {
                            var bs = i.ReadBytes(readInt32(i));
                            return new JSONBinary(bs, 0, bs.Length);
                        }

                    // floating point
                    case 0xca: return i32ToFloat(readInt32(i));
                    case 0xcb: return readDouble(i);

                    // unsigned int
                    case 0xcc: return i.ReadByte();
                    case 0xcd: return readUInt16(i);
                    case 0xce: return readUInt32(i);
                    case 0xcf: throw new Exception("UInt64 not supported");

                    // signed int
                    case 0xd0: return readInt8(i);
                    case 0xd1: return readInt16(i);
                    case 0xd2: return readInt32(i);
                    case 0xd3: return readInt64(i);

                    // string
                    case 0xd9: return readString(i, i.ReadByte());
                    case 0xda: return readString(i, readUInt16(i));
                    case 0xdb: return readString(i, readInt32(i));

                    // array 16, 32
                    case 0xdc: return readArray(i, readUInt16(i));
                    case 0xdd: return readArray(i, readInt32(i));

                    // map 16, 32
                    case 0xde: return readMap(i, readUInt16(i));
                    case 0xdf: return readMap(i, readInt32(i));

                        //default:
                        //    {

                        //    }
                }

                // default
                if (b < 0x80) { return b; }
                else if (b < 0x90) { return readMap(i, (0xf & b)); }// positive fix num
                else if (b < 0xa0) { return readArray(i, (0xf & b)); }// fix map
                else if (b < 0xc0) { return readString(i, 0x1f & b); }// fix array
                else if (b > 0xdf) { return -256 | b; }      // fix string, negative fix num
            }
            catch (Exception ex)
            {
                Logger.Error("Cannot decode msgpack: " + ex.ToString());
            }

            return JSONData.NULL;
        }

        public static float i32ToFloat(int i)
        {
            unchecked
            {
                SingleHelper helper = new SingleHelper(((float)(0)));
                if (global::System.BitConverter.IsLittleEndian)
                {
                    helper.i = i;
                }
                else
                {
                    helper.i = ((((((int)((((uint)(i)) >> 24))) & 255) | ((((i >> 16) & 255)) << 8)) | ((((i >> 8) & 255)) << 16)) | (((i & 255)) << 24));
                }

                return ((float)(helper.f));
            }
        }
        public static double i64ToDouble(int low, int high)
        {
            unchecked
            {
                FloatHelper helper = new FloatHelper(((double)(0.0)));
                if (global::System.BitConverter.IsLittleEndian)
                {
                    long this1 = ((long)((((long)((((long)(high)) << 32))) | ((long)((((long)(low)) & ((long)(0xffffffffL))))))));
                    helper.i = ((long)(this1));
                }
                else
                {
                    int i1 = high;
                    int i2 = low;
                    int j2 = ((((((int)((((uint)(i1)) >> 24))) & 255) | ((((i1 >> 16) & 255)) << 8)) | ((((i1 >> 8) & 255)) << 16)) | (((i1 & 255)) << 24));
                    int j1 = ((((((int)((((uint)(i2)) >> 24))) & 255) | ((((i2 >> 16) & 255)) << 8)) | ((((i2 >> 8) & 255)) << 16)) | (((i2 & 255)) << 24));
                    long this2 = ((long)((((long)((((long)(j1)) << 32))) | ((long)((((long)(j2)) & ((long)(0xffffffffL))))))));
                    helper.i = ((long)(this2));
                }

                return helper.f;
            }
        }
        public static ushort readUInt16(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int ch1 = reader.ReadByte();
                int ch2 = reader.ReadByte();
                //if (this.bigEndian)
                //{
                return (ushort)(ch2 | (ch1 << 8));
                //}
                //else
                //{
                //    return (ch1 | (ch2 << 8));
                //}

            }
        }
        public static short readInt16(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int ch1 = reader.ReadByte();
                int ch2 = reader.ReadByte();
                int n =
                    //((this.bigEndian) ? 
                    ((ch2 | (ch1 << 8)));
                //: 
                //((ch1 | (ch2 << 8))));
                if ((((n & 32768)) != 0))
                {
                    return (short)(n - 65536);
                }

                return (short)n;
            }
        }
        public static int readInt32(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int ch1 = reader.ReadByte();
                int ch2 = reader.ReadByte();
                int ch3 = reader.ReadByte();
                int ch4 = reader.ReadByte();
                //if (this.bigEndian)
                //{
                return (((ch4 | (ch3 << 8)) | (ch2 << 16)) | (ch1 << 24));
                //}
                //else
                //{
                //    return (((ch1 | (ch2 << 8)) | (ch3 << 16)) | (ch4 << 24));
                //}
            }
        }
        public static uint readUInt32(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int ch1 = reader.ReadByte();
                int ch2 = reader.ReadByte();
                int ch3 = reader.ReadByte();
                int ch4 = reader.ReadByte();
                //if (this.bigEndian)
                //{
                return (uint)(((ch4 | (ch3 << 8)) | (ch2 << 16)) | (ch1 << 24));
                //}
                //else
                //{
                //    return (((ch1 | (ch2 << 8)) | (ch3 << 16)) | (ch4 << 24));
                //}
            }
        }
        public static long readInt64(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int high = readInt32(reader);
                int low = readInt32(reader);
                long this1 = ((long)((((long)((((long)(high)) << 32))) | ((long)((((long)(low)) & ((long)(0xffffffffL))))))));
                return ((long)(this1));
            }
        }
        public static double readDouble(System.IO.BinaryReader reader)
        {
            int i1 = readInt32(reader);
            int i2 = readInt32(reader);
            //if (this.bigEndian)
            //{
            return i64ToDouble(i2, i1);
            //}
            //else
            //{
            //    return i64ToDouble(i1, i2);
            //}

        }
        public static string readString(System.IO.BinaryReader reader, int length)
        {
            var output = BufferPool.Obtain();
            reader.Read(output.ByteBuffer, 0, length);
            var str = Encoding.UTF8.GetString(output.ByteBuffer, 0, length);
            BufferPool.Free(output);
            return str;
        }
        public static JSONArray readArray(System.IO.BinaryReader reader, int length)
        {
            var a = new JSONArray();
            for (int i = 0; i < length; i++)
            {
                a.Add(Decode(reader));
            }
            return a;
        }
        public static sbyte readInt8(System.IO.BinaryReader reader)
        {
            unchecked
            {
                int n = reader.ReadByte();
                if ((n >= 128))
                {
                    return (sbyte)(n - 256);
                }

                return (sbyte)n;
            }
        }
        public static JSONObject readMap(System.IO.BinaryReader reader, int length)
        {
            var _out = new JSONObject();
            for (int i = 0; i < length; i++)
            {
                var k = Decode(reader);
                var v = Decode(reader);
                _out.Add(k, v);
            }

            return _out;
        }
    }
}
