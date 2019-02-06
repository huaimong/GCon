namespace VgcApis.Models.IServices
{
    public interface IWebService
    {
        string Fetch(string url, int proxyPort, int timeout);
        string Fetch(string url);
    }
}
