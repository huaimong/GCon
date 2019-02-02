using Pacman.Resources.Langs;

namespace Pacman
{
    public class Pacman : VgcApis.Models.BaseClasses.Plugin
    {
        VgcApis.Models.IServices.IAllServices api;
        Services.Settings settings;

        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.ISettingsService vgcSettings;
        Views.WinForms.FormMain formMain = null;

        // form=null;

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

            formMain = new Views.WinForms.FormMain(settings);
            formMain.FormClosed += (s, a) => formMain = null;
            formMain.Show();
        }

        protected override void Start(VgcApis.Models.IServices.IAllServices api)
        {
            this.api = api;
            this.settings = new Services.Settings();
            vgcServers = api.GetVgcServersService();
            vgcSettings = api.GetVgcSettingService();
            settings.Run(api);
        }

        protected override void Stop()
        {
            if (formMain != null)
            {
                formMain.Close();
            }
            settings?.Cleanup();
        }
        #endregion
    }
}
