namespace VgcApis.Models.IServices
{
    public interface IConfigMgrService
    {
        long RunSpeedTest(string rawConfig);
        string VmessLink2ConfigString(string vmessLink);
    }
}
