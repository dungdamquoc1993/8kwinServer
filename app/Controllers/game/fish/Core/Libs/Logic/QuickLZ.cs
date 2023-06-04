// QuickLZ data compression library
// Copyright (C) 2006-2011 Lasse Mikkel Reinhold
// lar@quicklz.com
//
// QuickLZ can be used for free under the GPL 1, 2 or 3 license (where anything 
// released into public must be open source) or under a commercial license if such 
// has been acquired (see http://www.quicklz.com/order.html). The commercial license 
// does not cover derived or ported versions created by third parties under GPL.
//
// Only a subset of the C library has been ported, namely level 1 and 3 not in 
// streaming mode. 
//
// Version: 1.5.0 final

using System;
using System.Collections.Generic;
using System.Text;

static public class QuickLZ
{
    public const int QLZ_VERSION_MAJOR = 1;
    public const int QLZ_VERSION_MINOR = 5;
    public const int QLZ_VERSION_REVISION = 0;

    // Streaming mode not supported
    public const int QLZ_STREAMING_BUFFER = 0;

    // Bounds checking not supported  Use try...catch instead
    public const int QLZ_MEMORY_SAFE = 0;

    // Decrease QLZ_POINTERS_3 to increase level 3 compression speed. Do not edit any other values!
    private const int HASH_VALUES = 4096;
    private const int MINOFFSET = 2;
    private const int UNCONDITIONAL_MATCHLEN = 6;
    private const int UNCOMPRESSED_END = 4;
    private const int CWORD_LEN = 4;
    private const int DEFAULT_HEADERLEN = 9;
    private const int QLZ_POINTERS_1 = 1;
    private const int QLZ_POINTERS_3 = 16;

    [ThreadStatic]
    private static int[,] hashtableLv1;

    [ThreadStatic]
    private static int[,] hashtableLv2;

    [ThreadStatic]
    private static int[] cachetable;

    [ThreadStatic]
    private static byte[] hash_counter;

    private static int[,] getHashtable(int level)
    {
        if (cachetable == null)
            cachetable = new int[HASH_VALUES];
        else
            Array.Clear(cachetable, 0, cachetable.Length);

        if (hash_counter == null)
            hash_counter = new byte[HASH_VALUES];
        else
            Array.Clear(hash_counter, 0, hash_counter.Length);

        if (level == 1)
        {
            if (hashtableLv1 == null)
                hashtableLv1 = new int[HASH_VALUES, QLZ_POINTERS_1];
            else
            {
                Array.Clear(hashtableLv1, 0, hashtableLv1.Length);
                //for(int i = 0, n = hashtableLv1.GetLength(0); i < n; i++)
                //{
                //    for (int j = 0, m = hashtableLv1.GetLength(1); j < m; j++)
                //    {
                //        hashtableLv1[i, j] = 0;
                //    }
                //}
            }
            return hashtableLv1;
        }
        else
        {
            if (hashtableLv2 == null)
                hashtableLv2 = new int[HASH_VALUES, QLZ_POINTERS_3];
            else
            {
                Array.Clear(hashtableLv2, 0, hashtableLv2.Length);
                //for (int i = 0, n = hashtableLv2.GetLength(0); i < n; i++)
                //{
                //    for (int j = 0, m = hashtableLv2.GetLength(1); j < m; j++)
                //    {
                //        hashtableLv2[i, j] = 0;
                //    }
                //}
            }
            return hashtableLv2;
        }
    }

    private static int headerLen(byte[] source, int offset = 0)
    {
        return ((source[offset] & 2) == 2) ? 9 : 3;
    }

    public static int sizeDecompressed(byte[] source, int offset = 0)
    {
        if (headerLen(source, offset) == 9)
            return source[5 + offset] | (source[6 + offset] << 8) | (source[7 + offset] << 16) | (source[8 + offset] << 24);
        else
            return source[2 + offset];
    }

