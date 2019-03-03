using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    public interface ILuaApis
    {

        string GetShareMemory(string key);

        List<string> ExtractVmessLinks(string text);

        List<string> ExtractSsLinks(string text);

        List<string> FindAllHrefs(string text);

        string Fetch(string url);

        string Fetch(string url, int proxyPort, int milliSeconds);

        List<ICoreServCtrl> GetAllServers();

        string GetAppDir();

        /// <summary>
        /// First running http server port.
        /// </summary>
        int GetProxyPort();

        /// <summary>
        /// V4 format. params can set to string.Empty
        /// </summary>
        string PackSelectedServers(string orgUid, string pkgName);

        string PatchHref(string url, string href);

        /// <summary>
        /// Show perdefined functions.
        /// </summary>
        string PredefinedFunctions();

        /// <summary>
        /// Api:Print("hello",", ","world","!")
        /// </summary>
        void Print(params object[] contents);

        void RequireFormMainReload();
        void ResetIndexQuiet();
        long RunSpeedTest(string rawConfig);
        bool RunSpeedTestOnSelectedServers();

        string ScanQrcode();
        string Search(string keywords, int first, int proxyPort);
        void SetShareMemory(string key, string value);
        void Sleep(int millisecond);
        void SortSelectedServersBySpeedTest();
        void SortSelectedServersBySummary();

        void UpdateAllSummary();
        int UpdateSubscriptions();
        int UpdateSubscriptions(int proxyPort);

        string ShareLink2ConfigString(string vmessLink);
    }
}
