#if !UNITY_WEBPLAYER
#define USE_FileIO
#endif

//#define Recycle

/* * * * *
 * A simple JSON Parser / builder
 * use #define USE_lzLib for lz4 compression
 * * * * */
#if USE_lzLib
using K4os.Compression.LZ4;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Utils;

namespace SimpleJSON
{
    public interface IJsonSerializable
    {
        JSONNode ToJson();

        void ParseJson(JSONNode data);
    }

    public enum JSONBinaryTag : byte
    {
        Array = 1,
        Object = 2,
        String = 3,
        IntValue = 4,
        DoubleValue = 5,
        BoolValue = 6,
        FloatValue = 7,
        LongValue = 8,
        ByteValue = 9,
        ShortValue = 10,
        NullValue = 11,
        Binary = 12,
        SByteValue = 13,
        UShortValue = 14,
        UIntValue = 15,

        // normal type use byte size by default to save space
        BigArray = 16,
        BigObject = 17,
        BigBinary = 18
    }

    public class BufferPool
    {
        public const int POOL_MAX = 128;
        private static List<Buffer> pools = new List<Buffer>(POOL_MAX);

        public static Buffer Obtain(int capacity = -1)
        {
            lock (pools)
            {
                if (pools.Count > 0)
                {
                    var res = pools[pools.Count - 1];
                    pools.RemoveAt(pools.Count - 1);
                    res.Start = res.Length = 0;
                    res.EnsureCapacity(capacity);
                    return res;
                }
            }

            var b = new Buffer(capacity);
            return b;
        }

        public static void Free(Buffer buffer)
        {
            lock (pools)
            {
                if (pools.Count < POOL_MAX)
                    pools.Add(buffer);
            }
        }
    }

    public class Buffer
    {
        /// <summary>
        /// Buffer pool item size for any serialize operation, make sure to set higher than any possible json
        /// </summary>
        public static int BUFFER_SIZE = 128 * 1024; // 4 MB

        public byte[] ByteBuffer { get; private set; }

        private int start;
        public int Start
        {
            get
            {
                return start;
            }

            set
            {
                start = value;
                boundStream.Position = start;
            }
        }

        private int length;
        public int Length
        {
            get
            {
                return length;
            }

            set
            {
                length = value;
                //boundStream.SetLength(length);
            }
        }

        internal MemoryStream boundStream;
        public BinaryWriter Writer { get; private set; }
        public BinaryReader Reader { get; private set; }

        public Buffer(int capacity = -1)
        {
            ByteBuffer = new byte[capacity <= BUFFER_SIZE ? BUFFER_SIZE : capacity];
            boundStream = new MemoryStream(ByteBuffer, 0, ByteBuffer.Length, true, true);
            Writer = new BinaryWriter(boundStream, Encoding.UTF8, true);
            Reader = new BinaryReader(boundStream, Encoding.UTF8, true);
            Start = Length = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            if (ByteBuffer.Length < capacity)
            {
                ByteBuffer = new byte[capacity];
                boundStream.Dispose();
                boundStream = new MemoryStream(ByteBuffer, 0, ByteBuffer.Length, true, true);
                Writer.Dispose();
                Writer = new BinaryWriter(boundStream, Encoding.UTF8, true);
                Reader.Dispose();
                Reader = new BinaryReader(boundStream, Encoding.UTF8, true);
            }
        }

        public MemoryStream CreateReadMemoryStream(int start, int length)
        {
            if (length <= 0) length = ByteBuffer.Length;

            var stream = boundStream;
            Length = length;
            Start = start;
            return stream;
        }

        public MemoryStream CreateWriteMemoryStream()
        {
            var stream = boundStream;
            Length = 0;
            Start = 0;
            return stream;
        }

        public void Free()
        {
            BufferPool.Free(this);
        }

        public override string ToString()
        {
            return JSON.ListByteToJson(ByteBuffer, Length).ToString();
        }

        public byte[] ToByteArray()
        {
            var res = new byte[Length];
            Array.Copy(ByteBuffer, Start, res, 0, Length);
            return res;
        }

        public void CopyByteArray(byte[] data, int offset = 0, int count = -1)
        {
            if (count == -1) count = data.Length;

            Array.Copy(data, offset, ByteBuffer, 0, count);
            Start = 0;
            Length = count;
        }

        public void ShiftRight(int count)
        {
            for (int i = Start + Length - 1; i >= Start; i--)
            {
                ByteBuffer[i + count] = ByteBuffer[i];
            }
            Start += count;
        }

        public void ShiftLeft(int count)
        {
            var maxId = Start + Length;
            for (int i = Start; i < maxId; i++)
            {
                ByteBuffer[i - count] = ByteBuffer[i];
            }
            Start -= count;
        }
    }

    public class JSONNode : IEnumerable
    {
        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string NULL = "null";

        protected JSONNode()
        {
        }

        #region common interface
        public static bool forceASCII = false; // Use Unicode by default
        public static bool allowLineComments = true; // allow "//"-style comments at the end of a line

        public virtual void Add(string aKey, JSONNode aItem) { }
        public virtual JSONNode this[int aIndex] { get { return null; } set { } }
        public virtual JSONNode this[string aKey] { get { return null; } set { } }
        public virtual string Value { get { return ""; } set { } }
        public virtual Dictionary<string, JSONNode>.KeyCollection Keys
        {
            get { return null; }
        }
        public virtual int Count { get { return 0; } }

        public virtual void Add(JSONNode aItem)
        {
            Add("", aItem);
        }

        public virtual JSONNode Remove(string aKey) { return null; }
        public virtual JSONNode Remove(int aIndex) { return null; }
        public virtual JSONNode Remove(JSONNode aNode) { return aNode; }

        public virtual IEnumerable<JSONNode> Childs { get { yield break; } }
        public IEnumerable<JSONNode> DeepChilds
        {
            get
            {
                foreach (var C in Childs)
                    foreach (var D in C.DeepChilds)
                        yield return D;
            }
        }

        public override string ToString()
        {
            return "JSONNode";
        }

        #endregion common interface

