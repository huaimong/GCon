using System.Collections.Generic;

namespace VgcApis.Models.Interfaces.Lua
{
    public interface ILuaWeb
    {
        List<string> ExtractBase64String(string text);
        List<string> ExtractV2cfgLinks(string text);
        List<string> ExtractVmessLinks(string text);
        List<string> ExtractSsLinks(string text);
        string Fetch(string url);
        string Fetch(string url, int proxyPort, int milliSeconds);
        List<string> FindAllHrefs(string text);
        int GetProxyPort();
        string PatchHref(string url, string href);
        string Search(string keywords, int first, int proxyPort);

        int UpdateSubscriptions();
        int UpdateSubscriptions(int proxyPort);
    }
}
