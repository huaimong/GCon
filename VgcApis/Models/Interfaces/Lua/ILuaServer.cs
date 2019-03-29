using System.Collections.Generic;

namespace VgcApis.Models.Interfaces.Lua
{
    public interface ILuaServer
    {
        List<ICoreServCtrl> GetAllServers();
        string PackSelectedServers(string orgUid, string pkgName);
        void RequireFormMainReload();
        void ResetIndexQuiet();
        long RunCustomSpeedTest(string rawConfig, string testUrl, int testTimeout);
        long RunSpeedTest(string rawConfig);
        bool RunSpeedTestOnSelectedServers();
        void SortSelectedServersBySpeedTest();
        void SortSelectedServersBySummary();
        void UpdateAllSummary();
    }
}
