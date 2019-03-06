using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static VgcApis.Libs.Utils;

namespace VgcApisTests
{
    [TestClass]
    public class UtilsTests
    {
        [DataTestMethod]
        [DataRow(
            @"{a:[{},{}],b:{}}",
            @"a:[],b:{},a.0:{},a.1:{}")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:{}}",
            @"a:[],b:{},a.0:[],a.1:[]")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:'abc',c:{a:[],b:{d:1}}}",
            @"a:[],a.0:[],a.1:[],c:{},c.a:[],c.b:{}")]
        public void GetterJsonDataStructWorkerTest(string jsonString, string expect)
        {
            var sections = new Dictionary<string, string>();

            var expDict = expect
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split(
                    new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(v => v[0], v => v[1]);

            var jtoken = JObject.Parse(jsonString);
            GetterJsonDataStruct(ref sections, jtoken, 2);

            foreach (var kv in expDict)
            {
                if (kv.Value != sections[kv.Key])
                {
                    Assert.Fail();
                }
            }

            foreach (var kv in sections)
            {
                if (kv.Value != expDict[kv.Key])
                {
                    Assert.Fail();
                }
            }
        }

        [DataTestMethod]
        [DataRow("EvABk文,tv字vvc", "字文", false)]
        [DataRow("EvABk文,tv字vvc", "ab字", true)]
        [DataRow("ab vvvc", "bc", true)]
        [DataRow("abc", "ac", true)]
        [DataRow("", "a", false)]
        [DataRow("", "", true)]
        public void PartialMatchTest(string source, string partial, bool expect)
        {
            var result = PartialMatchCi(source, partial);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com", @"http")]
        [DataRow(@"https://abc.com", @"https")]
        [DataRow(@"VMess://abc.com", @"vmess")]
        public void GetLinkPrefixTest(string link, string expect)
        {
            var prefix = GetLinkPrefix(link);
            Assert.AreEqual(expect, prefix);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.http)]
        [DataRow(@"V://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.v)]
        [DataRow(@"vmess://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.vmess)]
        [DataRow(@"v2cfg://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.v2cfg)]
        [DataRow(@"linkTypeNotExist://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.unknow)]
        [DataRow(@"abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.unknow)]
        [DataRow(@"ss://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.ss)]
        public void DetectLinkTypeTest(string link, VgcApis.Models.Datas.Enum.LinkTypes expect)
        {
            var linkType = DetectLinkType(link);
            Assert.AreEqual(expect, linkType);
        }


        [DataTestMethod]
        [DataRow(-4, -1)]
        [DataRow(-65535, -1)]
        [DataRow(-65535, -1)]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 3)]
        [DataRow(8, 4)]
        [DataRow(16, 5)]
        [DataRow(65535, 16)]
        [DataRow(65536, 17)]
        public void GetLenInBitsOfIntTest(int value, int expect)
        {
            var len = GetLenInBitsOfInt(value);
            Assert.AreEqual(expect, len);
        }

        [TestMethod]
        public void GetFreePortMultipleThreadsTest()
        {
            List<int> ports = new List<int>();
            object portsWriteLocker = new object();
            void checkPort(int p)
            {
                lock (portsWriteLocker)
                {
                    if (ports.Contains(p))
                    {
                        Assert.Fail();
                    }
                    ports.Add(p);
                }
            }

            void worker()
            {
                var freePort = GetFreeTcpPort();
                checkPort(freePort);
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port: freePort);
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(ep);
                    Sleep(500);
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 500; i++)
            {
                tasks.Add(RunInBackground(worker));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void GetFreePortSingleThreadTest()
        {
            List<int> ports = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                int port = GetFreeTcpPort();
                Assert.AreEqual(true, port > 0);
                Assert.AreEqual(false, ports.Contains(port));
                ports.Add(port);
            }
        }

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
