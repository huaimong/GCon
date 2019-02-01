using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.UserControls
{
    public partial class WelcomeUI :
        UserControl,
        Model.BaseClass.IFormMainFlyPanelComponent
    {

        Service.Servers servers;
        Service.Setting setting;

        int marginBottom;

        public WelcomeUI()
        {
            servers = Service.Servers.Instance;
            setting = Service.Setting.Instance;

            InitializeComponent();
            marginBottom = this.Height - pnlBasicUsage.Top;
        }

        #region public method
        public void Cleanup()
        {
        }
        #endregion

        private void WelcomeFlyPanelComponent_Load(object sender, System.EventArgs e)
        {
            var core = new Service.Core(setting);
            if (!core.IsExecutableExist())
            {
                return;
            }

            pnlBasicUsage.Top = pnlDownloadV2RayCore.Top;
            pnlDownloadV2RayCore.Visible = false;
            this.Height = pnlBasicUsage.Top + marginBottom;
        }

        private void lbDownloadV2rayCore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Views.WinForms.FormDownloadCore.GetForm();
        }

        private void lbV2rayCoreGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Lib.UI.VisitUrl(I18N.VisitV2rayCoreReleasePage, StrConst.V2rayCoreReleasePageUrl);
        }

        private void lbWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Lib.UI.VisitUrl(I18N.VistWikiPage, Properties.Resources.WikiLink);
        }

        private void lbIssue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Lib.UI.VisitUrl(I18N.VisitVGCIssuePage, Properties.Resources.IssueLink);
        }

        private void lbCopyFromClipboard_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string links = Lib.Utils.GetClipboardText();
            servers.ImportLinksWithOutV2RayLinks(links);
        }

        private void lbScanQRCode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            void Success(string link)
            {
                var msg = Lib.Utils.CutStr(link, 90);
                Service.Setting.Instance.SendLog($"QRCode: {msg}");
                servers.ImportLinksWithOutV2RayLinks(link);
            }

            void Fail()
            {
                MessageBox.Show(I18N.NoQRCode);
            }

            Lib.QRCode.QRCode.ScanQRCode(Success, Fail);
        }

        private void lbSimAddVmessWin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Views.WinForms.FormSimAddVmessClient.GetForm();
        }

        private void lbConfigEditor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new Views.WinForms.FormConfiger();
        }
    }
}
