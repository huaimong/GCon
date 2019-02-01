using Luna.Resources.Langs;
using System.Windows.Forms;

namespace Luna.Controllers
{
    internal sealed class MenuCtrl
    {
        TabEditorCtrl editorCtrl;
        Views.WinForms.FormMain formMain;

        public MenuCtrl(
            Views.WinForms.FormMain formMain,
            TabEditorCtrl editorCtrl,
            ToolStripMenuItem miLoad,
            ToolStripMenuItem miSaveAs,
            ToolStripMenuItem miExit)
        {
            BindControls(formMain, editorCtrl);
            NewMethod(miLoad, miSaveAs, miExit);
        }

        private void NewMethod(ToolStripMenuItem miLoad, ToolStripMenuItem miSaveAs, ToolStripMenuItem miExit)
        {
            // event handling
            miExit.Click += (s, a) =>
            {
                VgcApis.Libs.UI.RunInUiThread(
                    this.formMain,
                    () => this.formMain.Close());
            };

            miLoad.Click += (s, a) =>
            {
                if (editorCtrl.IsChanged()
                    && !VgcApis.Libs.UI.Confirm(I18N.DiscardUnsavedChanges))
                {
                    return;
                }

                string script = VgcApis.Libs.UI.ShowReadFileDialog(
                    VgcApis.Models.Consts.Files.LuaExt,
                    out string filename);

                // user cancelled.
                if (script == null)
                {
                    return;
                }

                editorCtrl.SetCurrentEditorContent(script);
            };

            miSaveAs.Click += (s, a) =>
            {
                var script = editorCtrl.GetCurrentEditorContent();

                var result = VgcApis.Libs.UI.ShowSaveFileDialog(
                    VgcApis.Models.Consts.Files.LuaExt,
                    script,
                    out string filename);

                switch (result)

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

            };
        }

        private void BindControls(Views.WinForms.FormMain formMain, TabEditorCtrl editorCtrl)
        {
            // binding
            this.editorCtrl = editorCtrl;
            this.formMain = formMain;
        }
    }
}
