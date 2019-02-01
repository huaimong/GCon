namespace VgcApis
{
    public interface IService
    {
        Models.IServices.ISettingService GetVgcSettingService();
        Models.IServices.IServersService GetVgcServersService();
    }
}
