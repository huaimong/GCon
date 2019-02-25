using System;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public sealed class Uuids :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        const int BytesPerUuid = Models.Consts.BitStream.BytesPerUuid;
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;

        Numbers numbers;

        public Uuids() { }

        public void Run(Numbers numbers)
        {
            this.numbers = numbers;
        }

        #region properties

        #endregion

        #region public methods
        public void Write(string uuid)
        {
            var u = Guid.Parse(uuid);
            var bytes = u.ToByteArray();
            foreach (var b in bytes)
            {
                numbers.Write(b, BitsPerByte);
            }
        }

        public Guid Read()
        {
            var uuid = new byte[BytesPerUuid];
            for (int i = 0; i < BytesPerUuid; i++)
            {
                uuid[i] = (byte)(numbers.Read(BitsPerByte));
            }
            return new Guid(uuid);
        }

        #endregion

        #region private methods
        #endregion

        #region protected methods

        #endregion
    }
}
