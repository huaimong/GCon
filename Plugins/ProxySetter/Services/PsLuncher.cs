namespace ProxySetter.Services
{
    class PsLuncher
    {
        PsSettings setting;
        PacServer pacServer;
        ServerTracker serverTracker;

        VgcApis.Models.IServices.IAllServices vgcApi;
        Model.Data.ProxyRegKeyValue orgSysProxySetting;
        Views.WinForms.FormMain formMain;

        public PsLuncher() { }

        public void Run(VgcApis.Models.IServices.IAllServices api)
        {
            orgSysProxySetting = Lib.Sys.ProxySetter.GetProxySetting();
            VgcApis.Libs.Sys.FileLogger.Info("ProxySetter: save sys proxy settings");

            this.vgcApi = api;

            var vgcSetting = api.GetVgcSettingService();
            var vgcServer = api.GetVgcServersService();

            pacServer = new PacServer();
            setting = new PsSettings();
            serverTracker = new ServerTracker();

            // dependency injection
            setting.Run(vgcSetting);
            pacServer.Run(setting);
            serverTracker.Run(setting, pacServer, vgcServer);

            setting.DebugLog("call Luncher.run");
        }

        public void Show()
        {
            if (formMain != null)
            {
                return;
            }

            formMain = new Views.WinForms.FormMain(
                setting,
                pacServer,
                serverTracker);
            formMain.FormClosed += (s, a) => formMain = null;
        }

        public void Cleanup()
        {
            setting.DebugLog("call Luncher.cleanup");
            setting.isCleaning = true;
            formMain?.Close();
            serverTracker.Cleanup();
            pacServer.Cleanup();
            setting.Cleanup();
            Lib.Sys.ProxySetter.UpdateProxySettingOnDemand(orgSysProxySetting);
            VgcApis.Libs.Sys.FileLogger.Info("ProxySetter: restore sys proxy settings");
        }
        #region properties
        #endregion
    }

}
