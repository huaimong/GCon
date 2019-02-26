using System.Collections.Generic;

namespace VgcApis.Libs.Streams.RawBitStream
{
    public static class Utils
    {
        const int MaxStringLen = Models.Consts.BitStream.MaxStringLen;
        const int BitsPerUnicode = Models.Consts.BitStream.BitsPerUnicode;
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;

        public static byte[] BoolList2Bytes(IEnumerable<bool> stream)
        {
            var padStream = PadRight(stream);
            var len = padStream.Count / BitsPerByte;
            var cache = new byte[len];
            for (int i = 0; i < len; i++)
            {
                var sum = 0;
                var pow = 1;
                var baseIndex = i * BitsPerByte;
                for (int j = 0; j < BitsPerByte; j++)
                {
                    sum += pow * (padStream[baseIndex + j] ? 1 : 0);
                    pow *= 2;
                }
                cache[i] = (byte)sum;
            }

            return cache;
        }

        public static List<bool> Bytes2BoolList(byte[] bytes)
        {
            var result = new List<bool>();
            foreach (var b in bytes)
            {
                int ascii = b;
                for (int i = 0; i < BitsPerByte; i++)
                {
                    result.Add(ascii % 2 == 1);
                    ascii /= 2;
                }
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
