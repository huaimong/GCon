using System.Collections.Generic;
using System.Linq;

namespace Luna.Models.Apis.Components
{
    public sealed class Server :
        VgcApis.Models.BaseClasses.ComponentOf<LuaApis>,
        VgcApis.Models.Interfaces.Lua.ILuaServer
    {
        Services.Settings settings;

        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.IConfigMgrService vgcConfigMgr;

        public Server(
             Services.Settings settings,
             VgcApis.Models.IServices.IApiService api)
        {
            this.settings = settings;
            vgcServers = api.GetServersService();
            vgcConfigMgr = api.GetConfigMgrService();
        }

        public void UpdateAllSummary() =>
            vgcServers.UpdateAllServersSummarySync();

        public void WriteLocalStorage(string key, string value) =>
            settings.SetLuaShareMemory(key, value);

        public string ReadLocalStorage(string key) =>
            settings.GetLuaShareMemory(key);

        public void ResetIndexQuiet() =>
            vgcServers.ResetIndexQuiet();

        public long RunSpeedTest(string rawConfig) =>
            vgcConfigMgr.RunSpeedTest(rawConfig);

        public List<VgcApis.Models.Interfaces.ICoreServCtrl> GetAllServers() =>
            vgcServers.GetAllServersOrderByIndex().ToList();

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


    }
}
