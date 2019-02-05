using Luna.Resources.Langs;

namespace Luna
{
    // Using lunar not lua to void naming conflicts.
    public class Luna : VgcApis.Models.BaseClasses.Plugin
    {
        VgcApis.Models.IServices.IApiService api;
        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.ISettingsService vgcSettings;
        VgcApis.Models.IServices.IConfigMgrService vgcConfigMgr;

        Views.WinForms.FormMain formMain = null;
        Services.Settings settings;
        Services.LuaServer luaServer;

        #region properties
        public override string Name => Properties.Resources.Name;
        public override string Version => Properties.Resources.Version;
        public override string Description => I18N.Description;
        #endregion

        #region protected overrides
        protected override void Popup()
        {
            if (formMain != null)
            {
                formMain.Activate();
                return;
            }

            formMain = new Views.WinForms.FormMain(
                vgcConfigMgr,
                vgcServers,
                settings,
                luaServer);

            formMain.FormClosed += (s, a) => formMain = null;
            formMain.Show();
        }

        protected override void Start(VgcApis.Models.IServices.IApiService api)
        {
            this.api = api;
            vgcServers = api.GetServersService();
            vgcSettings = api.GetSettingService();
            vgcConfigMgr = api.GetConfigMgrService();

            settings = new Services.Settings();
            luaServer = new Services.LuaServer();

            settings.Run(vgcSettings);
            luaServer.Run(settings, vgcServers, vgcConfigMgr);
        }

        protected override void Stop()
        {
            if (formMain != null)
            {
                formMain.Close();
            }

            luaServer?.Cleanup();
            settings?.Cleanup();
        }
        #endregion
    }
}
