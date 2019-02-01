using Statistics.Resources.Langs;

namespace Statistics
{
    public class Statistics : VgcApis.Models.BaseClasses.Plugin
    {
        VgcApis.IService api;
        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.ISettingService vgcSetting;
        Views.WinForms.FormMain formMain = null;
        Services.Settings settings;

        #region properties
        public override string Name => Properties.Resources.Name;
        public override string Version => Properties.Resources.Version;
        public override string Description => I18N.Description;
        #endregion

        #region protected override methods
        protected override void Start(VgcApis.IService api)
        {
            this.api = api;
            vgcSetting = api.GetVgcSettingService();
            vgcServers = api.GetVgcServersService();

            settings = new Services.Settings();
            settings.Run(vgcSetting, vgcServers);
        }

        protected override void Popup()
        {
            if (formMain != null)
            {
                formMain.Activate();
                return;
            }

            formMain = new Views.WinForms.FormMain(
                settings,
                vgcServers);
            formMain.FormClosed += (s, a) => formMain = null;
            formMain.Show();
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
