using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.UserControls
{
    public partial class ServerUI :
        UserControl,
        Model.BaseClass.IFormMainFlyPanelComponent,
        VgcApis.Models.Interfaces.IDropableControl
    {
        Service.Setting setting;
        Service.Servers servers;
        Service.ShareLinkMgr slinkMgr;
        VgcApis.Models.Interfaces.ICoreServCtrl coreServCtrl;

        int[] formHeight;
        Bitmap[] foldingButtonIcons;
        string[] keywords = null;

        public ServerUI(
            VgcApis.Models.Interfaces.ICoreServCtrl serverItem)
        {
            setting = Service.Setting.Instance;
            servers = Service.Servers.Instance;
            slinkMgr = Service.ShareLinkMgr.Instance;

            this.coreServCtrl = serverItem;
            InitializeComponent();

            this.foldingButtonIcons = new Bitmap[] {
                Properties.Resources.StepBackArrow_16x,
                Properties.Resources.StepOverArrow_16x,
            };

            this.formHeight = new int[] {
                this.Height,  // collapseLevel= 0
                this.cboxInbound.Top,
            };
        }

        private void ServerListItem_Load(object sender, EventArgs e)
        {
            SetStatusThen(string.Empty);
            RefreshUI(this, EventArgs.Empty);
            this.coreServCtrl.OnPropertyChanged += RefreshUI;
            rtboxServerTitle.BackColor = this.BackColor;
        }

        #region interface VgcApis.Models.IDropableControl
        public string GetTitle() =>
            coreServCtrl.GetCoreStates().GetTitle();

        public string GetUid() =>
            coreServCtrl.GetCoreStates().GetUid();
        #endregion

        #region private method
        private void HighLightTitleWithKeywords()
        {
            if (keywords == null)
            {
                return;
            }

            VgcApis.Libs.UI.RunInUiThread(rtboxServerTitle, () =>
            {
                var box = rtboxServerTitle;
                var title = box.Text.ToLower();
                var keyword = keywords.FirstOrDefault(
                    s => !string.IsNullOrEmpty(s)
                    && Lib.Utils.PartialMatch(title, s))?.ToLower();

                if (keyword == null)
                {
                    return;
                }

                var highlight = Color.DeepPink;

                int idxTitle = 0, idxKeyword = 0;
                while (idxTitle < title.Length && idxKeyword < keyword.Length)
                {
                    if (title[idxTitle] == keyword[idxKeyword])
                    {
                        box.SelectionStart = idxTitle;
                        box.SelectionLength = 1;
                        box.SelectionColor = highlight;
                        idxKeyword++;
                    }
                    idxTitle++;
                }
                box.SelectionStart = 0;
                box.SelectionLength = 0;
                box.DeselectAll();
            });
        }

        void RestartServer()
        {
            var server = this.coreServCtrl;
            servers.StopAllServersThen(
                () => server.GetCoreCtrl().RestartCoreThen());
        }

        void RefreshUI(object sender, EventArgs arg)
        {
            VgcApis.Libs.UI.RunInUiThread(rtboxServerTitle, () =>
            {
                Lib.UI.UpdateControlOnDemand(
                    cboxInbound, coreServCtrl.GetCoreStates().GetCustomInbType());

                Lib.UI.UpdateControlOnDemand(
                    rtboxServerTitle, coreServCtrl.GetCoreStates().GetTitle());

                Lib.UI.UpdateControlOnDemand(
                    lbStatus, coreServCtrl.GetCoreStates().GetStatus());

                UpdateServerOptionTickStat();
                UpdateInboundAddrOndemand();
                UpdateMarkLable();
                UpdateSelectedTickStat();
                UpdateOnOffLabel(coreServCtrl.GetCoreCtrl().IsCoreRunning());
                UpdateFilterMarkBox();
                UpdateBorderFoldingStat();
                UpdateToolsTip();
            });
        }

        private void UpdateServerOptionTickStat()
        {
            Lib.UI.UpdateControlOnDemand(
                globalImportToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsInjectImport());

            Lib.UI.UpdateControlOnDemand(
                skipCNWebsiteToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsInjectSkipCnSite());

            Lib.UI.UpdateControlOnDemand(
                autorunToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsAutoRun());

            Lib.UI.UpdateControlOnDemand(
                untrackToolStripMenuItem,
                coreServCtrl.GetCoreStates().IsUntrack());
        }

        void UpdateInboundAddrOndemand()
        {
            if (!Lib.Utils.TryParseIPAddr(
                tboxInboundAddr.Text, out string ip, out int port))
            {
                return;
            }

            var addr = coreServCtrl.GetCoreStates().GetCustomInbAddr();
            if (tboxInboundAddr.Text != addr)
            {
                tboxInboundAddr.Text = addr;
            }
        }

        private void UpdateToolsTip()
        {
            var status = coreServCtrl.GetCoreStates().GetStatus();
            if (toolTip1.GetToolTip(lbStatus) != status)
            {
                toolTip1.SetToolTip(lbStatus, status);
            }

            var title = rtboxServerTitle.Text;
            if (toolTip1.GetToolTip(rtboxServerTitle) != title)
            {
                toolTip1.SetToolTip(rtboxServerTitle, title);
            }
        }

        private void UpdateMarkLable()
        {
            var text = (coreServCtrl.GetCoreStates().IsAutoRun() ? "A" : "")
                + (coreServCtrl.GetCoreStates().IsInjectSkipCnSite() ? "C" : "")
                + (coreServCtrl.GetCoreStates().IsInjectImport() ? "I" : "")
                + (coreServCtrl.GetCoreStates().IsUntrack() ? "U" : "");

            if (lbIsAutorun.Text != text)
            {
                lbIsAutorun.Text = text;
            }
        }

        void UpdateBorderFoldingStat()
        {
            var level = Lib.Utils.Clamp(
                coreServCtrl.GetCoreStates().GetFoldingLevel(), 0, foldingButtonIcons.Length);

            if (btnIsCollapse.BackgroundImage != foldingButtonIcons[level])
            {
                btnIsCollapse.BackgroundImage = foldingButtonIcons[level];
            }

            var newHeight = this.formHeight[level];
            if (this.Height != newHeight)
            {
                this.Height = newHeight;
            }
        }

        void UpdateFilterMarkBox()
        {
            if (cboxMark.Text == coreServCtrl.GetCoreStates().GetCustomMark())
            {
                return;
            }

            cboxMark.Text = coreServCtrl.GetCoreStates().GetCustomMark();
        }

        void UpdateSelectedTickStat()
        {
            if (coreServCtrl.GetCoreStates().IsSelected() == chkSelected.Checked)
            {
                return;
            }

            chkSelected.Checked = coreServCtrl.GetCoreStates().IsSelected();
            HighlightSelectedServerItem(chkSelected.Checked);
        }

        void HighlightSelectedServerItem(bool selected)
        {
            var fontStyle = new Font(rtboxServerTitle.Font, selected ? FontStyle.Bold : FontStyle.Regular);
            var colorRed = selected ? Color.OrangeRed : Color.Black;
            rtboxServerTitle.Font = fontStyle;
            lbStatus.Font = fontStyle;
            lbStatus.ForeColor = colorRed;
        }

        private void UpdateOnOffLabel(bool isServerOn)
        {
            var text = isServerOn ? "ON" : "OFF";

            if (tboxInboundAddr.ReadOnly != isServerOn)
            {
                tboxInboundAddr.ReadOnly = isServerOn;
            }

            if (lbRunning.Text != text)
            {
                lbRunning.Text = text;
                lbRunning.ForeColor = isServerOn ? Color.DarkOrange : Color.Green;
            }
        }
        #endregion

        #region properties
        public bool isSelected
        {
            get
            {
                return coreServCtrl.GetCoreStates().IsSelected();
            }
            private set { }
        }
        #endregion

        #region public method
        public void SetKeywords(string keywords)
        {
            this.keywords = (keywords ?? "").Split(
               new char[] { ' ', ',' },
               StringSplitOptions.RemoveEmptyEntries);

            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                // control may be desposed, the sun may explode while this function is running
                try
                {
                    HighLightTitleWithKeywords();
                }
                catch { }
            });
        }

        public string GetConfig() => coreServCtrl.GetConfiger().GetConfig();

        public void SetStatusThen(string status, Action next = null)
        {
            if (lbStatus.IsDisposed)
            {
                next?.Invoke();
                return;
            }

            try
            {
                VgcApis.Libs.UI.RunInUiThread(lbStatus, () =>
                {
                    Lib.UI.UpdateControlOnDemand(lbStatus, status);
                });
            }
            catch { }
            next?.Invoke();
        }

        public void SetSelected(bool selected)
        {
            coreServCtrl.GetCoreStates().SetIsSelected(selected);
        }

        public double GetIndex() => coreServCtrl.GetCoreStates().GetIndex();

        public void SetIndex(double index)
        {
            coreServCtrl.GetCoreStates().SetIndex(index);
        }

        public void Cleanup()
        {
            this.coreServCtrl.OnPropertyChanged -= RefreshUI;
        }
        #endregion

        #region UI event
        private void ServerListItem_MouseDown(object sender, MouseEventArgs e)
        {
            // this effect is ugly and useless
            // Cursor.Current = Lib.UI.CreateCursorIconFromUserControl(this);
            DoDragDrop((ServerUI)sender, DragDropEffects.Move);
        }

        private void btnAction_Click(object sender, System.EventArgs e)
        {
            Button btnSender = sender as Button;
            Point pos = new Point(btnSender.Left, btnSender.Top + btnSender.Height);
            ctxMenuStripMore.Show(this, pos);
            //menu.Show(this, pos);
        }

        private void cboxInbound_SelectedIndexChanged(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetCustomInbType(cboxInbound.SelectedIndex);
        }

        private void chkSelected_CheckedChanged(object sender, EventArgs e)
        {
            var selected = chkSelected.Checked;
            if (selected == coreServCtrl.GetCoreStates().IsSelected())
            {
                return;
            }
            coreServCtrl.GetCoreStates().SetIsSelected(selected);
            HighlightSelectedServerItem(chkSelected.Checked);
        }

        private void tboxInboundAddr_TextChanged(object sender, EventArgs e)
        {
            if (Lib.Utils.TryParseIPAddr(tboxInboundAddr.Text, out string ip, out int port))
            {
                if (tboxInboundAddr.ForeColor != Color.Black)
                {
                    tboxInboundAddr.ForeColor = Color.Black;
                }
                coreServCtrl.GetCoreStates().SetCustomInbAddr(ip, port);
            }
            else
            {
                // UI operation is expansive
                if (tboxInboundAddr.ForeColor != Color.Red)
                {
                    tboxInboundAddr.ForeColor = Color.Red;
                }
            }
        }

        private void lbSummary_Click(object sender, EventArgs e)
        {
            chkSelected.Checked = !chkSelected.Checked;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RestartServer();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var config = coreServCtrl.GetConfiger().GetConfig();
            new Views.WinForms.FormConfiger(config);
        }

        private void vmessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var vmessLink = slinkMgr.EncodeVmessLink(GetConfig());
            var success = Lib.Utils.CopyToClipboard(vmessLink);
            MessageBox.Show(success ? I18N.LinksCopied : I18N.CopyFail);
        }

        private void v2rayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                           Lib.Utils.CopyToClipboard(
                               Lib.Utils.AddLinkPrefix(
                                   Lib.Utils.Base64Encode(GetConfig()),
                                   VgcApis.Models.Datas.Enum.LinkTypes.v2ray)) ?
                           I18N.LinksCopied :
                           I18N.CopyFail);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Lib.UI.Confirm(I18N.ConfirmDeleteControl))
            {
                return;
            }
            Cleanup();
            servers.DeleteServerByConfig(GetConfig());
        }

        private void logOfThisServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetLogger().ShowLogForm();
        }

        private void cboxMark_TextChanged(object sender, EventArgs e)
        {
            this.coreServCtrl.GetCoreStates().SetCustomMark(cboxMark.Text);
        }

        private void cboxMark_DropDown(object sender, EventArgs e)
        {
            var servers = Service.Servers.Instance;
            cboxMark.Items.Clear();
            foreach (var item in servers.GetMarkList())
            {
                cboxMark.Items.Add(item);
            }
            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxMark);
        }

        private void lbStatus_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void label4_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void lbRunning_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void btnMultiboxing_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().RestartCoreThen();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().StopCoreThen();
        }

        private void btnIsCollapse_Click(object sender, EventArgs e)
        {
            var level = (coreServCtrl.GetCoreStates().GetFoldingLevel() + 1) % 2;
            coreServCtrl.GetCoreStates().SetFoldingLevel(level);
        }

        private void lbIsAutorun_MouseDown(object sender, MouseEventArgs e)
        {
            ServerListItem_MouseDown(this, e);
        }

        private void rtboxServerTitle_Click(object sender, EventArgs e)
        {
            chkSelected.Checked = !chkSelected.Checked;
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RestartServer();
        }

        private void multiboxingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().RestartCoreThen();
        }

        private void stopToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreCtrl().StopCoreThen();
        }

        private void untrackToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsUntrack();

        private void autorunToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsAutoRun();


        private void globalImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().ToggleIsInjectImport();
        }

        private void skipCNWebsiteToolStripMenuItem_Click(object sender, EventArgs e) =>
            coreServCtrl.GetCoreStates().ToggleIsInjectSkipCnSite();

        private void runSpeedTestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            VgcApis.Libs.Utils.RunInBackground(() => coreServCtrl.GetCoreCtrl().RunSpeedTest());
        }

        private void moveToTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetIndex(0);
            servers.RequireFormMainReload();
        }

        private void moveToBottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            coreServCtrl.GetCoreStates().SetIndex(double.MaxValue);
            servers.RequireFormMainReload();
        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var finalConfig = coreServCtrl.GetConfiger().GetFinalConfig();
            new WinForms.FormConfiger(finalConfig.ToString(Formatting.Indented));
        }

        #endregion
    }
}
