using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProxySetter.Lib.Sys;

namespace ProxySetterTests
{
    [TestClass]
    public class WinInetTests
    {
        [TestMethod]
        public void GeneralTests()
        {
            var orgSettings = WinInet.GetProxySettings();
            var testSettings = new ProxySetter.Model.Data.ProxyRegKeyValue
            {
                proxyEnable = true,
                proxyServer = "192.168.1.1",
            };

            var success = WinInet.UpdateProxySettings(testSettings);
            Assert.AreEqual(true, success);

            var curSettings = WinInet.GetProxySettings();

            Assert.AreEqual(true, testSettings.IsEqualTo(curSettings));

            WinInet.UpdateProxySettings(orgSettings);
            curSettings = WinInet.GetProxySettings();
            Assert.AreEqual(true, orgSettings.IsEqualTo(curSettings));
        }

    }
}
