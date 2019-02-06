namespace V2RayGCon.Lib.Lua.ApiComponents
{
    public sealed class WebApi :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.IServices.IWebService
    {
        public string Fetch(string url, int proxyPort, int timeout) =>
          Utils.Fetch(url, proxyPort, timeout);

        public string Fetch(string url) => Utils.Fetch(url);
    }
}
