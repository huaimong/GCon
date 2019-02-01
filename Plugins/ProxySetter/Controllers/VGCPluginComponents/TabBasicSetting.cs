using System;
using System.Windows.Forms;
using static VgcApis.Libs.Utils;

namespace ProxySetter.Controllers.VGCPluginComponents
{
    class TabBasicSetting : ComponentCtrl
    {
        Services.PsSettings setting;
        string oldSetting;
        Model.Data.BasicSettings basicSettings;
        Services.ServerTracker servTracker;

        ComboBox cboxBasicSysProxyMode, cboxBasicPacMode, cboxBasicPacProtocol;
        TextBox tboxBasicProxyPort, tboxBaiscPacPort, tboxBasicCustomPacPath;
        CheckBox chkBasicAutoUpdateSysProxy, chkBasicPacAlwaysOn, chkBasicUseCustomPac;

        public TabBasicSetting(
            Services.PsSettings setting,
            Services.ServerTracker servTracker,

            ComboBox cboxBasicPacProtocol,
            ComboBox cboxBasicSysProxyMode,
            TextBox tboxBasicProxyPort,
            TextBox tboxBaiscPacPort,
            ComboBox cboxBasicPacMode,
            TextBox tboxBasicCustomPacPath,
            CheckBox chkBasicAutoUpdateSysProxy,
            CheckBox chkBasicPacAlwaysOn,
            CheckBox chkBasicUseCustomPac,
            Button btnBasicBrowseCustomPac)
        {
            this.setting = setting;
            this.servTracker = servTracker;

            basicSettings = setting.GetBasicSetting();
            oldSetting = VgcApis.Libs.Utils.SerializeObject(basicSettings);

            BindControls(
                cboxBasicPacProtocol,
                cboxBasicSysProxyMode,
                tboxBasicProxyPort,
                tboxBaiscPacPort,
                cboxBasicPacMode,
                tboxBasicCustomPacPath,
                chkBasicAutoUpdateSysProxy,
                chkBasicPacAlwaysOn,
                chkBasicUseCustomPac);

            InitControls();

            BindEvents(btnBasicBrowseCustomPac);

            servTracker.OnSysProxyChanged += OnSysProxyChangeHandler;
        }

        #region public method
        void OnSysProxyChangeHandler(object sender, EventArgs args)
        {
            basicSettings = setting.GetBasicSetting();
            oldSetting = VgcApis.Libs.Utils.SerializeObject(basicSettings);

            VgcApis.Libs.UI.RunInUiThread(
                chkBasicAutoUpdateSysProxy,
                () =>
                {
                    InitControls();
                });
        }

        public override bool IsOptionsChanged()
        {
            return VgcApis.Libs.Utils.SerializeObject(GetterSettings()) != oldSetting;
        }

        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            var bs = GetterSettings();
            oldSetting = VgcApis.Libs.Utils.SerializeObject(bs);
            setting.SaveBasicSetting(bs);
            return true;
        }

        public override void Cleanup()
        {
            servTracker.OnSysProxyChanged -= OnSysProxyChangeHandler;
        }
        #endregion

        #region private methods

        private void BindEvents(Button btnBasicBrowseCustomPac)
        {
            btnBasicBrowseCustomPac.Click += (s, a) =>
            {
                var filename = VgcApis.Libs.UI.ShowSelectFileDialog(
                    VgcApis.Models.Consts.Files.JsExt);

                if (!string.IsNullOrEmpty(filename))
                {
                    this.tboxBasicCustomPacPath.Text = filename;
                }
            };
        }

        Model.Data.BasicSettings GetterSettings()
        {
            return new Model.Data.BasicSettings
            {
                pacProtocol = Clamp(cboxBasicPacProtocol.SelectedIndex, 0, 2),
                sysProxyMode = Clamp(cboxBasicSysProxyMode.SelectedIndex, 0, 3),
                proxyPort = Str2Int(tboxBasicProxyPort.Text),
                pacServPort = Str2Int(tboxBaiscPacPort.Text),
                pacMode = Clamp(cboxBasicPacMode.SelectedIndex, 0, 2),
                customPacFileName = tboxBasicCustomPacPath.Text,
                isAutoUpdateSysProxy = chkBasicAutoUpdateSysProxy.Checked,
                isAlwaysStartPacServ = chkBasicPacAlwaysOn.Checked,
                isUseCustomPac = chkBasicUseCustomPac.Checked,
            };
        }

        private void BindControls(
            ComboBox cboxBasicPacProtocol,
            ComboBox cboxBasicSysProxyMode,
            TextBox tboxBasicProxyPort,
            TextBox tboxBaiscPacPort,
            ComboBox cboxBasicPacMode,
            TextBox tboxBasicCustomPacPath,
            CheckBox chkBasicAutoUpdateSysProxy,
            CheckBox chkBasicPacAlwaysOn,
            CheckBox chkBasicUseCustomPac)
        {
            this.cboxBasicPacProtocol = cboxBasicPacProtocol;
            this.cboxBasicSysProxyMode = cboxBasicSysProxyMode;
            this.tboxBasicProxyPort = tboxBasicProxyPort;
            this.tboxBaiscPacPort = tboxBaiscPacPort;
            this.cboxBasicPacMode = cboxBasicPacMode;
            this.tboxBasicCustomPacPath = tboxBasicCustomPacPath;
            this.chkBasicAutoUpdateSysProxy = chkBasicAutoUpdateSysProxy;
            this.chkBasicPacAlwaysOn = chkBasicPacAlwaysOn;
            this.chkBasicUseCustomPac = chkBasicUseCustomPac;
        }

        private void InitControls()
        {
            var s = basicSettings;

            cboxBasicPacProtocol.SelectedIndex = Clamp(s.pacProtocol, 0, 2);
            cboxBasicSysProxyMode.SelectedIndex = Clamp(s.sysProxyMode, 0, 3);
            tboxBasicProxyPort.Text = s.proxyPort.ToString();
            tboxBaiscPacPort.Text = s.pacServPort.ToString();
            cboxBasicPacMode.SelectedIndex = Clamp(s.pacMode, 0, 2);
            tboxBasicCustomPacPath.Text = s.customPacFileName;
            chkBasicAutoUpdateSysProxy.Checked = s.isAutoUpdateSysProxy;
            chkBasicPacAlwaysOn.Checked = s.isAlwaysStartPacServ;
            chkBasicUseCustomPac.Checked = s.isUseCustomPac;
        }
        #endregion
    }
}
