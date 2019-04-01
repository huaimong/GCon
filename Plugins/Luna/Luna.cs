using Luna.Resources.Langs;
using System.Drawing;

namespace Luna
{
    // Using lunar not lua to void naming conflicts.
    public class Luna : VgcApis.Models.BaseClasses.Plugin
    {
        Services.Settings settings;
        Services.LuaServer luaServer;
        Services.FormMgr formMgr;

        #region properties
        public override string Name => Properties.Resources.Name;
        public override string Version => Properties.Resources.Version;
        public override string Description => I18N.Description;

        // icon from http://lua-users.org/wiki/LuaLogo
        public override Image Icon => Properties.Resources.Lua_Logo_32x32;
        #endregion

        #region protected overrides
        protected override void Popup()
        {
            formMgr.ShowOrCreateFirstForm();
        }

        protected override void Start(VgcApis.Models.IServices.IApiService api)
        {
            var vgcServers = api.GetServersService();
            var vgcSettings = api.GetSettingService();
            var vgcConfigMgr = api.GetConfigMgrService();

            settings = new Services.Settings();
            luaServer = new Services.LuaServer();
            formMgr = new Services.FormMgr();

            settings.Run(vgcSettings);
            luaServer.Run(settings, api);
            formMgr.Run(settings, luaServer, api);
        }

        protected override void Stop()
        {
            settings?.SetIsDisposing(true);
            formMgr?.Dispose();
            luaServer?.Dispose();
            settings?.Dispose();
        }
        #endregion
    }
}
