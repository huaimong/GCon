using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using VgcApis.Resources.Langs;

namespace VgcApis.Libs
{
    public static class UI
    {
        #region update ui
        /// <summary>
        /// If control==null return;
        /// </summary>
        /// <param name="control">invokeable control</param>
        /// <param name="updateUi">UI updater</param>
        public static void RunInUiThread(Control control, Action updateUi)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                control.Invoke((MethodInvoker)delegate
                {
                    updateUi();
                });
            }
            else
            {
                updateUi();
            }
        }

        // https://stackoverflow.com/questions/87795/how-to-prevent-flickering-in-listview-when-updating-a-single-listviewitems-text
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }

        public static void ScrollToBottom(RichTextBox control)
        {
            control.SelectionStart = control.Text.Length;
            control.ScrollToCaret();
        }
        #endregion

        #region file
        /* return:
         * 
         * Null means cancelled.
         * string.Empty means file is empty or error occurred.
         */
        public static string ShowReadFileDialog(string extension, out string fileName)
        {
            OpenFileDialog readFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ShowHelp = true,
            };

            fileName = string.Empty;

            if (readFileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            fileName = readFileDialog.FileName;
            var content = string.Empty;
            try
            {
                content = File.ReadAllText(fileName);
            }
            catch { }
            return content;
        }

        public static Models.Datas.Enum.SaveFileErrorCode ShowSaveFileDialog(
            string extension, string content, out string fileName)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                Title = I18N.SaveAs,
                ShowHelp = true,
            };

            saveFileDialog.ShowDialog();

            fileName = saveFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                return Models.Datas.Enum.SaveFileErrorCode.Cancel;
            }

            try
            {
                File.WriteAllText(fileName, content);
                return Models.Datas.Enum.SaveFileErrorCode.Success;
            }
            catch { }
            return Models.Datas.Enum.SaveFileErrorCode.Fail;
        }

        /// <summary>
        /// Return file name.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string ShowSelectFileDialog(string extension)
        {
            OpenFileDialog readFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = extension,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true,
                ShowHelp = true,
            };

            var fileName = string.Empty;

            if (readFileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return readFileDialog.FileName;
        }

        #endregion

        #region popup
        public static void VisitUrl(string msg, string url)
        {
            var text = string.Format("{0}\n{1}", msg, url);
            if (Confirm(text))
            {
                Task.Factory.StartNew(() => System.Diagnostics.Process.Start(url));
            }
        }

        public static void MsgBox(string title, string content)
        {
            MessageBox.Show(content ?? string.Empty, title ?? string.Empty);
        }

        public static void MsgBoxAsync(string title, string content)
        {
            Task.Factory.StartNew(() => MsgBox(title, content));
        }

        public static bool Confirm(string content)
        {
            var confirm = MessageBox.Show(
                content,
                I18N.Confirm,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            return confirm == DialogResult.Yes;
        }

        #endregion

        #region winform
        public static void AutoSetFormIcon(Form form)
        {
#if DEBUG
            form.Icon = Properties.Resources.icon_light;
#else
            form.Icon = Properties.Resources.icon_dark;
#endif
        }

        public static System.Drawing.Icon GetAppIcon()
        {
#if DEBUG
            return Properties.Resources.icon_light;
#else
            return Properties.Resources.icon_dark;
#endif
        }
        #endregion
    }
}
