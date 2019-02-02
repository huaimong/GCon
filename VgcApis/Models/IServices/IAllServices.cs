namespace VgcApis.Models.IServices

{
    public interface IAllServices
    {
        Models.IServices.ISettingsService GetVgcSettingService();
        Models.IServices.IServersService GetVgcServersService();
    }
}
