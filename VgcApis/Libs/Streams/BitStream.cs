using System.Collections.Generic;
using System.Text;

namespace VgcApis.Libs.Streams
{
    public sealed class BitStream :
        Models.BaseClasses.ContainerTpl<BitStream>
    {
        int readPos = 0;
        readonly object streamWriteLock = new object();
        List<bool> bitStream = new List<bool>();
        const int BitsPerUtf16 = Models.Consts.BitStream.BitsPerUtf16;

        public BitStream() { }

        public void Run()
        {
            var numbers = new BitStreamComponents.Numbers();
            var characters = new BitStreamComponents.Characters();
            var asciiString = new BitStreamComponents.AsciiString();
            var utf16String = new BitStreamComponents.Utf16String();
            var uuids = new BitStreamComponents.Uuids();
            var address = new BitStreamComponents.Address();

            Plug(numbers);
            Plug(characters);
            Plug(asciiString);
            Plug(utf16String);
            Plug(uuids);
            Plug(address);

            characters.Run(numbers);
            asciiString.Run(numbers, characters);
            utf16String.Run(numbers);
            uuids.Run(numbers);
            address.Run(numbers, asciiString);
        }

        #region public methods
        List<bool> GetPatchdList()
        {
            lock (streamWriteLock)
            {
                var result = new List<bool>(bitStream);
                for (int i = 0; i < result.Count % BitsPerUtf16; i++)
                {
                    result.Add(false);
                }
                return result;
            }
        }

        public void FromString(string str)
        {
            lock (streamWriteLock)
            {
                Clear();
                foreach (var c in str)
                {
                    int ascii = c;
                    for (int i = 0; i < BitsPerUtf16; i++)
                    {
                        bitStream.Add(ascii % 2 == 1);
                        ascii /= 2;
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("");
            var clone = GetPatchdList();
            lock (streamWriteLock)
            {
                for (int i = 0; i < clone.Count / BitsPerUtf16; i++)
                {
                    var sum = 0;
                    var pow = 1;
                    for (int j = 0; j < BitsPerUtf16; j++)
                    {
                        var bit = clone[i * BitsPerUtf16 + j];
                        sum += pow * (bit ? 1 : 0);
                        pow *= 2;
                    }
                    sb.Append((char)sum);
                }
            }
            return sb.ToString();
        }

        public string CutString(string str)
        {
            const int MaxLen = Models.Consts.BitStream.MaxStringLen;

            if (str.Length > MaxLen)
            {
                return str.Substring(0, MaxLen);
            }
            return str;
        }

        public int Count() => bitStream.Count;

        public int GetIndex() => readPos;

        public void Clear()
        {
            lock (streamWriteLock)
            {
                readPos = 0;
                bitStream = new List<bool>();
            }
        }

        public void Rewind()
        {
            lock (streamWriteLock)
            {
                readPos = 0;
            }
        }

        public List<bool> Read(int len)
        {
            var result = new List<bool>();
            lock (streamWriteLock)
            {
                for (int i = 0; i < len && readPos < bitStream.Count; i++)
                {
                    var val = bitStream[readPos++];
                    result.Add(val);
                }
            }
            return result;
        }

        public bool? Read()
        {
            lock (streamWriteLock)
            {
                if (readPos >= bitStream.Count)
                {
                    return null;
                }
                var result = bitStream[readPos++];
                return result;
            }
        }

        public void Write(IEnumerable<bool> values)
        {
            lock (streamWriteLock)
            {
                foreach (var val in values)
                {
                    bitStream.Add(val);
                }
            }
        }

        public void Write(bool val)
        {
            lock (streamWriteLock)
            {
                bitStream.Add(val);
            }
        }
        #endregion

        #region private methods
        void Plug(Models.BaseClasses.ComponentOf<BitStream> component) =>
            Plug(this, component);
        #endregion

    }
}