    public static int sizeCompressed(byte[] source, int offset = 0)
    {
        if (headerLen(source, offset) == 9)
            return source[1 + offset] | (source[2 + offset] << 8) | (source[3 + offset] << 16) | (source[4 + offset] << 24);
        else
            return source[1 + offset];
    }

    private static void write_header(byte[] dst, int level, bool compressible, int size_compressed, int size_decompressed, int offset = 0)
    {
        dst[offset] = (byte)(2 | (compressible ? 1 : 0));
        dst[offset] |= (byte)(level << 2);
        dst[offset] |= (1 << 6);
        dst[offset] |= (0 << 4);
        fast_write(dst, 1, size_decompressed, 4, offset);
        fast_write(dst, 5, size_compressed, 4, offset);
    }

    public static byte[] compress(byte[] source, int level)
    {
        int src = 0;
        int dst = DEFAULT_HEADERLEN + CWORD_LEN;
        uint cword_val = 0x80000000;
        int cword_ptr = DEFAULT_HEADERLEN;
        byte[] destination = new byte[source.Length + 400];
        int[,] hashtable;
        byte[] d2;
        int fetch = 0;
        int last_matchstart = (source.Length - UNCONDITIONAL_MATCHLEN - UNCOMPRESSED_END - 1);
        int lits = 0;

        if (level != 1 && level != 3)
            throw new ArgumentException("C# version only supports level 1 and 3");

        hashtable = getHashtable(level);

        if (source.Length == 0)
            return new byte[0];

        if (src <= last_matchstart)
            fetch = source[src] | (source[src + 1] << 8) | (source[src + 2] << 16);

        while (src <= last_matchstart)
        {
            if ((cword_val & 1) == 1)
            {
                if (src > source.Length >> 1 && dst > src - (src >> 5))
                {
                    d2 = new byte[source.Length + DEFAULT_HEADERLEN];
                    write_header(d2, level, false, source.Length, source.Length + DEFAULT_HEADERLEN);
                    System.Array.Copy(source, 0, d2, DEFAULT_HEADERLEN, source.Length);
                    return d2;
                }

                fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), 4);
                cword_ptr = dst;
                dst += CWORD_LEN;
                cword_val = 0x80000000;
            }

            if (level == 1)
            {
                int hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);
                int o = hashtable[hash, 0];
                int cache = cachetable[hash] ^ fetch;
                cachetable[hash] = fetch;
                hashtable[hash, 0] = src;

