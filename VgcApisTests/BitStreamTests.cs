using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace VgcApisTests
{
    [TestClass]
    public class BitStreamTests
    {
        VgcApis.Libs.Streams.BitStream bitStream;
        VgcApis.Libs.Streams.BitStreamComponents.Numbers numbers;

        public BitStreamTests()
        {
            bitStream = new VgcApis.Libs.Streams.BitStream();
            numbers = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Numbers>();
        }

        [DataTestMethod]
        [DataRow(0, 3)]
        [DataRow(1, 3)]
        [DataRow(2048, 16)]
        [DataRow(65535, 16)]
        public void NumbersNormalTest(int val, int len)
        {
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
