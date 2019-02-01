using ProxySetter.Resources.Langs;
using System;
using System.Windows.Forms;

namespace ProxySetter.Controllers.VGCPluginComponents
{
    class TabStatus : ComponentCtrl
    {
        Label lbPacUrl, lbPacServerStatus;
        readonly Services.PsSettings setting;
        Services.PacServer pacServer;

        public TabStatus(
            Services.PsSettings setting,
            Services.PacServer pacServer,

            Label lbPacServerStatus,
            Label lbPacUrl,
            Button btnRestart,
            Button btnStop,
            Button btnSaveAs,
            Button btnDebug,
            Button btnCopy,
            Button btnClearSysProxy)
        {
            this.setting = setting;
            this.pacServer = pacServer;

            BindControls(lbPacServerStatus, lbPacUrl);
            BindEvents(
                pacServer,
                btnRestart,
                btnStop,
                btnSaveAs,
                btnDebug,
                btnCopy,
                btnClearSysProxy);

            OnPacServerStateChangedHandler(null, EventArgs.Empty);

            pacServer.OnPACServerStateChanged += OnPacServerStateChangedHandler;
        }

        private void BindEvents(
            Services.PacServer pacServer,
            Button btnRestart,
            Button btnStop,
            Button btnSaveAs,
            Button btnDebug,
            Button btnCopy,
            Button btnClearSysProxy)
        {
            btnSaveAs.Click += (s, a) =>
            {
                var content = pacServer.GetCurPacFileContent();
                var result = VgcApis.Libs.UI.ShowSaveFileDialog(
                    VgcApis.Models.Consts.Files.PacExt,
                    content,
                    out string filename);

                switch (result)

                {
                    case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Success:
                        MessageBox.Show(I18N.Done);
                        break;
                    case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Fail:
                        MessageBox.Show(I18N.WriteFileFail);
                        break;
                    case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Cancel:
                        // do nothing
                        break;
                }
            };

            btnClearSysProxy.Click += (s, a) =>
                Lib.Sys.ProxySetter.ClearSysProxy();

            btnRestart.Click += (s, a) => pacServer.StartPacServer();

            btnStop.Click += (s, a) => pacServer.StopPacServer();

            btnDebug.Click +=
                (s, a) => VgcApis.Libs.UI.VisitUrl(
                    I18N.VisitPacDebugger, GetDebugUrl());

            btnCopy.Click += (s, a) =>
            {
                MessageBox.Show(
                    VgcApis.Libs.Utils.CopyToClipboard(this.lbPacUrl.Text) ?
                    I18N.CopySuccess : I18N.CopyFail);
            };
        }

        #region private methods
        string GetDebugUrl()
        {
            return string.Format("{0}?&debug=true", pacServer.GetPacUrl());
        }

        void OnPacServerStateChangedHandler(object sender, EventArgs args)
        {
            VgcApis.Libs.UI.RunInUiThread(lbPacServerStatus, () =>
            {
                lbPacServerStatus.Text =
                    pacServer.isRunning ?
                    I18N.PacServerIsOn : I18N.PacServerIsOff;
            });
        }

        private void BindControls(Label lbPacServerStatus, Label lbPacUrl)
        {
            this.lbPacServerStatus = lbPacServerStatus;
            this.lbPacUrl = lbPacUrl;
        }
        #endregion

        #region public method
        public override bool IsOptionsChanged()
        {
            return false;
        }

        public override bool SaveOptions()
        {
            return false;
        }

        public override void Cleanup()
        {
            pacServer.OnPACServerStateChanged -= OnPacServerStateChangedHandler;
        }
        #endregion

    }
}
