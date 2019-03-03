namespace ProxySetter.Services
{
    class PsLuncher
    {
        PsSettings setting;
        PacServer pacServer;
        ServerTracker serverTracker;

        VgcApis.Models.IServices.IApiService vgcApi;
        Model.Data.ProxyRegKeyValue orgSysProxySetting;
        Views.WinForms.FormMain formMain;

        public PsLuncher() { }

        public void Run(VgcApis.Models.IServices.IApiService api)
        {
            orgSysProxySetting = Lib.Sys.ProxySetter.GetProxySetting();
            VgcApis.Libs.Sys.FileLogger.Info("ProxySetter: save sys proxy settings");

            this.vgcApi = api;

            var vgcSetting = api.GetSettingService();
            var vgcServer = api.GetServersService();

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
            formMain.Show();
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