        #region typecasting properties
        public virtual byte AsByte
        {
            get
            {
                byte v = 0;
                if (byte.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual short AsShort
        {
            get
            {
                short v = 0;
                if (short.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual int AsInt
        {
            get
            {
                int v = 0;
                if (int.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual long AsLong
        {
            get
            {
                long v = 0;
                if (long.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual sbyte AsSByte
        {
            get
            {
                sbyte v;
                if (sbyte.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual ushort AsUShort
        {
            get
            {
                ushort v;
                if (ushort.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual uint AsUInt
        {
            get
            {
                uint v;
                if (uint.TryParse(Value, out v))
                    return v;
                return 0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual float AsFloat
        {
            get
            {
                float v = 0.0f;
                if (float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return v;
                return 0.0f;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual double AsDouble
        {
            get
            {
                double v = 0.0;
                if (double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return v;
                return 0.0;
            }
            set
            {
                Value = value.ToString();
            }
        }
        public virtual bool AsBool
        {
            get
            {
                bool v = false;
                if (bool.TryParse(Value, out v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set
            {
                Value = (value) ? TRUE : FALSE;
            }
        }
        public virtual JSONArray AsArray
        {
            get
            {
                return this as JSONArray;
            }
        }
        public virtual JSONObject AsObject
        {
            get
            {
                return this as JSONObject;
            }
        }
        public virtual JSONBinary AsBinary
        {
            get
            {
                return this as JSONBinary;
            }
        }

        public virtual JSONBinaryTag GetJsonType()
        {
            return JSONBinaryTag.Object;
        }

        #endregion typecasting properties

        #region operators
#if Recycle
        public static implicit operator JSONNode(bool s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(byte s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(short s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(int s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(float s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(long s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(sbyte s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(ushort s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(uint s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(double s)
        {
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(string s)
        {
            if (s == null) return JSONData.NullValue;
            return JsonRecycle.GetData(s);
        }
        public static implicit operator JSONNode(byte[] s)
        {
            if (s == null) return JSONData.NullValue;
            return JsonRecycle.GetData(s);
        }
#else
        public static implicit operator JSONNode(bool s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(byte s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(short s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(int s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(float s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(long s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(sbyte s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(ushort s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(uint s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(double s)
        {
            return new JSONData(s);
        }
        public static implicit operator JSONNode(string s)
        {
            if (s == null)
                return JSONData.NullValue;
            return new JSONData(s);
        }
        public static implicit operator JSONNode(byte[] s)
        {
            if (s == null) return JSONData.NullValue;
            return new JSONBinary(s);
        }
#endif

        public static implicit operator string(JSONNode d) // use in cast to string
        {
            return (d == null) ? null : (d is JSONData ? d.Value : d.ToString());
        }
        public static implicit operator double(JSONNode d)
        {
            return (d == null) ? 0.0 : d.AsDouble;
        }
        public static implicit operator float(JSONNode d)
        {
            return (d == null) ? 0f : d.AsFloat;
        }
        public static implicit operator byte(JSONNode d)
        {
            return (d == null) ? (byte)0 : d.AsByte;
        }
        public static implicit operator short(JSONNode d)
        {
            return (d == null) ? (short)0 : d.AsShort;
        }
        public static implicit operator int(JSONNode d)
        {
            return (d == null) ? 0 : d.AsInt;
        }
        public static implicit operator long(JSONNode d)
        {
            return (d == null) ? 0L : d.AsLong;
        }
        public static implicit operator sbyte(JSONNode d)
        {
            return (d == null) ? (sbyte)0 : d.AsSByte;
        }
        public static implicit operator ushort(JSONNode d)
        {
            return (d == null) ? (ushort)0 : d.AsUShort;
        }
        public static implicit operator uint(JSONNode d)
        {
            return (d == null) ? (uint)0 : d.AsUInt;
        }
        public static implicit operator bool(JSONNode d)
        {
            return (d == null) ? false : d.AsBool;
        }

        public static bool operator ==(JSONNode a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;
            bool aIsNull = ReferenceEquals(a, null) || ReferenceEquals(a, JSONData.NullValue) || (a is JSONData && ((JSONData)a).JsonDataType == JSONBinaryTag.NullValue) || a is JSONLazyCreator;
            bool bIsNull = ReferenceEquals(b, null) || ReferenceEquals(b, JSONData.NullValue) || (b is JSONData && ((JSONData)b).JsonDataType == JSONBinaryTag.NullValue) || b is JSONLazyCreator;
            if (aIsNull && bIsNull)
                return true;
            return !aIsNull && a.Equals(b);
        }

        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion operators

        internal static void Escape(string aText, bool strBuilderContinue, ref StringBuilder output)
        {
            StringBuilder result = output;
            if (!strBuilderContinue)
            {
                result.Length = 0;
                var max = aText.Length + aText.Length / 10;
                if (result.Capacity < max)
                    result.Capacity = max;
            }
            for (int i = 0, n = aText.Length; i < n; i++)
            {
                char c = aText[i];
                switch (c)
                {
                    case '\\': result.Append(@"\\"); break;
                    case '\"': result.Append("\\\""); break;
                    case '\n': result.Append(@"\n"); break;
                    case '\r': result.Append(@"\r"); break;
                    case '\t': result.Append(@"\t"); break;
                    case '\b': result.Append(@"\b"); break;
                    case '\f': result.Append(@"\f"); break;
                    default:
                        if (c < ' ' || (forceASCII && c > 127))
                        {
                            ushort val = c;
                            result.Append("\\u").Append(val.ToString("X4"));
                        }
                        else
                            result.Append(c);
                        break;
                }
            }
        }

        private static List<Stack<JSONNode>> poolStack = new List<Stack<JSONNode>>();
        private static Stack<JSONNode> obtainStack()
        {
            lock (poolStack)
            {
                var last = poolStack.Count - 1;
                if (last >= 0)
                {
                    var item = poolStack[last];
                    poolStack.RemoveAt(last);
                    return item;
                }
            }

            return new Stack<JSONNode>();
        }
        private static void freeStack(Stack<JSONNode> stack)
        {
            stack.Clear();
            lock (poolStack)
                poolStack.Add(stack);
        }

        public static JSONNode Parse(string aJSON)
        {
            Stack<JSONNode> stack = obtainStack();
            JSONNode ctx = null;
            int i = 0;
            StringBuilder Token = JSON.obtainStringBuilder();
            string TokenName = "";
            bool QuoteMode = false;
            bool isEmpty = false;
            bool hasEndQuote = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
#if Recycle
                        stack.Push(JsonRecycle.GetObject());
#else
                        stack.Push(new JSONObject());
#endif
                        if (ctx != null)
                        {
                            if (!string.IsNullOrEmpty(TokenName))
                                ctx.Add(TokenName, stack.Peek());
                            else
                                ctx.Add(stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        hasEndQuote = false;
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
#if Recycle
                        stack.Push(JsonRecycle.GetArray());
#else
                        stack.Push(new JSONArray());
#endif

                        if (ctx != null)
                        {
                            if (!string.IsNullOrEmpty(TokenName))
                                ctx.Add(TokenName, stack.Peek());
                            else
                                ctx.Add(stack.Peek());
                        }
                        TokenName = "";
                        Token.Length = 0;
                        hasEndQuote = false;
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token.Length != 0)
                        {
                            var _value = Token.ToString().Trim();
                            if (!string.IsNullOrEmpty(TokenName))
                            {
                                if (!hasEndQuote && isNullStr(_value))
                                    ctx.Add(TokenName, JSONData.NullValue);
                                else
                                    ctx.Add(TokenName, _value);
                            }
                            else
                            {
                                if (!hasEndQuote && isNullStr(_value))
                                    ctx.Add(JSONData.NullValue);
                                else
                                    ctx.Add(_value);
                            }
                        }
                        else if (isEmpty)
                        {
                            if (!string.IsNullOrEmpty(TokenName))
                                ctx.Add(TokenName, "");
                            else
                                ctx.Add("");
                            isEmpty = false;
                        }
                        TokenName = "";
                        Token.Length = 0;
                        hasEndQuote = false;
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        TokenName = Token.ToString().Trim();
                        Token.Length = 0;
                        hasEndQuote = false;
                        break;

                    case '"':
                        QuoteMode ^= true;
                        if (QuoteMode) // open quote
                        {
                            isEmpty = false;
                        }
                        else // close quote
                        {
                            var lastI = i - 1;
                            if (lastI >= 0)
                            {
                                isEmpty = aJSON[lastI] == '"';
                            }
                            hasEndQuote = true;
                        }
                        break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token.Append(aJSON[i]);
                            break;
                        }
                        if (Token.Length != 0)
                        {
                            var _value = Token.ToString().Trim();
                            if (!string.IsNullOrEmpty(TokenName))
                            {
                                if (!hasEndQuote && isNullStr(_value))
                                    ctx.Add(TokenName, JSONData.NullValue);
                                else
                                    ctx.Add(TokenName, _value);
                            }
                            else
                            {
                                if (!hasEndQuote && isNullStr(_value))
                                    ctx.Add(JSONData.NullValue);
                                else
                                    ctx.Add(_value);
                            }
                        }
                        else if (isEmpty)
                        {
                            if (!string.IsNullOrEmpty(TokenName))
                                ctx.Add(TokenName, "");
                            else
                                ctx.Add("");
                            isEmpty = false;
                        }
                        TokenName = "";
                        Token.Length = 0;
                        hasEndQuote = false;
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token.Append(aJSON[i]);
                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't': Token.Append('\t'); break;
                                case 'r': Token.Append('\r'); break;
                                case 'n': Token.Append('\n'); break;
                                case 'b': Token.Append('\b'); break;
                                case 'f': Token.Append('\f'); break;
                                case 'u':
                                    {
                                        string s = aJSON.Substring(i + 1, 4);
                                        Token.Append((char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier));
                                        i += 4;
                                        break;
                                    }
                                default: Token.Append(C); break;
                            }
                        }
                        break;
                    case '/':
                        if (allowLineComments && !QuoteMode && i + 1 < aJSON.Length && aJSON[i + 1] == '/')
                        {
                            while (++i < aJSON.Length && aJSON[i] != '\n' && aJSON[i] != '\r') ;
                            break;
                        }
                        Token.Append(aJSON[i]);
                        break;
                    case '\uFEFF': // remove / ignore BOM (Byte Order Mark)
                        break;
                    default:
                        Token.Append(aJSON[i]);
                        break;
                }
                ++i;
            }
            JSON.freeStringBuilder(Token);
            freeStack(stack);
            if (QuoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seems to be messed up:\n" + aJSON);
            }
            if (ctx == null)
            {
                throw new Exception("JSON Parse: Json format not valid:\n" + aJSON);
            }

            return ctx;
        }
        internal static bool isNullStr(string str)
        {
            if (str.Length == NULL.Length)
            {
                for (int i = 0, n = str.Length; i < n; i++)
                {
                    if (Char.ToLower(str[i]) != NULL[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public virtual void Serialize(BinaryWriter aWriter) { }

        public virtual void SerializeMsgPack(BinaryWriter aWriter) { }

        public virtual bool HasKey(string key)
        {
            return false;
        }

#if USE_lzLib
        public void SaveToCompressedFile(string aFileName)
        {
#if USE_FileIO
            Directory.CreateDirectory((new FileInfo(aFileName)).Directory.FullName);
            var buf = SaveToCompressedBuffer();
            // Create a new stream to write to the file
            using (BinaryWriter Writer = new BinaryWriter(File.OpenWrite(aFileName)))
            {
                // Writer raw data                
                Writer.Write(buf.ByteBuffer, buf.Start, buf.Length);
            }
            BufferPool.Free(buf);

#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }
        public string SaveToCompressedBase64()
        {
            var buf = SaveToCompressedBuffer();
            var res = Convert.ToBase64String(buf.ByteBuffer, buf.Start, buf.Length);
            BufferPool.Free(buf);
            return res;
        }

        /// <summary>
        /// Make sure to call free in Buffer
        /// </summary>
        /// <returns>Internal byte buffer, use to avoid GC only</returns>
        public Buffer SaveToCompressedBuffer()
        {
            var byteBuffer = BufferPool.Obtain();
            var byteBuffer2 = BufferPool.Obtain();
            Serialize(byteBuffer.Writer);
            byteBuffer.Writer.Flush();
            byteBuffer.Length = (int)byteBuffer.boundStream.Position;
            byteBuffer.Start = 0;
            var encodedLength = LZ4Codec.Encode(byteBuffer.ByteBuffer, 0, byteBuffer.Length,
                byteBuffer2.ByteBuffer, 0, byteBuffer2.ByteBuffer.Length, LZ4Level.L12_MAX);
            BufferPool.Free(byteBuffer);
            byteBuffer2.Length = encodedLength;
            return byteBuffer2;
        }

#else
        public void SaveToCompressedStream(Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public void SaveToCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
        public string SaveToCompressedBase64()
        {
            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
        }
#endif
        /// <summary>
        /// Make sure to call free in Buffer
        /// </summary>
        /// <returns>Internal byte buffer, use to avoid GC only</returns>
        public Buffer SaveToQuickLzBuffer()
        {
            var byteBuffer = BufferPool.Obtain();
            var byteBuffer2 = BufferPool.Obtain();
            Serialize(byteBuffer.Writer);
            byteBuffer.Writer.Flush();
            byteBuffer.Length = (int)byteBuffer.boundStream.Position;
            byteBuffer.Start = 0;
            QuickLZ.compress(byteBuffer.ByteBuffer, byteBuffer.Start, byteBuffer.Length, 1, byteBuffer2.ByteBuffer, byteBuffer2.Start, out var compressedLength);
            byteBuffer2.Length = compressedLength;
            BufferPool.Free(byteBuffer);
            return byteBuffer2;
        }

        public void SaveToFile(string aFileName)
        {
#if USE_FileIO
            Directory.CreateDirectory((new FileInfo(aFileName)).Directory.FullName);
            using (var F = File.OpenWrite(aFileName))
            using (var b = new BinaryWriter(F, Encoding.UTF8))
            {
                Serialize(b);
            }
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }

#if USE_lzLib
        /// <summary>
        /// Make sure to call free in Buffer
        /// </summary>
        /// <returns>Internal byte buffer, use to avoid GC only</returns>
        public Buffer SaveToCompressedMsgPackBuffer()
        {
            var byteBuffer = BufferPool.Obtain();
            var byteBuffer2 = BufferPool.Obtain();
            SerializeMsgPack(byteBuffer.Writer);
            byteBuffer.Writer.Flush();
            byteBuffer.Length = (int)byteBuffer.boundStream.Position;
            byteBuffer.Start = 0;
            var encodedLength = LZ4Codec.Encode(byteBuffer.ByteBuffer, 0, byteBuffer.Length,
                byteBuffer2.ByteBuffer, 0, byteBuffer2.ByteBuffer.Length);
            BufferPool.Free(byteBuffer);
            byteBuffer2.Length = encodedLength;
            return byteBuffer2;
        }
#endif
        /// <summary>
        /// Make sure to call free in Buffer
        /// </summary>
        /// <returns>Internal byte buffer, use to avoid GC only</returns>
        public Buffer SaveToMsgPackBuffer()
        {
            var byteBuffer = BufferPool.Obtain();
            SerializeMsgPack(byteBuffer.Writer);
            byteBuffer.Writer.Flush();
            byteBuffer.Length = (int)byteBuffer.boundStream.Position;
            byteBuffer.Start = 0;

            return byteBuffer;
        }
        public string SaveToBase64()
        {
            var buf = SaveToBuffer();
            var res = Convert.ToBase64String(buf.ByteBuffer, buf.Start, buf.Length);
            BufferPool.Free(buf);
            return res;
        }
        public Buffer SaveToBuffer()
        {
            var byteBuffer = BufferPool.Obtain();
            Serialize(byteBuffer.Writer);
            byteBuffer.Writer.Flush();
            byteBuffer.Length = (int)byteBuffer.boundStream.Position;
            byteBuffer.Start = 0;
            return byteBuffer;
        }
        public static JSONNode Deserialize(BinaryReader aReader)
        {
            JSONBinaryTag type = (JSONBinaryTag)aReader.ReadByte();
            switch (type)
            {
                case JSONBinaryTag.Array:
                    {
                        int count = aReader.ReadByte();
#if Recycle
                        JSONArray tmp = JsonRecycle.GetArray();
#else
                        JSONArray tmp = new JSONArray();
#endif

                        for (int i = 0; i < count; i++)
                            tmp.Add(Deserialize(aReader));
                        return tmp;
                    }
                case JSONBinaryTag.Object:
                    {
                        int count = aReader.ReadByte();
#if Recycle
                        JSONObject tmp = JsonRecycle.GetObject();
#else
                        JSONObject tmp = new JSONObject();
#endif

                        for (int i = 0; i < count; i++)
                        {
                            string key = aReader.ReadString();
                            var val = Deserialize(aReader);
                            tmp.Add(key, val);
                        }
                        return tmp;
                    }
#if Recycle
                case JSONBinaryTag.String:
                    {
                        return JsonRecycle.GetData(aReader.ReadString());
                    }
                case JSONBinaryTag.ByteValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadByte());
                    }
                case JSONBinaryTag.SByteValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadSByte());
                    }
                case JSONBinaryTag.ShortValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadInt16());
                    }
                case JSONBinaryTag.UShortValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadUInt16());
                    }
                case JSONBinaryTag.IntValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadInt32());
                    }
                case JSONBinaryTag.UIntValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadUInt32());
                    }
                case JSONBinaryTag.BoolValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadBoolean());
                    }
                case JSONBinaryTag.FloatValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadSingle());
                    }
                case JSONBinaryTag.LongValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadInt64());
                    }
                case JSONBinaryTag.DoubleValue:
                    {
                        return JsonRecycle.GetData(aReader.ReadDouble());
                    }
#else
                case JSONBinaryTag.String:
                    {
                        return new JSONData(aReader.ReadString());
                    }
                case JSONBinaryTag.ByteValue:
                    {
                        return new JSONData(aReader.ReadByte());
                    }
                case JSONBinaryTag.SByteValue:
                    {
                        return new JSONData(aReader.ReadSByte());
                    }
                case JSONBinaryTag.ShortValue:
                    {
                        return new JSONData(aReader.ReadInt16());
                    }
                case JSONBinaryTag.UShortValue:
                    {
                        return new JSONData(aReader.ReadUInt16());
                    }
                case JSONBinaryTag.IntValue:
                    {
                        return new JSONData(aReader.ReadInt32());
                    }
                case JSONBinaryTag.UIntValue:
                    {
                        return new JSONData(aReader.ReadUInt32());
                    }
                case JSONBinaryTag.BoolValue:
                    {
                        return new JSONData(aReader.ReadBoolean());
                    }
                case JSONBinaryTag.FloatValue:
                    {
                        return new JSONData(aReader.ReadSingle());
                    }
                case JSONBinaryTag.LongValue:
                    {
                        return new JSONData(aReader.ReadInt64());
                    }
                case JSONBinaryTag.DoubleValue:
                    {
                        return new JSONData(aReader.ReadDouble());
                    }
#endif
                case JSONBinaryTag.NullValue:
                    {
                        return JSONData.NullValue;
                    }
                case JSONBinaryTag.Binary:
                    {
                        int count = aReader.ReadByte();
                        var bs = aReader.ReadBytes(count);
                        return new JSONBinary(bs);
                    }
                case JSONBinaryTag.BigArray:
                    {
                        int count = aReader.ReadInt32();
#if Recycle
                        JSONArray tmp = JsonRecycle.GetArray();
#else
                        JSONArray tmp = new JSONArray();
#endif

                        for (int i = 0; i < count; i++)
                            tmp.Add(Deserialize(aReader));
                        return tmp;
                    }
                case JSONBinaryTag.BigObject:
                    {
                        int count = aReader.ReadInt32();
#if Recycle
                        JSONObject tmp = JsonRecycle.GetObject();
#else
                        JSONObject tmp = new JSONObject();
#endif

                        for (int i = 0; i < count; i++)
                        {
                            string key = aReader.ReadString();
                            var val = Deserialize(aReader);
                            tmp.Add(key, val);
                        }
                        return tmp;
                    }
                case JSONBinaryTag.BigBinary:
                    {
                        int count = aReader.ReadInt32();
                        var bs = aReader.ReadBytes(count);
                        return new JSONBinary(bs);
                    }
                default:
                    {
                        throw new Exception("Error deserializing JSON. Unknown tag: " + type);
                    }
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            throw new Exception("Cannot enumerate on this object.");
        }
    } // End of JSONNode

    public class JSONArray : JSONNode
    {
        public static new JSONArray Parse(string aJson)
        {
            return JSON.Parse(aJson).AsArray;
        }

        private List<JSONNode> list;
#if Recycle
        internal JSONArray()
        {
            list = new List<JSONNode>();
        }
#else
        public JSONArray()
        {
            list = new List<JSONNode>();
        }
#endif
        public JSONArray(int capacity)
        {
            list = new List<JSONNode>(capacity);
        }

        public override JSONBinaryTag GetJsonType()
        {
            return JSONBinaryTag.Array;
        }

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= list.Count)
                    return new JSONLazyCreator(this);
                return list[aIndex];
            }
            set
            {
                var aItem = value;
                if (aItem == null) aItem = JSONData.NullValue;
                if (aIndex < 0 || aIndex >= list.Count)
                    list.Add(aItem);
                else
                    list[aIndex] = aItem;
            }
        }
        public override JSONNode this[string aKey]
        {
            get { return this[int.Parse(aKey)]; }
            set
            {
                this[int.Parse(aKey)] = value;
            }
        }
        public override Dictionary<string, JSONNode>.KeyCollection Keys
        {
            get { return null; }
        }
        public override int Count
        {
            get { return list.Count; }
        }
        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null) aItem = JSONData.NullValue;
            list.Add(aItem);
        }
        public void InsertAt(int aIndex, JSONNode aItem)
        {
            if (aItem == null) aItem = JSONData.NullValue;
            if (aIndex < 0 || aIndex >= list.Count)
                list.Add(aItem);
            else
                list.Insert(aIndex, aItem);
        }
        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= list.Count)
                return null;
            JSONNode tmp = list[aIndex];
            list.RemoveAt(aIndex);
            return tmp;
        }
        public void RemoveRange(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex >= list.Count)
                return;
            list.RemoveRange(startIndex, length);
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            list.Remove(aNode);
            return aNode;
        }
        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach (JSONNode N in list)
                    yield return N;
            }
        }
        public override IEnumerator GetEnumerator()
        {
            foreach (JSONNode N in list)
                yield return N;
        }
        public override string ToString()
        {
            StringBuilder result = JSON.obtainStringBuilder();
            result.Append('[');
            for (int i = 0, n = list.Count; i < n; i++)
            {
                JSONNode N = list[i];
                if (i > 0)
                    result.Append(',');
                result.Append(N.ToString());
            }
            result.Append(']');
            var str = result.ToString();
            JSON.freeStringBuilder(result);
            return str;
        }
        public override void Serialize(BinaryWriter aWriter)
        {
            if (list.Count <= byte.MaxValue)
            {
                aWriter.Write((byte)JSONBinaryTag.Array);
                aWriter.Write((byte)list.Count);
            }
            else
            {
                aWriter.Write((byte)JSONBinaryTag.BigArray);
                aWriter.Write(list.Count);
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Serialize(aWriter);
            }
        }

        public override void SerializeMsgPack(BinaryWriter aWriter)
        {
            MsgPackEncoder.writeArray(aWriter, this);
        }

        public void Clear()
        {
            list.Clear();
        }

        public void Sort(Comparison<JSONNode> comparer)
        {
            list.Sort(comparer);
        }
    } // End of JSONArray

    public class JSONObject : JSONNode
    {
        public static new JSONObject Parse(string aJson)
        {
            return JSON.Parse(aJson).AsObject;
        }

        private Dictionary<string, JSONNode> dict = new Dictionary<string, JSONNode>();

#if Recycle
        internal JSONObject() { }
#else
        public JSONObject() { }
#endif

        public override JSONBinaryTag GetJsonType()
        {
            return JSONBinaryTag.Object;
        }

        public override JSONNode this[string aKey]
        {
            get
            {
                if (dict.ContainsKey(aKey))
                    return dict[aKey];
                else
                    return new JSONLazyCreator(this, aKey);
            }
            set
            {
                if (value == null)
                {
                    dict[aKey] = JSONData.NullValue;
                }
                else
                {
                    dict[aKey] = value;
                }
            }
        }
        public override JSONNode this[int aIndex]
        {
            get
            {
                return this[aIndex.ToString()];
            }
            set
            {
                this[aIndex.ToString()] = value;
            }
        }
        public override int Count
        {
            get { return dict.Count; }
        }

        public override bool HasKey(string key)
        {
            return dict.ContainsKey(key);
        }

        public void Clear()
        {
            dict.Clear();
        }

        public override Dictionary<string, JSONNode>.KeyCollection Keys
        {
            get { return dict.Keys; }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null) aItem = JSONData.NullValue;
            if (aKey != null)
            {
                dict[aKey] = aItem;
            }
            else
                dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override JSONNode Remove(string aKey)
        {
            if (!dict.ContainsKey(aKey))
                return null;
            JSONNode tmp = dict[aKey];
            dict.Remove(aKey);
            return tmp;
        }
        public override JSONNode Remove(int aIndex)
        {
            //if (aIndex < 0 || aIndex >= m_Dict.Count)
            //    return null;
            //var item = m_Dict.ElementAt(aIndex);
            //m_Dict.Remove(item.Key);
            //return item.Value;
            throw new Exception("Json object not support this operation: remove by index");
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            foreach (var key in dict.Keys)
            {
                var item = dict[key];
                if (item == aNode)
                {
                    dict.Remove(key);
                    return aNode;
                }
            }
            return null;
        }

        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach (KeyValuePair<string, JSONNode> N in dict)
                    yield return N.Value;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, JSONNode> N in dict)
                yield return N;
        }
        public override string ToString()
        {
            StringBuilder result = JSON.obtainStringBuilder();
            StringBuilder temp = JSON.obtainStringBuilder();
            result.Append('{');
            int count = 0;
            foreach (KeyValuePair<string, JSONNode> N in dict)
            {
                if (count > 0)
                    result.Append(',');
                result.Append('\"');
                Escape(N.Key, false, ref temp);
                result.Append(temp);
                result.Append('\"');
                result.Append(':');
                result.Append(N.Value == null ? "null" : N.Value.ToString());
                count++;
            }
            result.Append('}');
            var str = result.ToString();
            JSON.freeStringBuilder(result);
            JSON.freeStringBuilder(temp);
            return str;
        }
        public override void Serialize(BinaryWriter aWriter)
        {
            if (dict.Count <= byte.MaxValue)
            {
                aWriter.Write((byte)JSONBinaryTag.Object);
                aWriter.Write((byte)dict.Count);
            }
            else
            {
                aWriter.Write((byte)JSONBinaryTag.BigObject);
                aWriter.Write(dict.Count);
            }

            foreach (string K in dict.Keys)
            {
                aWriter.Write(K);
                dict[K].Serialize(aWriter);
            }
        }

        public override void SerializeMsgPack(BinaryWriter aWriter)
        {
            MsgPackEncoder.writeMapLength(aWriter, dict.Count);

            foreach (string K in dict.Keys)
            {
                MsgPackEncoder.writeString(aWriter, K);
                dict[K].SerializeMsgPack(aWriter);
            }
        }
    } // End of JSONClass

    public class JSONData : JSONNode
    {
        public static readonly JSONData NullValue;
        static JSONData()
        {
            NullValue = new JSONData(NULL);
            NullValue.JsonDataType = JSONBinaryTag.NullValue;
        }

        private object data;
        private string value; // cache

        public JSONBinaryTag JsonDataType
        {
            get; private set;
        }

        public override JSONBinaryTag GetJsonType()
        {
            return JsonDataType;
        }

        public override string Value
        {
            get
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
                else if (JsonDataType == JSONBinaryTag.String)
                {
                    value = (string)data;
                    return value;
                }
                else if (JsonDataType == JSONBinaryTag.FloatValue)
                {
                    value = ((float)data).ToString(CultureInfo.InvariantCulture);
                    return value;
                }
                else if (JsonDataType == JSONBinaryTag.DoubleValue)
                {
                    value = ((double)data).ToString(CultureInfo.InvariantCulture);
                    return value;
                }
                else if (JsonDataType == JSONBinaryTag.BoolValue)
                {
                    value = ((bool)data) ? TRUE : FALSE;
                    return value;
                }
                else
                {
                    value = data.ToString();
                    return value;
                }
            }
            set
            {
                this.value = value;
                data = value;
                JsonDataType = JSONBinaryTag.String;
            }
        }

#if Recycle
        internal JSONData(object aData)
        {
            SetValue(aData);
        }
        internal JSONData(string aData)
        {
            SetValue(aData);
        }
        internal JSONData(float aData)
        {
            SetValue(aData);
        }
        internal JSONData(double aData)
        {
            SetValue(aData);
        }
        internal JSONData(bool aData)
        {
            SetValue(aData);
        }
        internal JSONData(byte aData)
        {
            SetValue(aData);
        }
        internal JSONData(short aData)
        {
            SetValue(aData);
        }
        internal JSONData(int aData)
        {
            SetValue(aData);
        }
        internal JSONData(long aData)
        {
            SetValue(aData);
        }
        internal JSONData(sbyte aData)
        {
            SetValue(aData);
        }
        internal JSONData(ushort aData)
        {
            SetValue(aData);
        }
        internal JSONData(uint aData)
        {
            SetValue(aData);
        }
#else
        public JSONData(object aData)
        {
            SetValue(aData);
        }
        public JSONData(string aData)
        {
            SetValue(aData);
        }
        public JSONData(float aData)
        {
            SetValue(aData);
        }
        public JSONData(double aData)
        {
            SetValue(aData);
        }
        public JSONData(bool aData)
        {
            SetValue(aData);
        }
        public JSONData(byte aData)
        {
            SetValue(aData);
        }
        public JSONData(short aData)
        {
            SetValue(aData);
        }
        public JSONData(int aData)
        {
            SetValue(aData);
        }
        public JSONData(long aData)
        {
            SetValue(aData);
        }
        public JSONData(sbyte aData)
        {
            SetValue(aData);
        }
        public JSONData(ushort aData)
        {
            SetValue(aData);
        }
        public JSONData(uint aData)
        {
            SetValue(aData);
        }
#endif

        public void SetValue(object aData)
        {
            if (aData == null)
            {
                Value = NULL;
                JsonDataType = JSONBinaryTag.NullValue;
            }
            else
            {
                Value = aData.ToString();
            }
        }
        public void SetValue(string aData)
        {
            JsonDataType = guestBestTypeFromString(aData, out data);
            //value = aData;
            //Logger.Info("best type for " + aData + " is " + JsonDataType);
        }
        public void SetValue(float aData)
        {
            data = aData;
            JsonDataType = JSONBinaryTag.FloatValue;
            value = null;
        }
        public void SetValue(double aData)
        {
            data = aData;
            JsonDataType = JSONBinaryTag.DoubleValue;
            value = null;
        }
        public void SetValue(bool aData)
        {
            data = aData;
            JsonDataType = JSONBinaryTag.BoolValue;
            value = aData ? TRUE : FALSE;
        }
        public void SetValue(byte aData)
        {
            data = aData;
            JsonDataType = JSONBinaryTag.ByteValue;
            value = null;
        }
        public void SetValue(short aData)
        {
            if (aData >= byte.MinValue && aData <= byte.MaxValue)
            {
                data = (byte)aData;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
            if (aData >= sbyte.MinValue && aData <= sbyte.MaxValue)
            {
                data = (sbyte)aData;
                JsonDataType = JSONBinaryTag.SByteValue;
            }
            else
            {
                data = aData;
                JsonDataType = JSONBinaryTag.ShortValue;
            }
            value = null;
        }
        public void SetValue(int aData)
        {
            if (aData >= byte.MinValue && aData <= byte.MaxValue)
            {
                data = (byte)aData;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
            if (aData >= sbyte.MinValue && aData <= sbyte.MaxValue)
            {
                data = (sbyte)aData;
                JsonDataType = JSONBinaryTag.SByteValue;
            }
            if (aData >= short.MinValue && aData <= short.MaxValue)
            {
                data = (short)aData;
                JsonDataType = JSONBinaryTag.ShortValue;
            }
            if (aData >= ushort.MinValue && aData <= ushort.MaxValue)
            {
                data = (ushort)aData;
                JsonDataType = JSONBinaryTag.UShortValue;
            }
            else
            {
                data = aData;
                JsonDataType = JSONBinaryTag.IntValue;
            }
            value = null;
        }
        public void SetValue(long aData)
        {
            if (aData >= byte.MinValue && aData <= byte.MaxValue)
            {
                data = (byte)aData;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
            if (aData >= sbyte.MinValue && aData <= sbyte.MaxValue)
            {
                data = (sbyte)aData;
                JsonDataType = JSONBinaryTag.SByteValue;
            }
            if (aData >= short.MinValue && aData <= short.MaxValue)
            {
                data = (short)aData;
                JsonDataType = JSONBinaryTag.ShortValue;
            }
            if (aData >= ushort.MinValue && aData <= ushort.MaxValue)
            {
                data = (ushort)aData;
                JsonDataType = JSONBinaryTag.UShortValue;
            }
            if (aData >= int.MinValue && aData <= int.MaxValue)
            {
                data = (int)aData;
                JsonDataType = JSONBinaryTag.IntValue;
            }
            if (aData >= uint.MinValue && aData <= uint.MaxValue)
            {
                data = (uint)aData;
                JsonDataType = JSONBinaryTag.UIntValue;
            }
            else
            {
                data = aData;
                JsonDataType = JSONBinaryTag.LongValue;
            }
            value = null;
        }
        public void SetValue(sbyte aData)
        {
            data = aData;
            JsonDataType = JSONBinaryTag.SByteValue;
            value = null;
        }
        public void SetValue(ushort aData)
        {
            if (aData >= byte.MinValue && aData <= byte.MaxValue)
            {
                data = (byte)aData;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
            else
            {
                data = aData;
                JsonDataType = JSONBinaryTag.UShortValue;
            }
            value = null;
        }
        public void SetValue(uint aData)
        {
            if (aData >= byte.MinValue && aData <= byte.MaxValue)
            {
                data = (byte)aData;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
            if (aData >= ushort.MinValue && aData <= ushort.MaxValue)
            {
                data = (ushort)aData;
                JsonDataType = JSONBinaryTag.UShortValue;
            }
            else
            {
                data = aData;
                JsonDataType = JSONBinaryTag.UIntValue;
                value = null;
            }
        }

        public override byte AsByte
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.ByteValue)
                {
                    return (byte)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = byte.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.ByteValue;
                    return b;
                }
                return Convert.ToByte(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.ByteValue;
            }
        }
        public override short AsShort
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.ShortValue)
                {
                    return (short)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = short.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.ShortValue;
                    return b;
                }
                return Convert.ToInt16(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.ShortValue;
            }
        }
        public override int AsInt
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.IntValue)
                {
                    return (int)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = int.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.IntValue;
                    return b;
                }
                return Convert.ToInt32(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.IntValue;
            }
        }
        public override long AsLong
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.LongValue)
                {
                    return (long)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = long.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.LongValue;
                    return b;
                }
                return Convert.ToInt64(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.LongValue;
            }
        }
        public override sbyte AsSByte
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.SByteValue)
                {
                    return (sbyte)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = sbyte.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.SByteValue;
                    return b;
                }
                return Convert.ToSByte(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.SByteValue;
            }
        }
        public override ushort AsUShort
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.UShortValue)
                {
                    return (ushort)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = ushort.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.UShortValue;
                    return b;
                }
                return Convert.ToUInt16(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.UShortValue;
            }
        }
        public override uint AsUInt
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.UIntValue)
                {
                    return (uint)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = uint.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.UIntValue;
                    return b;
                }
                return Convert.ToUInt32(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.UIntValue;
            }
        }
        public override float AsFloat
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.FloatValue)
                {
                    return (float)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = float.Parse(Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                    data = b;
                    JsonDataType = JSONBinaryTag.FloatValue;
                    return b;
                }
                return Convert.ToSingle(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.FloatValue;
            }
        }
        public override double AsDouble
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.DoubleValue)
                {
                    return (double)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = double.Parse(Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                    data = b;
                    JsonDataType = JSONBinaryTag.DoubleValue;
                    return b;
                }
                return Convert.ToDouble(data);
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.DoubleValue;
            }
        }
        public override bool AsBool
        {
            get
            {
                if (JsonDataType == JSONBinaryTag.BoolValue)
                {
                    return (bool)data;
                }
                if (JsonDataType == JSONBinaryTag.String)
                {
                    var b = bool.Parse(Value);
                    data = b;
                    JsonDataType = JSONBinaryTag.BoolValue;
                    return b;
                }
                return (bool)data;
            }
            set
            {
                if (JsonDataType == JSONBinaryTag.NullValue)
                    throw new Exception("Cannot set value to a json null");
                this.value = null;
                data = value;
                JsonDataType = JSONBinaryTag.BoolValue;
            }
        }

        public bool IsInteger()
        {
            return JsonDataType == JSONBinaryTag.SByteValue || JsonDataType == JSONBinaryTag.ByteValue ||
                JsonDataType == JSONBinaryTag.ShortValue || JsonDataType == JSONBinaryTag.UShortValue ||
                JsonDataType == JSONBinaryTag.IntValue || JsonDataType == JSONBinaryTag.UIntValue ||
                JsonDataType == JSONBinaryTag.LongValue;
        }

        public bool IsFloat()
        {
            return JsonDataType == JSONBinaryTag.FloatValue || JsonDataType == JSONBinaryTag.DoubleValue;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (base.Equals(obj))
                return true;
            JSONData s2 = obj as JSONData;
            if (s2 != null)
            {
                if (JsonDataType == JSONBinaryTag.BoolValue && s2.JsonDataType == JSONBinaryTag.BoolValue)
                {
                    return AsBool == s2.AsBool;
                }
                if (IsInteger() && s2.IsInteger())
                {
                    return AsLong == s2.AsLong;
                }
                if (IsFloat() && s2.IsFloat())
                {
                    return AsDouble == s2.AsDouble;
                }
                return Value.Equals(s2.Value);
            }
            try
            {
                if (JsonDataType == JSONBinaryTag.BoolValue)
                {
                    return AsBool == Convert.ToBoolean(obj);
                }
                if (IsInteger())
                {
                    return AsLong == Convert.ToInt64(obj);
                }
                if (IsFloat())
                {
                    return AsDouble == Convert.ToDouble(obj);
                }
            }
            catch (Exception)
            {
                return false;
            }
            var str = obj as string;
            if (str != null)
            {
                return str.Equals(Value);
            }
            return false;
        }

        public override string ToString()
        {
            var data = JSON.obtainStringBuilder();
            data.Length = 0;
            if (JsonDataType == JSONBinaryTag.String)
            {
                data.Append('\"');
            }
            Escape(Value, true, ref data);
            if (JsonDataType == JSONBinaryTag.String)
            {
                data.Append('\"');
            }
            var str = data.ToString();
            JSON.freeStringBuilder(data);
            return str;
        }

        public const long MAX_SAFE_INTEGER = 9007199254740991; // max in js
        public const long MIN_SAFE_INTEGER = -9007199254740991; // min in js
        private static JSONBinaryTag guestBestTypeFromString(string m_Data, out object output)
        {
            if (m_Data == null)
            {
                output = NULL;
                return JSONBinaryTag.NullValue;
            }

            if (m_Data.Length == 0)
            {
                output = string.Empty;
                return JSONBinaryTag.String;
            }

            // test for special number string, eg phone number
            // eg: " 123", "+84", "-00.12"  "001"  "0.0000"
            if (m_Data.StartsWith(" ") || m_Data.StartsWith("\t") || m_Data.StartsWith("+") || m_Data.Equals("-0")
                || (m_Data.Length > 2 && m_Data[0] == '-' && m_Data[1] == '0' && m_Data[2] != '.')
                || (m_Data.Length > 1 && m_Data[0] == '0' && m_Data[1] != '.'))
            {
                output = m_Data;
                return JSONBinaryTag.String;
            }

            if (bool.TryParse(m_Data, out var _b))
            {
                //Logger.Log("type: " + JSONBinaryTag.BoolValue);
                output = _b;
                return JSONBinaryTag.BoolValue;
            }

            double vd = 0.0;
            if (double.TryParse(m_Data, NumberStyles.Float, CultureInfo.InvariantCulture, out vd)) // must be number
            {
                if(vd > long.MaxValue || vd < long.MinValue) // very big number
                {
                    output = m_Data;
                    return JSONBinaryTag.String;
                }

                long lv = 0L;
                if (long.TryParse(m_Data, out lv)) // must be integer
                {
                    unchecked
                    {
                        var sb = (sbyte)lv;
                        if (sb == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.ByteValue);
                            output = sb;
                            return JSONBinaryTag.SByteValue;
                        }
                        var b = (byte)lv;
                        if (b == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.ByteValue);
                            output = b;
                            return JSONBinaryTag.ByteValue;
                        }
                        var s = (short)lv;
                        if (s == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.ShortValue);
                            output = s;
                            return JSONBinaryTag.ShortValue;
                        }
                        var us = (ushort)lv;
                        if (us == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.ShortValue);
                            output = us;
                            return JSONBinaryTag.UShortValue;
                        }
                        var i = (int)lv;
                        if (i == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.IntValue);
                            output = i;
                            return JSONBinaryTag.IntValue;
                        }
                        var ui = (uint)lv;
                        if (ui == lv)
                        {
                            //Logger.Log("type: " + JSONBinaryTag.IntValue);
                            output = ui;
                            return JSONBinaryTag.UIntValue;
                        }

                        {
                            //Logger.Log("type: " + JSONBinaryTag.LongValue);
                            output = lv;
                            return JSONBinaryTag.LongValue;
                        }
                    }
                }

                //if (m_Data.Length >= 4)
                //{
                var temp = JSON.obtainStringBuilder();
                var fv = (float)vd;
                temp.Append(fv);
                var length = temp.Length;
                JSON.freeStringBuilder(temp);
                if (length == m_Data.Length)
                {
                    //Logger.Log("type: " + JSONBinaryTag.FloatValue);
                    output = fv;
                    return JSONBinaryTag.FloatValue;
                }

                //Logger.Log("type: " + JSONBinaryTag.DoubleValue);
                output = vd;
                return JSONBinaryTag.DoubleValue;
                //}
                //else // use string as it will shorter
                //{
                //    //Logger.Log("type: " + JSONBinaryTag.String);
                //    output = m_Data;
                //    return JSONBinaryTag.String;
                //}
            }

            //Logger.Log("type: " + JSONBinaryTag.String);
            output = m_Data;
            return JSONBinaryTag.String;
        }

        public override void Serialize(BinaryWriter aWriter)
        {
            //try
            //{
            aWriter.Write((byte)JsonDataType);
            switch (JsonDataType)
            {
                case JSONBinaryTag.String:
                    aWriter.Write(Value);
                    break;
                case JSONBinaryTag.IntValue:
                    aWriter.Write(AsInt);
                    break;
                case JSONBinaryTag.DoubleValue:
                    aWriter.Write(AsDouble);
                    break;
                case JSONBinaryTag.BoolValue:
                    aWriter.Write(AsBool);
                    break;
                case JSONBinaryTag.FloatValue:
                    aWriter.Write(AsFloat);
                    break;
                case JSONBinaryTag.LongValue:
                    aWriter.Write(AsLong);
                    break;
                case JSONBinaryTag.ByteValue:
                    aWriter.Write(AsByte);
                    break;
                case JSONBinaryTag.ShortValue:
                    aWriter.Write(AsShort);
                    break;
                case JSONBinaryTag.SByteValue:
                    aWriter.Write(AsSByte);
                    break;
                case JSONBinaryTag.UShortValue:
                    aWriter.Write(AsUShort);
                    break;
                case JSONBinaryTag.UIntValue:
                    aWriter.Write(AsUInt);
                    break;
                case JSONBinaryTag.NullValue:
                    //aWriter.Write(NULL);
                    break;
                default:
                    throw new Exception("Invalid json data type: " + JsonDataType);
            }
            //}
            //catch(Exception ex)
            //{
            //    Logger.Error("error " + ex.ToString());
            //}
        }

        public override void SerializeMsgPack(BinaryWriter aWriter)
        {
            switch (JsonDataType)
            {
                case JSONBinaryTag.String:
                    MsgPackEncoder.writeString(aWriter, Value);
                    break;
                case JSONBinaryTag.DoubleValue:
                    MsgPackEncoder.writeDouble(aWriter, AsDouble);
                    break;
                case JSONBinaryTag.BoolValue:
                    MsgPackEncoder.writeBool(aWriter, AsBool);
                    break;
                case JSONBinaryTag.FloatValue:
                    MsgPackEncoder.writeFloat(aWriter, AsFloat);
                    break;
                case JSONBinaryTag.LongValue:
                    var _v = AsLong;
                    if (_v <= MAX_SAFE_INTEGER && _v >= MIN_SAFE_INTEGER)
                        MsgPackEncoder.writeInt64(aWriter, AsLong);
                    else
                        MsgPackEncoder.writeString(aWriter, Value);
                    break;
                case JSONBinaryTag.IntValue:
                    MsgPackEncoder.writeInt(aWriter, AsInt);
                    break;
                case JSONBinaryTag.ByteValue:
                    MsgPackEncoder.writeInt(aWriter, AsByte);
                    break;
                case JSONBinaryTag.ShortValue:
                    MsgPackEncoder.writeInt(aWriter, AsShort);
                    break;
                case JSONBinaryTag.SByteValue:
                    MsgPackEncoder.writeInt(aWriter, AsSByte);
                    break;
                case JSONBinaryTag.UShortValue:
                    MsgPackEncoder.writeInt(aWriter, AsUShort);
                    break;
                case JSONBinaryTag.UIntValue:
                    MsgPackEncoder.writeInt(aWriter, AsUInt);
                    break;
                case JSONBinaryTag.NullValue:
                    MsgPackEncoder.writeNull(aWriter);
                    break;
                default:
                    throw new Exception("Invalid json data type: " + JsonDataType);
            }
        }
    } // End of JSONData

    /// <summary>
    /// Allow embed byte[] in json
    /// </summary>
    public class JSONBinary : JSONNode
    {
        public byte[] Data { get; internal set; }
        public int Start { get; internal set; }
        public int Length { get; internal set; }

        public JSONBinary(byte[] aData, int start = 0, int length = -1)
        {
            SetBytes(aData, start, length);
        }

        public override int Count
        {
            get { return Length; }
        }
        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach (var N in Data)
                    yield return N;
            }
        }
        public override IEnumerator GetEnumerator()
        {
            foreach (var N in Data)
                yield return N;
        }

