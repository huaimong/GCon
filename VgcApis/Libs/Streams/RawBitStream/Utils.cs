using System;
using System.Collections.Generic;
using System.Linq;

namespace VgcApis.Libs.Streams.RawBitStream
{
    public static class Utils
    {
        const int MaxStringLen = Models.Consts.BitStream.MaxStringLen;
        const int BitsPerUnicode = Models.Consts.BitStream.BitsPerUnicode;
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;

        #region private methods
        private static void CheckByteLengthIsBiggerThenOne(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 2)
            {
                throw new ArgumentOutOfRangeException(
                    @"Bytes length must bigger then 2!");
            }
        }

        private static void CheckMajorVersionRange(int major)
        {
            if (major < 0 || major > 255)
            {
                throw new ArgumentOutOfRangeException(
                    @"Major version out of range!");
            }
        }

        private static void CheckSubVersionRange(int sub)
        {
            if (sub < 0 || sub > 26)
            {
                throw new ArgumentOutOfRangeException(
                        @"Sub version out of range!");
            }
        }

        static Tuple<int, int> ParseVersionString(string version)
        {
            var verLen = version.Length;
            CheckVersionLength(verLen);
            int major = 0;
            int pow = 10;
            for (int i = 0; i < version.Length - 1; i++)
            {
                var c = version[i];
                if (!char.IsNumber(c))
                {
                    throw new ArgumentOutOfRangeException(
                        @"Major version must be number!");
                }
                major *= pow;
                major += c - '0';
            }

            CheckMajorVersionRange(major);
            var lastIndex = version.Length - 1;
            int sub = version.ToLower()[lastIndex] - 'a';
            CheckSubVersionRange(sub);
            return new Tuple<int, int>(major, sub);
        }

        private static void CheckVersionLength(int verLen)
        {
            if (verLen < 2 || verLen > 4)
            {
                throw new ArgumentOutOfRangeException(
                    @"Version length must between 2 and 4.");
            }
        }

        #endregion

        #region public methods

        public static string ReadVersion(byte[] bytes)
        {
            CheckByteLengthIsBiggerThenOne(bytes);
            int major = bytes[0];
            int sub = bytes[1] / BitsPerByte;
            CheckSubVersionRange(sub);
            char c = (char)(sub + 'a');
            return $"{major}{c}";
        }


        public static void WriteVersion(string version, byte[] bytes)
        {
            CheckByteLengthIsBiggerThenOne(bytes);
            var ver = ParseVersionString(version);
            bytes[0] = (byte)ver.Item1;
            int len = bytes[1] % BitsPerByte;
            bytes[1] = (byte)(ver.Item2 * BitsPerByte + len);
        }

        public static byte[] BoolList2Bytes(IEnumerable<bool> stream)
        {
            // The first byte (decimal 256) is reserved for major version.
            // The second byte record how many valid bits of last byte as length mark.
            // If the last byte used 8 bits then length mark will be set to zero.
            // So length mark only takes 3 bits max.
            // The rest 5 bits (decimal 32) is reserved for sub version.
            // But we only use a-z (26 numbers) for sub version.
            // Yet 6 numbers will be wasted.

            var bitLen = stream.Count();
            var byteLen = bitLen / BitsPerByte;
            var lastByteLen = bitLen - byteLen * BitsPerByte;

            var cacheLen = 2 + byteLen + (lastByteLen == 0 ? 0 : 1);
            var cache = new byte[cacheLen];

            int i = 0, j = 1, sum = 0, pow = 1;
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
            cache[cacheLen - 1] = (byte)sum;

            int lenMark = cache[1];
            cache[1] = (byte)(((lenMark >> 3) << 3) + lastByteLen);
            return cache;
        }

        public static List<bool> Bytes2BoolList(byte[] bytes)
        {
            if (bytes.Length <= 2)
            {
                return new List<bool>();
            }

            // if last byte used 8bit then length mark is zero
            int ascii;
            var result = new List<bool>();

            for (int i = 2; i < bytes.Length - 1; i++)
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
            var lenMark = bytes[1] % BitsPerByte;
            var lastByteLen = lenMark == 0 ? BitsPerByte : lenMark;
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
        #endregion
    }
}
