namespace VgcApis.Models.IServices
{
    public interface IWebService
    {
        string Search(string query, int start, int proxyPort, int timeout);
        string Fetch(string url, int proxyPort, int timeout);
    }
}
