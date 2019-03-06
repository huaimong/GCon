using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormOption : Form
    {
        #region Sigleton
        static readonly VgcApis.Models.BaseClasses.AuxSiWinForm<FormOption> auxSiForm =
            new VgcApis.Models.BaseClasses.AuxSiWinForm<FormOption>();
        static public FormOption GetForm() => auxSiForm.GetForm();
        static public void ShowForm() => auxSiForm.ShowForm();
        #endregion

        Controller.FormOptionCtrl optionCtrl;

        public FormOption()
        {
            InitializeComponent();

            VgcApis.Libs.UI.AutoSetFormIcon(this);
        }

        private void FormOption_Shown(object sender, System.EventArgs e)
        {
            // throw new System.ArgumentException("for debug");

            this.optionCtrl = InitOptionCtrl();

            this.FormClosing += (s, a) =>
            {
                if (!this.optionCtrl.IsOptionsSaved())
                {
                    a.Cancel = !Lib.UI.Confirm(I18N.ConfirmCloseWinWithoutSave);
                }
            };

            this.FormClosed += (s, a) =>
            {
                Service.Setting.Instance.LazyGC();
            };
        }


        #region public method

        #endregion

        #region private method
        private Controller.FormOptionCtrl InitOptionCtrl()
        {
            var ctrl = new Controller.FormOptionCtrl();

            ctrl.Plug(
                new Controller.OptionComponent.Import(
                    flyImportPanel,
                    btnImportAdd));

            ctrl.Plug(
                new Controller.OptionComponent.Subscription(
                    flySubsUrlContainer,
                    btnAddSubsUrl,
                    btnUpdateViaSubscription,
                    chkSubsIsUseProxy));

            ctrl.Plug(
                new Controller.OptionComponent.TabPlugin(
                    flyPluginsItemsContainer));

            ctrl.Plug(
                new Controller.OptionComponent.TabSetting(
                    cboxSettingLanguage,
                    cboxSettingPageSize,
                    chkSetServAutotrack,
                    chkSetSysPortable,
                    chkSetUseV4,
                    chkSetServStatistics,
                    rbtnSetUpgradeToVgcFull,
                    chkSetUpgradeUseProxy));

            return ctrl;
        }

        #endregion

        #region UI event
        private void btnOptionExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnOptionSave_Click(object sender, System.EventArgs e)
        {
            this.optionCtrl.SaveAllOptions();
            MessageBox.Show(I18N.Done);
        }

        private void btnBakBackup_Click(object sender, System.EventArgs e)
        {
            optionCtrl.BackupOptions();
        }

        private void btnBakRestore_Click(object sender, System.EventArgs e)
        {
            optionCtrl.RestoreOptions();
        }
        #endregion
    }
}
