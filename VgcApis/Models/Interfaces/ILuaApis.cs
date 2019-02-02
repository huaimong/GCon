using System.Collections.Generic;

namespace VgcApis.Models.Interfaces
{
    public interface ILuaApis
    {
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

        List<Interfaces.ICoreCtrl> GetAllServers();
    }
}
