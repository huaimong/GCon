using AutoUpdaterDotNET;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    internal sealed class Updater :
        Model.BaseClass.SingletonService<Updater>
    {
        Setting setting;
        Servers servers;
        Notifier notifier;

        VgcApis.Libs.Tasks.Bar updateBar = new VgcApis.Libs.Tasks.Bar();
        readonly string LoopBackIP = VgcApis.Models.Consts.Webs.LoopBackIP;

        Updater() { }

        #region public methods
        public void CheckForUpdate(bool isShowErrorWithMsgbox)
        {
            if (!updateBar.Install())
            {
                VgcApis.Libs.UI.MsgBoxAsync(I18N.UpdatingPleaseWait);
                return;
            }

            flagShowErrorWithMsgbox = isShowErrorWithMsgbox;
            AutoSetUpdaterProxy();
            AutoUpdater.Start(Properties.Resources.LatestVersionInfoUrl);
        }

        public void Run(
            Setting setting,
            Servers servers,
            Notifier notifier)
        {
            this.notifier = notifier;
            this.setting = setting;
            this.servers = servers;
            InitAutoUpdater();
        }
        #endregion

        #region private methods
        bool flagShowErrorWithMsgbox = true;
        bool CheckArgs(UpdateInfoEventArgs args)
        {
            if (args != null && args.IsUpdateAvailable)
            {
                return true;
            }

            if (!flagShowErrorWithMsgbox)
            {
                setting.SendLog(
                    args == null ?
                    I18N.FetchUpdateInfoFail :
                    I18N.NoUpdateTryLater);
                return false;
            }

            if (args == null)
            {
                MessageBox.Show(
                   I18N.FetchUpdateInfoFail,
                   I18N.Error,
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(I18N.NoUpdateTryLater);
            }
            return false;
        }

        void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (!CheckArgs(args)
                || !ConfirmUpdate(args.CurrentVersion.ToString()))
            {
                updateBar.Remove();
                return;
            }

            try
            {
                if (AutoUpdater.DownloadUpdate())
                {
                    setting.isShutdown = true;
                    Application.Exit();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    exception.GetType().ToString(),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        bool ConfirmUpdate(string version)
        {
            var tag = Lib.Utils.TrimVersionString(version);
            var msg = string.Format(I18N.ConfirmUpgradeVgc, tag);

            if (!flagShowErrorWithMsgbox)
            {
                return VgcApis.Libs.UI.Confirm(msg);
            }

            msg += Environment.NewLine + I18N.ClickHelpToSeeReleaseNote;
            var result = MessageBox.Show(
                msg,
                I18N.Confirm,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button2,
                0,
                Properties.Resources.ReleaseNoteUrl,
                Properties.Resources.ReleaseNoteKeyWord);
            return result == DialogResult.Yes;
        }

        void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            var updateInfo = JsonConvert
                .DeserializeObject<Model.Data.UpdateInfo>(
                    args.RemoteData);

            var url = string.Format(
                VgcApis.Models.Consts.Webs.ReleaseDownloadUrlTpl,
                Lib.Utils.TrimVersionString(updateInfo.version));

            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = new Version(updateInfo.version),
                ChangelogURL = Properties.Resources.ChangeLogUrl,
                Mandatory = false,
                DownloadURL = url,
                HashingAlgorithm = "MD5",
                Checksum = updateInfo.md5,
            };
        }

        void AutoSetUpdaterProxy()
        {
            if (!setting.isUpdateUseProxy)
            {
                return;
            }

            var port = servers.GetAvailableHttpProxyPort();
            if (port > 0)
            {
                var proxy = new WebProxy($"{LoopBackIP}:{port}", true);
                AutoUpdater.Proxy = proxy;
                return;
            }

            if (flagShowErrorWithMsgbox)
            {
                MessageBox.Show(I18N.NoQualifyProxyServer);
            }
            else
            {
                setting.SendLog(I18N.NoQualifyProxyServer);
            }
        }

        void InitAutoUpdater()
        {
            AutoUpdater.ReportErrors = true;
            AutoUpdater.ParseUpdateInfoEvent +=
                AutoUpdaterOnParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent +=
                AutoUpdaterOnCheckForUpdateEvent;
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            AutoUpdater.ParseUpdateInfoEvent -=
                AutoUpdaterOnParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent -=
                AutoUpdaterOnCheckForUpdateEvent;
        }
        #endregion
    }
}