        public override JSONNode this[int aIndex] { get => Data[aIndex]; set => Data[aIndex] = value; }
        public override JSONNode this[string aKey] { get => Data[int.Parse(aKey)]; set => Data[int.Parse(aKey)] = value; }

        public override JSONBinaryTag GetJsonType()
        {
            return JSONBinaryTag.Binary;
        }

        /// <summary>
        /// Only return data if type is binary
        /// </summary>
        /// <returns>a warp around byte[]</returns>
        public void SetBytes(byte[] aData, int start = 0, int length = -1)
        {
            if (length == -1) length = aData.Length;

            Data = aData;
            Start = start;
            Length = length;
        }
        public override string ToString()
        {
            StringBuilder result = JSON.obtainStringBuilder();
            result.Append('[');
            for (int i = Start, n = Length; i < n; i++)
            {
                var N = Data[i];
                if (i > 0)
                    result.Append(',');
                result.Append(N);
            }
            result.Append(']');
            var str = result.ToString();
            JSON.freeStringBuilder(result);
            return str;
        }
        public override void Serialize(BinaryWriter aWriter)
        {
            if (Length <= byte.MaxValue)
            {
                aWriter.Write((byte)JSONBinaryTag.Binary);
                aWriter.Write((byte)Length);
            }
            else
            {
                aWriter.Write((byte)JSONBinaryTag.BigBinary);
                aWriter.Write(Length);
            }

            aWriter.Write(Data, Start, Length);
        }

