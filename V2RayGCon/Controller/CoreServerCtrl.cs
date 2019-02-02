using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class CoreServerCtrl : VgcApis.Models.Interfaces.ICoreCtrl
    {
        Service.Cache cache;
        Service.Servers servers;
        Service.Setting setting;
        Service.Core coreServ;

        public event EventHandler
            OnPropertyChanged,
            OnRequireStatusBarUpdate,
            OnRequireMenuUpdate,
            OnRequireNotifierUpdate,
            OnCoreClosing;

        /// <summary>
        /// false: stop true: start
        /// </summary>
        public event EventHandler<VgcApis.Models.Datas.BoolEvent> OnRequireKeepTrack;

        Views.WinForms.FormSingleServerLog logForm = null;
        VgcApis.Models.Datas.CoreInfo coreInfo;

        public CoreServerCtrl(
            VgcApis.Models.Datas.CoreInfo coreInfo)
        {
            this.coreInfo = coreInfo;
            isCoreRunning = false;
            curStatusCache = string.Empty;
            speedTestResult = -1;
        }

        public void Run(
             Service.Cache cache,
             Service.Setting setting,
             Service.Servers servers)
        {
            this.cache = cache;
            this.servers = servers;
            this.setting = setting;

            coreServ = new Service.Core(setting);
            coreServ.OnLog += OnLogHandler;
            coreServ.OnCoreStatusChanged += OnCoreStateChangedHandler;
        }

        #region properties
        public long speedTestResult { get; private set; }

        string _curStatusCache = "";
        public string curStatusCache
        {
            get => _curStatusCache;
            private set
            {
                SetPropertyOnDemand(ref _curStatusCache, value);
            }
        }

        ConcurrentQueue<string> _logCache = new ConcurrentQueue<string>();
        public string logCache
        {
            get
            {
                return string.Join(Environment.NewLine, _logCache);
            }
            private set
            {
                _logCache.Enqueue(value);

                int maxLogLines = Service.Setting.maxLogLines;
                VgcApis.Libs.Utils.TrimDownConcurrentQueue(
                    _logCache, maxLogLines, maxLogLines / 3);
            }
        }

        int statsPort;
        bool isCoreRunning = false;
        #endregion

        #region ICoreCtrl interface
        public bool IsCoreRunning() => isCoreRunning;
        public bool IsInjectImport() => coreInfo.isInjectImport;
        public bool IsInjectSkipCnSite() => coreInfo.isInjectSkipCNSite;
        public bool IsUntrack() => coreInfo.isUntrack;
        public bool IsSelected() => coreInfo.isSelected;
        public bool IsAutoRun() => coreInfo.isAutoRun;

        public int GetFoldingLevel() => coreInfo.foldingLevel;
        public double GetIndex() => coreInfo.index;
        public string GetName() => coreInfo.name;
        public string GetStatus() => curStatusCache;
        public string GetConfig() => coreInfo.config;
        public int GetCustomInbType() => coreInfo.customInbType;
        public string GetMark() => coreInfo.customMark;
        public long GetSpeedTestResult() => speedTestResult;
        public string GetSummary() => coreInfo.summary;

        public void SetFoldingLevel(int level) =>
            SetPropertyOnDemand(ref coreInfo.foldingLevel, level);

        public string GetCustomInbAddr()
        {
            return $"{coreInfo.inbIp}:{coreInfo.inbPort}";
        }


        public string GetTitle()
        {
            var ci = coreInfo;
            var result = $"{ci.index}.[{ci.name}] {ci.summary}";
            return Lib.Utils.CutStr(result, 60);
        }

        public string GetCustomMark() => coreInfo.customMark;

        public void SetCustomMark(string mark)
        {
            if (coreInfo.customMark == mark)
            {
                return;
            }

            coreInfo.customMark = mark;
            servers.UpdateMarkList(mark);
            InvokeEventOnPropertyChange();
        }

        public void SetCustomInbType(int type)
        {
            if (coreInfo.customInbType == type)
            {
                return;
            }

            coreInfo.customInbType = Lib.Utils.Clamp(
                type, 0, Model.Data.Table.customInbTypeNames.Length);

            InvokeEventOnPropertyChange();
            if (isCoreRunning)
            {
                RestartCoreThen();
            }
        }

        public void SetCustomInbAddr(string ip, int port)
        {
            var changed = false;

            if (ip != coreInfo.inbIp)
            {
                coreInfo.inbIp = ip;
                changed = true;
            }

            if (port != coreInfo.inbPort)
            {
                coreInfo.inbPort = port;
                changed = true;

            }

            if (changed)
            {
                InvokeEventOnPropertyChange();
            }
        }

        public void ChangeCoreConfig(string newConfig)
        {
            if (coreInfo.config == newConfig)
            {
                return;
            }

            coreInfo.config = newConfig;
            InvokeEventOnPropertyChange();
            UpdateSummaryThen(() =>
            {
                InvokeEventOnRequireMenuUpdate();
            });

            if (coreServ.isRunning)
            {
                RestartCoreThen();
            }
        }

        public void SetIsSelected(bool selected)
        {
            if (selected == coreInfo.isSelected)
            {
                return;
            }
            coreInfo.isSelected = selected;
            InvokeEventOnRequireStatusBarUpdate();
            InvokeEventOnPropertyChange();
        }

        readonly object genUidLocker = new object();
        public string GetUid()
        {
            lock (genUidLocker)
            {
                if (string.IsNullOrEmpty(coreInfo.uid))
                {
                    var uidList = servers
                        .GetServerList()
                        .Select(s => s.GetRawUid())
                        .ToList();

                    string newUid;
                    do
                    {
                        newUid = Lib.Utils.RandomHex(16);
                    } while (uidList.Contains(newUid));

                    coreInfo.uid = newUid;
                    InvokeEventOnPropertyChange();
                }
            }
            return coreInfo.uid;
        }

        public void RestartCoreAsync() => RestartCoreThen();
        public void StopCoreAsync() => StopCoreThen();
        public void RunSpeedTestAsync() =>
            Task.Factory.StartNew(RunSpeedTest);

        public void RunSpeedTest()
        {
            void log(string msg)
            {
                SendLog(msg);
                curStatusCache = msg;
            }

            var port = Lib.Utils.GetFreeTcpPort();
            var config = PrepareSpeedTestConfig(port);

            if (string.IsNullOrEmpty(config))
            {
                log(I18N.DecodeImportFail);
                return;
            }

            var url = StrConst.SpeedTestUrl;
            var text = I18N.Testing;
            log(text);
            SendLog(url);

            var speedTester = new Service.Core(setting)
            {
                title = GetTitle()
            };
            speedTester.OnLog += OnLogHandler;
            speedTester.RestartCore(config);

            this.speedTestResult = Lib.Utils.VisitWebPageSpeedTest(url, port);

            text = string.Format("{0}",
                speedTestResult < long.MaxValue ?
                speedTestResult.ToString() + "ms" :
                I18N.Timeout);

            log(text);
            speedTester.StopCore();
            speedTester.OnLog -= OnLogHandler;
        }

        public void RestartCore()
        {
            AutoResetEvent done = new AutoResetEvent(false);
            RestartCoreThen(() => done.Set());
            done.WaitOne();
        }

        public void StopCore()
        {
            AutoResetEvent done = new AutoResetEvent(false);
            StopCoreThen(() => done.Set());
            done.WaitOne();
        }

        public VgcApis.Models.Datas.StatsSample TakeStatisticsSample()
        {
            if (!setting.isEnableStatistics
                || this.statsPort <= 0)
            {
                return null;
            }

            var up = this.coreServ.QueryStatsApi(this.statsPort, true);
            var down = this.coreServ.QueryStatsApi(this.statsPort, false);
            return new VgcApis.Models.Datas.StatsSample(up, down);
        }
        #endregion

        #region public method
        public VgcApis.Models.Datas.CoreInfo GetCoreInfo() => coreInfo;

        public bool IsSuitableToBeUsedAsSysProxy(
            bool isGlobal,
            out bool isSocks,
            out int port)
        {
            isSocks = false;
            port = 0;

            var inboundInfo = GetParsedInboundInfo();
            if (inboundInfo == null)
            {
                SendLog(I18N.GetInboundInfoFail);
                return false;
            }

            var protocol = inboundInfo.Item1;
            port = inboundInfo.Item3;

            if (!IsProtocolMatchProxyRequirment(isGlobal, protocol))
            {
                return false;
            }

            isSocks = protocol == "socks";
            return true;
        }

        public bool ShowLogForm()
        {
            if (logForm != null)
            {
                return false;
            }
            logForm = new Views.WinForms.FormSingleServerLog(this);

            logForm.FormClosed += (s, a) =>
            {
                logForm.Dispose();
                logForm = null;
            };
            return true;
        }

        public long logTimeStamp { get; private set; } = DateTime.Now.Ticks;

        public void ToggleIsInjectSkipCnSite()
        {
            ToggleBoolPropertyOnDemand(ref coreInfo.isInjectSkipCNSite, true);
        }

        public void ToggleIsAutoRun() =>
            ToggleBoolPropertyOnDemand(ref coreInfo.isAutoRun);

        public void ToggleIsUntrack() =>
            ToggleBoolPropertyOnDemand(ref coreInfo.isUntrack);

        public void ToggleIsInjectImport()
        {
            ToggleBoolPropertyOnDemand(ref coreInfo.isInjectImport, true);
            UpdateSummaryThen(() => InvokeEventOnRequireMenuUpdate());
        }

        public void UpdateSummaryThen(Action lambda = null)
        {
            Task.Factory.StartNew(() =>
            {
                var configString = coreInfo.isInjectImport ?
                    InjectGlobalImport(coreInfo.config, false, true) :
                    coreInfo.config;
                try
                {
                    UpdateSummary(
                        servers.ParseImport(configString));
                }
                catch
                {
                    UpdateSummary(JObject.Parse(configString));
                }

                // update summary should not clear status
                // this.status = string.Empty;
                InvokeEventOnPropertyChange();
                lambda?.Invoke();
            });
        }

        public void CleanupThen(Action next)
        {
            OnCoreClosing?.Invoke(this, EventArgs.Empty);
            this.coreServ.StopCoreThen(() =>
            {
                this.coreServ.OnLog -= OnLogHandler;
                this.coreServ.OnCoreStatusChanged -= OnCoreStateChangedHandler;
                Task.Factory.StartNew(() =>
                {
                    next?.Invoke();
                });
            });
        }

        public void StopCoreThen(Action next = null)
        {
            OnCoreClosing?.Invoke(this, EventArgs.Empty);
            Task.Factory.StartNew(() => coreServ.StopCoreThen(
                () =>
                {
                    OnRequireNotifierUpdate?.Invoke(this, EventArgs.Empty);
                    OnRequireKeepTrack?.Invoke(this, new VgcApis.Models.Datas.BoolEvent(false));
                    next?.Invoke();
                }));
        }

        public void RestartCoreThen(Action next = null)
        {
            Task.Factory.StartNew(() => RestartCoreWorker(next));
        }

        public void GetterInboundInfoFor(Action<string> next)
        {
            var serverName = coreInfo.name;
            Task.Factory.StartNew(() =>
            {
                var inInfo = GetParsedInboundInfo();
                if (inInfo == null)
                {
                    next(string.Format("[{0}]", serverName));
                    return;
                }
                if (string.IsNullOrEmpty(inInfo.Item2))
                {
                    next(string.Format("[{0}] {1}", serverName, inInfo.Item1));
                    return;
                }
                next(string.Format("[{0}] {1}://{2}:{3}",
                    serverName,
                    inInfo.Item1,
                    inInfo.Item2,
                    inInfo.Item3));
            });
        }

        public void InvokeEventOnPropertyChange()
        {
            // things happen while invoking
            try
            {
                OnPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        public void SetIndexQuiet(double index) => SetIndexWorker(index, true);

        public void SetIndex(double index) => SetIndexWorker(index, false);

        public bool GetterInfoFor(Func<string[], bool> filter)
        {
            var ci = coreInfo;
            return filter(new string[] {
                // index 0
                ci.name+ci.summary,

                // index 1
                GetInProtocolNameByNumber(ci.customInbType)
                +ci.inbIp
                +ci.inbPort.ToString(),

                // index 2
                ci.customMark??"",
            });
        }
        #endregion

        #region private method
        void ToggleBoolPropertyOnDemand(ref bool property, bool requireRestart = false)
        {
            property = !property;

            // refresh UI immediately
            InvokeEventOnPropertyChange();

            // time consuming things
            if (requireRestart && isCoreRunning)
            {
                RestartCoreThen();
            }
        }

        string InjectGlobalImport(string config, bool isIncludeSpeedTest, bool isIncludeActivate)
        {
            JObject import = Lib.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                isIncludeSpeedTest,
                isIncludeActivate);

            Lib.Utils.MergeJson(ref import, JObject.Parse(config));
            return import.ToString();
        }

        string PrepareSpeedTestConfig(int port)
        {
            var empty = string.Empty;
            if (port <= 0)
            {
                return empty;
            }

            var config = GetDecodedConfig(true, true, false);

            if (config == null)
            {
                return empty;
            }

            if (!OverwriteInboundSettings(
                ref config,
                (int)Model.Data.Enum.ProxyTypes.HTTP,
                "127.0.0.1",
                port))
            {
                return empty;
            }

            return config.ToString(Formatting.None);
        }

        /// <summary>
        /// return Tuple(protocol, ip, port)
        /// </summary>
        /// <returns></returns>
        Tuple<string, string, int> GetParsedInboundInfo()
        {
            var protocol = GetInProtocolNameByNumber(coreInfo.customInbType);
            var ip = coreInfo.inbIp;
            var port = coreInfo.inbPort;

            if (protocol != "config")
            {
                return new Tuple<string, string, int>(protocol, ip, port);
            }

            var parsedConfig = GetDecodedConfig(true, false, true);
            if (parsedConfig == null)
            {
                return null;
            }

            string prefix = "inbound";
            foreach (var p in new string[] { "inbound", "inbounds.0" })
            {
                prefix = p;
                protocol = Lib.Utils.GetValue<string>(parsedConfig, prefix, "protocol");
                if (!string.IsNullOrEmpty(protocol))
                {
                    break;
                }
            }

            ip = Lib.Utils.GetValue<string>(parsedConfig, prefix, "listen");
            port = Lib.Utils.GetValue<int>(parsedConfig, prefix, "port");
            return new Tuple<string, string, int>(protocol, ip, port);
        }

        string GetInProtocolNameByNumber(int typeNumber)
        {
            var table = Model.Data.Table.customInbTypeNames;
            return table[Lib.Utils.Clamp(typeNumber, 0, table.Length)];
        }

        void SetIndexWorker(double index, bool quiet)
        {
            if (Lib.Utils.AreEqual(coreInfo.index, index))
            {
                return;
            }

            coreInfo.index = index;
            this.coreServ.title = GetTitle();
            if (!quiet)
            {
                InvokeEventOnPropertyChange();
            }
        }

        bool IsProtocolMatchProxyRequirment(bool isGlobalProxy, string protocol)
        {
            if (isGlobalProxy && protocol != "http")
            {
                return false;
            }

            if (protocol != "socks" && protocol != "http")
            {
                return false;
            }

            return true;
        }

        void RestartCoreWorker(Action next)
        {
            JObject cfg = GetDecodedConfig(true, false, true);
            if (cfg == null)
            {
                StopCoreThen(next);
                return;
            }

            if (!OverwriteInboundSettings(
                ref cfg,
                coreInfo.customInbType,
                coreInfo.inbIp,
                coreInfo.inbPort))
            {
                StopCoreThen(next);
                return;
            }

            InjectSkipCnSiteSettingsIntoConfig(ref cfg);
            InjectStatsSettingsIntoConfig(ref cfg);

            // debug
            var configStr = cfg.ToString(Formatting.Indented);

            coreServ.title = GetTitle();
            coreServ.RestartCoreThen(
                cfg.ToString(),
                () =>
                {
                    OnRequireNotifierUpdate?.Invoke(this, EventArgs.Empty);
                    OnRequireKeepTrack?.Invoke(this, new VgcApis.Models.Datas.BoolEvent(true));
                    next?.Invoke();
                },
                Lib.Utils.GetEnvVarsFromConfig(cfg));
        }

        void InjectStatsSettingsIntoConfig(ref JObject config)
        {
            if (!setting.isEnableStatistics)
            {
                return;
            }

            statsPort = Lib.Utils.GetFreeTcpPort();
            if (statsPort <= 0)
            {
                return;
            }

            var result = cache.tpl.LoadTemplate("statsApiV4Inb") as JObject;
            result["inbounds"][0]["port"] = statsPort;
            Lib.Utils.CombineConfig(ref result, config);
            result["inbounds"][0]["tag"] = "agentin";

            var statsTpl = cache.tpl.LoadTemplate("statsApiV4Tpl") as JObject;
            Lib.Utils.CombineConfig(ref result, statsTpl);
            config = result;
        }

        void InjectSkipCnSiteSettingsIntoConfig(ref JObject config)
        {
            if (!coreInfo.isInjectSkipCNSite)
            {
                return;
            }

            // 优先考虑兼容旧配置。
            servers.InjectSkipCnSiteSettingsIntoConfig(
                ref config,
                false);
        }

        bool SetPropertyOnDemandWorker<T>(ref T property, T value)
        {
            bool changed = false;
            if (!EqualityComparer<T>.Default.Equals(property, value))
            {
                property = value;
                InvokeEventOnPropertyChange();
                changed = true;
            }
            return changed;
        }

        JObject GetDecodedConfig(bool isUseCache, bool isIncludeSpeedTest, bool isIncludeActivate)
        {
            var coreConfig = coreInfo.config;
            JObject decodedConfig = null;

            try
            {
                string injectedConfig;
                if (coreInfo.isInjectImport)
                {
                    injectedConfig = InjectGlobalImport(
                        coreInfo.config,
                        isIncludeSpeedTest,
                        isIncludeActivate);
                }
                else
                {
                    injectedConfig = coreConfig;
                }
                decodedConfig = servers.ParseImport(injectedConfig);
                cache.core[coreConfig] = decodedConfig.ToString(Formatting.None);
            }
            catch { }

            if (decodedConfig == null)
            {
                SendLog(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        decodedConfig = JObject.Parse(cache.core[coreConfig]);
                    }
                    catch (KeyNotFoundException) { }
                    SendLog(I18N.UsingDecodeCache);
                }
            }

            return decodedConfig;
        }

        bool NeedToStopCoreFirst()
        {
            if (!isCoreRunning)
            {
                return false;
            }

            if (coreInfo.customInbType != (int)Model.Data.Enum.ProxyTypes.HTTP
                && coreInfo.customInbType != (int)Model.Data.Enum.ProxyTypes.SOCKS)
            {
                return false;

            }

            Task.Factory.StartNew(() => MessageBox.Show(I18N.StopServerFirst));
            return true;
        }

        bool OverwriteInboundSettings(
            ref JObject config,
            int inboundType,
            string ip,
            int port)
        {
            switch (inboundType)
            {
                case (int)Model.Data.Enum.ProxyTypes.HTTP:
                case (int)Model.Data.Enum.ProxyTypes.SOCKS:
                    break;
                default:
                    return true;
            }

            var protocol = GetInProtocolNameByNumber(inboundType);
            var part = protocol + "In";
            try
            {
                JObject o = CreateInboundSetting(inboundType, ip, port, protocol, part);
                ReplaceInboundSetting(ref config, o);
#if DEBUG
                var debug = config.ToString(Formatting.Indented);
#endif
                return true;
            }
            catch
            {
                SendLog(I18N.CoreCantSetLocalAddr);
            }
            return false;
        }

        private void ReplaceInboundSetting(ref JObject config, JObject o)
        {
            // Bug. Stream setting will mess things up.
            // Lib.Utils.MergeJson(ref config, o);
            var hasInbound = Lib.Utils.GetKey(config, "inbound") != null;
            var hasInbounds = Lib.Utils.GetKey(config, "inbounds.0") != null;
            var isUseV4 = !hasInbound && (hasInbounds || setting.isUseV4);

            if (isUseV4)
            {
                if (!hasInbounds)
                {
                    config["inbounds"] = JArray.Parse(@"[{}]");
                }
                config["inbounds"][0] = o;
            }
            else
            {
                config["inbound"] = o;
            }
        }

        private JObject CreateInboundSetting(int inboundType, string ip, int port, string protocol, string part)
        {
            var o = JObject.Parse(@"{}");
            o["tag"] = "agentin";
            o["protocol"] = protocol;
            o["listen"] = ip;
            o["port"] = port;
            o["settings"] = cache.tpl.LoadTemplate(part);

            if (inboundType == (int)Model.Data.Enum.ProxyTypes.SOCKS)
            {
                o["settings"]["ip"] = ip;
            }

            return o;
        }

        bool SetPropertyOnDemand(ref string property, string value) =>
           SetPropertyOnDemandWorker(ref property, value);

        bool SetPropertyOnDemand<T>(ref T property, T value)
            where T : struct =>
            SetPropertyOnDemandWorker(ref property, value);

        void SendLog(string message)
        {
            logCache = message;
            try
            {
                setting.SendLog($"[{coreInfo.name}] {message}");
            }
            catch { }
            logTimeStamp = DateTime.Now.Ticks;
        }

        void OnLogHandler(object sender, VgcApis.Models.Datas.StrEvent arg)
        {
            SendLog(arg.Data);
        }

        void InvokeEventOnRequireStatusBarUpdate()
        {
            OnRequireStatusBarUpdate?.Invoke(this, EventArgs.Empty);
        }

        void InvokeEventOnRequireMenuUpdate()
        {
            OnRequireMenuUpdate?.Invoke(this, EventArgs.Empty);
        }

        void UpdateSummary(JObject config)
        {
            coreInfo.name = Lib.Utils.GetAliasFromConfig(config);
            coreInfo.summary = Lib.Utils.GetSummaryFromConfig(config);
        }

        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            isCoreRunning = coreServ.isRunning;
            if (!isCoreRunning)
            {
                statsPort = 0;
            }
            InvokeEventOnPropertyChange();
        }

        #endregion


        #region protected methods
        protected string GetRawUid() => coreInfo.uid;
        #endregion
    }
}
