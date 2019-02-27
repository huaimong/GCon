using System.Collections.Generic;
using System.Linq;

namespace VgcApis.Libs.Streams.RawBitStream
{
    public static class Utils
    {
        const int MaxStringLen = Models.Consts.BitStream.MaxStringLen;
        const int BitsPerUnicode = Models.Consts.BitStream.BitsPerUnicode;
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;

        public static byte[] BoolList2Bytes(IEnumerable<bool> stream)
        {
            // the first byte record how many valid bit of last byte
            // if last byte used 8bit then byte[0] = 0

            var bitLen = stream.Count();
            var byteLen = bitLen / BitsPerByte;
            var lastByteLen = bitLen - byteLen * BitsPerByte;

            var cacheLen = 1 + byteLen + (lastByteLen == 0 ? 0 : 1);
            var cache = new byte[cacheLen];

            int i = 0, j = 0, sum = 0, pow = 1;
            foreach (var bit in stream)
            {
                if (i % BitsPerByte == 0)
                {
                    cache[j++] = (byte)sum;
                    sum = 0;
                    pow = 1;
                }
                sum += pow * (bit ? 1 : 0);
                pow *= 2;
                i++;
            }
            cache[0] = (byte)lastByteLen;
            cache[cacheLen - 1] = (byte)sum;
            return cache;
        }

        public static List<bool> Bytes2BoolList(byte[] bytes)
        {
            if (bytes.Length <= 1)
            {
                return new List<bool>();
            }

            // the first byte record how many valid bit of last byte
            // if last byte used 8bit then byte[0] = 0
            int ascii;
            var result = new List<bool>();

            for (int i = 1; i < bytes.Length - 1; i++)
            {
                ascii = bytes[i];
                for (int j = 0; j < BitsPerByte; j++)
                {
                    result.Add(ascii % 2 == 1);
                    ascii /= 2;
                }
            }

            // last byte
            ascii = bytes[bytes.Length - 1];
            var lastByteLen = bytes[0] == 0 ? BitsPerByte : bytes[0];
            for (int j = 0; j < lastByteLen; j++)
            {
                result.Add(ascii % 2 == 1);
                ascii /= 2;
            }

            return result;
        }

        public static List<bool> PadRight(IEnumerable<bool> bitStream)
        {
            var result = new List<bool>(bitStream);
            for (int i = 0; i < result.Count % BitsPerByte; i++)
            {
                result.Add(false);
            }
            return result;
        }

        public static byte[] CutBytes(byte[] bytes)
        {
            var len = bytes.Length;
            if (bytes.Length <= MaxStringLen)
            {
                return bytes;
            }

            var result = new byte[MaxStringLen];
            for (int i = 0; i < MaxStringLen; i++)
            {
                result[i] = bytes[i];
            }
            return result;
        }

        public static int BoolList2Int(IEnumerable<bool> bools)
        {
            int pow = 1;
            int sum = 0;
            foreach (var bit in bools)
            {
                sum += pow * (bit ? 1 : 0);
                pow *= 2;
            }
            return sum;
        }

        public static List<bool> Int2BoolList(int val, int len)
        {
            var cache = new List<bool>();
            while (len > 0)
            {
                cache.Add(val % 2 == 1);
                val /= 2;
                len--;
            }
            return cache;
        }
    }
}
