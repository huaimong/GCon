using Luna.Resources.Langs;

namespace Luna
{
    // Using lunar not lua to void naming conflicts.
    public class Luna : VgcApis.Models.BaseClasses.Plugin
    {
        VgcApis.IService api;
        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.ISettingService vgcSettings;

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
                vgcServers,
                settings,
                luaServer);

            formMain.FormClosed += (s, a) => formMain = null;
            formMain.Show();
        }

        protected override void Start(VgcApis.IService api)
        {
            this.api = api;
            vgcServers = api.GetVgcServersService();
            vgcSettings = api.GetVgcSettingService();

            settings = new Services.Settings();
            luaServer = new Services.LuaServer();

            settings.Run(vgcSettings);
            luaServer.Run(settings, vgcServers);
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
