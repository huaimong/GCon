using System;
using System.Collections.Generic;
using System.Linq;

namespace Luna.Models.Apis
{
    public class LuaApis :
        VgcApis.Models.Interfaces.ILuaApis
    {
        Services.Settings settings;
        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.IConfigMgrService vgcConfigMgr;
        VgcApis.Models.IServices.IWebService vgcWeb;
        VgcApis.Models.IServices.IUtilsService vgcUtils;

        Action<string> redirectLogWorker;

        public LuaApis(
            Services.Settings settings,
            VgcApis.Models.IServices.IApiService api)
        {
            this.settings = settings;
            this.vgcConfigMgr = api.GetConfigMgrService();
            this.vgcServers = api.GetServersService();
            this.vgcWeb = api.GetWebService();
            this.vgcUtils = api.GetUtilsService();
            this.redirectLogWorker = settings.SendLog;
        }

        #region ILuaApis
        public string GetAppDir() => VgcApis.Libs.Utils.GetAppDir();

        public string VmessLink2ConfigString(string vmessLink) =>
            vgcConfigMgr.VmessLink2ConfigString(vmessLink);

        public string Search(string query, int start, int proxyPort) =>
            vgcWeb.Search(query, 0, proxyPort, 20 * 1000);

        public List<string> ExtractHttpLink(string text) =>
            vgcUtils.ExtractLinks(
                text,
                VgcApis.Models.Datas.Enum.LinkTypes.http);

        public List<string> ExtractVmessLink(string text) =>
            vgcUtils.ExtractLinks(text,
                VgcApis.Models.Datas.Enum.LinkTypes.vmess);

        public long RunSpeedTest(string rawConfig) =>
            vgcConfigMgr.RunSpeedTest(rawConfig);

        public int GetProxyPort() =>
            vgcServers.GetAvailableHttpProxyPort();

        public string Fetch(string url, int proxyPort, int timeout) =>
            vgcWeb.Fetch(url, proxyPort, timeout * 1000);

        public string Fetch(string url) => vgcWeb.Fetch(url, -1, -1);

        public List<VgcApis.Models.Interfaces.ICoreServCtrl> GetAllServers() =>
            vgcServers.GetAllServersOrderByIndex().ToList();

        public void Sleep(int milliseconds) =>
            System.Threading.Thread.Sleep(milliseconds);

        public void Print(params object[] contents)
        {
            var text = "";
            foreach (var c in contents)
            {
                text += c.ToString();
            }
            redirectLogWorker(text);
        }
        #endregion

        #region public methods
        public void SetRedirectLogWorker(Action<string> worker)
        {
            if (worker != null)
            {
                redirectLogWorker = worker;
            }
        }

        public string PerdefinedFunctions() =>
            VgcApis.Models.Consts.Libs.LuaPerdefinedFunctions;
        #endregion

        #region private methods
        #endregion
    }
}
