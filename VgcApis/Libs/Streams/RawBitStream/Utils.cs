using System.Collections.Generic;
using System.Text;

namespace VgcApis.Libs.Streams.RawBitStream
{
    public static class Utils
    {
        const int MaxStringLen = Models.Consts.BitStream.MaxStringLen;
        const int BitsPerUnicode = Models.Consts.BitStream.BitsPerUnicode;
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;

        public static string BoolList2Str(IEnumerable<bool> stream)
        {
            StringBuilder sb = new StringBuilder("");
            var padStream = PadRight(stream);
            var cache = new bool[BitsPerUnicode];
            var index = 0;
            foreach (var bit in padStream)
            {
                cache[index++] = bit;
                if (index == BitsPerUnicode)
                {
                    sb.Append((char)BoolList2Int(cache));
                    index = 0;
                }
            }
            return sb.ToString();
        }

        public static List<bool> Str2BoolList(string str)
        {
            var result = new List<bool>();
            foreach (var c in str)
            {
                var cache = Int2BoolList(c, BitsPerUnicode);
                foreach (var bit in cache)
                {
                    result.Add(bit);
                }
            }
            return result;
        }

        public static List<bool> PadRight(IEnumerable<bool> bitStream)
        {
            var result = new List<bool>(bitStream);
            for (int i = 0; i < result.Count % BitsPerUnicode; i++)
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
