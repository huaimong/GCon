using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormBatchModifyServerSetting : Form
    {
        #region Sigleton
        static FormBatchModifyServerSetting _instant;
        public static FormBatchModifyServerSetting GetForm()
        {
            if (_instant == null || _instant.IsDisposed)
            {
                _instant = new FormBatchModifyServerSetting();
            }
            return _instant;
        }
        #endregion

        Service.Servers servers;

        FormBatchModifyServerSetting()
        {
            servers = Service.Servers.Instance;

            InitializeComponent();

            VgcApis.Libs.UI.AutoSetFormIcon(this);
            this.Show();
        }

        private void FormBatchModifyServerInfo_Shown(object sender, EventArgs e)
        {
            this.cboxMark.Items.Clear();
            foreach (var mark in servers.GetMarkList())
            {
                this.cboxMark.Items.Add(mark);
            }

            var firstCtrl = servers
                .GetServerList()
                .Where(s => s.GetStates().IsSelected())
                .FirstOrDefault();
            if (firstCtrl == null)
            {
                return;
            }

            var first = firstCtrl.GetStates().GetAllRawCoreInfo();

            this.cboxInMode.SelectedIndex = first.customInbType;
            this.tboxInIP.Text = first.inbIp;
            this.tboxInPort.Text = first.inbPort.ToString();
            this.cboxMark.Text = first.customMark;
            this.cboxAutorun.SelectedIndex = first.isAutoRun ? 0 : 1;
            this.cboxImport.SelectedIndex = first.isInjectImport ? 0 : 1;
            this.cboxIsInjectSkipCNSite.SelectedIndex = first.isInjectSkipCNSite ? 0 : 1;
        }

        #region UI event
        private void chkShareOverLAN_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = chkShareOverLAN.Checked;
            if (isChecked)
            {
                tboxInIP.Text = "0.0.0.0";
            }
            else
            {
                tboxInIP.Text = "127.0.0.1";
            }
            tboxInIP.Enabled = !isChecked;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            var list = servers
                .GetServerList()
                .Where(s => s.GetStates().IsSelected())
                .ToList();

            var newMode = chkInMode.Checked ? cboxInMode.SelectedIndex : -1;
            var newIP = chkInIP.Checked ? tboxInIP.Text : null;
            var newPort = chkInPort.Checked ? Lib.Utils.Str2Int(tboxInPort.Text) : -1;
            var newMark = chkMark.Checked ? cboxMark.Text : null;
            var newAutorun = chkAutorun.Checked ? cboxAutorun.SelectedIndex : -1;
            var newImport = chkImport.Checked ? cboxImport.SelectedIndex : -1;
            var newSkipCN = chkIsInjectSkipCNSite.Checked ? cboxIsInjectSkipCNSite.SelectedIndex : -1;
            var isPortAutoIncrease = chkIncrement.Checked;


            ModifyServersSetting(
                list,
                newMode, newIP, newPort, isPortAutoIncrease,
                newMark, newAutorun, newImport, newSkipCN);
        }

        #endregion

        #region private method
        void ModifyServersSetting(
            List<Controller.CoreServerCtrl> list,
            int newMode, string newIP, int newPort, bool isPortAutoIncrease,
            string newMark, int newAutorun, int newImport, int newSkipCN)
        {
            Action<int, Action> worker = (index, next) =>
            {
                var portNumber = isPortAutoIncrease ? newPort + index : newPort;

                var server = list[index];
                if (!server.GetCoreCtrl().IsCoreRunning())
                {
                    ModifyServerSetting(
                        ref server,
                        newMode, newIP, portNumber,
                        newMark, newAutorun, newImport, newSkipCN);
                    server.InvokeEventOnPropertyChange();
                    next();
                    return;
                }

                server.GetCoreCtrl().StopCoreThen(() =>
                {
                    ModifyServerSetting(
                        ref server,
                        newMode, newIP, portNumber,
                        newMark, newAutorun, newImport, newSkipCN);
                    server.GetCoreCtrl().RestartCoreThen();
                    next();
                });
            };

            var that = this;
            Action done = () =>
            {
                servers.UpdateMarkList();
                VgcApis.Libs.UI.RunInUiThread(btnModify, () =>
                {
                    that.Close();
                });
            };

            Lib.Utils.ChainActionHelperAsync(list.Count, worker, done);

        }

        void ModifyServerSetting(
            ref Controller.CoreServerCtrl serverCtrl,
            int newMode, string newIP, int newPort,
            string newMark, int newAutorun, int newImport, int newSkipCN)
        {
            var server = serverCtrl.GetStates().GetAllRawCoreInfo();

            if (newSkipCN >= 0)
            {
                server.isInjectSkipCNSite = newSkipCN == 0;
            }

            if (newAutorun >= 0)
            {
                server.isAutoRun = newAutorun == 0;
            }

            if (newImport >= 0)
            {
                server.isInjectImport = newImport == 0;
            }

            if (newMode >= 0)
            {
                server.customInbType = newMode;
            }

            if (newIP != null)
            {
                server.inbIp = newIP;
            }
            if (newPort >= 0)
            {
                server.inbPort = newPort;
            }

            if (newMark != null)
            {
                server.customMark = newMark;
            }
        }

        #endregion
    }
}
