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
        VgcApis.Libs.Views.RepaintCtrl repaintCtrl;

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
            repaintCtrl = new VgcApis.Libs.Views.RepaintCtrl(rtBoxLogger);
            updateLogTimer.Tick += UpdateLog;
            updateLogTimer.Start();
        }

        readonly object updateLogLocker = new object();

        long updateTimeStamp = DateTime.Now.Ticks;


        VgcApis.Libs.Tasks.Bar bar = new VgcApis.Libs.Tasks.Bar();
        void UpdateLog(object sender, EventArgs args)
        {
            if (!bar.Install())
            {
                return;
            }

            var timestamp = setting.GetLogTimestamp();

            if (updateTimeStamp == timestamp)
            {
                bar.Remove();
                return;
            }

            try
            {
                repaintCtrl.Disable();
                rtBoxLogger.Text = setting.GetLogContent();
                ScrollToBottom();
                updateTimeStamp = timestamp;
                repaintCtrl.Enable();
            }
            catch { }
            finally
            {
                bar.Remove();
            }
        }
    }
}
