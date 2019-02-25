namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public class Characters :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        //7bit per char 
        const int BitPerChar = Models.Consts.BitStream.BitsPerChar;
        Numbers numbers;

        public Characters() { }

        public void Run(Numbers numbers)
        {
            this.numbers = numbers;
        }

        #region public methods
        public void Write(char c)
        {
            var ascii = Char2Int(c);
            numbers.Write(ascii, BitPerChar);
        }

        public char Read()
        {
            var ascii = numbers.Read(BitPerChar);
            RangeCheck(ascii);
            return Int2Char(ascii);
        }


        public char Int2Char(int ascii)
        {
            RangeCheck(ascii);
            return (char)ascii;
        }

        public int Char2Int(char c)
        {
            RangeCheck(c);
            return c;
        }


        #endregion

        #region private methods
        void RangeCheck(int ascii)
        {
            if (!isSupportChar(ascii))
            {
                throw new System.ArgumentOutOfRangeException("character must between 0-127");
            }
        }

        // 7bit support 0-127
        bool isSupportChar(int ascii) =>
            ascii < 128 && ascii >= 0;
        #endregion
    }
}
