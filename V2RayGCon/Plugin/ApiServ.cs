using VgcApis.Models.IServices;

namespace V2RayGCon.Plugin
{
    class ApiServ : VgcApis.Models.IServices.IAllServices
    {
        IServersService serversService;
        ISettingsService settingService;

        public void Run(
            Service.Setting setting,
            Service.Servers servers)
        {
            this.settingService = setting;
            this.serversService = servers;
        }

        #region IApi interfaces
        public IServersService GetVgcServersService()
            => this.serversService;

        public ISettingsService GetVgcSettingService()
            => this.settingService;
        #endregion
    }
}
