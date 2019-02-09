using ProxySetter.Resources.Langs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxySetter.Services
{
    class ServerTracker
    {
        PsSettings setting;
        PacServer pacServer;

        VgcApis.Models.IServices.IServersService servers;

        public event EventHandler OnSysProxyChanged;
        bool isTracking { get; set; }

        public ServerTracker()
        {
            isTracking = false;
        }

        #region public method
        public void Run(
            PsSettings setting,
            PacServer pacServer,
            VgcApis.Models.IServices.IServersService servers)
        {
            this.setting = setting;
            this.pacServer = pacServer;
            this.servers = servers;

            Restart();
        }

        public void Restart()
        {
            var bs = setting.GetBasicSetting();
            var isStartPacServer = bs.isAlwaysStartPacServ;
            var isStartTracker = bs.isAutoUpdateSysProxy;

            switch ((Model.Data.Enum.SystemProxyModes)bs.sysProxyMode)
            {
                case Model.Data.Enum.SystemProxyModes.Global:
                    Lib.Sys.ProxySetter.SetGlobalProxy(bs.proxyPort);
                    break;
                case Model.Data.Enum.SystemProxyModes.PAC:
                    isStartPacServer = true;
                    Lib.Sys.ProxySetter.SetPacProxy(pacServer.GetPacUrl());
                    break;
                default:
                    isStartTracker = false;
                    break;
            }

            if (isStartPacServer)
            {
                pacServer.Reload();
            }
            else
            {
                pacServer.StopPacServer();
            }

            if (isStartTracker)
            {
                StartTracking();
            }
            else
            {
                StopTracking();
            }
        }

        public void Cleanup()
        {
            lazyProxyUpdateTimer?.Release();
            StopTracking();
        }
        #endregion

        #region private method
        void InvokeOnSysProxyChange()
        {
            try
            {
                OnSysProxyChanged?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        Lib.Sys.CancelableTimeout lazyProxyUpdateTimer = null;
        void WakeupLazyProxyUpdater()
        {
            if (lazyProxyUpdateTimer == null)
            {
                lazyProxyUpdateTimer =
                    new Lib.Sys.CancelableTimeout(
                        LazyProxyUpdater, 2000);
            }
            lazyProxyUpdateTimer.Start();
        }

        void SearchForAvailableProxyServer(
            bool isGlobal,
            List<VgcApis.Models.Interfaces.ICoreServCtrl> serverList)
        {
            foreach (var serv in serverList)
            {
                if (serv.GetConfiger().IsSuitableToBeUsedAsSysProxy(
                   isGlobal, out bool isSocks, out int port))
                {
                    UpdateSysProxySetting(
                        serv.GetCoreStates().GetTitle(),
                        isSocks,
                        port);
                    return;
                }
            }
            setting.SendLog(I18N.NoServerCapableOfSysProxy);
        }

        bool IsProxySettingChanged(bool isSocks, int port)
        {
            var bs = setting.GetBasicSetting();
            var isGlobal = bs.sysProxyMode == (int)Model.Data.Enum.SystemProxyModes.Global;

            if (!isGlobal)
            {
                var curPacProtoIsSocks = (bs.pacProtocol == (int)Model.Data.Enum.PacProtocols.SOCKS);
                if (isSocks != curPacProtoIsSocks)
                {
                    return true;
                }
            }

            if (port != bs.proxyPort)
            {
                return true;
            }
            return false;
        }

        void UpdateSysProxySetting(string servTitle, bool isSocks, int port)
        {
            setting.SendLog(I18N.SysProxyChangeTo + " " + servTitle);
            if (!IsProxySettingChanged(isSocks, port))
            {
                setting.SendLog(I18N.SystemProxySettingRemain);
                return;
            }

            var bs = setting.GetBasicSetting();
            bs.proxyPort = VgcApis.Libs.Utils.Clamp(port, 0, 65536);
            if (bs.sysProxyMode == (int)Model.Data.Enum.SystemProxyModes.Global)
            {
                Lib.Sys.ProxySetter.SetGlobalProxy(port);
            }
            else
            {
                bs.pacProtocol =
                    (int)(isSocks ?
                    Model.Data.Enum.PacProtocols.SOCKS :
                    Model.Data.Enum.PacProtocols.HTTP);
                Lib.Sys.ProxySetter.SetPacProxy(pacServer.GetPacUrl());
            }

            setting.SaveBasicSetting(bs);
            setting.SendLog(I18N.SystemProxySettingUpdated);
            InvokeOnSysProxyChange();
        }

        void LazyProxyUpdater()
        {
            var serverList = servers.GetTrackableServerList();
            var isGlobal =
                    setting.GetBasicSetting().sysProxyMode ==
                    (int)Model.Data.Enum.SystemProxyModes.Global;

            var curServ = serverList.FirstOrDefault(s => s.GetConfiger().GetConfig() == curServerConfig);
            if (curServ != null)
            {
                if (curServ.GetConfiger().IsSuitableToBeUsedAsSysProxy(
                    isGlobal, out bool isSocks, out int port))
                {
                    UpdateSysProxySetting(
                        curServ.GetCoreStates().GetTitle(),
                        isSocks,
                        port);
                    return;
                }
            }
            SearchForAvailableProxyServer(isGlobal, serverList.ToList());
        }

        string curServerConfig;
        bool isServerStart;

        void OnCoreStartHandler(object sender, EventArgs args)
        {
            var coreCtrl = sender as VgcApis.Models.Interfaces.ICoreServCtrl;
            curServerConfig = coreCtrl.GetConfiger().GetConfig();
            isServerStart = true;
            WakeupLazyProxyUpdater();
        }

        void OnCoreClosingHandler(object sender, EventArgs args)
        {
            var coreCtrl = sender as VgcApis.Models.Interfaces.ICoreServCtrl;
            curServerConfig = coreCtrl.GetConfiger().GetConfig();
            isServerStart = false;
            WakeupLazyProxyUpdater();
        }

        readonly object trackingLocker = new object();
        void StartTracking()
        {
            lock (trackingLocker)
            {
                if (isTracking)
                {
                    return;
                }

                servers.OnCoreClosing += OnCoreClosingHandler;
                servers.OnCoreStart += OnCoreStartHandler;
                isTracking = true;
            }
            setting.DebugLog("Start tracking.");
        }

        void StopTracking()
        {
            lock (trackingLocker)
            {
                if (!isTracking)
                {
                    return;
                }

                servers.OnCoreClosing -= OnCoreClosingHandler;
                servers.OnCoreStart -= OnCoreStartHandler;

                isTracking = false;
            }
            setting.DebugLog("Stop tracking.");
        }
        #endregion
    }
}
