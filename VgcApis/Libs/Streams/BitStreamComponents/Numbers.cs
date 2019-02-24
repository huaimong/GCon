using System;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public sealed class Numbers :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        public Numbers() { }

        #region public methods
        public void Write(int val, int len)
        {
            if (val < 0 || val > 65535)
            {
                throw new ArgumentOutOfRangeException("val must between 0 and 65535");
            }

            CheckLen(len);

            var stream = GetContainer().GetBitStream();
            while (len > 0)
            {
                stream.Enqueue(val % 2 == 1);
                val /= 2;
                len--;
            }
        }

        public int Read(int len)
        {
            CheckLen(len);

            var stream = GetContainer().GetBitStream();
            int pow = 1;
            int sum = 0;
            for (int i = 0; i < len; i++)
            {
                if (!stream.TryDequeue(out bool bit))
                {
                    throw new NullReferenceException("Queue is empty!");
                }
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
