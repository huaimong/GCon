using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    public interface ILuaApis
    {
        List<string> ExtractVmessLinks(string text);

        List<string> ExtractSsLinks(string text);

        List<string> FindAllHrefs(string text);

        string Fetch(string url);

        /// <summary>
        /// timeout seconds
        /// </summary>
        string Fetch(string url, int proxyPort, int timeout);

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
        string PerdefinedFunctions();

        /// <summary>
        /// Api:Print("hello",", ","world","!")
        /// </summary>
        void Print(params object[] contents);

        void RequireFormMainReload();
        void ResetIndexQuiet();
        long RunSpeedTest(string rawConfig);
        bool RunSpeedTestOnSelectedServers();

        void SortSelectedServersBySummary();
        void SortSelectedServersBySpeedTest();

        string Search(string keywords, int first, int proxyPort);

        /// <summary>
        /// Api:Sleep(1000) // one second
        /// </summary>
        /// <param name="millisecond"></param>
        void Sleep(int millisecond);

        string VmessLink2ConfigString(string vmessLink);
    }
}
