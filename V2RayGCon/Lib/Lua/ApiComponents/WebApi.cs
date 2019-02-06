namespace V2RayGCon.Lib.Lua.ApiComponents
{
    public sealed class WebApi :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.IServices.IWebService
    {

        public string Search(string query, int start, int proxyPort, int timeout)
        {
            var url = Lib.Utils.GenSearchUrl(query, start);
            return Fetch(url, proxyPort, timeout);
        }

        public string Fetch(string url, int proxyPort, int timeout) =>
          Utils.Fetch(url, proxyPort, timeout);
    }
}