        public override void SerializeMsgPack(BinaryWriter aWriter)
        {
            MsgPackEncoder.writeBinary(aWriter, Data, Start, Length);
        }
    }

    internal class JSONLazyCreator : JSONNode
    {
        private JSONNode m_Node = null;
        private string m_Key = null;

        public JSONLazyCreator(JSONNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }
        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        public override JSONBinaryTag GetJsonType()
        {
            return JSONBinaryTag.Object;
        }

        private void Set(JSONNode aVal)
        {
            if (m_Key == null)
            {
                m_Node.Add(aVal);
            }
            else
            {
                m_Node.Add(m_Key, aVal);
            }
            m_Node = null; // Be GC friendly.
        }

        public override JSONNode this[int aIndex]
        {
            get
            {
                return new JSONLazyCreator(this);
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetArray();
#else
                var tmp = new JSONArray();
#endif
                tmp.Add(value);
                Set(tmp);
            }
        }

        public override JSONNode this[string aKey]
        {
            get
            {
                return new JSONLazyCreator(this, aKey);
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetObject();
#else
                JSONObject tmp = new JSONObject();
#endif
                tmp.Add(aKey, value);
                Set(tmp);
            }
        }
        public override void Add(JSONNode aItem)
        {
#if Recycle
            var tmp = JsonRecycle.GetArray();
#else
            var tmp = new JSONArray();
#endif
            tmp.Add(aItem);
            Set(tmp);
        }
        public override void Add(string aKey, JSONNode aItem)
        {
#if Recycle
            var tmp = JsonRecycle.GetObject();
#else
            JSONObject tmp = new JSONObject();
#endif
            tmp.Add(aKey, aItem);
            Set(tmp);
        }
        public static bool operator ==(JSONLazyCreator a, object b)
        {
            if (b == null)
                return true;
            return System.Object.ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONLazyCreator a, object b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return true;
            return System.Object.ReferenceEquals(this, obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "";
        }

        public override byte AsByte
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0);
#else
                JSONData tmp = new JSONData(0);
#endif

