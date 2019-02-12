using System.Collections.Generic;

namespace Luna.Services
{
    public class Settings :
        VgcApis.Models.BaseClasses.Disposable

    {
        VgcApis.Models.IServices.ISettingsService vgcSetting;
        readonly string pluginName = Properties.Resources.Name;
        Models.Data.UserSettings userSettings;
        VgcApis.Libs.Tasks.LazyGuy bookKeeper;

        public Settings() { }

        #region public methods
        public void SendLog(string contnet)
        {
            var name = Properties.Resources.Name;
            vgcSetting.SendLog(string.Format("[{0}] {1}", name, contnet));
        }

        bool isDisposing = false;
        public bool IsShutdown() => isDisposing || vgcSetting.IsShutdown();

        public void SetIsDisposing(bool value) => isDisposing = value;

        public void Run(
            VgcApis.Models.IServices.ISettingsService vgcSetting)
        {
            this.vgcSetting = vgcSetting;

            userSettings = VgcApis.Libs.Utils
                .LoadPluginSetting<Models.Data.UserSettings>(
                    pluginName, vgcSetting);

            bookKeeper = new VgcApis.Libs.Tasks.LazyGuy(
                SaveUserSettingsNow, 30000);
        }

        public List<Models.Data.LuaCoreSetting> GetLuaCoreSettings()
        {
            if (userSettings.luaServers == null)
            {
                userSettings.luaServers =
                    new List<Models.Data.LuaCoreSetting>();
                SaveSettings();
            }
            return userSettings.luaServers;
        }

        public void SaveSettings()
        {
            bookKeeper.DoItLater();
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            bookKeeper.DoItNow();
            bookKeeper.Quit();
        }
        #endregion

        #region private methods
        void SaveUserSettingsNow() =>
            VgcApis.Libs.Utils.SavePluginSetting(
                pluginName, userSettings, vgcSetting);

        #endregion
    }
}
