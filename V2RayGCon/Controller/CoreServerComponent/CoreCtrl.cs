using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.CoreServerComponent
{
    sealed public class CoreCtrl :
        VgcApis.Models.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Models.Interfaces.CoreCtrlComponents.ICoreCtrl
    {
        Service.Core coreServ;
        Service.Setting setting;
        Service.Servers servers;

        public CoreCtrl(
            Service.Setting setting,
            Service.Servers servers)
        {
            this.setting = setting;
            this.servers = servers;
        }

        CoreStates states;
        Configer configer;
        Logger logger;
        CoreServerCtrl container;
        public override void Prepare()
        {
            this.coreServ = new Service.Core(setting);

            container = GetContainer();
            states = container.GetComponent<CoreStates>();
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
            var statsPort = states.GetStatPort();
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
            Task.Run(() => StopCoreWorker(null));

        public void StopCoreThen(Action next) =>
            Task.Run(() => StopCoreWorker(next));

        public void RestartCoreThen() => RestartCoreThen(null);
        public void RestartCoreThen(Action next) =>
            Task.Factory.StartNew(() => RestartCoreWorker(next));

        public bool IsCoreRunning() => coreServ.isRunning;

        public void RunSpeedTest() =>
            BeginSpeedTestWorker(configer.GetConfig());
        #endregion

        #region private methods
        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            if (!coreServ.isRunning)
            {
                states.SetStatPort(0);
            }
            container.InvokeEventOnPropertyChange();
        }

        void BeginSpeedTestWorker(string rawConfig)
        {
            states.SetStatus(I18N.Testing);
            logger.Log(I18N.Testing);

            var delay = servers.SpeedTestWorker(
                rawConfig,
                states.GetTitle(),
                true,
                true,
                false,
                (s, a) => logger.Log(a.Data));

            states.SetSpeedTestResult(delay);

            var speedTestResult = delay < long.MaxValue ?
                $"{delay.ToString()} ms" :
                I18N.Timeout;

            states.SetStatus(speedTestResult);
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
            JObject cfg = servers.DecodeConfig(
                configer.GetConfig(), true, false, true);
            if (cfg == null)
            {
                StopCoreThen(next);
                return;
            }

            if (!servers.ReplaceInboundWithCustomSetting(
                ref cfg,
                states.GetCustomInbType(),
                states.GetInbIp(),
                states.GetInbPort()))
            {
                StopCoreThen(next);
                return;
            }

            configer.InjectSkipCnSitesConfig(ref cfg);
            configer.InjectStatisticsConfig(ref cfg);

            // debug
            var configStr = cfg.ToString(Formatting.Indented);

            coreServ.title = states.GetTitle();
            coreServ.RestartCoreThen(
                cfg.ToString(),
                () =>
                {
                    container.InvokeEventOnRequireNotifierUpdate();
                    container.InvokeEventOnTrackCoreStart();
                    next?.Invoke();
                },
                Lib.Utils.GetEnvVarsFromConfig(cfg));
        }
        #endregion
    }
}
