using System.IO;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.FormMainComponent
{
    class MenuItemsBasic : FormMainComponentController
    {
        Service.Servers servers;

        public MenuItemsBasic(
            ToolStripMenuItem miSimVmessServer,
            ToolStripMenuItem miImportLinkFromClipboard,
            ToolStripMenuItem miExportAllServer,
            ToolStripMenuItem importFromFile,
            ToolStripMenuItem miAbout,
            ToolStripMenuItem miHelp,
            ToolStripMenuItem miFormConfigEditor,
            ToolStripMenuItem miFormQRCode,
            ToolStripMenuItem miFormLog,
            ToolStripMenuItem miFormOptions,
            ToolStripMenuItem miDownloadV2rayCore,
            ToolStripMenuItem miRemoveV2rayCore)
        {
            servers = Service.Servers.Instance;

            InitMenuFile(miSimVmessServer, miImportLinkFromClipboard, miExportAllServer, importFromFile);
            InitMenuWindows(miFormConfigEditor, miFormQRCode, miFormLog, miFormOptions);
            InitMenuAbout(miAbout, miHelp, miDownloadV2rayCore, miRemoveV2rayCore);
        }

        #region public method
        public void ImportServersFromTextFile()
        {
            string v2rayLinks = VgcApis.Libs.UI.ShowReadFileDialog(StrConst.ExtText, out string filename);

            if (v2rayLinks == null)
            {
                return;
            }

            servers.ImportLinksWithV2RayLinks(v2rayLinks);
        }

        public void ExportAllServersToTextFile()
        {
            if (this.servers.IsEmpty())
            {
                MessageBox.Show(I18N.ServerListIsEmpty);
                return;
            }

            var serverList = servers.GetServerList();
            string s = string.Empty;

            foreach (var server in serverList)
            {
                var vlink = Lib.Utils.AddLinkPrefix(
                    Lib.Utils.Base64Encode(server.config),
                    Model.Data.Enum.LinkTypes.v2ray);

                s += vlink + System.Environment.NewLine + System.Environment.NewLine;
            }

            switch (VgcApis.Libs.UI.ShowSaveFileDialog(
                StrConst.ExtText,
                s,
                out string filename))
            {
                case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Success:
                    MessageBox.Show(I18N.Done);
                    break;
                case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Fail:
                    MessageBox.Show(I18N.WriteFileFail);
                    break;
                case VgcApis.Models.Datas.Enum.SaveFileErrorCode.Cancel:
                    // do nothing
                    break;
            }
        }

        public override bool RefreshUI() { return false; }
        public override void Cleanup() { }
        #endregion

        #region private method
        private void InitMenuAbout(ToolStripMenuItem about, ToolStripMenuItem help, ToolStripMenuItem downloadV2rayCore, ToolStripMenuItem removeV2rayCore)
        {
            // menu about
            downloadV2rayCore.Click += (s, a) => Views.WinForms.FormDownloadCore.GetForm();

            removeV2rayCore.Click += (s, a) => RemoveV2RayCore();

            about.Click += (s, a) =>
                Lib.UI.VisitUrl(I18N.VistPorjectPage, Properties.Resources.ProjectLink);

            help.Click += (s, a) =>
                Lib.UI.VisitUrl(I18N.VistWikiPage, Properties.Resources.WikiLink);
        }

        private void InitMenuFile(ToolStripMenuItem simVmessServer, ToolStripMenuItem importLinkFromClipboard, ToolStripMenuItem exportAllServer, ToolStripMenuItem importFromFile)
        {
            // menu file
            simVmessServer.Click +=
                (s, a) => Views.WinForms.FormSimAddVmessClient.GetForm();

            importLinkFromClipboard.Click += (s, a) =>
            {
                string links = Lib.Utils.GetClipboardText();
                servers.ImportLinksWithV2RayLinks(links);
            };

            exportAllServer.Click += (s, a) => ExportAllServersToTextFile();

            importFromFile.Click += (s, a) => ImportServersFromTextFile();
        }

        private static void InitMenuWindows(ToolStripMenuItem miFormConfigEditor, ToolStripMenuItem miFormQRCode, ToolStripMenuItem miFormLog, ToolStripMenuItem miFormOptions)
        {
            // menu window
            miFormConfigEditor.Click += (s, a) => new Views.WinForms.FormConfiger();

            miFormQRCode.Click += (s, a) => Views.WinForms.FormQRCode.GetForm();

            miFormLog.Click += (s, a) => Views.WinForms.FormLog.GetForm();

            miFormOptions.Click += (s, a) => Views.WinForms.FormOption.GetForm();
        }

        private void RemoveV2RayCore()
        {
            if (!Lib.UI.Confirm(I18N.ConfirmRemoveV2RayCore))
            {
                return;
            }

            if (!Directory.Exists(Lib.Utils.GetSysAppDataFolder()))
            {
                MessageBox.Show(I18N.Done);
                return;
            }

            servers.StopAllServersThen(() =>
            {
                try
                {
                    Lib.Utils.DeleteAppDataFolder();
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show(I18N.FileInUse);
                    return;
                }
                MessageBox.Show(I18N.Done);
            });
        }
        #endregion
    }
}
