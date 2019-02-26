using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace V2RayGCon.Test
{
    [TestClass]
    public class Base64Tests
    {
        string[] normalStrings = new string[] {
            @"",
            @"1234",
            @"abcd",
            @"1中23 文",
            @"中123ac文"
        };

        string GenRandHex() => Lib.Utils.RandomHex(7);

        string GenUtf16String()
        {
            using (var bs1 = new VgcApis.Libs.Streams.BitStream())
            {
                var uuid = Guid.NewGuid();
                bs1.Write(true);
                bs1.Write(12345);
                bs1.Write(uuid);
                bs1.WriteAddress("abc.com");
                bs1.WriteAddress("::1");
                bs1.WriteAddress("1.2.3.4");
                bs1.Write(GenRandHex());
                bs1.Write(GenRandHex());
                bs1.Write("1中23文");
                var result = bs1.ToString();
                return result;
            }
        }

        string GenRawVeeString()
        {
            var v1 = new Model.Data.Vee
            {
                address = "::1",
                alias = "中文abc 123",
                description = "描述abc123",
                isUseTls = true,
                streamParam = "/v2ray?#abc",
                streamType = "ws",
                port = 123,
                uuid = Guid.NewGuid(),
            };
            return v1.ToString();
        }

        [TestMethod]
        public void VeeLinkTest()
        {
            var e = GenRawVeeString();
            var eBytes = Encoding.Unicode.GetBytes(e);
            var eB64 = Convert.ToBase64String(eBytes);
            var deBytes = Convert.FromBase64String(eB64);
            var de = Encoding.Unicode.GetString(deBytes);

            Assert.AreEqual(eBytes.Length, deBytes.Length);
            var len = eBytes.Length;
            for (int i = 0; i < len; i++)
            {
                Assert.AreEqual(eBytes[i], deBytes[i]);
            }
        }

        [TestMethod]
        public void Utf16EncodingTest()
        {
            for (int i = 0; i < 10; i++)
            {
                var str = GenUtf16String();
                var encoded = Lib.Utils.Base64Encode(str);
                var decoded = Lib.Utils.Base64Decode(encoded);
                Assert.AreEqual(str, decoded);
            }
        }

        [TestMethod]
        public void NormalEncodingTest()
        {
            foreach (var str in normalStrings)
            {
                var encoded = Lib.Utils.Base64Encode(str);
                var decoded = Lib.Utils.Base64Decode(encoded);
                Assert.AreEqual(str, decoded);
            }

        }


    }
}
