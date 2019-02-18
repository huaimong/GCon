using VgcApis.Models.IServices;

namespace V2RayGCon.Lib.Lua
{
    public class Apis :
        VgcApis.Models.BaseClasses.Disposable,
        IApiService
    {
        IServersService serversService;
        ISettingsService settingService;
        IConfigMgrService configMgrService;
        ApiComponents.UtilsApi utilsService;
        ApiComponents.WebApi webService;

        public void Run(
            ISettingsService setting,
            IServersService servers,
            IConfigMgrService configMgr)
        {
            this.configMgrService = configMgr;
            this.settingService = setting;
            this.serversService = servers;

            this.utilsService = new ApiComponents.UtilsApi();
            this.webService = new ApiComponents.WebApi();
        }

        #region IApi interfaces
        public IServersService GetServersService()
            => this.serversService;

        public ISettingsService GetSettingService()
            => this.settingService;

        public IConfigMgrService GetConfigMgrService()
            => this.configMgrService;

        public IUtilsService GetUtilsService() => utilsService;
        public IWebService GetWebService() => webService;

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            utilsService?.Dispose();
            webService?.Dispose();
        }

        #endregion
    }
}
