using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static VgcApis.Libs.Utils;

namespace VgcApisTests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void LazyGuyTest()
        {
            var str = "";

            void task()
            {
                str += ".";
            }
            var adam = new VgcApis.Libs.Tasks.LazyGuy(task, 100);
            adam.DoItNow();
            Assert.AreEqual(".", str);

            str = "";
            adam.DoItLater();
            adam.ForgetIt();
            Assert.AreEqual("", str);

#if DEBUG
            str = "";
            adam.DoItLater();
            adam.DoItLater();
            adam.DoItLater();
            Thread.Sleep(1000);
            Assert.AreEqual(".", str);

            str = "";
            adam.DoItLater();
            Thread.Sleep(300);
            Assert.AreEqual(".", str);
#endif
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("11,22,abc")]
        public void CloneTest(string orgStr)
        {
            var org = orgStr?.Split(',').ToList();
            var clone = Clone<List<string>>(org);
            var sClone = SerializeObject(clone);
            var sOrg = SerializeObject(org);
            Assert.AreEqual(sOrg, sClone);
        }

        [DataTestMethod]
        [DataRow("0", 0)]
        [DataRow("-1", -1)]
        [DataRow("str-1.234", 0)]
        [DataRow("-1.234str", 0)]
        [DataRow("-1.234", -1)]
        [DataRow("1.432", 1)]
        [DataRow("1.678", 2)]
        [DataRow("-1.678", -2)]
        public void Str2Int(string value, int expect)
        {
            Assert.AreEqual(expect, VgcApis.Libs.Utils.Str2Int(value));
        }
    }
}
