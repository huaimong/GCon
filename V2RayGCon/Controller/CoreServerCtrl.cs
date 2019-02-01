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
    public class CoreServerCtrl : VgcApis.Models.IControllers.ICoreCtrl
    {
        [JsonIgnore]
        Service.Cache cache;
        [JsonIgnore]
        Service.Servers servers;
        [JsonIgnore]
        Service.Setting setting;

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

        // private variables will not be serialized
        public string config; // plain text of config.json
        public bool isAutoRun, isInjectImport, isSelected, isInjectSkipCNSite, isUntrack;
        public string name, summary, inboundIP, mark, uid;
        public int overwriteInboundType, inboundPort, foldingLevel;
        public double index;

        public CoreServerCtrl()
        {
            // new server will displays at the bottom
            index = double.MaxValue;

            isSelected = false;
            isUntrack = false;
            isServerOn = false;
            isAutoRun = false;
            isInjectImport = false;

            foldingLevel = 0;

            mark = string.Empty;
            status = string.Empty;
            name = string.Empty;
            summary = string.Empty;
            config = string.Empty;
            uid = string.Empty;
            speedTestResult = -1;

            overwriteInboundType = 1;
            inboundIP = "127.0.0.1";
            inboundPort = 1080;
        }

        public void Run(
             Service.Cache cache,
             Service.Setting setting,
             Service.Servers servers)
        {
            this.cache = cache;
            this.servers = servers;
            this.setting = setting;

            server = new Service.Core(setting);
            server.OnLog += OnLogHandler;
            server.OnCoreStatusChanged += OnCoreStateChangedHandler;
        }

        #region properties

        #endregion

        #region non-serialize properties
        [JsonIgnore]
        int statsPort { get; set; }

        [JsonIgnore]
        public long speedTestResult;

        [JsonIgnore]
        Views.WinForms.FormSingleServerLog logForm = null;

        [JsonIgnore]
        public string status;

        [JsonIgnore]
        public bool isServerOn;

        [JsonIgnore]
        public Service.Core server;

        [JsonIgnore]
        ConcurrentQueue<string> _logCache = new ConcurrentQueue<string>();

        [JsonIgnore]
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
        #endregion

        #region ICoreCtrl interface
        public string GetInboundIpAndPort() => string.Format("{0}:{1}", inboundIP, inboundPort);
        public int GetInboundMode() => overwriteInboundType;
        public string GetMark() => mark;
        public long GetSpeedTestResult() => speedTestResult;
        public string GetSummary() => summary;

        public string GetTitle()
        {
            var result = string.Format("{0}.[{1}] {2}",
                (int)this.index,
                this.name,
                this.summary);
            return Lib.Utils.CutStr(result, 60);
        }

        public void SetMark(string mark)
        {
            if (this.mark == mark)
            {
                return;
            }

            this.mark = mark;
            if (!string.IsNullOrEmpty(mark)
                && !(this.servers.GetMarkList().Contains(mark)))
            {
                this.servers.UpdateMarkList();
            }
            InvokeEventOnPropertyChange();
        }

        public void ChangeInboundMode(int type)
        {
            if (this.overwriteInboundType == type)
            {
                return;
            }

            this.overwriteInboundType = Lib.Utils.Clamp(
                type, 0, Model.Data.Table.inboundOverwriteTypesName.Length);

            InvokeEventOnPropertyChange();
            if (isServerOn)
            {
                // time consuming things
                RestartCoreThen();
            }
        }

        public void ChangeInboundIpAndPort(string ip, int port)
        {
            var changed = false;

            if (ip != this.inboundIP)
            {
                this.inboundIP = ip;
                changed = true;
            }

            if (port != this.inboundPort)
            {
                this.inboundPort = port;
                changed = true;

            }

            if (changed)
            {
                InvokeEventOnPropertyChange();
            }
        }

        public void ChangeConfig(string config)
        {
            if (this.config == config)
            {
                return;
            }

            this.config = config;
            InvokeEventOnPropertyChange();
            UpdateSummaryThen(() =>
            {
                InvokeEventOnRequireMenuUpdate();
            });

            if (server.isRunning)
            {
                RestartCoreThen();
            }
        }

        public void SetIsSelected(bool selected)
        {
            if (selected == isSelected)
            {
                return;
            }
            this.isSelected = selected;
            InvokeEventOnRequireStatusBarUpdate();
            InvokeEventOnPropertyChange();
        }

        public string GetUid()
        {
            if (string.IsNullOrEmpty(uid))
            {
                var uidList = servers
                    .GetServerList()
                    .Select(s => s.uid)
                    .ToList();
                do
                {
                    uid = Lib.Utils.RandomHex(16);
                } while (uidList.Contains(uid));
                InvokeEventOnPropertyChange();
            }
            return uid;
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
                SetPropertyOnDemand(ref status, msg);
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

        public double GetIndex() => this.index;
        public string GetName() => this.name;
        public string GetStatus() => this.status;
        public string GetConfig() => this.config;
        public bool IsCoreRunning() => this.isServerOn;
        public bool IsUntrack() => this.isUntrack;
        public bool IsSelected() => this.isSelected;

        public VgcApis.Models.Datas.StatsSample TakeStatisticsSample()
        {
            if (!setting.isEnableStatistics
                || this.statsPort <= 0)
            {
                return null;
            }

            var up = this.server.QueryStatsApi(this.statsPort, true);
            var down = this.server.QueryStatsApi(this.statsPort, false);
            return new VgcApis.Models.Datas.StatsSample(up, down);
        }
        #endregion

        #region public method

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

        public void SetPropertyOnDemand(ref string property, string value, bool isNeedCoreStopped = false)
        {
            SetPropertyOnDemand<string>(ref property, value, isNeedCoreStopped);
        }

        public void SetPropertyOnDemand(ref int property, int value, bool isNeedCoreStopped = false)
        {
            SetPropertyOnDemand<int>(ref property, value, isNeedCoreStopped);
        }

        public void SetPropertyOnDemand(ref double property, double value, bool isNeedCoreStopped = false)
        {
            SetPropertyOnDemand<double>(ref property, value, isNeedCoreStopped);
        }

        public void SetPropertyOnDemand(ref bool property, bool value, bool isNeedCoreStopped = false)
        {
            SetPropertyOnDemand<bool>(ref property, value, isNeedCoreStopped);
        }

        public void ToggleBoolPropertyOnDemand(ref bool property, bool requireRestart = false)
        {
            property = !property;

            // refresh UI immediately
            InvokeEventOnPropertyChange();

            // time consuming things
            if (requireRestart && isServerOn)
            {
                RestartCoreThen();
            }
        }

        public void ToggleIsInjectImport()
        {
            this.isInjectImport = !this.isInjectImport;

            // refresh UI immediately
            InvokeEventOnPropertyChange();

            // time consuming things
            if (isServerOn)
            {
                RestartCoreThen();
            }

            UpdateSummaryThen(() => InvokeEventOnRequireMenuUpdate());
        }

        public void UpdateSummaryThen(Action lambda = null)
        {
            Task.Factory.StartNew(() =>
            {
                var configString = isInjectImport ?
                    InjectGlobalImport(this.config, false, true) :
                    this.config;
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
            this.server.StopCoreThen(() =>
            {
                this.server.OnLog -= OnLogHandler;
                this.server.OnCoreStatusChanged -= OnCoreStateChangedHandler;
                Task.Factory.StartNew(() =>
                {
                    next?.Invoke();
                });
            });
        }

        public void StopCoreThen(Action next = null)
        {
            OnCoreClosing?.Invoke(this, EventArgs.Empty);
            Task.Factory.StartNew(() => server.StopCoreThen(
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
            var serverName = this.name;
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

        public void ChangeIndex(double index)
        {
            if (Lib.Utils.AreEqual(this.index, index))
            {
                return;
            }

            this.index = index;
            this.server.title = GetTitle();
            InvokeEventOnPropertyChange();
        }

        public bool GetterInfoFor(Func<string[], bool> filter)
        {
            return filter(new string[] {
                // index 0
                name+summary,

                // index 1
                GetInProtocolNameByNumber(overwriteInboundType)
                +inboundIP
                +inboundPort.ToString(),

                // index 2
                this.mark??"",
            });
        }
        #endregion

        #region private method
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
            var protocol = GetInProtocolNameByNumber(overwriteInboundType);
            var ip = inboundIP;
            var port = inboundPort;

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

        static string GetInProtocolNameByNumber(int typeNumber)
        {
            var table = Model.Data.Table.inboundOverwriteTypesName;
            return table[Lib.Utils.Clamp(typeNumber, 0, table.Length)];
        }

        static bool IsProtocolMatchProxyRequirment(bool isGlobalProxy, string protocol)
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
                overwriteInboundType,
                this.inboundIP,
                this.inboundPort))
            {
                StopCoreThen(next);
                return;
            }

            InjectSkipCnSiteSettingsIntoConfig(ref cfg);
            InjectStatsSettingsIntoConfig(ref cfg);

            // debug
            var configStr = cfg.ToString(Formatting.Indented);

            server.title = GetTitle();
            server.RestartCoreThen(
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
            if (!this.isInjectSkipCNSite)
            {
                return;
            }

            // 优先考虑兼容旧配置。
            servers.InjectSkipCnSiteSettingsIntoConfig(
                ref config,
                false);
        }

        void SetPropertyOnDemand<T>(
            ref T property,
            T value,
            bool isNeedCoreStopped)
        {
            if (EqualityComparer<T>.Default.Equals(property, value))
            {
                return;
            }

            if (isNeedCoreStopped)
            {
                if (NeedStopCoreFirst())
                {
                    InvokeEventOnPropertyChange(); // refresh ui
                    return;
                }
            }

            property = value;
            InvokeEventOnPropertyChange();
        }

        JObject GetDecodedConfig(bool isUseCache, bool isIncludeSpeedTest, bool isIncludeActivate)
        {
            JObject cfg = null;
            try
            {
                cfg = servers.ParseImport(
                    isInjectImport ?
                    InjectGlobalImport(config, isIncludeSpeedTest, isIncludeActivate) :
                    config);

                cache.core[config] = cfg.ToString(Formatting.None);

            }
            catch { }

            if (cfg == null)
            {
                SendLog(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        cfg = JObject.Parse(cache.core[config]);
                    }
                    catch (KeyNotFoundException) { }
                    SendLog(I18N.UsingDecodeCache);
                }
            }

            return cfg;
        }

        bool NeedStopCoreFirst()
        {
            if (!isServerOn)
            {
                return false;
            }

            if (overwriteInboundType != (int)Model.Data.Enum.ProxyTypes.HTTP
                && overwriteInboundType != (int)Model.Data.Enum.ProxyTypes.SOCKS)
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

        public long logTimeStamp { get; private set; } = DateTime.Now.Ticks;
        void SendLog(string message)
        {
            logCache = message;
            try
            {
                setting.SendLog($"[{this.name}] {message}");
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
            this.name = Lib.Utils.GetAliasFromConfig(config);
            this.summary = Lib.Utils.GetSummaryFromConfig(config);
        }

        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            isServerOn = server.isRunning;
            if (!isServerOn)
            {
                statsPort = 0;
            }
            InvokeEventOnPropertyChange();
        }

        #endregion
    }
}
