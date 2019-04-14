using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    class Launcher
    {
        Setting setting;
        Servers servers;
        Updater updater;

        bool isCleanupDone = false;
        List<IDisposable> services = new List<IDisposable>();

        public Launcher() { }

        #region public method
        public void Run()
        {
            setting = Setting.Instance;
            servers = Servers.Instance;
            updater = Updater.Instance;

            SetCulture(setting.culture);

            Prepare();

            BindEvents();

            Lib.Utils.SupportProtocolTLS12();

            if (servers.IsEmpty())
            {
                Views.WinForms.FormMain.ShowForm();
            }
            else
            {
                servers.WakeupServersInBootList();
            }

            if (setting.isCheckUpdateWhenAppStart)
            {
                VgcApis.Libs.Utils.RunInBackground(() =>
                {
#if DEBUG
#else
                    Thread.Sleep(VgcApis.Models.Consts.Webs.CheckForUpdateDelay);
#endif
                    updater.CheckForUpdate(false);
                });
            }

#if DEBUG
            This_Function_Is_Used_For_Debugging();
#endif
        }

        #endregion

        #region debug
#if DEBUG
        void This_Function_Is_Used_For_Debugging()
        {
            //notifier.InjectDebugMenuItem(new ToolStripMenuItem(
            //    "Debug",
            //    null,
            //    (s, a) =>
            //    {
            //        servers.DbgFastRestartTest(100);
            //    }));

            // new Views.WinForms.FormConfiger(@"{}");
            // new Views.WinForms.FormConfigTester();
            // Views.WinForms.FormOption.GetForm();
            Views.WinForms.FormMain.ShowForm();
            Views.WinForms.FormLog.ShowForm();
            // setting.WakeupAutorunServer();
            // Views.WinForms.FormSimAddVmessClient.GetForm();
            // Views.WinForms.FormDownloadCore.GetForm();
        }
#endif
        #endregion

        #region private method

        void Prepare()
        {
            // warn-up
            var cache = Cache.Instance;
            var configMgr = ConfigMgr.Instance;
            var slinkMgr = ShareLinkMgr.Instance;
            var notifier = Notifier.Instance;
            var pluginsServ = PluginsServer.Instance;

            // by dispose order
            services = new List<IDisposable> {
                updater,
                pluginsServ,
                notifier,
                slinkMgr,
                servers,
                configMgr,
                setting,
            };

            // dependency injection
            cache.Run(setting);
            configMgr.Run(setting, cache, servers);
            servers.Run(setting, cache, configMgr);
            slinkMgr.Run(setting, servers, cache);
            notifier.Run(setting, servers, slinkMgr);
            pluginsServ.Run(setting, servers, configMgr, slinkMgr, notifier);
            updater.Run(setting, servers);
        }

        void BindEvents()
        {
            Application.ApplicationExit +=
                (s, a) => OnApplicationExitHandler(false);

            Microsoft.Win32.SystemEvents.SessionEnding +=
                (s, a) => OnApplicationExitHandler(true);

            Application.ThreadException +=
                (s, a) => ShowExceptionDetailAndExit(
                    a.Exception.ToString());

            AppDomain.CurrentDomain.UnhandledException +=
                (s, a) => ShowExceptionDetailAndExit(
                    (a.ExceptionObject as Exception).ToString());
        }

        readonly object cleanupLocker = new object();
        void OnApplicationExitHandler(bool isShutdown)
        {
            // throw new NullReferenceException("for debugging");

            lock (cleanupLocker)
            {
                if (isCleanupDone)
                {
                    return;
                }

                if (!setting.isShutdown)
                {
                    setting.isShutdown = isShutdown;
                }

                foreach (var service in services)
                {
                    service.Dispose();
                }

                isCleanupDone = true;
            }
        }


        void SetCulture(Model.Data.Enum.Cultures culture)
        {
            string cultureString = null;

            switch (culture)
            {
                case Model.Data.Enum.Cultures.enUS:
                    cultureString = "";
                    break;
                case Model.Data.Enum.Cultures.zhCN:
                    cultureString = "zh-CN";
                    break;
                case Model.Data.Enum.Cultures.auto:
                    return;
            }

            var ci = new CultureInfo(cultureString);

            Thread.CurrentThread.CurrentCulture.GetType()
                .GetProperty("DefaultThreadCurrentCulture")
                .SetValue(Thread.CurrentThread.CurrentCulture, ci, null);

            Thread.CurrentThread.CurrentCulture.GetType()
                .GetProperty("DefaultThreadCurrentUICulture")
                .SetValue(Thread.CurrentThread.CurrentCulture, ci, null);
        }
        #endregion

        #region unhandle exception
        void ShowExceptionDetailAndExit(string detail)
        {
            var log = detail;
            try
            {
                log += Environment.NewLine
                    + Environment.NewLine
                    + setting.GetLogContent();
            }
            catch
            {
                // Why must I write sth. here?
            }

            if (!setting.isShutdown)
            {
                VgcApis.Libs.Sys.NotepadHelper.ShowMessage(log, "V2RayGCon bug report");
                MessageBox.Show(I18N.LooksLikeABug);
            }
            Application.Exit();
        }

        #endregion
    }
}
