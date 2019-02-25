using System.Text;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public class AsciiString :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        // 7 bits per char
        Characters characters;
        Numbers numbers;

        const int MaxLenBits = Models.Consts.BitStream.MaxStringLenInBits;
        const int MaxLen = Models.Consts.BitStream.MaxStringLen;

        public AsciiString() { }

        public void Run(
            Numbers numbers,
            Characters characters)
        {
            this.numbers = numbers;
            this.characters = characters;
        }

        #region public methods
        public void Write(string str)
        {
            var s = GetContainer().CutString(str);
            int len = s.Length;

            numbers.Write(len, MaxLenBits);
            foreach (var c in str)
            {
                characters.Write(c);
            }
        }

        public string Read()
        {
            var len = numbers.Read(MaxLenBits);
            StringBuilder sb = new StringBuilder("");

            for (int i = 0; i < len; i++)
            {
                var c = characters.Read();
                sb.Append(c);
            }

            return sb.ToString();
        }
        #endregion

        #region private methods

        #endregion
    }
}
