using System.Text;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public sealed class Utf16String :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        Numbers numbers;

        const int MaxLenBits = Models.Consts.BitStream.MaxStringLenInBits;
        const int MaxLen = Models.Consts.BitStream.MaxStringLen;
        const int BitsPerUtf16 = Models.Consts.BitStream.BitsPerUtf16;

        public Utf16String() { }

        public void Run(Numbers numbers)
        {
            this.numbers = numbers;
        }

        #region properties

        #endregion

        #region public methods
        public void Write(string str)
        {
            var s = GetContainer().CutString(str);
            var len = s.Length;

            numbers.Write(len, MaxLenBits);
            foreach (var c in s)
            {
                numbers.Write(c, BitsPerUtf16);
            }
        }

        public string Read()
        {
            var len = numbers.Read(MaxLenBits);
            StringBuilder sb = new StringBuilder("");
            for (int i = 0; i < len; i++)
            {
                var utf16 = numbers.Read(BitsPerUtf16);
                sb.Append((char)utf16);
            }
            return sb.ToString();
        }

        #endregion

        #region private methods

        #endregion

        #region protected methods

        #endregion
    }
}