                if (cache == 0 && hash_counter[hash] != 0 && (src - o > MINOFFSET || (src == o + 1 && lits >= 3 && src > 3 && source[src] == source[src - 3] && source[src] == source[src - 2] && source[src] == source[src - 1] && source[src] == source[src + 1] && source[src] == source[src + 2])))
                {
                    cword_val = ((cword_val >> 1) | 0x80000000);
                    if (source[o + 3] != source[src + 3])
                    {
                        int f = 3 - 2 | (hash << 4);
                        destination[dst + 0] = (byte)(f >> 0 * 8);
                        destination[dst + 1] = (byte)(f >> 1 * 8);
                        src += 3;
                        dst += 2;
                    }
                    else
                    {
                        int old_src = src;
                        int remaining = ((source.Length - UNCOMPRESSED_END - src + 1 - 1) > 255 ? 255 : (source.Length - UNCOMPRESSED_END - src + 1 - 1));

                        src += 4;
                        if (source[o + src - old_src] == source[src])
                        {
                            src++;
                            if (source[o + src - old_src] == source[src])
                            {
                                src++;
                                while (source[o + (src - old_src)] == source[src] && (src - old_src) < remaining)
                                    src++;
                            }
                        }

                        int matchlen = src - old_src;

                        hash <<= 4;
                        if (matchlen < 18)
                        {
                            int f = (hash | (matchlen - 2));
                            destination[dst + 0] = (byte)(f >> 0 * 8);
                            destination[dst + 1] = (byte)(f >> 1 * 8);
                            dst += 2;
                        }
                        else
                        {
                            fast_write(destination, dst, hash | (matchlen << 16), 3);
                            dst += 3;
                        }
                    }
                    fetch = source[src] | (source[src + 1] << 8) | (source[src + 2] << 16);
                    lits = 0;
                }
                else
                {
                    lits++;
                    hash_counter[hash] = 1;
                    destination[dst] = source[src];
                    cword_val = (cword_val >> 1);
                    src++;
                    dst++;
                    fetch = ((fetch >> 8) & 0xffff) | (source[src + 2] << 16);
                }

            }
            else
            {
                fetch = source[src] | (source[src + 1] << 8) | (source[src + 2] << 16);

                int o, offset2;
                int matchlen, k, m, best_k = 0;
                byte c;
                int remaining = ((source.Length - UNCOMPRESSED_END - src + 1 - 1) > 255 ? 255 : (source.Length - UNCOMPRESSED_END - src + 1 - 1));
                int hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);

                c = hash_counter[hash];
                matchlen = 0;
                offset2 = 0;
                for (k = 0; k < QLZ_POINTERS_3 && c > k; k++)
                {
                    o = hashtable[hash, k];
                    if ((byte)fetch == source[o] && (byte)(fetch >> 8) == source[o + 1] && (byte)(fetch >> 16) == source[o + 2] && o < src - MINOFFSET)
                    {
                        m = 3;
                        while (source[o + m] == source[src + m] && m < remaining)
                            m++;
                        if ((m > matchlen) || (m == matchlen && o > offset2))
                        {
                            offset2 = o;
                            matchlen = m;
                            best_k = k;
                        }
                    }
                }
                o = offset2;
                hashtable[hash, c & (QLZ_POINTERS_3 - 1)] = src;
                c++;
                hash_counter[hash] = c;

                if (matchlen >= 3 && src - o < 131071)
                {
                    int offset = src - o;

                    for (int u = 1; u < matchlen; u++)
                    {
                        fetch = source[src + u] | (source[src + u + 1] << 8) | (source[src + u + 2] << 16);
                        hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);
                        c = hash_counter[hash]++;
                        hashtable[hash, c & (QLZ_POINTERS_3 - 1)] = src + u;
                    }

                    src += matchlen;
                    cword_val = ((cword_val >> 1) | 0x80000000);

                    if (matchlen == 3 && offset <= 63)
                    {
                        fast_write(destination, dst, offset << 2, 1);
                        dst++;
                    }
                    else if (matchlen == 3 && offset <= 16383)
                    {
                        fast_write(destination, dst, (offset << 2) | 1, 2);
                        dst += 2;
                    }
                    else if (matchlen <= 18 && offset <= 1023)
                    {
                        fast_write(destination, dst, ((matchlen - 3) << 2) | (offset << 6) | 2, 2);
                        dst += 2;
                    }
                    else if (matchlen <= 33)
                    {
                        fast_write(destination, dst, ((matchlen - 2) << 2) | (offset << 7) | 3, 3);
                        dst += 3;
                    }
                    else
                    {
                        fast_write(destination, dst, ((matchlen - 3) << 7) | (offset << 15) | 3, 4);
                        dst += 4;
                    }
                    lits = 0;
                }
                else
                {
                    destination[dst] = source[src];
                    cword_val = (cword_val >> 1);
                    src++;
                    dst++;
                }
            }
        }
        while (src <= source.Length - 1)
        {
            if ((cword_val & 1) == 1)
            {
                fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), 4);
                cword_ptr = dst;
                dst += CWORD_LEN;
                cword_val = 0x80000000;
            }

            destination[dst] = source[src];
            src++;
            dst++;
            cword_val = (cword_val >> 1);
        }
        while ((cword_val & 1) != 1)
        {
            cword_val = (cword_val >> 1);
        }
        fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), CWORD_LEN);
        write_header(destination, level, true, source.Length, dst);
        d2 = new byte[dst];
        System.Array.Copy(destination, d2, dst);
        return d2;
    }

    public static void compress(byte[] source, int start, int length, int level, byte[] output, int outputOffet, out int outputLength)
    {
        if (length + 400 > output.Length - outputOffet)
            throw new ArgumentException("Output is not big enough to compress: " + length + 400);

        int src = 0;
        int srcStart = start;
        int dst = DEFAULT_HEADERLEN + CWORD_LEN;
        uint cword_val = 0x80000000;
        int cword_ptr = DEFAULT_HEADERLEN;
        byte[] destination = output;
        int[,] hashtable;
        int fetch = 0;
        int last_matchstart = (length - UNCONDITIONAL_MATCHLEN - UNCOMPRESSED_END - 1);
        int lits = 0;
        outputLength = 0;
        if (level != 1 && level != 3)
            throw new ArgumentException("C# version only supports level 1 and 3");

        hashtable = getHashtable(level);

        if (length == 0)
        {
            return;
        }

        if (src <= last_matchstart)
            fetch = source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16);

        while (src <= last_matchstart)
        {
            if ((cword_val & 1) == 1)
            {
                if (src > length >> 1 && dst > src - (src >> 5))
                {
                    outputLength = length + DEFAULT_HEADERLEN;
                    //output = new byte[length + DEFAULT_HEADERLEN];
                    write_header(output, level, false, length, length + DEFAULT_HEADERLEN, outputOffet);
                    System.Array.Copy(source, start, output, outputOffet + DEFAULT_HEADERLEN, length);
                    return;
                }

                fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), 4, outputOffet);
                cword_ptr = dst;
                dst += CWORD_LEN;
                cword_val = 0x80000000;
            }

            if (level == 1)
            {
                int hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);
                int o = hashtable[hash, 0];
                int cache = cachetable[hash] ^ fetch;
                cachetable[hash] = fetch;
                hashtable[hash, 0] = src;

                if (cache == 0 && hash_counter[hash] != 0 && (src - o > MINOFFSET || (src == o + 1 && lits >= 3 && src > 3 &&
                    source[srcStart] == source[srcStart - 3] && source[srcStart] == source[srcStart - 2] &&
                    source[srcStart] == source[srcStart - 1] && source[srcStart] == source[srcStart + 1] && source[srcStart] == source[srcStart + 2])))
                {
                    cword_val = ((cword_val >> 1) | 0x80000000);
                    if (source[o + 3 + start] != source[srcStart + 3])
                    {
                        int f = 3 - 2 | (hash << 4);
                        destination[dst + 0 + outputOffet] = (byte)(f >> 0 * 8);
                        destination[dst + 1 + outputOffet] = (byte)(f >> 1 * 8);
                        src += 3;
                        dst += 2;
                        srcStart = src + start;
                    }
                    else
                    {
                        int old_src = src;
                        int remaining = ((length - UNCOMPRESSED_END - src + 1 - 1) > 255 ? 255 : (length - UNCOMPRESSED_END - src + 1 - 1));

                        src += 4;
                        srcStart = src + start;
                        if (source[o + srcStart - old_src] == source[srcStart])
                        {
                            src++;
                            srcStart = src + start;
                            if (source[o + srcStart - old_src] == source[srcStart])
                            {
                                src++;
                                srcStart = src + start;
                                while (source[o + (srcStart - old_src)] == source[srcStart] && (src - old_src) < remaining)
                                {
                                    src++;
                                    srcStart = src + start;
                                }
                            }
                        }

                        int matchlen = src - old_src;

                        hash <<= 4;
                        if (matchlen < 18)
                        {
                            int f = (hash | (matchlen - 2));
                            destination[dst + 0 + outputOffet] = (byte)(f >> 0 * 8);
                            destination[dst + 1 + outputOffet] = (byte)(f >> 1 * 8);
                            dst += 2;
                        }
                        else
                        {
                            fast_write(destination, dst, hash | (matchlen << 16), 3, outputOffet);
                            dst += 3;
                        }
                    }
                    fetch = source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16);
                    lits = 0;
                }
                else
                {
                    lits++;
                    hash_counter[hash] = 1;
                    destination[dst + outputOffet] = source[srcStart];
                    cword_val = (cword_val >> 1);
                    src++;
                    dst++;
                    srcStart = src + start;
                    fetch = ((fetch >> 8) & 0xffff) | (source[srcStart + 2] << 16);
                }

            }
            else
            {
                fetch = source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16);

                int o, offset2;
                int matchlen, k, m, best_k = 0;
                byte c;
                int remaining = ((length - UNCOMPRESSED_END - src + 1 - 1) > 255 ? 255 : (length - UNCOMPRESSED_END - src + 1 - 1));
                int hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);

                c = hash_counter[hash];
                matchlen = 0;
                offset2 = 0;
                for (k = 0; k < QLZ_POINTERS_3 && c > k; k++)
                {
                    o = hashtable[hash, k];
                    if ((byte)fetch == source[o + start] && (byte)(fetch >> 8) == source[o + 1 + start] && (byte)(fetch >> 16) == source[o + 2 + start] && o < src - MINOFFSET)
                    {
                        m = 3;
                        while (source[o + m + start] == source[srcStart + m] && m < remaining)
                            m++;
                        if ((m > matchlen) || (m == matchlen && o > offset2))
                        {
                            offset2 = o;
                            matchlen = m;
                            best_k = k;
                        }
                    }
                }
                o = offset2;
                hashtable[hash, c & (QLZ_POINTERS_3 - 1)] = src;
                c++;
                hash_counter[hash] = c;

                if (matchlen >= 3 && src - o < 131071)
                {
                    int offset = src - o;

                    for (int u = 1; u < matchlen; u++)
                    {
                        fetch = source[srcStart + u] | (source[srcStart + u + 1] << 8) | (source[srcStart + u + 2] << 16);
                        hash = ((fetch >> 12) ^ fetch) & (HASH_VALUES - 1);
                        c = hash_counter[hash]++;
                        hashtable[hash, c & (QLZ_POINTERS_3 - 1)] = src + u;
                    }

                    src += matchlen;
                    srcStart = src + start;
                    cword_val = ((cword_val >> 1) | 0x80000000);

                    if (matchlen == 3 && offset <= 63)
                    {
                        fast_write(destination, dst, offset << 2, 1, outputOffet);
                        dst++;
                    }
                    else if (matchlen == 3 && offset <= 16383)
                    {
                        fast_write(destination, dst, (offset << 2) | 1, 2, outputOffet);
                        dst += 2;
                    }
                    else if (matchlen <= 18 && offset <= 1023)
                    {
                        fast_write(destination, dst, ((matchlen - 3) << 2) | (offset << 6) | 2, 2, outputOffet);
                        dst += 2;
                    }
                    else if (matchlen <= 33)
                    {
                        fast_write(destination, dst, ((matchlen - 2) << 2) | (offset << 7) | 3, 3, outputOffet);
                        dst += 3;
                    }
                    else
                    {
                        fast_write(destination, dst, ((matchlen - 3) << 7) | (offset << 15) | 3, 4, outputOffet);
                        dst += 4;
                    }
                    lits = 0;
                }
                else
                {
                    destination[dst + outputOffet] = source[srcStart];
                    cword_val = (cword_val >> 1);
                    src++;
                    dst++;
                    srcStart = src + start;
                }
            }
        }
        while (src <= length - 1)
        {
            if ((cword_val & 1) == 1)
            {
                fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), 4, outputOffet);
                cword_ptr = dst;
                dst += CWORD_LEN;
                cword_val = 0x80000000;
            }

            destination[dst + outputOffet] = source[srcStart];
            src++;
            dst++;
            srcStart = src + start;
            cword_val = (cword_val >> 1);
        }
        while ((cword_val & 1) != 1)
        {
            cword_val = (cword_val >> 1);
        }
        fast_write(destination, cword_ptr, (int)((cword_val >> 1) | 0x80000000), CWORD_LEN, outputOffet);
        write_header(destination, level, true, length, dst, outputOffet);
        outputLength = dst;
        return;
    }

    private static void fast_write(byte[] a, int i, int value, int numbytes, int offset = 0)
    {
        for (int j = 0; j < numbytes; j++)
            a[i + j + offset] = (byte)(value >> (j * 8));
    }

    public static byte[] decompress(byte[] source)
    {
        if (cachetable == null)
            cachetable = new int[HASH_VALUES];
        else
            Array.Clear(cachetable, 0, cachetable.Length);

        if (hash_counter == null)
            hash_counter = new byte[HASH_VALUES];
        else
            Array.Clear(hash_counter, 0, hash_counter.Length);

        var hashtable = cachetable;
        int level;
        int size = sizeDecompressed(source);
        int src = headerLen(source);
        int dst = 0;
        uint cword_val = 1;
        byte[] destination = new byte[size];
        int last_matchstart = size - UNCONDITIONAL_MATCHLEN - UNCOMPRESSED_END - 1;
        int last_hashed = -1;
        int hash;
        uint fetch = 0;

        level = (source[0] >> 2) & 0x3;

        if (level != 1 && level != 3)
            throw new ArgumentException("C# version only supports level 1 and 3");

        if ((source[0] & 1) != 1)
        {
            byte[] d2 = new byte[size];
            System.Array.Copy(source, headerLen(source), d2, 0, size);
            return d2;
        }

        for (; ; )
        {
            if (cword_val == 1)
            {
                cword_val = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                src += 4;
                if (dst <= last_matchstart)
                {
                    if (level == 1)
                        fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16));
                    else
                        fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                }
            }

            if ((cword_val & 1) == 1)
            {
                uint matchlen;
                uint offset2;

                cword_val = cword_val >> 1;

                if (level == 1)
                {
                    hash = ((int)fetch >> 4) & 0xfff;
                    offset2 = (uint)hashtable[hash];

                    if ((fetch & 0xf) != 0)
                    {
                        matchlen = (fetch & 0xf) + 2;
                        src += 2;
                    }
                    else
                    {
                        matchlen = source[src + 2];
                        src += 3;
                    }
                }
                else
                {
                    uint offset;
                    if ((fetch & 3) == 0)
                    {
                        offset = (fetch & 0xff) >> 2;
                        matchlen = 3;
                        src++;
                    }
                    else if ((fetch & 2) == 0)
                    {
                        offset = (fetch & 0xffff) >> 2;
                        matchlen = 3;
                        src += 2;
                    }
                    else if ((fetch & 1) == 0)
                    {
                        offset = (fetch & 0xffff) >> 6;
                        matchlen = ((fetch >> 2) & 15) + 3;
                        src += 2;
                    }
                    else if ((fetch & 127) != 3)
                    {
                        offset = (fetch >> 7) & 0x1ffff;
                        matchlen = ((fetch >> 2) & 0x1f) + 2;
                        src += 3;
                    }
                    else
                    {
                        offset = (fetch >> 15);
                        matchlen = ((fetch >> 7) & 255) + 3;
                        src += 4;
                    }
                    offset2 = (uint)(dst - offset);
                }

                destination[dst + 0] = destination[offset2 + 0];
                destination[dst + 1] = destination[offset2 + 1];
                destination[dst + 2] = destination[offset2 + 2];

                for (int i = 3; i < matchlen; i += 1)
                {
                    destination[dst + i] = destination[offset2 + i];
                }

                dst += (int)matchlen;

                if (level == 1)
                {
                    fetch = (uint)(destination[last_hashed + 1] | (destination[last_hashed + 2] << 8) | (destination[last_hashed + 3] << 16));
                    while (last_hashed < dst - matchlen)
                    {
                        last_hashed++;
                        hash = (int)(((fetch >> 12) ^ fetch) & (HASH_VALUES - 1));
                        hashtable[hash] = last_hashed;
                        hash_counter[hash] = 1;
                        fetch = (uint)(fetch >> 8 & 0xffff | destination[last_hashed + 3] << 16);
                    }
                    fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16));
                }
                else
                {
                    fetch = (uint)(source[src] | (source[src + 1] << 8) | (source[src + 2] << 16) | (source[src + 3] << 24));
                }
                last_hashed = dst - 1;
            }
            else
            {
                if (dst <= last_matchstart)
                {
                    destination[dst] = source[src];
                    dst += 1;
                    src += 1;
                    cword_val = cword_val >> 1;

                    if (level == 1)
                    {
                        while (last_hashed < dst - 3)
                        {
                            last_hashed++;
                            int fetch2 = destination[last_hashed] | (destination[last_hashed + 1] << 8) | (destination[last_hashed + 2] << 16);
                            hash = ((fetch2 >> 12) ^ fetch2) & (HASH_VALUES - 1);
                            hashtable[hash] = last_hashed;
                            hash_counter[hash] = 1;
                        }
                        fetch = (uint)(fetch >> 8 & 0xffff | source[src + 2] << 16);
                    }
                    else
                    {
                        fetch = (uint)(fetch >> 8 & 0xffff | source[src + 2] << 16 | source[src + 3] << 24);
                    }
                }
                else
                {
                    while (dst <= size - 1)
                    {
                        if (cword_val == 1)
                        {
                            src += CWORD_LEN;
                            cword_val = 0x80000000;
                        }

                        destination[dst] = source[src];
                        dst++;
                        src++;
                        cword_val = cword_val >> 1;
                    }
                    return destination;
                }
            }
        }
    }

    public static void decompress(byte[] source, int start, byte[] output, int outputOffet, out int outputLength)
    {
        if (cachetable == null)
            cachetable = new int[HASH_VALUES];
        else
            Array.Clear(cachetable, 0, cachetable.Length);

        if (hash_counter == null)
            hash_counter = new byte[HASH_VALUES];
        else
            Array.Clear(hash_counter, 0, hash_counter.Length);
        var hashtable = cachetable;
        int level;
        int size = sizeDecompressed(source, start);
        if(size > output.Length - outputOffet)
            throw new ArgumentException("Output is not big enough to decompress: " + size);

        int src = headerLen(source, start);
        int srcStart = src + start;
        int dst = 0;
        uint cword_val = 1;
        byte[] destination = output;
        int last_matchstart = size - UNCONDITIONAL_MATCHLEN - UNCOMPRESSED_END - 1;
        int last_hashed = -1;
        int hash;
        uint fetch = 0;
        outputLength = size;
        level = (source[start] >> 2) & 0x3;

        if (level != 1 && level != 3)
            throw new ArgumentException("C# version only supports level 1 and 3");

        if ((source[start] & 1) != 1)
        {
            System.Array.Copy(source, srcStart, destination, outputOffet, size);
            return;
        }

        for (; ; )
        {
            if (cword_val == 1)
            {
                cword_val = (uint)(source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16) | (source[srcStart + 3] << 24));
                src += 4;
                srcStart = src + start;
                if (dst <= last_matchstart)
                {
                    if (level == 1)
                        fetch = (uint)(source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16));
                    else
                        fetch = (uint)(source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16) | (source[srcStart + 3] << 24));
                }
            }

            if ((cword_val & 1) == 1)
            {
                uint matchlen;
                uint offset2;

                cword_val = cword_val >> 1;

                if (level == 1)
                {
                    hash = ((int)fetch >> 4) & 0xfff;
                    offset2 = (uint)hashtable[hash];

                    if ((fetch & 0xf) != 0)
                    {
                        matchlen = (fetch & 0xf) + 2;
                        src += 2;
                    }
                    else
                    {
                        matchlen = source[srcStart + 2];
                        src += 3;
                    }
                    srcStart = src + start;
                }
                else
                {
                    uint offset;
                    if ((fetch & 3) == 0)
                    {
                        offset = (fetch & 0xff) >> 2;
                        matchlen = 3;
                        src++;
                    }
                    else if ((fetch & 2) == 0)
                    {
                        offset = (fetch & 0xffff) >> 2;
                        matchlen = 3;
                        src += 2;
                    }
                    else if ((fetch & 1) == 0)
                    {
                        offset = (fetch & 0xffff) >> 6;
                        matchlen = ((fetch >> 2) & 15) + 3;
                        src += 2;
                    }
                    else if ((fetch & 127) != 3)
                    {
                        offset = (fetch >> 7) & 0x1ffff;
                        matchlen = ((fetch >> 2) & 0x1f) + 2;
                        src += 3;
                    }
                    else
                    {
                        offset = (fetch >> 15);
                        matchlen = ((fetch >> 7) & 255) + 3;
                        src += 4;
                    }
                    srcStart = src + start;
                    offset2 = (uint)(dst - offset);
                }

                destination[dst + 0 + outputOffet] = destination[offset2 + 0 + outputOffet];
                destination[dst + 1 + outputOffet] = destination[offset2 + 1 + outputOffet];
                destination[dst + 2 + outputOffet] = destination[offset2 + 2 + outputOffet];

                for (int i = 3; i < matchlen; i += 1)
                {
                    destination[dst + i + outputOffet] = destination[offset2 + i + outputOffet];
                }

                dst += (int)matchlen;

                if (level == 1)
                {
                    fetch = (uint)(destination[last_hashed + 1 + outputOffet] | (destination[last_hashed + 2 + outputOffet] << 8) | (destination[last_hashed + 3 + outputOffet] << 16));
                    while (last_hashed < dst - matchlen)
                    {
                        last_hashed++;
                        hash = (int)(((fetch >> 12) ^ fetch) & (HASH_VALUES - 1));
                        hashtable[hash] = last_hashed;
                        hash_counter[hash] = 1;
                        fetch = (uint)(fetch >> 8 & 0xffff | destination[last_hashed + 3 + outputOffet] << 16);
                    }
                    fetch = (uint)(source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16));
                }
                else
                {
                    fetch = (uint)(source[srcStart] | (source[srcStart + 1] << 8) | (source[srcStart + 2] << 16) | (source[srcStart + 3] << 24));
                }
                last_hashed = dst - 1;
            }
            else
            {
                if (dst <= last_matchstart)
                {
                    destination[dst + outputOffet] = source[srcStart];
                    dst += 1;
                    src += 1;
                    srcStart = src + start;
                    cword_val = cword_val >> 1;

                    if (level == 1)
                    {
                        while (last_hashed < dst - 3)
                        {
                            last_hashed++;
                            int fetch2 = destination[last_hashed + outputOffet] | (destination[last_hashed + 1 + outputOffet] << 8) | (destination[last_hashed + 2 + outputOffet] << 16);
                            hash = ((fetch2 >> 12) ^ fetch2) & (HASH_VALUES - 1);
                            hashtable[hash] = last_hashed;
                            hash_counter[hash] = 1;
                        }
                        fetch = (uint)(fetch >> 8 & 0xffff | source[srcStart + 2] << 16);
                    }
                    else
                    {
                        fetch = (uint)(fetch >> 8 & 0xffff | source[srcStart + 2] << 16 | source[srcStart + 3] << 24);
                    }
                }
                else
                {
                    while (dst <= size - 1)
                    {
                        if (cword_val == 1)
                        {
                            src += CWORD_LEN;
                            srcStart = src + start;
                            cword_val = 0x80000000;
                        }

                        destination[dst + outputOffet] = source[srcStart];
                        dst++;
                        src++;
                        srcStart = src + start;
                        cword_val = cword_val >> 1;
                    }
                    return;
                }
            }
        }
    }
}
