using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace VgcApisTests
{
    [TestClass]
    public class BitStreamTests
    {
        const int BitsPerByte = VgcApis.Models.Consts.BitStream.BitsPerByte;
        const int BitsPerUtf16 = VgcApis.Models.Consts.BitStream.BitsPerUtf16;
        const int BitPerChar = VgcApis.Models.Consts.BitStream.BitsPerChar;
        const int MaxStrLenInBits = VgcApis.Models.Consts.BitStream.MaxStringLenInBits;
        const int MaxStrLen = VgcApis.Models.Consts.BitStream.MaxStringLen;

        VgcApis.Libs.Streams.BitStream bitStream;
        VgcApis.Libs.Streams.BitStreamComponents.Numbers numbers;
        VgcApis.Libs.Streams.BitStreamComponents.Characters characters;
        VgcApis.Libs.Streams.BitStreamComponents.AsciiString asciiString;
        VgcApis.Libs.Streams.BitStreamComponents.Utf16String utf16String;
        VgcApis.Libs.Streams.BitStreamComponents.Uuids uuids;
        VgcApis.Libs.Streams.BitStreamComponents.Address address;


        public BitStreamTests()
        {
            bitStream = new VgcApis.Libs.Streams.BitStream();
            bitStream.Run();

            numbers = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Numbers>();
            characters = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Characters>();
            asciiString = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.AsciiString>();
            utf16String = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Utf16String>();
            uuids = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Uuids>();
            address = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Address>();
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("123abcAbc")]
        [DataRow("abcd1234中文")]
        public void BitStreamGenTest(string str)
        {
            var s1 = new VgcApis.Libs.Streams.BitStream();
            s1.FromString(str);
            var result = s1.ToString();
            Assert.AreEqual(str, result);
        }


        [DataTestMethod]
        [DataRow("abc.com")]
        [DataRow("1.2.3.4")]
        [DataRow("::1")]
        [DataRow("2001:4860:4860::8888")]
        public void AddressNormalTest(string val)
        {
            bitStream.Clear();
            address.Write(val);
            var read = address.Read();
            Assert.AreEqual(read, val);
        }

        [TestMethod]
        public void UuidNormalTest()
        {
            bitStream.Clear();
            for (int i = 0; i < 10; i++)
            {
                var gid = Guid.NewGuid();
                uuids.Write(gid.ToString());
                var read = uuids.Read();
                Assert.AreEqual(read.ToString(), gid.ToString());
            }
        }

        [DataTestMethod]
        [DataRow(@"")]
        [DataRow(@"abc123")]
        [DataRow(@"中文abc1{23+}-./")]
        [DataRow(@"a中文abc1{23+}-./")]
        public void Utf16StringNormalTest(string val)
        {
            bitStream.Clear();
            utf16String.Write(val);
            var read = utf16String.Read();
            Assert.AreEqual(val.Length, read.Length);
            Assert.AreEqual(val, read);
        }

        [TestMethod]
        public void AsciiStringNormalTest()
        {
            bitStream.Clear();
            asciiString.Write("");
            Assert.AreEqual(MaxStrLenInBits, bitStream.Count());
            Assert.AreEqual("", asciiString.Read());

            for (int i = 0; i < MaxStrLen; i++)
            {
                var str = VgcApis.Libs.Utils.RandomHex(MaxStrLen);
                bitStream.Clear();
                asciiString.Write(str);
                var result = asciiString.Read();
                Assert.AreEqual(str, result);
            }

            var overflow = VgcApis.Libs.Utils.RandomHex(MaxStrLen + 1);
            bitStream.Clear();
            asciiString.Write(overflow);
            var read = asciiString.Read();
            Assert.AreEqual(MaxStrLen, read.Length);
            Assert.AreNotEqual(read, overflow);
            Assert.AreEqual(read, overflow.Substring(0, MaxStrLen));
        }

        [DataTestMethod]
        [DataRow(@"中文")]
        public void AsciiStringFailTest(string val)
        {
            bitStream.Clear();

            try
            {
                asciiString.Write(val);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void CharactersNormalTest()
        {

            bitStream.Clear();

            for (int i = 0; i < 128; i++)
            {
                characters.Write((char)i);
            }
            Assert.AreEqual(BitPerChar * 128, bitStream.Count());
            for (int i = 0; i < 128; i++)
            {
                var j = characters.Read();
                Assert.AreEqual(j, i);
            }
            Assert.AreEqual(bitStream.Count(), bitStream.GetIndex());
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(128)]
        public void CharactersFailTest(int ascii)
        {
            bitStream.Clear();

            try
            {
                var c = characters.Int2Char(ascii);
                characters.Write(c);
                var r = characters.Read();
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            Assert.Fail();
        }


        [TestMethod]
        public void BitStreamReadWritTest()
        {
            bitStream.Write(true);
            bitStream.Clear();
            Assert.AreEqual(0, bitStream.Count());
            Assert.AreEqual(0, bitStream.GetIndex());
            bitStream.Write(true);
            Assert.AreEqual(1, bitStream.Count());
            Assert.AreEqual(0, bitStream.GetIndex());
            var bit = bitStream.Read();
            Assert.AreEqual(1, bitStream.Count());
            Assert.AreEqual(1, bitStream.GetIndex());
            Assert.AreEqual(true, bit);
            bit = bitStream.Read();
            Assert.AreEqual(null, bit);
            Assert.AreEqual(1, bitStream.GetIndex());
            bitStream.Rewind();
            Assert.AreEqual(0, bitStream.GetIndex());
            Assert.AreEqual(1, bitStream.Count());
        }

        [DataTestMethod]
        [DataRow(0, 3)]
        [DataRow(1, 3)]
        [DataRow(2048, 16)]
        [DataRow(65535, 16)]
        public void NumbersNormalTest(int val, int len)
        {
            bitStream.Clear();

            numbers.Write(val, len);
            var result = numbers.Read(len);
            Assert.AreEqual(result, val);
        }


        [DataTestMethod]
        [DataRow(-1, 3)]
        [DataRow(1, 0)]
        [DataRow(2048, 17)]
        public void NumbersFailTest(int val, int len)
        {
            bitStream.Clear();

            try
            {
                numbers.Write(val, len);
                var result = numbers.Read(len);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            Assert.Fail();
        }


    }
}
