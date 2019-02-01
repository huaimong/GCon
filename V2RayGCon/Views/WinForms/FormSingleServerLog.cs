using System;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormSingleServerLog : Form
    {
        Controller.CoreServerCtrl serverItem;
        Timer updateLogTimer = new Timer { Interval = 500 };

        public FormSingleServerLog(Controller.CoreServerCtrl serverItem)
        {

            this.serverItem = serverItem;

            InitializeComponent();

            this.FormClosed += (s, e) =>
            {
                if (updateLogTimer != null)
                {
                    updateLogTimer.Stop();
                    updateLogTimer.Tick -= UpdateLog;
                    updateLogTimer.Dispose();
                }
            };

            VgcApis.Libs.UI.AutoSetFormIcon(this);
            this.Show();
            this.Text = I18N.Log + " - " + serverItem.GetTitle();
        }

        private void FormSingleServerLog_Load(object sender, System.EventArgs e)
        {
            updateLogTimer.Tick += UpdateLog;
            updateLogTimer.Start();
        }

        private void ScrollToBottom()
        {
            rtBoxLogger.SelectionStart = rtBoxLogger.Text.Length;
            rtBoxLogger.ScrollToCaret();
        }

        readonly object updateLogLocker = new object();
        bool isUpdating = false;
        long updateTimeStamp = DateTime.Now.Ticks;

        void UpdateLog(object sender, EventArgs args)
        {
            lock (updateLogLocker)
            {
                if (isUpdating || updateTimeStamp == serverItem.logTimeStamp)
                {
                    return;
                }
                isUpdating = true;
            }

            try
            {
                updateTimeStamp = serverItem.logTimeStamp;
                rtBoxLogger.Text = serverItem.logCache;
                ScrollToBottom();
            }
            catch { }

            lock (updateLogLocker)
            {
                isUpdating = false;
            }

        }
    }
}
