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
        ConfigMgr configMgr;

        ServersComponents.LinkImporter linkImporter;
        ServersComponents.QueryHandler queryHandler;

        public event EventHandler
            OnCoreStart, // ICoreServCtrl sender
            OnCoreClosing, // ICoreServCtrl sender

            OnRequireNotifyTextUpdate,
            OnRequireMenuUpdate,
            OnRequireStatusBarUpdate,
            OnRequireFlyPanelUpdate,
            OnRequireFlyPanelReload;

        List<Controller.CoreServerCtrl> coreServList =
            new List<Controller.CoreServerCtrl>();

        List<string> markList = null;

        VgcApis.Libs.Tasks.LazyGuy serverSaver;
        readonly object serverListWriteLock = new object();
        VgcApis.Libs.Tasks.Bar speedTestingBar = new VgcApis.Libs.Tasks.Bar();

        Servers()
        {
            serverSaver = new VgcApis.Libs.Tasks.LazyGuy(
                SaveCurrentServerList,
                VgcApis.Models.Consts.Intervals.SaveServerListIntreval);
        }

        public void Run(
           Setting setting,
           Cache cache,
           ConfigMgr configMgr)
        {
            this.configMgr = configMgr;
            this.cache = cache;
            this.setting = setting;
            InitServerCtrlList();

            linkImporter = new ServersComponents.LinkImporter(
                setting, this, configMgr, serverSaver);

            queryHandler = new ServersComponents.QueryHandler(
                serverListWriteLock,
                coreServList);
        }

        #region querys
        public ReadOnlyCollection<VgcApis.Models.Interfaces.ICoreServCtrl>
          GetRunningServers() =>
          queryHandler.GetRunningServers();

        public ReadOnlyCollection<VgcApis.Models.Interfaces.ICoreServCtrl>
           GetAllServersOrderByIndex() =>
           queryHandler.GetAllServersOrderByIndex();

        public ReadOnlyCollection<VgcApis.Models.Interfaces.ICoreServCtrl>
            GetTrackableServerList() =>
            queryHandler.GetTrackableServerList();

        #endregion

        #region event relay

        void NotifierTextUpdateHandler(object sender, EventArgs args) =>
            InvokeEventOnRequireNotifyTextUpdateIgnoreError();

        void InvokeEventHandlerIgnoreError(EventHandler handler, object sender, EventArgs args)
        {
            try
            {
                handler?.Invoke(sender, args);
            }
            catch { }
        }

        void InvokeEventOnRequireNotifyTextUpdateIgnoreError() =>
            InvokeEventHandlerIgnoreError(OnRequireNotifyTextUpdate, this, EventArgs.Empty);

        void InvokeEventOnCoreStartIgnoreError(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnCoreStart, sender, EventArgs.Empty);

        void InvokeEventOnCoreClosingIgnoreError(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnCoreClosing, sender, EventArgs.Empty);

        void InvokeEventOnRequireStatusBarUpdate(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnRequireStatusBarUpdate, null, EventArgs.Empty);

        void InvokeEventOnRequireMenuUpdate(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnRequireMenuUpdate, null, EventArgs.Empty);

        public void InvokeEventOnRequireFlyPanelUpdate() =>
            InvokeEventHandlerIgnoreError(OnRequireFlyPanelUpdate, this, EventArgs.Empty);

        public void InvokeEventOnRequireFlyPanelReload() =>
            InvokeEventHandlerIgnoreError(OnRequireFlyPanelReload, this, EventArgs.Empty);

        void ServerItemPropertyChangedHandler(object sender, EventArgs arg) =>
            serverSaver.DoItLater();

        void OnTrackCoreStartHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, true);

        void OnTrackCoreStopHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, false);

        void BindEventsTo(Controller.CoreServerCtrl server)
        {
            server.OnCoreClosing += InvokeEventOnCoreClosingIgnoreError;

            server.OnTrackCoreStart += OnTrackCoreStartHandler;
            server.OnTrackCoreStop += OnTrackCoreStopHandler;

            server.OnPropertyChanged += ServerItemPropertyChangedHandler;
            server.OnRequireMenuUpdate += InvokeEventOnRequireMenuUpdate;
            server.OnRequireStatusBarUpdate += InvokeEventOnRequireStatusBarUpdate;
            server.OnRequireNotifierUpdate += NotifierTextUpdateHandler;
        }

        void ReleaseEventsFrom(Controller.CoreServerCtrl server)
        {
            server.OnCoreClosing -= InvokeEventOnCoreClosingIgnoreError;

            server.OnTrackCoreStart -= OnTrackCoreStartHandler;
            server.OnTrackCoreStop -= OnTrackCoreStopHandler;

            server.OnPropertyChanged -= ServerItemPropertyChangedHandler;
            server.OnRequireMenuUpdate -= InvokeEventOnRequireMenuUpdate;
            server.OnRequireStatusBarUpdate -= InvokeEventOnRequireStatusBarUpdate;
            server.OnRequireNotifierUpdate -= NotifierTextUpdateHandler;
        }
        #endregion

        #region server tracking

        void ServerTrackingUpdateWorker(
            Controller.CoreServerCtrl coreServCtrl,
            bool isStart)
        {
            var curTrackerSetting =
                configMgr.GenCurTrackerSetting(
                    coreServList.AsReadOnly(),
                    coreServCtrl.GetConfiger().GetConfig(),
                    isStart);

            setting.SaveServerTrackerSetting(curTrackerSetting);
            return;
        }

        Lib.Sys.CancelableTimeout lazyServerTrackingTimer = null;
        void DoServerTrackingLater(Action onTimeout)
        {
            lazyServerTrackingTimer?.Release();
            lazyServerTrackingTimer = null;
            lazyServerTrackingTimer = new Lib.Sys.CancelableTimeout(onTimeout, 2000);
            lazyServerTrackingTimer.Start();
        }

        void TrackCoreRunningStateHandler(object sender, bool isCoreStart)
        {
            // for plugins
            if (isCoreStart)
            {
                InvokeEventOnCoreStartIgnoreError(sender, EventArgs.Empty);
            }
            else
            {
                InvokeEventOnCoreClosingIgnoreError(sender, EventArgs.Empty);
            }

            if (!setting.isServerTrackerOn)
            {
                return;
            }

            var server = sender as Controller.CoreServerCtrl;
            if (server.GetCoreStates().IsUntrack())
            {
                return;
            }

            DoServerTrackingLater(
                () => ServerTrackingUpdateWorker(
                    server, isCoreStart));
        }
        #endregion

        #region public method
        /// <summary>
        /// linkList=List(string[]{0: text, 1: mark}>)
        /// </summary>
        /// <param name="linkList"></param>
        /// <param name="includingV2rayLinks"></param>
        public void ImportLinksBatchMode(
            IEnumerable<string[]> linkList,
            bool includingV2rayLinks) =>
            linkImporter.ImportLinksBatchMode(
                linkList, includingV2rayLinks);

        public void ImportLinkWithOutV2RayLinks(string text) =>
            linkImporter.ImportLinkWithOutV2RayLinks(text);

        public void ImportLinkWithV2RayLinks(string text) =>
            linkImporter.ImportLinkWithV2RayLinks(text);

        /// <summary>
        /// return -1 when fail
        /// </summary>
        /// <returns></returns>
        public int GetAvailableHttpProxyPort()
        {
            var list = GetAllServersOrderByIndex()
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

        public void UpdateTrackerSettingNow()
        {
            var fakeCtrl = new Controller.CoreServerCtrl(
                new VgcApis.Models.Datas.CoreInfo());
            ServerTrackingUpdateWorker(fakeCtrl, false);
        }

        public void Cleanup()
        {
            setting.isServerTrackerOn = false;
            serverSaver.DoItNow();
            serverSaver.Quit();
            lazyServerTrackingTimer?.Release();

            AutoResetEvent sayGoodbye = new AutoResetEvent(false);
            StopAllServersThen(() => sayGoodbye.Set());
            sayGoodbye.WaitOne();
        }

        public int CountSelectedServers() =>
            coreServList.Count(s => s.GetCoreStates().IsSelected());

        public int CountAllServers() => coreServList.Count;

        public void SetAllServerIsSelected(bool isSelected)
        {
            coreServList
                .Select(s =>
                {
                    s.GetCoreStates().SetIsSelected(isSelected);
                    return true;
                })
                .ToList();
        }

        public ReadOnlyCollection<string> GetMarkList()
        {
            if (this.markList == null)
            {
                UpdateMarkList();
            }
            return markList.AsReadOnly();
        }

        public void AddNewMark(string newMark)
        {
            if (!markList.Contains(newMark))
            {
                UpdateMarkList();
            }
        }

        public void UpdateMarkList()
        {
            markList = coreServList
                .Select(s => s.GetCoreStates().GetCustomMark())
                .Distinct()
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        public void RestartServersWithImportMark()
        {
            var list = coreServList
                .Where(s => s.GetCoreStates().IsInjectImport() && s.GetCoreCtrl().IsCoreRunning())
                .OrderBy(s => s.GetCoreStates().GetIndex())
                .ToList();

            RestartServersThen(list);
        }

        public bool IsEmpty()
        {
            return !(this.coreServList.Any());
        }

        public bool IsSelecteAnyServer()
        {
            return coreServList.Any(s => s.GetCoreStates().IsSelected());
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


            JObject package = configMgr.GenV4ServersPackage(servList, packageName);

            var newConfig = package.ToString(Formatting.None);
            string newUid = ReplaceOrAddNewServer(orgUid, newConfig);

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
                    var package = configMgr.ExtractOutboundInfoFromConfig(
                        server.GetConfiger().GetConfig(), id, port, index, tagPrefix);
                    Lib.Utils.UnionJson(ref packages, package);
                    var vnext = configMgr.GenVnextConfigPart(index, port, id);
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
            if (!speedTestingBar.Install())
            {
                return false;
            }

            var list = queryHandler.GetSelectedServers(false);

            Task.Factory.StartNew(() =>
            {
                Lib.Utils.ExecuteInParallel(list, (server) =>
                {
                    server.GetCoreCtrl().RunSpeedTest();

                    // ExecuteInParallel require a return value
                    return "";
                });

                speedTestingBar.Remove();
                MessageBox.Show(I18N.SpeedTestFinished);
            });

            return true;
        }

        public void RestartServersThen(
            IEnumerable<VgcApis.Models.Interfaces.ICoreServCtrl> servers,
            Action done = null)
        {
            var list = servers.ToList();
            void worker(int index, Action next)
            {
                list[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(list.Count, worker, done);
        }

        public void WakeupServersInBootList()
        {
            List<Controller.CoreServerCtrl> bootList = configMgr.GenServersBootList(coreServList);

            void worker(int index, Action next)
            {
                bootList[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(bootList.Count, worker);
        }

        public void RestartSelectedServersThen(Action done = null)
        {
            void worker(int index, Action next)
            {
                if (coreServList[index].GetCoreStates().IsSelected())
                {
                    coreServList[index].GetCoreCtrl().RestartCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Lib.Utils.ChainActionHelperAsync(coreServList.Count, worker, done);
        }

        public void StopSelectedServersThen(Action lambda = null)
        {
            void worker(int index, Action next)
            {
                if (coreServList[index].GetCoreStates().IsSelected())
                {
                    coreServList[index].GetCoreCtrl().StopCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Lib.Utils.ChainActionHelperAsync(coreServList.Count, worker, lambda);
        }

        public void StopAllServersThen(Action lambda = null)
        {
            void worker(int index, Action next)
            {
                coreServList[index].GetCoreCtrl().StopCoreThen(next);
            }

            Lib.Utils.ChainActionHelperAsync(coreServList.Count, worker, lambda);
        }

        public void DeleteSelectedServersThen(Action done = null)
        {
            if (!speedTestingBar.Install())
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            void worker(int index, Action next)
            {
                if (!coreServList[index].GetCoreStates().IsSelected())
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

                speedTestingBar.Remove();

                done?.Invoke();
            }

            Lib.Utils.ChainActionHelperAsync(coreServList.Count, worker, finish);
        }

        public void DeleteAllServersThen(Action done = null)
        {
            if (!speedTestingBar.Install())
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

                speedTestingBar.Remove();
                done?.Invoke();
            }

            Lib.Utils.ChainActionHelperAsync(
                coreServList.Count,
                RemoveServerItemFromListThen,
                finish);
        }

        public void UpdateAllServersSummary()
        {
            void worker(int index, Action next)
            {
                try
                {
                    coreServList[index].GetConfiger().UpdateSummaryThen(next);
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

            Lib.Utils.ChainActionHelperAsync(coreServList.Count, worker, done);
        }

        public void DeleteServerByConfig(string config)
        {
            if (!speedTestingBar.Install())
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            var index = GetServerIndexByConfig(config);
            if (index < 0)
            {
                MessageBox.Show(I18N.CantFindOrgServDelFail);
                speedTestingBar.Remove();
                return;
            }

            Task.Factory.StartNew(
                () => RemoveServerItemFromListThen(index, () =>
                {
                    NotifierTextUpdateHandler(this, EventArgs.Empty);
                    serverSaver.DoItLater();
                    UpdateMarkList();
                    InvokeEventOnRequireMenuUpdate(coreServList, EventArgs.Empty);
                    InvokeEventOnRequireFlyPanelUpdate();
                    speedTestingBar.Remove();
                }));
        }

        public bool IsServerExist(string config)
        {
            return coreServList.Any(s => s.GetConfiger().GetConfig() == config);
        }

        public bool AddServer(string config, string mark, bool quiet = false)
        {
            // duplicate
            if (IsServerExist(config))
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
                coreServList.Add(newServer);
            }

            newServer.Run(cache, setting, configMgr, this);
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

            coreServList[index].GetConfiger().SetConfig(newConfig);
            return true;
        }

        public string ReplaceOrAddNewServer(string orgUid, string newConfig)
        {
            var servUid = "";

            var orgServ = coreServList.FirstOrDefault(s => s.GetCoreStates().GetUid() == orgUid);
            if (orgServ != null)
            {
                ReplaceServerConfig(orgServ.GetConfiger().GetConfig(), newConfig);
                servUid = orgUid;
            }
            else
            {
                AddServer(newConfig, "PackageV4");
                var newServ = coreServList.FirstOrDefault(s => s.GetConfiger().GetConfig() == newConfig);
                if (newServ != null)
                {
                    servUid = newServ.GetCoreStates().GetUid();
                }
            }

            return servUid;
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
                    coreServList.Add(server);
                }
            }

            foreach (var server in coreServList)
            {
                server.Run(cache, setting, configMgr, this);
                BindEventsTo(server);
            }
        }

        int GetServerIndexByConfig(string config)
        {
            for (int i = 0; i < coreServList.Count; i++)
            {
                var coreCfg = coreServList[i].GetConfiger().GetConfig();
                if (coreCfg == config)
                {
                    return i;
                }
            }
            return -1;
        }

        void SaveCurrentServerList()
        {
            lock (serverListWriteLock)
            {
                var coreInfoList = coreServList
                    .Select(s => s.GetCoreStates().GetAllRawCoreInfo())
                    .ToList();
                setting.SaveServerList(coreInfoList);
            }
        }

        void RemoveServerItemFromListThen(int index, Action next = null)
        {
            var server = coreServList[index];
            Task.Run(() =>
            {
                lock (serverListWriteLock)
                {
                    ReleaseEventsFrom(server);
                    server.Dispose();
                    coreServList.RemoveAt(index);
                }
                next?.Invoke();
            });
        }

        #endregion

        #region debug
#if DEBUG
        public void DbgFastRestartTest(int round)
        {
            var list = coreServList.ToList();
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
