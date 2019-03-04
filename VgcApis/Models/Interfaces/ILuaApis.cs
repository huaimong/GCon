using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    public interface ILuaApis
    {
        #region local storage
        string ReadLocalStorage(string key);
        void WriteLocalStorage(string key, string value);
        #endregion

        #region Lua script
        string GetAppDir();
        string PredefinedFunctions(); // Show perdefined functions.
        void Print(params object[] contents);// Api:Print("hello",", ","world","!")
        void Sleep(int millisecond);
        #endregion

        #region core controlling
        List<ICoreServCtrl> GetAllServers();

        int GetProxyPort();  // First running http server port.
        string PackSelectedServers(string orgUid, string pkgName); // param nullable
        void RequireFormMainReload();
        void ResetIndexQuiet();
        long RunSpeedTest(string rawConfig);
        bool RunSpeedTestOnSelectedServers();
        string ScanQrcode();
        string ShareLink2ConfigString(string vmessLink);
        void SortSelectedServersBySpeedTest();
        void SortSelectedServersBySummary();
        void UpdateAllSummary();
        int UpdateSubscriptions();
        int UpdateSubscriptions(int proxyPort);
        #endregion

        #region web crawling 
        List<string> ExtractSsLinks(string text);
        List<string> ExtractVmessLinks(string text);
        string Fetch(string url);
        string Fetch(string url, int proxyPort, int milliSeconds);
        List<string> FindAllHrefs(string text);
        string PatchHref(string url, string href);
        string Search(string keywords, int first, int proxyPort);
        #endregion

    }
}
