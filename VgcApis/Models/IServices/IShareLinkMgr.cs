namespace VgcApis.Models.IServices
{
    public interface IShareLinkMgrService
    {
        string DecodeShareLinkToConfig(string shareLink);

        string EncodeConfigToShareLink(
            string config, Datas.Enum.LinkTypes linkType);

        int UpdateSubscriptions(int proxyPort);
    }
}
