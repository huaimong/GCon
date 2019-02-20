using System;
using System.Threading;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.CoreServerComponent
{
    sealed public class CoreCtrl :
        VgcApis.Models.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Models.Interfaces.CoreCtrlComponents.ICoreCtrl
    {
        Lib.V2Ray.Core coreServ;
        Service.Setting setting;
        Service.Servers servers;
        Service.ConfigMgr configMgr;

        public CoreCtrl(
            Service.Setting setting,
            Service.Servers servers,
            Service.ConfigMgr configMgr)
        {
            this.setting = setting;
            this.servers = servers;
            this.configMgr = configMgr;
        }

        CoreStates coreStates;
        Configer configer;
        Logger logger;
        CoreServerCtrl container;
        public override void Prepare()
        {
            this.coreServ = new V2RayGCon.Lib.V2Ray.Core(setting);

            container = GetContainer();
            coreStates = container.GetComponent<CoreStates>();
            configer = container.GetComponent<Configer>();
            logger = container.GetComponent<Logger>();
        }

        #region public mehtods
        // 非正常终止时调用 
        public void SetTitle(string title) => coreServ.title = title;

        public void BindEvents()
        {
            coreServ.OnLog += OnLogHandler;
            coreServ.OnCoreStatusChanged += OnCoreStateChangedHandler;
        }
        public void ReleaseEvents()
        {
            coreServ.OnLog -= OnLogHandler;
            coreServ.OnCoreStatusChanged -= OnCoreStateChangedHandler;
        }

        public VgcApis.Models.Datas.StatsSample TakeStatisticsSample()
        {
            var statsPort = coreStates.GetStatPort();
            if (!setting.isEnableStatistics
                || statsPort <= 0)
            {
                return null;
            }

            var up = this.coreServ.QueryStatsApi(statsPort, true);
            var down = this.coreServ.QueryStatsApi(statsPort, false);
            return new VgcApis.Models.Datas.StatsSample(up, down);
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

        public void StopCoreThen() =>
            VgcApis.Libs.Utils.RunInBackground(() => StopCoreWorker(null));

        public void StopCoreThen(Action next) =>
            VgcApis.Libs.Utils.RunInBackground(() => StopCoreWorker(next));

        public void RestartCoreThen() => RestartCoreThen(null);
        public void RestartCoreThen(Action next) =>
            VgcApis.Libs.Utils.RunInBackground(() => RestartCoreWorker(next));

        public bool IsCoreRunning() => coreServ.isRunning;

        public void RunSpeedTest() =>
            BeginSpeedTestWorker(configer.GetConfig());
        #endregion

        #region private methods
        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            if (!coreServ.isRunning)
            {
                coreStates.SetStatPort(0);
            }
            container.InvokeEventOnPropertyChange();
        }

        void BeginSpeedTestWorker(string rawConfig)
        {
            coreStates.SetStatus(I18N.Testing);
            logger.Log(I18N.Testing);

            var delay = configMgr.SpeedTestWorker(
                rawConfig,
                coreStates.GetTitle(),
                true,
                true,
                false,
                (s, a) => logger.Log(a.Data));

            coreStates.SetSpeedTestResult(delay);

            var speedTestResult = delay < long.MaxValue ?
                $"{delay.ToString()} ms" :
                I18N.Timeout;

            coreStates.SetStatus(speedTestResult);
            logger.Log(speedTestResult);
        }

        void OnLogHandler(object sender, VgcApis.Models.Datas.StrEvent arg) =>
            logger.Log(arg.Data);


        void StopCoreWorker(Action next)
        {
            container.InvokeEventOnCoreClosing();
            coreServ.StopCoreThen(
                () =>
                {
                    container.InvokeEventOnRequireNotifierUpdate();
                    container.InvokeEventOnTrackCoreStop();
                    next?.Invoke();
                });
        }

        void RestartCoreWorker(Action next)
        {
            var finalConfig = configer.GetFinalConfig();
            if (finalConfig == null)
            {
                StopCoreThen(next);
                return;
            }

            coreServ.title = coreStates.GetTitle();
            coreServ.RestartCoreThen(
                finalConfig.ToString(),
                () =>
                {
                    container.InvokeEventOnRequireNotifierUpdate();
                    container.InvokeEventOnTrackCoreStart();
                    next?.Invoke();
                },
                Lib.Utils.GetEnvVarsFromConfig(finalConfig));
        }
        #endregion
    }
}
