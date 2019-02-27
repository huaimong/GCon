using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace V2RayGCon.Test.VeeTests
{
    [TestClass]
    public class Ver001ModelTest
    {
        [TestMethod]
        public void Ver001NormalTest()
        {
            for (int i = 0; i < 10; i++)
            {
                var v1 = new Model.VeeShareLinks.Ver001
                {
                    address = "::1",
                    alias = "中文abc 123",
                    description = "描述abc123",
                    isUseTls = true,
                    streamParam1 = "/v2ray?#abc",
                    streamParam2 = Lib.Utils.RandomHex(7),
                    streamParam3 = Lib.Utils.RandomHex(7),
                    streamType = Lib.Utils.RandomHex(7),
                    port = 123,
                    uuid = Guid.NewGuid(),
                };
                var bytes = v1.ToBytes();
                var bitStream = new VgcApis.Libs.Streams.BitStream(bytes);
                var v2 = new Model.VeeShareLinks.Ver001(bitStream);
                Assert.AreEqual(true, v1.EqTo(v2));
            }
        }

    }
}