                Set(tmp);
                return 0;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override short AsShort
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0);
#else
                JSONData tmp = new JSONData(0);
#endif
                Set(tmp);
                return 0;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override int AsInt
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0);
#else
                JSONData tmp = new JSONData(0);
#endif
                Set(tmp);
                return 0;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override long AsLong
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0);
#else
                JSONData tmp = new JSONData(0);
#endif
                Set(tmp);
                return 0;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override float AsFloat
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0f);
#else
                JSONData tmp = new JSONData(0f);
#endif
                Set(tmp);
                return 0.0f;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override double AsDouble
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(0.0);
#else
                JSONData tmp = new JSONData(0.0);
#endif
                Set(tmp);
                return 0.0;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override bool AsBool
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetData(false);
#else
                JSONData tmp = new JSONData(false);
#endif
                Set(tmp);
                return false;
            }
            set
            {
#if Recycle
                var tmp = JsonRecycle.GetData(value);
#else
                JSONData tmp = new JSONData(value);
#endif
                Set(tmp);
            }
        }
        public override JSONArray AsArray
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetArray();
#else
                var tmp = new JSONArray();
#endif
                Set(tmp);
                return tmp;
            }
        }
        public override JSONObject AsObject
        {
            get
            {
#if Recycle
                var tmp = JsonRecycle.GetObject();
#else
                JSONObject tmp = new JSONObject();
#endif

                Set(tmp);
                return tmp;
            }
        }
    } // End of JSONLazyCreator

    public static class JSON
    {
        private const int StringBuilderDefaultLength = 128;
        private static List<StringBuilder> strBuilderPool = new List<StringBuilder>();
        internal static StringBuilder obtainStringBuilder()
        {
            StringBuilder item = null;
            lock (strBuilderPool)
            {
                var last = strBuilderPool.Count - 1;
                if (last >= 0)
                {
                    item = strBuilderPool[last];
                    strBuilderPool.RemoveAt(last);
                }
            }
            if (item != null)
            {
                item.Length = 0;
                return item;
            }
            else
            {
                return new StringBuilder(StringBuilderDefaultLength);
            }
        }

        internal static void freeStringBuilder(StringBuilder builder)
        {
            lock (strBuilderPool)
            {
                strBuilderPool.Add(builder);
            }
        }

        public static JSONNode Parse(string aJSON)
        {
            return JSONNode.Parse(aJSON);
        }

        public static JSONArray ListToJson(IEnumerable array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item.ToString());
            }
            return res;
        }

        public static JSONArray ListFloatToJson(IEnumerable<float> array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item);
            }
            return res;
        }

        public static JSONArray ListIntToJson(IEnumerable<int> array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item);
            }
            return res;
        }

        public static JSONArray ListByteToJson(IEnumerable<byte> array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item);
            }
            return res;
        }
        public static JSONArray ListByteToJson(IEnumerable<byte> array, int length)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item);
                if (res.Count == length) break;
            }
            return res;
        }

        public static JSONArray ListObjToJson(IEnumerable<IJsonSerializable> array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var item in array)
            {
                res.Add(item.ToJson());
            }
            return res;
        }

        public static JSONArray List2ToJson(IEnumerable<IEnumerable> array)
        {
#if Recycle
            var res = JsonRecycle.GetArray();
#else
            var res = new JSONArray();
#endif
            foreach (var subA in array)
            {
#if Recycle
                var sa = JsonRecycle.GetArray();
#else
                var sa = new JSONArray();
#endif
                res.Add(sa);
                foreach (var item in subA)
                {
                    sa.Add(item.ToString());
                }
            }
            return res;
        }

        public static T[] JsonArrayToArray<T>(JSONArray data) where T : IJsonSerializable, new()
        {
            var res = new T[data.Count];
            for (int i = 0, n = data.Count; i < n; i++)
            {
                var t = new T();
                t.ParseJson(data[i]);
                res[i] = t;
            }
            return res;
        }

        public static byte[] JsonArrToByteArr(JSONArray arr)
        {
            var res = new byte[arr.Count];
            for (int i = 0, n = arr.Count; i < n; i++)
            {
                res[i] = arr[i];
            }
            return res;
        }
        public static int[] JsonArrToIntArr(JSONArray arr)
        {
            var res = new int[arr.Count];
            for (int i = 0, n = arr.Count; i < n; i++)
            {
                res[i] = arr[i];
            }
            return res;
        }
        public static float[] JsonArrToFloatArr(JSONArray arr)
        {
            var res = new float[arr.Count];
            for (int i = 0, n = arr.Count; i < n; i++)
            {
                res[i] = arr[i];
            }
            return res;
        }

