using System.Collections.Generic;

namespace V2RayGCon.Model.Data
{
    class UserSettings
    {
        #region public properties

        // FormDownloadCore
        public bool isDownloadWin32V2RayCore { get; set; } = true;
        public List<string> V2RayCoreDownloadVersionList = null;

        public int ServerPanelPageSize { get; set; }
        public bool isEnableStat { get; set; } = false;
        public bool isUseV4Format { get; set; }
        public bool CfgShowToolPanel { get; set; }
        public bool isPortable { get; set; }
        public bool isUpdateToVgcFull { get; set; }
        public bool isUpdateUseProxy { get; set; }

        public string ImportUrls { get; set; }
        public string DecodeCache { get; set; }
        public string SubscribeUrls { get; set; }

        public string PluginInfoItems { get; set; }
        public string PluginsSetting { get; set; }

        public string Culture { get; set; }
        public string ServerList { get; set; }
        public string PacServerSettings { get; set; }
        public string SysProxySetting { get; set; }
        public string ServerTracker { get; set; }
        public string WinFormPosList { get; set; }
        #endregion

        public UserSettings()
        {
            ServerPanelPageSize = 7;

#if V2RAYGCON_LITE
            isUpdateToVgcFull = false;
#else
            isUpdateToVgcFull = true;
#endif

            isUpdateUseProxy = false;
            isUseV4Format = true;
            CfgShowToolPanel = true;
            isPortable = true;

            ImportUrls = string.Empty;
            DecodeCache = string.Empty;
            SubscribeUrls = string.Empty;

            PluginInfoItems = string.Empty;
            PluginsSetting = string.Empty;

            Culture = string.Empty;
            ServerList = string.Empty;
            PacServerSettings = string.Empty;
            SysProxySetting = string.Empty;
            ServerTracker = string.Empty;
            WinFormPosList = string.Empty;
        }
    }
}
