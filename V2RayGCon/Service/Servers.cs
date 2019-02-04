using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    public class Servers :
        Model.BaseClass.SingletonService<Servers>,
        VgcApis.Models.IServices.IServersService
    {
        Setting setting = null;
        Cache cache = null;

        public event EventHandler<VgcApis.Models.Datas.BoolEvent>
            OnServerStateChange;

        public event EventHandler<VgcApis.Models.Datas.StrEvent>
            OnCoreClosing; // single core closing.

        public event EventHandler
            OnRequireMenuUpdate,
            OnRequireStatusBarUpdate,
            OnRequireFlyPanelUpdate,
            OnRequireFlyPanelReload;

        List<Controller.CoreServerCtrl> serverList = new List<Controller.CoreServerCtrl>();
        List<string> markList = null;

        VgcApis.Libs.Tasks.LazyGuy serverSaver, notifierUpdater;
        readonly object serverListWriteLock = new object();

        Servers()
        {
            isTesting = false;
            serverSaver = new VgcApis.Libs.Tasks.LazyGuy(
                SaveCurrentServerList,
                VgcApis.Models.Consts.Intervals.SaveServerListIntreval);

            notifierUpdater = new VgcApis.Libs.Tasks.LazyGuy(
                UpdateNotifierText,
                VgcApis.Models.Consts.Intervals.NotifierTextUpdateIntreval);
        }

        public void Run(
           Setting setting,
           Cache cache)
        {
            this.cache = cache;
            this.setting = setting;
            InitServerCtrlList();
        }

        #region interface for plugins
        public ReadOnlyCollection<VgcApis.Models.Interfaces.ICoreServCtrl> GetTrackableServerList()
        {
            lock (serverListWriteLock)
            {
                return serverList
                    .Where(s => s.GetCoreCtrl().IsCoreRunning() && !s.GetCoreStates().IsUntrack())
                    .Select(s => s as VgcApis.Models.Interfaces.ICoreServCtrl)
                    .ToList()
                    .AsReadOnly();
            }
        }


        public ReadOnlyCollection<VgcApis.Models.Interfaces.ICoreServCtrl> GetAllServersList()
        {
            lock (serverListWriteLock)
            {
                return serverList
                .Select(s => s as VgcApis.Models.Interfaces.ICoreServCtrl)
                .ToList()
                .AsReadOnly();
            }
        }
        #endregion

        #region property
        bool _isTesting;
        readonly object _isTestingLock = new object();
        bool isTesting
        {
            get
            {
                lock (_isTestingLock)
                {
                    return _isTesting;
                }
            }
            set
            {
                lock (_isTestingLock)
                {
                    _isTesting = value;
                }
            }
        }
        #endregion

        #region private method
        void InitServerCtrlList()
        {
            lock (serverListWriteLock)
            {
                var coreInfoList = setting.LoadCoreInfoList();
                foreach (var coreInfo in coreInfoList)
                {
                    var server = new Controller.CoreServerCtrl(coreInfo);
                    serverList.Add(server);
                }
            }

            foreach (var server in serverList)
            {
                server.Run(cache, setting, this);
                BindEventsTo(server);
            }
        }

        void ImportLinks(string links, bool includeV2RayLinks)
        {
            var taskList = new List<Task<Tuple<bool, List<string[]>>>>();

            taskList.Add(
                new Task<Tuple<bool, List<string[]>>>(
                    () => ImportVmessLinks(links)));


            taskList.Add(
                new Task<Tuple<bool, List<string[]>>>(
                    () => ImportSSLinks(links)));


            if (includeV2RayLinks)
            {
                taskList.Add(
                    new Task<Tuple<bool, List<string[]>>>(
                        () => ImportV2RayLinks(links)));
            }

            var tasks = taskList.ToArray();

            Task.Factory.StartNew(() =>
            {
                foreach (var task in tasks)
                {
                    task.Start();
                }
                Task.WaitAll(tasks);

                var results = GetterImportLinksResult(tasks);
                ShowImportLinksResult(results);
            });
        }

        private string ImportPackageV4Config(string orgUid, JObject package)
        {
            var newConfig = package.ToString(Formatting.None);
            var servUid = "";

            var orgServ = serverList.FirstOrDefault(s => s.GetCoreStates().GetUid() == orgUid);
            if (orgServ != null)
            {
                ReplaceServerConfig(orgServ.GetConfiger().GetConfig(), newConfig);
                servUid = orgUid;
            }
            else
            {
                AddServer(newConfig, "PackageV4");
                var newServ = serverList.FirstOrDefault(s => s.GetConfiger().GetConfig() == newConfig);
                if (newServ != null)
                {
                    servUid = newServ.GetCoreStates().GetUid();
                }
            }

            return servUid;
        }

        private JObject GenPackageV4Config(List<VgcApis.Models.Interfaces.ICoreServCtrl> servList, string packageName)
        {
            var package = cache.tpl.LoadPackage("pkgV4Tpl");
            package["v2raygcon"]["alias"] = string.IsNullOrEmpty(packageName) ? "PackageV4" : packageName;
            var outbounds = package["outbounds"] as JArray;
            var description = "";

            for (var i = 0; i < servList.Count; i++)
            {
                var s = servList[i];
                var parts = Lib.Utils.ExtractOutboundsFromConfig(
                    s.GetConfiger().GetConfig());
                var c = 0;
                foreach (JObject p in parts)
                {
                    p["tag"] = $"agentout{i}s{c++}";
                    outbounds.Add(p);
                }
                var name = s.GetCoreStates().GetName();
                if (c == 0)
                {
                    setting.SendLog(I18N.PackageFail + ": " + name);
                }
                else
                {
                    description += $" {i}.[{name}]";
                    setting.SendLog(I18N.PackageSuccess + ": " + name);
                }
            }
            package["v2raygcon"]["description"] = description;
            return package;
        }

        private List<Controller.CoreServerCtrl> GenBootServerList()
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            if (!trackerSetting.isTrackerOn)
            {
                return serverList.Where(s => s.GetCoreStates().IsAutoRun()).ToList();
            }

            setting.isServerTrackerOn = true;
            var trackList = trackerSetting.serverList;

            var bootList = serverList
                .Where(s => s.GetCoreStates().IsAutoRun()
                || trackList.Contains(s.GetConfiger().GetConfig()))
                .ToList();

            if (string.IsNullOrEmpty(trackerSetting.curServer))
            {
                return bootList;
            }

            bootList.RemoveAll(s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            var lastServer = serverList.FirstOrDefault(
                    s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            if (lastServer != null && !lastServer.GetCoreStates().IsUntrack())
            {
                bootList.Insert(0, lastServer);
            }
            return bootList;
        }

        void BindEventsTo(Controller.CoreServerCtrl server)
        {
            server.OnCoreClosing += InvokeEventOnCoreClosing;

            server.OnTrackCoreStart += OnTrackCoreStartHandler;
            server.OnTrackCoreStop += OnTrackCoreStopHandler;

            server.OnPropertyChanged += ServerItemPropertyChangedHandler;
            server.OnRequireMenuUpdate += InvokeEventOnRequireMenuUpdate;
            server.OnRequireStatusBarUpdate += InvokeEventOnRequireStatusBarUpdate;
            server.OnRequireNotifierUpdate += NotifierTextUpdateHandler;
        }

        void ReleaseEventsFrom(Controller.CoreServerCtrl server)
        {
            server.OnCoreClosing -= InvokeEventOnCoreClosing;

            server.OnTrackCoreStart -= OnTrackCoreStartHandler;
            server.OnTrackCoreStop -= OnTrackCoreStopHandler;

            server.OnPropertyChanged -= ServerItemPropertyChangedHandler;
            server.OnRequireMenuUpdate -= InvokeEventOnRequireMenuUpdate;
            server.OnRequireStatusBarUpdate -= InvokeEventOnRequireStatusBarUpdate;
            server.OnRequireNotifierUpdate -= NotifierTextUpdateHandler;
        }


        List<string> GetHtmlContentFromCache(List<string> urls)
        {
            return urls.Count <= 0 ?
                urls :
                Lib.Utils.ExecuteInParallel<string, string>(
                    urls,
                    (url) => cache.html[url]);
        }


        /// <summary>
        /// update running servers list
        /// </summary>
        /// <param name="includeCurServer"></param>
        Model.Data.ServerTracker GenCurTrackerSetting(string curServer, bool isStart)
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            var tracked = trackerSetting.serverList;

            var running = GetServerList()
                .Where(s => s.GetCoreCtrl().IsCoreRunning()
                    && !s.GetCoreStates().IsUntrack())
                .Select(s => s.GetConfiger().GetConfig())
                .ToList();

            tracked.RemoveAll(c => !running.Any(r => r == c));  // remove stopped
            running.RemoveAll(r => tracked.Any(t => t == r));
            tracked.AddRange(running);
            tracked.Remove(curServer);

            if (isStart)
            {
                trackerSetting.curServer = curServer;
            }
            else
            {
                trackerSetting.curServer = null;
            }

            trackerSetting.serverList = tracked;
            return trackerSetting;
        }

        void InvokeOnServerStateChange(
            object sender,
            VgcApis.Models.Datas.BoolEvent isServerStart)
        {
            try
            {
                OnServerStateChange?.Invoke(sender, isServerStart);
            }
            catch { }
        }

        void LazyServerTrackUpdateWorker(
            Controller.CoreServerCtrl servCtrl,
            bool isStart)
        {
            var curTrackerSetting = GenCurTrackerSetting(servCtrl.GetConfiger().GetConfig(), isStart);
            setting.SaveServerTrackerSetting(curTrackerSetting);
            return;
        }

        int GetServerIndexByConfig(string config)
        {
            for (int i = 0; i < serverList.Count; i++)
            {
                if (serverList[i].GetConfiger().GetConfig() == config)
                {
                    return i;
                }
            }
            return -1;
        }

        List<Controller.CoreServerCtrl> GetSelectedServerList(bool descending = false)
        {
            var list = serverList.Where(s => s.GetCoreStates().IsSelected());
            if (descending)
            {
                return list.OrderByDescending(s => s.GetCoreStates().GetIndex()).ToList();
            }

            return list.OrderBy(s => s.GetCoreStates().GetIndex()).ToList();
        }

        string[] GenImportResult(string link, bool success, string reason, string mark)
        {
            return new string[]
            {
                string.Empty,  // reserve for index
                link,
                mark,
                success?"√":"×",
                reason,
            };
        }

        JObject SSLink2Config(string ssLink)
        {
            Model.Data.Shadowsocks ss = Lib.Utils.SSLink2SS(ssLink);
            if (ss == null)
            {
                return null;
            }

            Lib.Utils.TryParseIPAddr(ss.addr, out string ip, out int port);

            var config = cache.tpl.LoadTemplate("tplImportSS");

            var setting = config["outbound"]["settings"]["servers"][0];
            setting["address"] = ip;
            setting["port"] = port;
            setting["method"] = ss.method;
            setting["password"] = ss.pass;

            return config.DeepClone() as JObject;
        }

        Tuple<bool, List<string[]>> ImportSSLinks(string text, string mark = "")
        {
            var isAddNewServer = false;
            var links = Lib.Utils.ExtractLinks(text, Model.Data.Enum.LinkTypes.ss);
            List<string[]> result = new List<string[]>();

            foreach (var link in links)
            {
                var config = SSLink2Config(link);

                if (config == null)
                {
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                    continue;
                }

                if (AddServer(Lib.Utils.Config2String(config), mark, true))
                {
                    isAddNewServer = true;
                    result.Add(GenImportResult(link, true, I18N.Success, mark));
                }
                else
                {
                    result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        Tuple<bool, List<string[]>> ImportV2RayLinks(string text, string mark = "")
        {
            bool isAddNewServer = false;
            var links = Lib.Utils.ExtractLinks(text, Model.Data.Enum.LinkTypes.v2ray);
            List<string[]> result = new List<string[]>();

            foreach (var link in links)
            {
                try
                {
                    var config = JObject.Parse(
                        Lib.Utils.Base64Decode(
                            Lib.Utils.GetLinkBody(link)));

                    if (config != null)
                    {
                        if (AddServer(Lib.Utils.Config2String(config), mark, true))
                        {
                            isAddNewServer = true;
                            result.Add(GenImportResult(link, true, I18N.Success, mark));
                        }
                        else
                        {
                            result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                        }
                    }
                }
                catch
                {
                    // skip if error occured
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        JToken GenStreamSetting(Model.Data.Vmess vmess)
        {
            // insert stream type
            string[] streamTypes = { "ws", "tcp", "kcp", "h2" };
            string streamType = vmess.net.ToLower();

            if (!streamTypes.Contains(streamType))
            {
                return JToken.Parse(@"{}");
            }

            var streamToken = cache.tpl.LoadTemplate(streamType);
            try
            {
                switch (streamType)
                {
                    case "kcp":
                        streamToken["kcpSettings"]["header"]["type"] = vmess.type;
                        break;
                    case "ws":
                        streamToken["wsSettings"]["path"] = string.IsNullOrEmpty(vmess.v) ? vmess.host : vmess.path;
                        if (vmess.v == "2" && !string.IsNullOrEmpty(vmess.host))
                        {
                            streamToken["wsSettings"]["headers"]["Host"] = vmess.host;
                        }
                        break;
                    case "h2":
                        streamToken["httpSettings"]["path"] = vmess.path;
                        streamToken["httpSettings"]["host"] = Lib.Utils.Str2JArray(vmess.host);
                        break;
                }
            }
            catch { }

            try
            {
                streamToken["security"] =
                    vmess.tls?.ToLower() == "tls" ?
                    "tls" : "none";
            }
            catch { }

            return streamToken;
        }

        JObject Vmess2Config(Model.Data.Vmess vmess)
        {
            if (vmess == null)
            {
                return null;
            }

            // prepare template
            var config = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            config["v2raygcon"]["alias"] = vmess.ps;

            var outVmess = cache.tpl.LoadTemplate("outbVmess");
            outVmess["streamSettings"] = GenStreamSetting(vmess);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vmess.add;
            node["port"] = Lib.Utils.Str2Int(vmess.port);
            node["users"][0]["id"] = vmess.id;
            node["users"][0]["alterId"] = Lib.Utils.Str2Int(vmess.aid);

            var isV4 = setting.isUseV4;
            var inbound = Lib.Utils.CreateJObject(
                (isV4 ? "inbounds.0" : "inbound"),
                cache.tpl.LoadTemplate("inbSimSock"));

            var outbound = Lib.Utils.CreateJObject(
                (isV4 ? "outbounds.0" : "outbound"),
                outVmess);

            Lib.Utils.MergeJson(ref config, inbound);
            Lib.Utils.MergeJson(ref config, outbound);
            return config.DeepClone() as JObject;
        }

        Tuple<bool, List<string[]>> ImportVmessLinks(string text, string mark = "")
        {
            var links = Lib.Utils.ExtractLinks(text, Model.Data.Enum.LinkTypes.vmess);
            var result = new List<string[]>();
            var isAddNewServer = false;

            foreach (var link in links)
            {
                var vmess = Lib.Utils.VmessLink2Vmess(link);
                var config = Vmess2Config(vmess);

                if (config == null)
                {
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                    continue;
                }

                if (AddServer(Lib.Utils.Config2String(config), mark, true))
                {
                    result.Add(GenImportResult(link, true, I18N.Success, mark));
                    isAddNewServer = true;
                }
                else
                {
                    result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        void SaveCurrentServerList()
        {
            lock (serverListWriteLock)
            {
                var coreInfoList = serverList
                    .Select(s => s.GetCoreStates().GetAllRawCoreInfo())
                    .ToList();
                setting.SaveServerList(coreInfoList);
            }
        }

        void NotifierTextUpdateHandler(object sender, EventArgs args) =>
            notifierUpdater.DoItLater();

        void UpdateNotifierText()
        {
            var list = serverList
                .Where(s => s.GetCoreCtrl().IsCoreRunning())
                .OrderBy(s => s.GetCoreStates().GetIndex())
                .ToList();

            var count = list.Count;

            if (count <= 0 || count > 2)
            {
                setting.runningServerSummary = count <= 0 ?
                    I18N.Description :
                    count.ToString() + I18N.ServersAreRunning;

                setting.InvokeEventIgnoreErrorOnRequireNotifyTextUpdate();
                return;
            }

            var texts = new List<string>();

            void done()
            {
                setting.runningServerSummary = string.Join(Environment.NewLine, texts);
                setting.InvokeEventIgnoreErrorOnRequireNotifyTextUpdate();
            }

            void worker(int index, Action next)
            {
                list[index].GetConfiger().GetterInboundInfoThen(s =>
                {
                    texts.Add(s);
                    next?.Invoke();
                });
            }

            Lib.Utils.ChainActionHelperAsync(count, worker, done);
        }

        void InvokeEventOnCoreClosing(object sender, EventArgs args)
        {
            try
            {
                var coreCtrl = sender as Controller.CoreServerCtrl;
                var uid = coreCtrl.GetCoreStates().GetUid();
                OnCoreClosing?.Invoke(null, new VgcApis.Models.Datas.StrEvent(uid));
            }
            catch { }
        }

        void InvokeEventOnRequireStatusBarUpdate(object sender, EventArgs args)
        {
            try
            {
                OnRequireStatusBarUpdate?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        void InvokeEventOnRequireMenuUpdate(object sender, EventArgs args)
        {
            try
            {
                OnRequireMenuUpdate?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        void ServerItemPropertyChangedHandler(object sender, EventArgs arg) =>
            serverSaver.DoItLater();


        void RemoveServerItemFromListThen(int index, Action next = null)
        {
            var server = serverList[index];
            Task.Run(() =>
            {
                lock (serverListWriteLock)
                {
                    ReleaseEventsFrom(server);
                    server.Dispose();
                    serverList.RemoveAt(index);
                }
                next?.Invoke();
            });
        }

        JObject ExtractOutboundInfoFromConfig(string configString, string id, int portBase, int index, string tagPrefix)
        {
            var pkg = cache.tpl.LoadPackage("package");
            var config = ParseImport(configString);

            var tagin = tagPrefix + "in" + index.ToString();
            var tagout = tagPrefix + "out" + index.ToString();
            var port = portBase + index;

            pkg["routing"]["settings"]["rules"][0]["inboundTag"][0] = tagin;
            pkg["routing"]["settings"]["rules"][0]["outboundTag"] = tagout;

            pkg["inboundDetour"][0]["port"] = port;
            pkg["inboundDetour"][0]["tag"] = tagin;
            pkg["inboundDetour"][0]["settings"]["port"] = port;
            pkg["inboundDetour"][0]["settings"]["clients"][0]["id"] = id;

            pkg["outboundDetour"][0]["protocol"] = config["outbound"]["protocol"];
            pkg["outboundDetour"][0]["tag"] = tagout;
            pkg["outboundDetour"][0]["settings"] = config["outbound"]["settings"];
            pkg["outboundDetour"][0]["streamSettings"] = config["outbound"]["streamSettings"];

            return pkg;
        }

        JObject GenVnextConfigPart(int index, int basePort, string id)
        {
            var vnext = cache.tpl.LoadPackage("vnext");
            vnext["outbound"]["settings"]["vnext"][0]["port"] = basePort + index;
            vnext["outbound"]["settings"]["vnext"][0]["users"][0]["id"] = id;
            return vnext;
        }

        private void ShowImportLinksResult(Tuple<bool, List<string[]>> results)
        {
            var isAddNewServer = results.Item1;
            var allResults = results.Item2;

            if (isAddNewServer)
            {
                UpdateAllServersSummary();
                serverSaver.DoItLater();
            }

            setting.LazyGC();

            if (allResults.Count > 0)
            {
                new Views.WinForms.FormImportLinksResult(allResults);
                Application.Run();
            }
            else
            {
                MessageBox.Show(I18N.NoLinkFound);
            }
        }

        private static Tuple<bool, List<string[]>> GetterImportLinksResult(Task<Tuple<bool, List<string[]>>>[] tasks)
        {
            var allResults = new List<string[]>();
            var isAddNewServer = false;
            foreach (var task in tasks)
            {
                isAddNewServer = isAddNewServer || task.Result.Item1;
                allResults.AddRange(task.Result.Item2);
                task.Dispose();
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, allResults);
        }

        Lib.Sys.CancelableTimeout lazyServerTrackerTimer = null;
        public void SetLazyServerTrackerUpdater(Action onTimeout)
        {
            lazyServerTrackerTimer?.Release();
            lazyServerTrackerTimer = null;
            lazyServerTrackerTimer = new Lib.Sys.CancelableTimeout(onTimeout, 2000);
            lazyServerTrackerTimer.Start();
        }

        void OnTrackCoreStartHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, true);

        void OnTrackCoreStopHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, false);

        void TrackCoreRunningStateHandler(object sender, bool isCoreStart)
        {
            // for plugins
            InvokeOnServerStateChange(
                sender,
                new VgcApis.Models.Datas.BoolEvent(isCoreStart));

            if (!setting.isServerTrackerOn)
            {
                return;
            }

            var server = sender as Controller.CoreServerCtrl;
            if (server.GetCoreStates().IsUntrack())
            {
                return;
            }

            SetLazyServerTrackerUpdater(
                () => LazyServerTrackUpdateWorker(
                    server, isCoreStart));
        }
        #endregion

        #region public method
        /// <summary>
        /// return -1 when fail
        /// </summary>
        /// <returns></returns>
        public int GetAvailableHttpProxyPort()
        {
            var list = GetServerList()
                .Where(s => s.GetCoreCtrl().IsCoreRunning());

            foreach (var serv in list)
            {
                if (serv.GetConfiger().IsSuitableToBeUsedAsSysProxy(
                    true, out bool isSocks, out int port))
                {
                    return port;
                }
            }
            return -1;
        }


        /// <summary>
        /// ref means config will change after the function is executed.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public void InjectSkipCnSiteSettingsIntoConfig(
            ref JObject config,
            bool useV4)
        {
            var c = JObject.Parse(@"{}");

            var dict = new Dictionary<string, string> {
                { "dns","dnsCFnGoogle" },
                { "routing",GetRoutingTplName(config, useV4) },
            };

            foreach (var item in dict)
            {
                var tpl = Lib.Utils.CreateJObject(item.Key);
                var value = cache.tpl.LoadExample(item.Value);
                tpl[item.Key] = value;

                if (!Lib.Utils.Contains(config, tpl))
                {
                    c[item.Key] = value;
                }
            }

            // put dns/routing settings in front of user settings
            Lib.Utils.CombineConfig(ref config, c);

            // put outbounds after user settings
            var hasOutbounds = Lib.Utils.GetKey(config, "outbounds") != null;
            var hasOutDtr = Lib.Utils.GetKey(config, "outboundDetour") != null;

            var outboundTag = "outboundDetour";
            if (!hasOutDtr && (hasOutbounds || useV4))
            {
                outboundTag = "outbounds";
            }

            var o = Lib.Utils.CreateJObject(
                outboundTag,
                cache.tpl.LoadExample("outDtrFreedom"));

            if (!Lib.Utils.Contains(config, o))
            {
                Lib.Utils.CombineConfig(ref o, config);
                config = o;
            }
        }

        private static string GetRoutingTplName(JObject config, bool useV4)
        {
            var routingRules = Lib.Utils.GetKey(config, "routing.rules");
            var routingSettingsRules = Lib.Utils.GetKey(config, "routing.settings.rules");
            var hasRoutingV4 = routingRules == null ? false : (routingRules is JArray);
            var hasRoutingV3 = routingSettingsRules == null ? false : (routingSettingsRules is JArray);

            var isUseRoutingV4 = !hasRoutingV3 && (useV4 || hasRoutingV4);
            return isUseRoutingV4 ? "routeCnipV4" : "routeCNIP";
        }

        public void UpdateTrackerSettingNow()
        {
            var fakeCtrl = new Controller.CoreServerCtrl(
                new VgcApis.Models.Datas.CoreInfo());
            LazyServerTrackUpdateWorker(fakeCtrl, false);
        }

        /*
         * exceptions  
         * test<FormatException> base64 decode fail
         * test<System.Net.WebException> url not exist
         * test<Newtonsoft.Json.JsonReaderException> json decode fail
         */
        public JObject ParseImport(string configString)
        {
            var maxDepth = Lib.Utils.Str2Int(StrConst.ParseImportDepth);

            var result = Lib.Utils.ParseImportRecursively(
                GetHtmlContentFromCache,
                JObject.Parse(configString),
                maxDepth);

            try
            {
                Lib.Utils.RemoveKeyFromJObject(result, "v2raygcon.import");
            }
            catch (KeyNotFoundException)
            {
                // do nothing;
            }

            return result;
        }

        public void Cleanup()
        {
            setting.isServerTrackerOn = false;
            serverSaver.DoItNow();
            serverSaver.Quit();
            notifierUpdater.Quit();
            lazyServerTrackerTimer?.Release();

            AutoResetEvent sayGoodbye = new AutoResetEvent(false);
            StopAllServersThen(() => sayGoodbye.Set());
            sayGoodbye.WaitOne();
        }

        public int GetTotalSelectedServerCount()
        {
            return serverList.Count(s => s.GetCoreStates().IsSelected());
        }

        public int GetTotalServerCount()
        {
            return serverList.Count;
        }

        public void InvokeEventOnRequireFlyPanelUpdate()
        {
            try
            {
                OnRequireFlyPanelUpdate?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        public void InvokeEventOnRequireFlyPanelReload()
        {
            try
            {
                OnRequireFlyPanelReload?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        public ReadOnlyCollection<string> GetMarkList()
        {
            if (this.markList == null)
            {
                UpdateMarkList();
            }
            return markList.AsReadOnly();
        }

        public void SetAllServerIsSelected(bool isSelected)
        {
            serverList
                .Select(s =>
                {
                    s.GetCoreStates().SetIsSelected(isSelected);
                    return true;
                })
                .ToList();
        }

        public void UpdateMarkList(string newMark)
        {
            if (!markList.Contains(newMark))
            {
                UpdateMarkList();
            }
        }

        public void UpdateMarkList()
        {
            markList = serverList
                .Select(s => s.GetCoreStates().GetCustomMark())
                .Distinct()
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        public void RestartInjectImportServers()
        {
            var list = serverList
                .Where(s => s.GetCoreStates().IsInjectImport() && s.GetCoreCtrl().IsCoreRunning())
                .OrderBy(s => s.GetCoreStates().GetIndex())
                .ToList();

            RestartServersByListThen(list);
        }

        public ReadOnlyCollection<Controller.CoreServerCtrl> GetServerList()
        {
            return serverList.OrderBy(s => s.GetCoreStates().GetIndex()).ToList().AsReadOnly();
        }

        public bool IsEmpty()
        {
            return !(this.serverList.Any());
        }

        public void ImportLinks(List<string[]> linkList)
        {
            // linkList:[ linksString, mark] 
            var taskList = new List<Task<Tuple<bool, List<string[]>>>>();

            foreach (var link in linkList)
            {
                taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                    () => ImportVmessLinks(link[0], link[1])));
                taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                    () => ImportSSLinks(link[0], link[1])));
                taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                    () => ImportV2RayLinks(link[0], link[1])));
            }

            var tasks = taskList.ToArray();
            Task.Factory.StartNew(() =>
            {
                foreach (var task in tasks)
                {
                    task.Start();
                }
                Task.WaitAll(tasks);

                var results = GetterImportLinksResult(tasks);
                UpdateMarkList();
                ShowImportLinksResult(results);
            });
        }

        public void ImportLinksWithOutV2RayLinks(string links) =>
            ImportLinks(links, false);

        public void ImportLinksWithV2RayLinks(string links) =>
            ImportLinks(links, true);

        public bool IsSelecteAnyServer()
        {
            return serverList.Any(s => s.GetCoreStates().IsSelected());
        }


        /// <summary>
        /// packageName is Null or empty ? "PackageV4" : packageName
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="servList"></param>
        public string PackServersIntoV4Package(
            List<VgcApis.Models.Interfaces.ICoreServCtrl> servList,
            string orgUid,
            string packageName)
        {
            if (servList == null || servList.Count <= 0)
            {
                VgcApis.Libs.UI.MsgBoxAsync(null, I18N.ListIsEmpty);
                return "";
            }

            JObject package = GenPackageV4Config(servList, packageName);
            string newUid = ImportPackageV4Config(orgUid, package);

            UpdateMarkList();
            setting.SendLog(I18N.PackageDone);
            Lib.UI.ShowMessageBoxDoneAsync();

            return newUid;
        }

        public void PackServersIntoV3Package(
            List<VgcApis.Models.Interfaces.ICoreServCtrl> servList)
        {
            var packages = JObject.Parse(@"{}");
            var serverNameList = new List<string>();

            var id = Guid.NewGuid().ToString();
            var port = Lib.Utils.Str2Int(StrConst.PacmanInitPort);
            var tagPrefix = StrConst.PacmanTagPrefix;

            void done()
            {
                var config = cache.tpl.LoadPackage("main");
                config["v2raygcon"]["description"] = string.Join(" ", serverNameList);
                Lib.Utils.UnionJson(ref config, packages);
                setting.SendLog(I18N.PackageDone);
                AddServer(config.ToString(Formatting.None), "PackageV3");
                UpdateMarkList();
                Lib.UI.ShowMessageBoxDoneAsync();
            }

            void worker(int index, Action next)
            {
                var server = servList[index];
                var name = server.GetCoreStates().GetName();

                try
                {
                    var package = ExtractOutboundInfoFromConfig(
                        server.GetConfiger().GetConfig(), id, port, index, tagPrefix);
                    Lib.Utils.UnionJson(ref packages, package);
                    var vnext = GenVnextConfigPart(index, port, id);
                    Lib.Utils.UnionJson(ref packages, vnext);

                    serverNameList.Add(
                        string.Format("{0}.[{1}]", index, name));

                    setting.SendLog(I18N.PackageSuccess + ": " + name);
                }
                catch
                {
                    setting.SendLog(I18N.PackageFail + ": " + name);
                }
                next();
            }

            Lib.Utils.ChainActionHelperAsync(servList.Count, worker, done);
        }

        public bool RunSpeedTestOnSelectedServers()
        {
            if (isTesting)
            {
                return false;
            }
            isTesting = true;

            var list = GetSelectedServerList(false);

            Task.Factory.StartNew(() =>
            {
                Lib.Utils.ExecuteInParallel<Controller.CoreServerCtrl, bool>(list, (server) =>
                {
                    server.GetCoreCtrl().RunSpeedTest();
                    return true;
                });

                isTesting = false;
                MessageBox.Show(I18N.SpeedTestFinished);
            });

            return true;
        }

        public List<Controller.CoreServerCtrl> GetActiveServerList()
        {
            return serverList.Where(s => s.GetCoreCtrl().IsCoreRunning()).ToList();
        }

        public void RestartServersByListThen(List<Controller.CoreServerCtrl> servers, Action done = null)
        {
            var list = servers;
            void worker(int index, Action next)
            {
                list[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(list.Count, worker, done);
        }

        public void WakeupServers()
        {
            List<Controller.CoreServerCtrl> bootList = GenBootServerList();

            void worker(int index, Action next)
            {
                bootList[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(bootList.Count, worker);
        }

        public void RestartAllSelectedServersThen(Action done = null)
        {
            void worker(int index, Action next)
            {
                if (serverList[index].GetCoreStates().IsSelected())
                {
                    serverList[index].GetCoreCtrl().RestartCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Lib.Utils.ChainActionHelperAsync(serverList.Count, worker, done);
        }

        public void StopAllSelectedThen(Action lambda = null)
        {
            void worker(int index, Action next)
            {
                if (serverList[index].GetCoreStates().IsSelected())
                {
                    serverList[index].GetCoreCtrl().StopCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Lib.Utils.ChainActionHelperAsync(serverList.Count, worker, lambda);
        }

        public void StopAllServersThen(Action lambda = null)
        {
            void worker(int index, Action next)
            {
                serverList[index].GetCoreCtrl().StopCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(serverList.Count, worker, lambda);
        }

        public void DeleteSelectedServersThen(Action done = null)
        {
            if (isTesting)
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            void worker(int index, Action next)
            {
                if (!serverList[index].GetCoreStates().IsSelected())
                {
                    next();
                    return;
                }

                RemoveServerItemFromListThen(index, next);
            }

            void finish()
            {
                NotifierTextUpdateHandler(this, EventArgs.Empty);
                serverSaver.DoItLater();
                UpdateMarkList();
                InvokeEventOnRequireFlyPanelUpdate();
                InvokeEventOnRequireMenuUpdate(this, EventArgs.Empty);
                done?.Invoke();
            }

            Lib.Utils.ChainActionHelperAsync(serverList.Count, worker, finish);
        }

        public void DeleteAllServersThen(Action done = null)
        {
            if (isTesting)
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            void finish()
            {
                serverSaver.DoItLater();
                UpdateMarkList();
                InvokeEventOnRequireFlyPanelUpdate();
                InvokeEventOnRequireMenuUpdate(this, EventArgs.Empty);
                done?.Invoke();
            }

            Lib.Utils.ChainActionHelperAsync(
                serverList.Count,
                RemoveServerItemFromListThen,
                finish);
        }

        public void UpdateAllServersSummary()
        {
            void worker(int index, Action next)
            {
                try
                {
                    serverList[index].GetConfiger().UpdateSummaryThen(next);
                }
                catch
                {
                    // skip if something goes wrong
                    next();
                }
            }

            void done()
            {
                setting.LazyGC();
                serverSaver.DoItLater();
                InvokeEventOnRequireFlyPanelUpdate();
                InvokeEventOnRequireMenuUpdate(this, EventArgs.Empty);
            }

            Lib.Utils.ChainActionHelperAsync(serverList.Count, worker, done);
        }

        public void DeleteServerByConfig(string config)
        {
            if (isTesting)
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            var index = GetServerIndexByConfig(config);
            if (index < 0)
            {
                MessageBox.Show(I18N.CantFindOrgServDelFail);
                return;
            }

            Task.Factory.StartNew(
                () => RemoveServerItemFromListThen(index, () =>
                {
                    NotifierTextUpdateHandler(this, EventArgs.Empty);
                    serverSaver.DoItLater();
                    UpdateMarkList();
                    InvokeEventOnRequireMenuUpdate(serverList, EventArgs.Empty);
                    InvokeEventOnRequireFlyPanelUpdate();
                }));
        }

        public bool IsServerItemExist(string config)
        {
            return serverList.Any(s => s.GetConfiger().GetConfig() == config);
        }

        public bool AddServer(string config, string mark, bool quiet = false)
        {
            // duplicate
            if (IsServerItemExist(config))
            {
                return false;
            }

            var coreInfo = new VgcApis.Models.Datas.CoreInfo
            {
                config = config,
                customMark = mark,
            };

            var newServer = new Controller.CoreServerCtrl(coreInfo);
            lock (serverListWriteLock)
            {
                serverList.Add(newServer);
            }

            newServer.Run(cache, setting, this);
            BindEventsTo(newServer);

            if (!quiet)
            {
                newServer.GetConfiger().UpdateSummaryThen(() =>
                {
                    InvokeEventOnRequireMenuUpdate(this, EventArgs.Empty);
                    InvokeEventOnRequireFlyPanelUpdate();
                });
            }

            setting.LazyGC();
            serverSaver.DoItLater();
            return true;
        }

        public bool ReplaceServerConfig(string orgConfig, string newConfig)
        {
            var index = GetServerIndexByConfig(orgConfig);

            if (index < 0)
            {
                return false;
            }

            serverList[index].GetConfiger().SetConfig(newConfig);
            return true;
        }

        #endregion

        #region speed test public methods

        public long RunSpeedTest(string rawConfig) =>
            SpeedTestWorker(rawConfig, "testing", false, false, false, null);

        public long SpeedTestWorker(
          string rawConfig,
          string title,
          bool isUseCache,
          bool isInjectSpeedTestTpl,
          bool isInjectActivateTpl,
          EventHandler<VgcApis.Models.Datas.StrEvent> logDeliever)
        {
            var port = Lib.Utils.GetFreeTcpPort();
            var speedTestConfig = CreateSpeedTestConfig(
                rawConfig, port, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);

            if (string.IsNullOrEmpty(speedTestConfig))
            {
                logDeliever?.Invoke(this, new VgcApis.Models.Datas.StrEvent(I18N.DecodeImportFail));
                return long.MaxValue;
            }

            return DoSpeedTest(
                speedTestConfig,
                title,
                VgcApis.Models.Consts.Webs.SpeedTestUrl,
                port,
                logDeliever);
        }

        public string InjectImportTpls(
          string config,
          bool isIncludeSpeedTest,
          bool isIncludeActivate)
        {
            JObject import = Lib.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                isIncludeSpeedTest,
                isIncludeActivate);

            Lib.Utils.MergeJson(ref import, JObject.Parse(config));
            return import.ToString();
        }

        public JObject DecodeConfig(
            string rawConfig,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var coreConfig = rawConfig;
            JObject decodedConfig = null;

            try
            {
                string injectedConfig = coreConfig;
                if (isInjectActivateTpl || isInjectSpeedTestTpl)
                {
                    injectedConfig = InjectImportTpls(
                        rawConfig,
                        isInjectSpeedTestTpl,
                        isInjectActivateTpl);
                }

                decodedConfig = ParseImport(injectedConfig);
                cache.core[coreConfig] = decodedConfig.ToString(Formatting.None);
            }
            catch { }

            if (decodedConfig == null)
            {
                setting.SendLog(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        decodedConfig = JObject.Parse(cache.core[coreConfig]);
                    }
                    catch (KeyNotFoundException) { }
                    setting.SendLog(I18N.UsingDecodeCache);
                }
            }

            return decodedConfig;
        }

        public bool ReplaceInboundWithCustomSetting(
            ref JObject config,
            int inbType,
            string ip,
            int port)
        {
            switch (inbType)
            {
                case (int)Model.Data.Enum.ProxyTypes.HTTP:
                case (int)Model.Data.Enum.ProxyTypes.SOCKS:
                    break;

                case (int)Model.Data.Enum.ProxyTypes.Config:
                default:
                    return true;
            }

            var protocol = Lib.Utils.InboundTypeNumberToName(inbType);
            var part = protocol + "In";
            try
            {
                JObject o = CreateInboundSetting(
                    inbType, ip, port, protocol, part);

                ReplaceInboundSetting(ref config, o);
#if DEBUG
                var debug = config.ToString(Formatting.Indented);
#endif
                return true;
            }
            catch
            {
                setting.SendLog(I18N.CoreCantSetLocalAddr);
            }
            return false;
        }

        #endregion

        #region speed test helper functions
        long DoSpeedTest(
            string speedTestableConfig,
            string title,
            string testUrl,
            int testPort,
            EventHandler<VgcApis.Models.Datas.StrEvent> logDeliever)
        {
            var speedTester = new Core(setting)
            {
                title = title
            };

            if (logDeliever != null)
            {
                speedTester.OnLog += logDeliever;
            }
            speedTester.RestartCore(speedTestableConfig);
            long testResult = Lib.Utils.VisitWebPageSpeedTest(testUrl, testPort);
            speedTester.StopCore();
            if (logDeliever != null)
            {
                speedTester.OnLog -= logDeliever;
            }
            return testResult;
        }

        void ReplaceInboundSetting(ref JObject config, JObject o)
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

        JObject CreateInboundSetting(
            int inboundType,
            string ip,
            int port,
            string protocol,
            string part)
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

        string CreateSpeedTestConfig(
            string rawConfig,
            int port,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var empty = string.Empty;
            if (port <= 0)
            {
                return empty;
            }

            var config = DecodeConfig(
                rawConfig, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);

            if (config == null)
            {
                return empty;
            }

            if (!ReplaceInboundWithCustomSetting(
                ref config,
                (int)Model.Data.Enum.ProxyTypes.HTTP,
                "127.0.0.1",
                port))
            {
                return empty;
            }

            return config.ToString(Formatting.None);
        }

        #endregion

        #region debug
#if DEBUG
        public void DbgFastRestartTest(int round)
        {
            var list = serverList.ToList();
            var rnd = new Random();

            var count = list.Count;
            Task.Factory.StartNew(() =>
            {
                var taskList = new List<Task>();
                for (int i = 0; i < round; i++)
                {
                    var index = rnd.Next(0, count);
                    var isStopCore = rnd.Next(0, 2) == 0;
                    var server = list[index];

                    var task = new Task(() =>
                    {
                        AutoResetEvent sayGoodbye = new AutoResetEvent(false);
                        if (isStopCore)
                        {
                            server.GetCoreCtrl().StopCoreThen(() => sayGoodbye.Set());
                        }
                        else
                        {
                            server.GetCoreCtrl().RestartCoreThen(() => sayGoodbye.Set());
                        }
                        sayGoodbye.WaitOne();
                    });

                    taskList.Add(task);
                    task.Start();
                }

                Task.WaitAll(taskList.ToArray());
                MessageBox.Show(I18N.Done);
            });
        }
#endif
        #endregion
    }
}