#if USE_lzLib
        public static JSONNode LoadFromCompressedStream(Stream aData)
        {
            var byteBuffer = BufferPool.Obtain();
            int read;
            int start = 0;
            while ((read = aData.Read(byteBuffer.ByteBuffer, start, byteBuffer.ByteBuffer.Length)) > 0)
            {
                start = read;
            }
            var res = LoadFromCompressedBytes(byteBuffer.ByteBuffer, 0, start);
            BufferPool.Free(byteBuffer);
            return res;
        }
        public static JSONNode LoadFromCompressedFile(string aFileName)
        {
#if USE_FileIO
            return LoadFromCompressedBytes(File.ReadAllBytes(aFileName));
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }
        public static JSONNode LoadFromCompressedBase64(string aBase64)
        {
            var tmp = Convert.FromBase64String(aBase64);
            return LoadFromCompressedBytes(tmp);
        }
        public static JSONNode LoadFromCompressedBytes(byte[] source, int offset, int length)
        {
            var byteBuffer = BufferPool.Obtain();
            var decoded = LZ4Codec.Decode(
                source, offset, length,
                byteBuffer.ByteBuffer, 0, byteBuffer.ByteBuffer.Length);

            byteBuffer.Length = decoded;
            byteBuffer.Start = 0;
            JSONNode res = JSONNode.Deserialize(byteBuffer.Reader);
            BufferPool.Free(byteBuffer);
            return res;
        }
        public static JSONNode LoadFromCompressedBytesNewBuffer(byte[] source, int offset, int length, int bufferSize)
        {
            var byteBuffer = new Buffer(bufferSize);
            var decoded = LZ4Codec.Decode(
                source, offset, length,
                byteBuffer.ByteBuffer, 0, byteBuffer.ByteBuffer.Length);

            byteBuffer.Length = decoded;
            byteBuffer.Start = 0;
            JSONNode res = JSONNode.Deserialize(byteBuffer.Reader);
            return res;
        }
        public static JSONNode LoadFromCompressedBytes(byte[] a)
        {
            return LoadFromCompressedBytes(a, 0, a.Length);
        }

        public static JSONNode LoadFromCompressedBuffer(Buffer byteBuffer)
        {
            var byteBuffer2 = BufferPool.Obtain();
            var decoded = LZ4Codec.Decode(
                byteBuffer.ByteBuffer, byteBuffer.Start, byteBuffer.Length,
                byteBuffer2.ByteBuffer, 0, byteBuffer2.ByteBuffer.Length);

            byteBuffer2.Length = decoded;
            byteBuffer2.Start = 0;
            JSONNode res = JSONNode.Deserialize(byteBuffer2.Reader);
            BufferPool.Free(byteBuffer2);
            return res;
        }

