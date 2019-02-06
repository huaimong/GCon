namespace VgcApis.Models.IServices

{
    public interface IApiService
    {

        ISettingsService GetSettingService();
        IServersService GetServersService();
        IConfigMgrService GetConfigMgrService();
        IWebService GetWebService();
        IUtilsService GetUtilsService();
    }
}
