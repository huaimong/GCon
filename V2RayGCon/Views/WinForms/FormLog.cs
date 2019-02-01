using System;
using System.Windows.Forms;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormLog : Form
    {
        #region Sigleton
        static FormLog _instant;
        public static FormLog GetForm()
        {
            if (_instant == null || _instant.IsDisposed)
            {
                _instant = new FormLog();
            }
            return _instant;
        }
        #endregion

        Service.Setting setting;

        FormLog()
        {
            setting = Service.Setting.Instance;

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

            Lib.UI.SetFormLocation<FormLog>(this, Model.Data.Enum.FormLocations.BottomLeft);
            VgcApis.Libs.UI.AutoSetFormIcon(this);
            this.Show();
        }

        private void ScrollToBottom()
        {
            rtBoxLogger.SelectionStart = rtBoxLogger.Text.Length;
            rtBoxLogger.ScrollToCaret();
        }

        Timer updateLogTimer = new Timer { Interval = 500 };
        private void FormLog_Load(object sender, System.EventArgs e)
        {

            updateLogTimer.Tick += UpdateLog;
            updateLogTimer.Start();
        }

        readonly object updateLogLocker = new object();
        bool isUpdating = false;
        long updateTimeStamp = DateTime.Now.Ticks;

        void UpdateLog(object sender, EventArgs args)
        {
            lock (updateLogLocker)
            {
                if (isUpdating || updateTimeStamp == setting.logTimeStamp)
                {
                    return;
                }
                isUpdating = true;
            }

            try
            {
                updateTimeStamp = setting.logTimeStamp;
                rtBoxLogger.Text = setting.logCache;
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
