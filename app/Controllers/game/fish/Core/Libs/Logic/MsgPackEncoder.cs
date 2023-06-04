using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class MsgPackEncoder
    {
        public static void writeMapLength(System.IO.BinaryWriter aWriter, int length)
        {
            if (length < 0x10)
            {
                // fix map
                aWriter.Write((byte)(0x80 | length));
            }
            else if (length < 0x10000)
            {
                // map 16
                aWriter.Write((byte)0xde);
                //aWriter.Write((ushort)length);
                aWriter.Write((byte)(length >> 8));
                aWriter.Write((byte)(length & 0xFF));
            }
            else
            {
                // map 32
                aWriter.Write((byte)0xdf);
                //aWriter.Write(length);
                aWriter.Write((byte)((int)((uint)length >> 24)));
                aWriter.Write((byte)((length >> 16) & 0xFF));
                aWriter.Write((byte)((length >> 8) & 0xFF));
                aWriter.Write((byte)(length & 0xFF));
            }
        }
        public static void writeString(System.IO.BinaryWriter aWriter, string b)
        {
            var output = BufferPool.Obtain();
            var length = Encoding.UTF8.GetBytes(b, 0, b.Length, output.ByteBuffer, 0);
            if (length < 0x20)
            {
                // fix string
                aWriter.Write((byte)(0xa0 | length));
            }
            else if (length < 0x100)
            {
                // string 8
                aWriter.Write((byte)0xd9);
                aWriter.Write((byte)length);
            }
            else if (length < 0x10000)
            {
                // string 16
                aWriter.Write((byte)0xda);
                //aWriter.Write((ushort)length);
                aWriter.Write((byte)(length >> 8));
                aWriter.Write((byte)(length & 0xFF));
            }
            else
            {
                // string 32
                aWriter.Write((byte)0xdb);
                //aWriter.Write(length);
                aWriter.Write((byte)((int)((uint)length >> 24)));
                aWriter.Write((byte)((length >> 16) & 0xFF));
                aWriter.Write((byte)((length >> 8) & 0xFF));
                aWriter.Write((byte)(length & 0xFF));
            }

            aWriter.Write(output.ByteBuffer, 0, length);
            BufferPool.Free(output);
        }
        public static void writeInt64(System.IO.BinaryWriter aWriter, long d)
        {
            aWriter.Write((byte)0xd3);
            //aWriter.Write(d);
            aWriter.Write((byte)((long)((ulong)d >> 56)));
            aWriter.Write((byte)((d >> 48) & 0xFF));
            aWriter.Write((byte)((d >> 40) & 0xFF));
            aWriter.Write((byte)((d >> 32) & 0xFF));
            aWriter.Write((byte)((d >> 24) & 0xFF));
            aWriter.Write((byte)((d >> 16) & 0xFF));
            aWriter.Write((byte)((d >> 8) & 0xFF));
            aWriter.Write((byte)(d & 0xFF));
        }

        public static void writeInt(System.IO.BinaryWriter aWriter, long d)
        {
            if (d < -(1 << 5))
            {
                // less than negative fixnum ?
                if (d < -(1 << 15))
                {
                    // signed int 32
                    aWriter.Write((byte)0xd2);
                    //aWriter.Write(d);
                    aWriter.Write((byte)((int)((uint)d >> 24)));
                    aWriter.Write((byte)((d >> 16) & 0xFF));
                    aWriter.Write((byte)((d >> 8) & 0xFF));
                    aWriter.Write((byte)(d & 0xFF));
                }
                else if (d < -(1 << 7))
                {
                    // signed int 16
                    aWriter.Write((byte)0xd1);
                    //aWriter.Write((short)d);
                    aWriter.Write((byte)((short)((ushort)d >> 8)));
                    aWriter.Write((byte)(d & 0xFF));
                }
                else
                {
                    // signed int 8
                    aWriter.Write((byte)0xd0);
                    aWriter.Write((sbyte)d);
                }
            }
            else if (d < (1 << 7))
            {
                // negative fixnum < d < positive fixnum [fixnum]
                aWriter.Write((byte)(d & 0x000000ff));
            }
            else
            {
                // unsigned land
                if (d < (1 << 8))
                {
                    // unsigned int 8
                    aWriter.Write((byte)0xcc);
                    aWriter.Write((byte)(d & 0xFF));
                }
                else if (d < (1 << 16))
                {
                    // unsigned int 16
                    aWriter.Write((byte)0xcd);
                    //aWriter.Write((ushort)d);
                    aWriter.Write((byte)(d >> 8));
                    aWriter.Write((byte)(d & 0xFF));
                }
                else
                {
                    // unsigned int 32
                    aWriter.Write((byte)0xce);
                    //aWriter.Write((uint)d);
                    aWriter.Write((byte)(d >> 24));
                    aWriter.Write((byte)((d >> 16) & 0xFF));
                    aWriter.Write((byte)((d >> 8) & 0xFF));
                    aWriter.Write((byte)(d & 0xFF));
                }
            }
        }
        public static int floatToI32(float f)
        {
            unchecked
            {
                SingleHelper helper = new SingleHelper((f));
                if (global::System.BitConverter.IsLittleEndian)
                {
                    return helper.i;
                }
                else
                {
                    int i = helper.i;
                    return ((((((int)((((uint)(i)) >> 24))) & 255) | ((((i >> 16) & 255)) << 8)) | ((((i >> 8) & 255)) << 16)) | (((i & 255)) << 24));
                }
            }
        }
        public static long doubleToI64(double v)
        {
            unchecked
            {
                FloatHelper helper = new FloatHelper(v);
                if (global::System.BitConverter.IsLittleEndian)
                {
                    return helper.i;
                }
                else
                {
                    long i = helper.i;
                    int i1 = ((int)(((long)((((long)(i)) >> 32)))));
                    int i2 = ((int)(((long)(i))));
                    int j2 = ((int)((((int)((((int)((((int)((((int)(((int)((((uint)(i1)) >> 24))))) & 255))) | ((int)(((((int)((((int)((i1 >> 16))) & 255)))) << 8)))))) | ((int)(((((int)((((int)((i1 >> 8))) & 255)))) << 16)))))) | ((int)(((((int)((i1 & 255)))) << 24))))));
                    int j1 = ((int)((((int)((((int)((((int)((((int)(((int)((((uint)(i2)) >> 24))))) & 255))) | ((int)(((((int)((((int)((i2 >> 16))) & 255)))) << 8)))))) | ((int)(((((int)((((int)((i2 >> 8))) & 255)))) << 16)))))) | ((int)(((((int)((i2 & 255)))) << 24))))));
                    long this1 = ((long)((((long)((((long)(j1)) << 32))) | ((long)((((long)(j2)) & ((long)(0xffffffffL))))))));
                    return ((long)(this1));
                }
            }
        }
        public static void writeFloat(System.IO.BinaryWriter aWriter, float f)
        {
            // Single Precision Floating
            aWriter.Write((byte)0xca);
            int d = floatToI32(f);
            aWriter.Write((byte)((int)((uint)d >> 24)));
            aWriter.Write((byte)((d >> 16) & 0xFF));
            aWriter.Write((byte)((d >> 8) & 0xFF));
            aWriter.Write((byte)(d & 0xFF));
        }
        public static void writeDouble(System.IO.BinaryWriter aWriter, double v)
        {
            // Double Precision Floating
            aWriter.Write((byte)0xcb);
            long d = doubleToI64(v);
            //aWriter.Write(d);
            aWriter.Write((byte)((long)((ulong)d >> 56)));
            aWriter.Write((byte)((d >> 48) & 0xFF));
            aWriter.Write((byte)((d >> 40) & 0xFF));
            aWriter.Write((byte)((d >> 32) & 0xFF));
            aWriter.Write((byte)((d >> 24) & 0xFF));
            aWriter.Write((byte)((d >> 16) & 0xFF));
            aWriter.Write((byte)((d >> 8) & 0xFF));
            aWriter.Write((byte)(d & 0xFF));
        }
        public static void writeBinary(System.IO.BinaryWriter aWriter, byte[] b, int start, int length)
        {
            if (length < 0x100)
            {
                // binary 8
                aWriter.Write((byte)0xc4);
                aWriter.Write((byte)length);
            }
            else if (length < 0x10000)
            {
                // binary 16
                aWriter.Write((byte)0xc5);
                //aWriter.Write((ushort)length);
                aWriter.Write((byte)(length >> 8));
                aWriter.Write((byte)(length & 0xFF));
            }
            else
            {
                // binary 32
                aWriter.Write((byte)0xc6);
                //aWriter.Write(length);
                aWriter.Write((byte)((int)((uint)length >> 24)));
                aWriter.Write((byte)((length >> 16) & 0xFF));
                aWriter.Write((byte)((length >> 8) & 0xFF));
                aWriter.Write((byte)(length & 0xFF));
            }
            aWriter.Write(b, start, length);
        }
        public static void writeArray(System.IO.BinaryWriter aWriter, JSONArray b)
        {
            var length = b.Count;
            if (length < 0x10)
            {
                // fix array
                aWriter.Write((byte)(0x90 | length));
            }
            else if (length < 0x10000)
            {
                // array 16
                aWriter.Write((byte)0xdc);
                //aWriter.Write((ushort)length);
                aWriter.Write((byte)(length >> 8));
                aWriter.Write((byte)(length & 0xFF));
            }
            else
            {
                // array 32
                aWriter.Write((byte)0xdd);
                //aWriter.Write(length);
                aWriter.Write((byte)((int)((uint)length >> 24)));
                aWriter.Write((byte)((length >> 16) & 0xFF));
                aWriter.Write((byte)((length >> 8) & 0xFF));
                aWriter.Write((byte)(length & 0xFF));
            }

            for (int i = 0; i < length; i++)
            {
                b[i].SerializeMsgPack(aWriter);
            }
        }
        public static void writeBool(System.IO.BinaryWriter aWriter, bool b)
        {
            aWriter.Write((byte)(b ? 0xc3 : 0xc2));
        }
        public static void writeNull(System.IO.BinaryWriter aWriter)
        {
            aWriter.Write((byte)0xc0);
        }
    }
}
