using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    public interface ILuaApis
    {
        /// <summary>
        /// First running http server port.
        /// </summary>
        /// <returns></returns>
        int GetProxyPort();

        /// <summary>
        /// timeout seconds
        /// </summary>
        /// <param name="url"></param>
        /// <param name="proxyPort"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        string Fetch(string url, int proxyPort, int timeout);

        string Fetch(string url);

        /// <summary>
        /// Show perdefined functions.
        /// </summary>
        /// <returns></returns>
        string PerdefinedFunctions();

        /// <summary>
        /// Api:Print("hello",", ","world","!")
        /// </summary>
        /// <param name="contents">objects</param>
        void Print(params object[] contents);

        /// <summary>
        /// Api:Sleep(1000) // one second
        /// </summary>
        /// <param name="millisecond"></param>
        void Sleep(int millisecond);

        List<Interfaces.ICoreServCtrl> GetAllServers();
        long RunSpeedTest(string rawConfig);
    }
}
