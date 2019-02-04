using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.CoreServerComponent
{
    sealed public class Core :
        VgcApis.Models.BaseClasses.ComponentOf<CoreServerCtrl>,
        VgcApis.Models.Interfaces.CoreCtrlComponents.ICore
    {
        Service.Core coreServ;
        Service.Setting setting;

        public Core(Service.Setting setting)
        {
            this.setting = setting;
            this.coreServ = new Service.Core(setting);
        }

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

        void OnCoreStateChangedHandler(object sender, EventArgs args)
        {
            if (!coreServ.isRunning)
            {
                states.SetStatPort(0);
            }
            container.InvokeEventOnPropertyChange();
        }

        States states;
        Config configer;
        Logger logger;
        CoreServerCtrl container;
        public override void Prepare()
        {
            container = GetContainer();
            states = container.GetComponent<States>();
            configer = container.GetComponent<Config>();
            logger = container.GetComponent<Logger>();
        }

        #region public mehtods
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
        #endregion

        #region private methods
        public void RunSpeedTest()
        {
            void log(string msg)
            {
                logger.Log(msg);
                states.SetStatus(msg);
            }

            var port = Lib.Utils.GetFreeTcpPort();
            var config = configer.PrepareSpeedTestConfig(port);

            if (string.IsNullOrEmpty(config))
            {
                log(I18N.DecodeImportFail);
                return;
            }

            var url = StrConst.SpeedTestUrl;
            var text = I18N.Testing;
            log(text);
            logger.Log(url);

            var speedTester = new Service.Core(setting)
            {
                title = states.GetTitle()
            };
            speedTester.OnLog += OnLogHandler;
            speedTester.RestartCore(config);

            states.SetSpeedTestResult(
                Lib.Utils.VisitWebPageSpeedTest(url, port));

            var speed = states.GetSpeedTestResult();
            text = string.Format(
                "{0}",
                (speed < long.MaxValue ? speed.ToString() + "ms" : I18N.Timeout));

            log(text);
            speedTester.StopCore();
            speedTester.OnLog -= OnLogHandler;
        }

        void OnLogHandler(object sender, VgcApis.Models.Datas.StrEvent arg) =>
            logger.Log(arg.Data);


        public void StopCoreWorker(Action next)
        {
            container.InvokeEventOnCoreClosing();
            coreServ.StopCoreThen(
                () =>
                {
                    container.InvokeEventOnRequireNotifierUpdate();
                    container.InvokeEventOnRequireKeepTrack(false);
                    next?.Invoke();
                });
        }

        void RestartCoreWorker(Action next)
        {
            JObject cfg = configer.GetDecodedConfig(true, false, true);
            if (cfg == null)
            {
                StopCoreThen(next);
                return;
            }

            if (!configer.OverwriteInboundSettings(
                ref cfg,
                states.GetCustomInbType(),
                states.GetInbIp(),
                states.GetInbPort()))
            {
                StopCoreThen(next);
                return;
            }

            configer.InjectSkipCnSiteSettingsIntoConfig(ref cfg);
            configer.InjectStatsSettingsIntoConfig(ref cfg);

            // debug
            var configStr = cfg.ToString(Formatting.Indented);

            coreServ.title = states.GetTitle();
            coreServ.RestartCoreThen(
                cfg.ToString(),
                () =>
                {
                    container.InvokeEventOnRequireNotifierUpdate();
                    container.InvokeEventOnRequireKeepTrack(true);
                    next?.Invoke();
                },
                Lib.Utils.GetEnvVarsFromConfig(cfg));
        }
        #endregion
    }
}
