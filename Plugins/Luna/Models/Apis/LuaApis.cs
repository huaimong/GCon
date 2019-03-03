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
        VgcApis.Models.IServices.IShareLinkMgrService vgcSlinkMgr;


        Action<string> redirectLogWorker;

        public LuaApis(
            Services.Settings settings,
            VgcApis.Models.IServices.IApiService api)
        {
            this.settings = settings;
            this.vgcConfigMgr = api.GetConfigMgrService();
            this.vgcSlinkMgr = api.GetShareLinkMgrService();
            this.vgcServers = api.GetServersService();
            this.vgcWeb = api.GetWebService();
            this.vgcUtils = api.GetUtilsService();
            this.redirectLogWorker = settings.SendLog;
        }

        #region ILuaApis
        public int UpdateSubscriptions() =>
            vgcSlinkMgr.UpdateSubscriptions(-1);

        public int UpdateSubscriptions(int proxyPort) =>
            vgcSlinkMgr.UpdateSubscriptions(proxyPort);

        public void UpdateAllSummary() =>
            vgcServers.UpdateAllServersSummarySync();

        public void SetShareMemory(string key, string value) =>
            settings.SetLuaShareMemory(key, value);

        public string GetShareMemory(string key) =>
            settings.GetLuaShareMemory(key);

        public void ResetIndexQuiet() =>
            vgcServers.ResetIndexQuiet();

        public void RequireFormMainReload() =>
            vgcServers.RequireFormMainReload();

        public void SortSelectedServersBySummary() =>
            vgcServers.SortSelectedBySummary();

        public void SortSelectedServersBySpeedTest() =>
            vgcServers.SortSelectedBySpeedTest();

        public bool RunSpeedTestOnSelectedServers() =>
            vgcServers.RunSpeedTestOnSelectedServers();

        public string PackSelectedServers(
            string orgUid, string pkgName) =>
            vgcServers.PackSelectedServersIntoV4Package(orgUid, pkgName);

        public string PatchHref(string url, string href) =>
            vgcWeb.PatchHref(url, href);

        public List<string> FindAllHrefs(string text) =>
            vgcWeb.FindAllHrefs(text);

        public string GetAppDir() => VgcApis.Libs.Utils.GetAppDir();

        public string ShareLink2ConfigString(string shareLink) =>
            vgcSlinkMgr.DecodeShareLinkToConfig(shareLink) ?? @"";

        public string Search(string keywords, int first, int proxyPort) =>
            vgcWeb.Search(keywords, first, proxyPort, 20 * 1000);

        public List<string> ExtractVmessLinks(string text) =>
            vgcWeb.ExtractLinks(text,
                VgcApis.Models.Datas.Enum.LinkTypes.vmess);

        public List<string> ExtractSsLinks(string text) =>
            vgcWeb.ExtractLinks(text,
                VgcApis.Models.Datas.Enum.LinkTypes.ss);

        public long RunSpeedTest(string rawConfig) =>
            vgcConfigMgr.RunSpeedTest(rawConfig);

        public int GetProxyPort() =>
            vgcServers.GetAvailableHttpProxyPort();

        public string Fetch(string url, int proxyPort, int milliSeconds) =>
            vgcWeb.Fetch(url, proxyPort, milliSeconds);

        public string Fetch(string url) => vgcWeb.Fetch(url, -1, -1);

        public List<VgcApis.Models.Interfaces.ICoreServCtrl> GetAllServers() =>
            vgcServers.GetAllServersOrderByIndex().ToList();

        public void Sleep(int milliseconds) =>
            System.Threading.Thread.Sleep(milliseconds);

        public void Print(params object[] contents)
        {
            var text = "";
            foreach (var content in contents)
            {
                if (content == null)
                {
                    text += @"nil";
                }
                else
                {
                    text += content.ToString();
                }
            }
            redirectLogWorker?.Invoke(text);
        }
        #endregion

        #region public methods
        public void SendLog(string message) =>
            redirectLogWorker?.Invoke(message);

        public void SetRedirectLogWorker(Action<string> worker)
        {
            if (worker != null)
            {
                redirectLogWorker = worker;
            }
        }

        public string PredefinedFunctions() =>
            Resources.Files.Datas.LuaPredefinedFunctions;
        #endregion

        #region private methods
        #endregion
    }
}
