namespace VgcApis.Models.IServices
{
    public interface IShareLinkMgrService
    {
        string DecodeVmessLink(string vmessLink);
        string EncodeVmessLink(string config);
        string EncodeVeeLink(string config);
        int UpdateSubscriptions(int proxyPort);
    }
}
