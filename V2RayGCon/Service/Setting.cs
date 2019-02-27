using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    public class Setting :
        Model.BaseClass.SingletonService<Setting>,
        VgcApis.Models.IServices.ISettingsService
    {
        Model.Data.UserSettings userSettings;

        // Singleton need this private ctor.
        Setting()
        {
            userSettings = LoadUserSettings();
            isShutdown = false;
        }

        #region Properties
        public bool isDownloadWin32V2RayCore
        {
            get => userSettings.isDownloadWin32V2RayCore;
            set
            {
                userSettings.isDownloadWin32V2RayCore = value;
                LazySaveUserSettings();
            }
        }

        public bool isShutdown { get; set; }

        public string decodeCache
        {
            get
            {
                return userSettings.DecodeCache;
            }
            set
            {
                userSettings.DecodeCache = value;
                LazySaveUserSettings();
            }
        }

        public bool isEnableStatistics
        {
            get => userSettings.isEnableStat;
            set
            {
                userSettings.isEnableStat = value;
                LazySaveUserSettings();
            }
        }

        public bool isUseV4
        {
            get => userSettings.isUseV4Format;
            set
            {
                userSettings.isUseV4Format = value;
                LazySaveUserSettings();
            }
        }

        public bool isUpdateUseProxy
        {
            get => userSettings.isUpdateUseProxy;
            set
            {
                userSettings.isUpdateUseProxy = value;
                LazySaveUserSettings();
            }
        }

        public bool isUpdateToVgcFull
        {
            get => userSettings.isUpdateToVgcFull;
            set
            {
                userSettings.isUpdateToVgcFull = value;
                LazySaveUserSettings();
            }
        }

        public bool isPortable
        {
            get
            {
                return userSettings.isPortable;
            }
            set
            {
                userSettings.isPortable = value;
                LazySaveUserSettings();
            }
        }

        public bool isServerTrackerOn = false;

        public int serverPanelPageSize
        {
            get
            {
                var size = userSettings.ServerPanelPageSize;
                return Lib.Utils.Clamp(size, 1, 101);
            }
            set
            {
                userSettings.ServerPanelPageSize = Lib.Utils.Clamp(value, 1, 101);
                LazySaveUserSettings();
            }
        }

        public CultureInfo orgCulture = null;

        VgcApis.Libs.Sys.QueueLogger qLogger = new VgcApis.Libs.Sys.QueueLogger();
        public long GetLogTimestamp() => qLogger.GetTimestamp();
        public string GetLogContent() => qLogger.GetLogAsString(true);
        public void SendLog(string log) => qLogger.Log(log);

        public Model.Data.Enum.Cultures culture
        {
            get
            {
                var cultures = Model.Data.Table.Cultures;
                var c = userSettings.Culture;

                if (!cultures.ContainsValue(c))
                {
                    return Model.Data.Enum.Cultures.auto;
                }

                return cultures.Where(s => s.Value == c).First().Key;
            }

            set
            {
                var cultures = Model.Data.Table.Cultures;
                var c = Model.Data.Enum.Cultures.auto;
                if (cultures.ContainsKey(value))
                {
                    c = value;
                }
                userSettings.Culture = Model.Data.Table.Cultures[c];
                LazySaveUserSettings();
            }
        }

        public bool isShowConfigerToolsPanel
        {
            get
            {
                return userSettings.CfgShowToolPanel == true;
            }
            set
            {
                userSettings.CfgShowToolPanel = value;
                LazySaveUserSettings();
            }
        }

        public const int maxLogLines = 1000;

        #endregion

        #region public methods
        public void SaveV2RayCoreVersionList(List<string> versions)
        {
            // clone version list
            userSettings.V2RayCoreDownloadVersionList =
                new List<string>(versions);
            LazySaveUserSettings();
        }

        public ReadOnlyCollection<string> GetV2RayCoreVersionList()
        {
            var result = userSettings.V2RayCoreDownloadVersionList ??
                new List<string> { "v3.48", "v3.47", "v3.46" };
            return result.AsReadOnly();
        }

        // ISettingService thing
        public bool IsShutdown() => isShutdown;

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public string GetPluginsSetting(string pluginName)
        {
            var pluginsSetting = DeserializePluginsSetting();

            if (pluginsSetting != null
                && pluginsSetting.ContainsKey(pluginName))
            {
                return pluginsSetting[pluginName];
            }
            return null;
        }

        public void SavePluginsSetting(string pluginName, string value)
        {
            if (string.IsNullOrEmpty(pluginName))
            {
                return;
            }

            var pluginsSetting = DeserializePluginsSetting();
            pluginsSetting[pluginName] = value;

            try
            {
                userSettings.PluginsSetting =
                    JsonConvert.SerializeObject(pluginsSetting);
            }
            catch { }
            LazySaveUserSettings();
        }

        private Dictionary<string, string> DeserializePluginsSetting()
        {
            var empty = new Dictionary<string, string>();
            Dictionary<string, string> pluginsSetting = null;

            try
            {
                pluginsSetting = JsonConvert
                    .DeserializeObject<Dictionary<string, string>>(
                        userSettings.PluginsSetting);
            }
            catch { }
            if (pluginsSetting == null)
            {
                pluginsSetting = empty;
            }

            return pluginsSetting;
        }

        readonly object saveUserSettingsLocker = new object();
        public void SaveUserSettingsNow()
        {
            lock (saveUserSettingsLocker)
            {
                if (userSettings.isPortable)
                {
                    DebugSendLog("Try save settings to file.");
                    SaveUserSettingsToFile();
                    return;
                }

                DebugSendLog("Try save settings to properties");
                SetUserSettingFileIsPortableToFalse();
                SaveUserSettingsToProperties();
            }
        }

        /*
         * string something;
         * if(something == null){} // Boom!
         */
        Lib.Sys.CancelableTimeout lazyGCTimer = null;
        public void LazyGC()
        {
            // Create on demand.
            if (lazyGCTimer == null)
            {
                lazyGCTimer = new Lib.Sys.CancelableTimeout(
                    () => GC.Collect(),
                    VgcApis.Models.Consts.Intervals.LazyGCDelay);
            }

            lazyGCTimer.Start();
        }

        public void SaveServerTrackerSetting(Model.Data.ServerTracker serverTrackerSetting)
        {
            userSettings.ServerTracker =
                JsonConvert.SerializeObject(serverTrackerSetting);
            LazySaveUserSettings();
        }

        public Model.Data.ServerTracker GetServerTrackerSetting()
        {
            var empty = new Model.Data.ServerTracker();
            Model.Data.ServerTracker r = null;
            try
            {
                r = JsonConvert
                    .DeserializeObject<Model.Data.ServerTracker>(
                        userSettings.ServerTracker);

                if (r.serverList == null)
                {
                    r.serverList = new List<string>();
                }
            }
            catch
            {
                return empty;
            }
            return r ?? empty;
        }

        public List<VgcApis.Models.Datas.CoreInfo> LoadCoreInfoList()
        {
            var empty = new List<VgcApis.Models.Datas.CoreInfo>();

            List<VgcApis.Models.Datas.CoreInfo> list = null;
            try
            {
                list = JsonConvert
                    .DeserializeObject<List<VgcApis.Models.Datas.CoreInfo>>(
                        userSettings.CoreInfoList);
            }
            catch { }

            if (list == null)
            {
                return empty;
            }

            // make sure every config of server can be parsed correctly
            return list.Where(s =>
            {
                try
                {
                    return JObject.Parse(s.config) != null;
                }
                catch { }
                return false;
            }).ToList();
        }

        public void SaveFormRect(Form form)
        {
            var key = form.GetType().Name;
            var list = GetWinFormRectList();

            list[key] = new Rectangle(form.Left, form.Top, form.Width, form.Height);
            userSettings.WinFormPosList = JsonConvert.SerializeObject(list);
            LazySaveUserSettings();
        }

        public void RestoreFormRect(Form form)
        {
            var key = form.GetType().Name;
            var list = GetWinFormRectList();

            if (!list.ContainsKey(key))
            {
                return;
            }

            var rect = list[key];
            var screen = Screen.PrimaryScreen.WorkingArea;

            form.Width = Math.Max(rect.Width, 300);
            form.Height = Math.Max(rect.Height, 200);
            form.Left = Lib.Utils.Clamp(rect.Left, 0, screen.Right - form.Width);
            form.Top = Lib.Utils.Clamp(rect.Top, 0, screen.Bottom - form.Height);
        }

        public List<Model.Data.ImportItem> GetGlobalImportItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.ImportItem>>(
                        userSettings.ImportUrls);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.ImportItem>();
        }

        public void SaveGlobalImportItems(string options)
        {
            userSettings.ImportUrls = options;
            LazySaveUserSettings();
        }

        public List<Model.Data.PluginInfoItem> GetPluginInfoItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.PluginInfoItem>>(
                        userSettings.PluginInfoItems);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.PluginInfoItem>();
        }

        /// <summary>
        /// Feel free to pass null.
        /// </summary>
        /// <param name="itemList"></param>
        public void SavePluginInfoItems(
            List<Model.Data.PluginInfoItem> itemList)
        {
            string json = JsonConvert.SerializeObject(
                itemList ?? new List<Model.Data.PluginInfoItem>());

            userSettings.PluginInfoItems = json;
            LazySaveUserSettings();
        }

        public List<Model.Data.SubscriptionItem> GetSubscriptionItems()
        {
            try
            {
                var items = JsonConvert
                    .DeserializeObject<List<Model.Data.SubscriptionItem>>(
                        userSettings.SubscribeUrls);

                if (items != null)
                {
                    return items;
                }
            }
            catch { };
            return new List<Model.Data.SubscriptionItem>();
        }

        public void SaveSubscriptionItems(string options)
        {
            userSettings.SubscribeUrls = options;
            LazySaveUserSettings();
        }

        public void SaveServerList(List<VgcApis.Models.Datas.CoreInfo> coreInfoList)
        {
            string json = JsonConvert.SerializeObject(
                coreInfoList ?? new List<VgcApis.Models.Datas.CoreInfo>());

            userSettings.CoreInfoList = json;
            LazySaveUserSettings();
        }
        #endregion

        #region private method
        void SetUserSettingFileIsPortableToFalse()
        {
            var filename = Lib.Utils.GetUserSettingsFullFileName();
            if (!File.Exists(filename))
            {
                DebugSendLog("setting file not exists");
                return;
            }

            try
            {
                var s = JsonConvert
                    .DeserializeObject<Model.Data.UserSettings>(
                    File.ReadAllText(filename));

                DebugSendLog("Read setting file for unset portable");

                if (s.isPortable)
                {
                    s.isPortable = false;
                    DebugSendLog("Write setting file for unset portable");
                    File.WriteAllText(filename, JsonConvert.SerializeObject(s));
                }

                DebugSendLog("unset portable done");
            }
            catch
            {
                if (!isShutdown)
                {
                    // this is important do not use task
                    var msg = string.Format(I18N.UnsetPortableModeFail, filename);
                    MessageBox.Show(msg);
                }
            }
        }

        void SaveUserSettingsToProperties()
        {
            try
            {
                Properties.Settings.Default.UserSettings =
                    JsonConvert.SerializeObject(userSettings);
                Properties.Settings.Default.Save();
            }
            catch
            {
                DebugSendLog("Save user settings to Properties fail!");
            }
        }

        void SaveUserSettingsToFile()
        {
            var filename = Lib.Utils.GetUserSettingsFullFileName();
            try
            {
                var content = JsonConvert.SerializeObject(userSettings);
                File.WriteAllText(filename, content);
            }
            catch
            {
                if (!isShutdown)
                {
                    // this is important do not use task!
                    MessageBox.Show(I18N.SaveUserSettingsToFileFail);
                }
            }
        }

        Model.Data.UserSettings LoadUserSettingsFromPorperties()
        {
            try
            {
                var us = JsonConvert
                    .DeserializeObject<Model.Data.UserSettings>(
                        Properties.Settings.Default.UserSettings);

                if (us != null)
                {
                    DebugSendLog("Read user settings from Properties.Usersettings");
                    return us;
                }
            }
            catch { }

            return new Model.Data.UserSettings();
        }

        Model.Data.UserSettings LoadUserSettingsFromFile()
        {
            DebugSendLog("Try read setting from file");
            var filename = Lib.Utils.GetUserSettingsFullFileName();
            if (File.Exists(filename))
            {
                try
                {
                    var result = JsonConvert
                        .DeserializeObject<Model.Data.UserSettings>(
                            File.ReadAllText(filename));
                    return result.isPortable ? result : null;
                }
                catch { }
            }
            return null;
        }

        Model.Data.UserSettings LoadUserSettings()
        {
            return LoadUserSettingsFromFile() ?? LoadUserSettingsFromPorperties();
        }

        Lib.Sys.CancelableTimeout lazySaveUserSettingsTimer = null;
        void LazySaveUserSettings()
        {
            if (lazySaveUserSettingsTimer == null)
            {
                lazySaveUserSettingsTimer = new Lib.Sys.CancelableTimeout(
                    SaveUserSettingsNow,
                    VgcApis.Models.Consts.Intervals.LazySaveUserSettingsDelay);
            }

            lazySaveUserSettingsTimer.Start();
        }

        Dictionary<string, Rectangle> winFormRectListCache = null;
        Dictionary<string, Rectangle> GetWinFormRectList()
        {
            if (winFormRectListCache != null)
            {
                return winFormRectListCache;
            }

            try
            {
                winFormRectListCache = JsonConvert
                    .DeserializeObject<Dictionary<string, Rectangle>>(
                        userSettings.WinFormPosList);
            }
            catch { }

            if (winFormRectListCache == null)
            {
                winFormRectListCache = new Dictionary<string, Rectangle>();
            }

            return winFormRectListCache;
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            lazyGCTimer?.Release();
            lazySaveUserSettingsTimer?.Release();
            SaveUserSettingsNow();
            qLogger.Dispose();
        }
        #endregion

        #region debug
        void DebugSendLog(string content)
        {
#if DEBUG
            SendLog($"(Debug) {content}");
#endif
        }
        #endregion
    }
}