#else
        public static JSONNode LoadFromCompressedFile(string aFileName)
        {
            throw new Exception("Can't use compressed functions. You need include the USE_FileIO and uncomment the define at the top of SimpleJSON");
        }
        public static JSONNode LoadFromCompressedStream(Stream aData)
        {
            throw new Exception("Can't use compressed functions. You need include the USE_FileIO and uncomment the define at the top of SimpleJSON");
        }
        public static JSONNode LoadFromCompressedBase64(string aBase64)
        {
            throw new Exception("Can't use compressed functions. You need include the USE_FileIO and uncomment the define at the top of SimpleJSON");
        }
#endif

        public static JSONNode LoadFromQuickLzBuffer(Buffer byteBuffer)
        {
            var byteBuffer2 = BufferPool.Obtain();
            QuickLZ.decompress(byteBuffer.ByteBuffer, byteBuffer.Start, byteBuffer2.ByteBuffer, byteBuffer2.Start, out var decompressLength);
            byteBuffer2.Length = decompressLength;
            byteBuffer2.Start = 0;
            JSONNode res = JSONNode.Deserialize(byteBuffer2.Reader);
            BufferPool.Free(byteBuffer2);
            return res;
        }

        public static JSONNode LoadFromQuickLzBytes(byte[] source, int offset, int length)
        {
            Buffer byteBuffer = BufferPool.Obtain();
            byteBuffer.CopyByteArray(source, offset, length);
            var byteBuffer2 = BufferPool.Obtain();
            QuickLZ.decompress(byteBuffer.ByteBuffer, byteBuffer.Start, byteBuffer2.ByteBuffer, byteBuffer2.Start, out var decompressLength);
            byteBuffer2.Length = decompressLength;
            byteBuffer2.Start = 0;
            JSONNode res = JSONNode.Deserialize(byteBuffer2.Reader);
            BufferPool.Free(byteBuffer2);
            BufferPool.Free(byteBuffer);
            return res;
        }

        public static JSONNode LoadFromFile(string aFileName)
        {
#if USE_FileIO
            using (var F = File.OpenRead(aFileName))
            using (var b = new BinaryReader(F, Encoding.UTF8))
            {
                return JSONNode.Deserialize(b);
            }
#else
            throw new Exception("Can't use File IO stuff in webplayer");
#endif
        }
        public static JSONNode LoadFromBase64(string aBase64)
        {
            var tmp = System.Convert.FromBase64String(aBase64);
            return LoadFromBytes(tmp);
        }
        public static JSONNode LoadFromBytes(byte[] tmp, int offset = 0, int length = -1)
        {
            if (length == -1) length = tmp.Length;

            var byteBuffer = BufferPool.Obtain();
            byteBuffer.Writer.Write(tmp, offset, length);
            byteBuffer.Start = 0;
            byteBuffer.Length = length;
            var res = JSONNode.Deserialize(byteBuffer.Reader);
            BufferPool.Free(byteBuffer);
            return res;
        }

        public static JSONNode LoadFromBuffer(Buffer byteBuffer)
        {
            var res = JSONNode.Deserialize(byteBuffer.Reader);
            return res;
        }

        public static JSONNode LoadFromMsgPackBytes(byte[] a, int offset = 0, int length = -1)
        {
            if (length == -1) length = a.Length;

            var byteBuffer = BufferPool.Obtain();
            byteBuffer.Writer.Write(a, offset, length);
            byteBuffer.Start = 0;
            byteBuffer.Length = length;
            JSONNode res = MsgPackDecoder.Decode(byteBuffer.Reader);
            BufferPool.Free(byteBuffer);
            return res;
        }

#if USE_lzLib
        public static JSONNode LoadFromCompressedMsgPackBytes(byte[] source, int offset = 0, int length = -1)
        {
            if (length == -1) length = source.Length;

            var byteBuffer = BufferPool.Obtain();
            var decoded = LZ4Codec.Decode(
                source, offset, length,
                byteBuffer.ByteBuffer, 0, byteBuffer.ByteBuffer.Length);

            byteBuffer.Length = decoded;
            byteBuffer.Start = 0;
            JSONNode res = MsgPackDecoder.Decode(byteBuffer.Reader);
            BufferPool.Free(byteBuffer);
            return res;
        }
#endif

        public static string Escape(string input)
        {
            var data = JSON.obtainStringBuilder();
            JSONNode.Escape(input, false, ref data);
            var str = data.ToString();
            JSON.freeStringBuilder(data);
            return str;
        }
    }
}