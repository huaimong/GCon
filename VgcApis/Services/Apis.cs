using VgcApis.Models.IServices;

namespace VgcApis.Services
{
    public class Apis : IApiService
    {
        IServersService serversService;
        ISettingsService settingService;
        IConfigMgrService configMgrService;

        public void Run(
            ISettingsService setting,
            IServersService servers,
            IConfigMgrService configMgr)
        {
            this.configMgrService = configMgr;
            this.settingService = setting;
            this.serversService = servers;
        }

        #region IApi interfaces
        public IServersService GetServersService()
            => this.serversService;

        public ISettingsService GetSettingService()
            => this.settingService;

        public IConfigMgrService GetConfigMgrService()
            => this.configMgrService;
        #endregion
    }
}
