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

            userSettings.NormalizeData();

            bookKeeper = new VgcApis.Libs.Tasks.LazyGuy(
                SaveUserSettingsNow, 30000);

            bookKeeper.DoItLater();
        }

        public string GetLuaShareMemory(string key)
        {
            if (!userSettings.luaShareMemory.ContainsKey(key))
            {
                return @"";
            }
            return userSettings.luaShareMemory[key];
        }

        readonly object shareMemoryLocker = new object();
        public void SetLuaShareMemory(string key, string value)
        {
            lock (shareMemoryLocker)
            {
                userSettings.luaShareMemory[key] = value;
            }
            SaveSettings();
        }

        public List<Models.Data.LuaCoreSetting> GetLuaCoreSettings() =>
            userSettings.luaServers;

        public void SaveSettings() =>
            bookKeeper.DoItLater();
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
