using System;
using System.Drawing;
using System.Windows.Forms;

namespace Luna.Views.UserControls
{
    public partial class LuaUI : UserControl
    {
        Controllers.LuaCoreCtrl luaCoreCtrl;

        public LuaUI(Controllers.LuaCoreCtrl luaCoreCtrl)
        {
            this.luaCoreCtrl = luaCoreCtrl;
            InitializeComponent();
        }

        private void LuaUI_Load(object sender, EventArgs e)
        {
            this.lbName.Text = luaCoreCtrl.name;
            this.chkIsAutoRun.Checked = luaCoreCtrl.isAutoRun;

            UpdateRunningState();
            luaCoreCtrl.OnStateChange += OnLuaCoreStateChangeHandler;

        }

        #region public methods

        public void Cleanup()
        {
            luaCoreCtrl.OnStateChange -= OnLuaCoreStateChangeHandler;
        }


        #endregion

        #region private methods
        void OnLuaCoreStateChangeHandler(object sender, EventArgs args)
        {
            RunInUiThread(() =>
            {
                UpdateRunningState();
            });
        }

        void RunInUiThread(Action updater)
        {
            VgcApis.Libs.UI.RunInUiThread(lbName, () =>
            {
                updater();
            });
        }

        void UpdateRunningState()
        {
            var isRunning = luaCoreCtrl.isRunning;
            var text = isRunning ? "ON" : "OFF";
            if (lbRunningState.Text == text)
            {
                return;
            }

            lbRunningState.Text = text;
            lbRunningState.ForeColor = isRunning ? Color.DarkOrange : Color.Green;
        }
        #endregion

        private void chkIsAutoRun_CheckedChanged(object sender, EventArgs e)
        {
            var isAutoRun = chkIsAutoRun.Checked;
            luaCoreCtrl.isAutoRun = isAutoRun;
        }

        private void btnKill_Click(object sender, EventArgs e)
        {
            luaCoreCtrl.Kill();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            luaCoreCtrl.Stop();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            luaCoreCtrl.Start();
        }
    }
}
