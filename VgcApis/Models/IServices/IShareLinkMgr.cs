namespace VgcApis.Models.IServices
{
    public interface IShareLinkMgrService
    {
        string DecodeVmessLink(string vmessLink);
        string EncodeVmessLink(string config);
        string EncodeV2rayLink(string config);
    }
}
