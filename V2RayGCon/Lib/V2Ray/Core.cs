using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Lib.V2Ray
{
    public class Core
    {
        #region support ctrl+c
        // https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
        internal const int CTRL_C_EVENT = 0;
        #endregion

        public event EventHandler<VgcApis.Models.Datas.StrEvent> OnLog;
        public event EventHandler OnCoreStatusChanged;
        event EventHandler OnCoreReady;

        Process v2rayCore;
        static object coreLock = new object();
        Service.Setting setting;
        string config;

        static VgcApis.Libs.Tasks.Bar globalCoreStartStopToken = new VgcApis.Libs.Tasks.Bar();

        public Core(Service.Setting setting)
        {
            isRunning = false;
            isCheckCoreReady = false;
            v2rayCore = null;
            config = string.Empty;
            this.setting = setting;
        }

        #region property
        string _v2ctl = "";
        string v2ctl
        {
            get
            {
                if (string.IsNullOrEmpty(_v2ctl))
                {
                    _v2ctl = GetExecutablePath(StrConst.ExecutableV2ctl);
                }
                return _v2ctl;
            }
        }

        string _title;
        public string title
        {
            get
            {
                return string.IsNullOrEmpty(_title) ?
                    string.Empty :
                    Lib.Utils.CutStr(_title, 46);
            }
            set
            {
                _title = value;
            }
        }

        public bool isRunning
        {
            get;
            private set;
        }

        bool isCheckCoreReady
        {
            get;
            set;
        }
        #endregion

        #region public method
        public int QueryStatsApi(int port, bool isUplink)
        {
            if (string.IsNullOrEmpty(v2ctl))
            {
                return 0;
            }

            var queryParam = string.Format(
                StrConst.StatsQueryParamTpl,
                port.ToString(),
                isUplink ? "uplink" : "downlink");

            try
            {
                var output = Lib.Utils.GetOutputFromExecutable(
                    v2ctl,
                    queryParam,
                    VgcApis.Models.Consts.Core.GetStatisticsTimeout);

                // Regex pattern = new Regex(@"(?<value>(\d+))");
                var value = VgcApis.Libs.Utils.ExtractStringWithPattern(
                    "value", @"(\d+)", output);

                return Lib.Utils.Str2Int(value);
            }
            catch { }
            return 0;
        }

        public string GetCoreVersion()
        {
            if (!IsExecutableExist())
            {
                return string.Empty;
            }

            var output = Lib.Utils.GetOutputFromExecutable(
                GetExecutablePath(),
                "-version",
                VgcApis.Models.Consts.Core.GetVersionTimeout);

            // since 3.46.* v is deleted
            // Regex pattern = new Regex(@"(?<version>(\d+\.)+\d+)");
            // Regex pattern = new Regex(@"v(?<version>[\d\.]+)");
            return VgcApis.Libs.Utils.ExtractStringWithPattern(
                "version", @"(\d+\.)+\d+", output);
        }

        public bool IsExecutableExist()
        {
            return !string.IsNullOrEmpty(GetExecutablePath());
        }

        public string GetExecutablePath(string fileName = null)
        {
            List<string> folders = GenV2RayCoreSearchPaths(setting.isPortable);
            for (var i = 0; i < folders.Count; i++)
            {
                var file = Path.Combine(folders[i], fileName ?? StrConst.ExecutableV2ray);
                if (File.Exists(file))
                {
                    return file;
                }
            }
            return string.Empty;
        }

        private static List<string> GenV2RayCoreSearchPaths(bool isPortable)
        {
            var folders = new List<string>{
                Lib.Utils.GetSysAppDataFolder(), // %appdata%
                VgcApis.Libs.Utils.GetAppDir(),
                VgcApis.Libs.Utils.GetCoreFolderFullPath(),
            };

            if (isPortable)
            {
                folders.Reverse();
            }

            return folders;
        }

        VgcApis.Libs.Tasks.Bar coreStartStopToken = null;
        public void WaitForTokenHurry() =>
            WaitForTokenWorker(VgcApis.Models.Consts.Intervals.GetCoreTokenIntervalFast);

        public void WaitForToken() =>
            WaitForTokenWorker(VgcApis.Models.Consts.Intervals.GetCoreTokenIntervalSlow);

        public void ReleaseToken()
        {
            if (coreStartStopToken == null)
            {
                throw new ArgumentNullException(@"Token is null!");
            }

            var token = coreStartStopToken;
            coreStartStopToken = null;
            token.Remove();
        }

        // blocking
        public void RestartCore(
            string config,
            Dictionary<string, string> env = null)
        {
            lock (coreLock)
            {
                if (isRunning)
                {
                    StopCoreWorker();
                }

                if (IsExecutableExist())
                {
                    StartCore(config, env);
                }
                else
                {
                    VgcApis.Libs.Utils.RunInBackground(
                        () => MessageBox.Show(I18N.ExeNotFound));
                }
            }
            VgcApis.Libs.Utils.RunInBackground(() => InvokeEventOnCoreStatusChanged());
        }

        // non-blocking 
        public void RestartCoreThen(
            string config,
            Action next = null,
            Dictionary<string, string> env = null)
        {
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                RestartCore(config, env);
                InvokeActionIgnoreError(next);
            });
        }

        // blocking
        public void StopCore()
        {
            lock (coreLock)
            {
                StopCoreWorker();
            }
        }

        // non-blocking
        public void StopCoreThen(Action next = null)
        {
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                StopCore();
                InvokeActionIgnoreError(next);
            });
        }
        #endregion

        #region private method
        void WaitForTokenWorker(int requestInterval)
        {
            while (!globalCoreStartStopToken.Install())
            {
                Thread.Sleep(requestInterval);
            }
            coreStartStopToken = globalCoreStartStopToken;
        }

        void InvokeEventOnCoreReady()
        {
            try
            {
                OnCoreReady?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        void InvokeEventOnCoreStatusChanged()
        {
            try
            {
                OnCoreStatusChanged?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        void StopCoreWorker()
        {
            if (v2rayCore == null)
            {
                isRunning = false;
            }

            if (!isRunning)
            {
                return;
            }

            var success = SendCtrlCSignalToV2RayCore();
            if (!success)
            {
                try
                {
                    // kill if send ctrl+c fail
                    KillCore();
                }
                catch { }
            }
            isRunning = false;
        }

        bool SendCtrlCSignalToV2RayCore()
        {
            var success = false;
            try
            {
                if (Lib.Sys.SafeNativeMethods.AttachConsole((uint)v2rayCore.Id))
                {
                    AutoResetEvent done = new AutoResetEvent(false);
                    v2rayCore.Exited += (s, a) =>
                    {
                        v2rayCore.Close();
                        done.Set();
                    };


                    Lib.Sys.SafeNativeMethods.SetConsoleCtrlHandler(null, true);
                    Lib.Sys.SafeNativeMethods.GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);

                    if (done.WaitOne(VgcApis.Models.Consts.Core.SendCtrlCTimeout))
                    {
                        success = true;
                    }

                    Lib.Sys.SafeNativeMethods.FreeConsole();
                    Lib.Sys.SafeNativeMethods.SetConsoleCtrlHandler(null, false);
                }
            }
            catch { }

            return success;
        }

        void KillCore()
        {
            Debug.WriteLine("Kill core!");
            AutoResetEvent finished = new AutoResetEvent(false);

            SendLog(I18N.AttachToV2rayCoreProcessFail);

            v2rayCore.Exited += (s, a) =>
            {
                finished.Set();
            };

            Lib.Utils.KillProcessAndChildrens(v2rayCore.Id);
            finished.WaitOne(VgcApis.Models.Consts.Core.KillCoreTimeout);
        }

        static void InvokeActionIgnoreError(Action lambda)
        {
            try
            {
                lambda?.Invoke();
            }
            catch { }
        }

        Process CreateV2RayCoreProcess()
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetExecutablePath(),
                    Arguments = "-config=stdin: -format=json",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };
            p.EnableRaisingEvents = true;
            return p;
        }

        void InjectEnv(Process proc, Dictionary<string, string> envs)
        {
            if (envs == null || envs.Count <= 0)
            {
                return;
            }

            var procEnv = proc.StartInfo.EnvironmentVariables;
            foreach (var env in envs)
            {
                procEnv[env.Key] = env.Value;
            }
        }

        void ShowExitErrorMessage()
        {
            MessageBox.Show(title + I18N.V2rayCoreExitAbnormally);
        }

        void OnCoreExited(object sender, EventArgs args)
        {
            SendLog(I18N.CoreExit);
            ReleaseEvents(v2rayCore);

            var err = v2rayCore.ExitCode;
            if (err != 0)
            {
                v2rayCore.Close();
                VgcApis.Libs.Utils.RunInBackground(() => ShowExitErrorMessage());
            }

            // SendLog("Exit code: " + err);
            isRunning = false;
            VgcApis.Libs.Utils.RunInBackground(() => InvokeEventOnCoreStatusChanged());
        }

        void BindEvents(Process proc)
        {
            proc.Exited += OnCoreExited;
            proc.ErrorDataReceived += SendLogHandler;
            proc.OutputDataReceived += SendLogHandler;
        }

        void ReleaseEvents(Process proc)
        {
            proc.Exited -= OnCoreExited;
            proc.ErrorDataReceived -= SendLogHandler;
            proc.OutputDataReceived -= SendLogHandler;
        }

        void StartCore(string config, Dictionary<string, string> envs = null)
        {
            this.config = config;
            v2rayCore = CreateV2RayCoreProcess();
            InjectEnv(v2rayCore, envs);
            BindEvents(v2rayCore);

            AutoResetEvent ready = new AutoResetEvent(false);
            EventHandler onCoreReady = (s, a) =>
            {
                try { ready.Set(); } catch { }
            };
            isCheckCoreReady = true;
            OnCoreReady += onCoreReady;

            isRunning = true;
            v2rayCore.Start();

            // Add to JOB object require win8+.
            Lib.Sys.ChildProcessTracker.AddProcess(v2rayCore);

            v2rayCore.StandardInput.WriteLine(config);
            v2rayCore.StandardInput.Close();
            v2rayCore.BeginErrorReadLine();
            v2rayCore.BeginOutputReadLine();

            ready.WaitOne();
            OnCoreReady -= onCoreReady;
            isCheckCoreReady = false;
        }

        void SendLogHandler(object sender, DataReceivedEventArgs args)
        {
            var msg = args.Data;

            if (msg == null)
            {
                return;
            }

            if (isCheckCoreReady && MatchAllReadyMarks(msg))
            {
                InvokeEventOnCoreReady();
            }

            SendLog(msg);
        }

        bool MatchAllReadyMarks(string message)
        {
            var lowerMsg = message.ToLower();
            foreach (var mark in VgcApis.Models.Consts.Core.ReadyLogMarks)
            {
                if (!lowerMsg.Contains(mark))
                {
                    return false;
                }
            }
            return true;
        }

        void SendLog(string log)
        {
            var arg = new VgcApis.Models.Datas.StrEvent(log);
            try
            {
                OnLog?.Invoke(this, arg);
            }
            catch { }
        }

        #endregion
    }
}
