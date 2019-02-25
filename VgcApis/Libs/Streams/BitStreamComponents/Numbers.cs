using System;
using System.Collections.Generic;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public sealed class Numbers :
        Models.BaseClasses.ComponentOf<BitStream>
    {

        const int BitsPerPort = Models.Consts.BitStream.BitsPerPort;
        public Numbers() { }

        #region public methods
        public void WritePortNum(int val) =>
            Write(val, BitsPerPort);

        public int ReadPortNum() =>
            Read(BitsPerPort);

        public void Write(int val, int len)
        {
            if (val < 0 || val > 65535)
            {
                throw new ArgumentOutOfRangeException("val must between 0 and 65535");
            }

            CheckLen(len);

            var cache = new List<bool>();
            while (len > 0)
            {
                cache.Add(val % 2 == 1);
                val /= 2;
                len--;
            }

            GetContainer().Write(cache);
        }

        public int Read(int len)
        {
            CheckLen(len);

            var cache = GetContainer().Read(len);
            if (cache.Count != len)
            {
                throw new NullReferenceException("Read overflow!");
            }
            int pow = 1;
            int sum = 0;
            for (int i = 0; i < len; i++)
            {
                var bit = cache[i];
                sum += pow * (bit ? 1 : 0);
                pow *= 2;
            }
            return sum;
        }
        #endregion

        #region private methods
        void CheckLen(int len)
        {
            if (len < 1 || len > 16)
            {
                throw new ArgumentOutOfRangeException("len must between 1 and 16");
            }
        }
        #endregion
    }
}
