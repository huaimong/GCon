using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace V2RayGCon.Test
{
    [TestClass]
    public class CoreServerTest
    {
        // download v2ray-core into test folder first
        V2RayGCon.Service.Core core;

        public CoreServerTest()
        {
            core = new Service.Core(Service.Setting.Instance);
        }

        [TestMethod]
        public void TestGetExecutablePath()
        {

#if DEBUG
            var exe = core.GetExecutablePath();
            Assert.AreEqual(false, string.IsNullOrEmpty(exe));
#endif

        }

        [TestMethod]
        public void TestIsExecutableExist()
        {
#if DEBUG
            var exist = core.IsExecutableExist();
            Assert.AreEqual(true, exist);
#endif
        }

        [TestMethod]
        public void TestGetCoreVersion()
        {
#if DEBUG
            var ver = core.GetCoreVersion();
            Assert.AreEqual(false, string.IsNullOrEmpty(ver));
#endif
        }


    }
}
