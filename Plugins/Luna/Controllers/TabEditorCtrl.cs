using Luna.Resources.Langs;
using ScintillaNET;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Forms;

namespace Luna.Controllers
{
    internal sealed class TabEditorCtrl
    {
        Services.Settings settings;
        Services.LuaServer luaServer;
        VgcApis.Models.IServices.IApiService api;
        VgcApis.Models.IServices.IConfigMgrService configMgr;
        VgcApis.Models.IServices.IServersService vgcServers;
        LuaCoreCtrl luaCoreCtrl;
        VgcApis.WinForms.FormSearch formSearch = null;
        VgcApis.Libs.Views.RepaintCtrl repaintCtrl;

        #region controls
        Scintilla luaEditor;
        ComboBox cboxScriptName;
        Button btnNewScript,
            btnSaveScript,
            btnDeleteScript,
            btnRunScript,
            btnStopScript,
            btnKillScript,
            btnClearOutput;

        RichTextBox rtboxOutput;
        Panel pnlEditorContainer;
        #endregion

        string preScriptName = string.Empty;
        string preScriptContent = string.Empty;

        public TabEditorCtrl(
            ComboBox cboxScriptName,
            Button btnNewScript,
            Button btnSaveScript,
            Button btnDeleteScript,
            Button btnRunScript,
            Button btnStopScript,
            Button btnKillScript,
            Button btnClearOutput,
            RichTextBox rtboxOutput,
            Panel pnlEditorContainer)
        {
            BindControls(
                cboxScriptName,
                btnNewScript,
                btnSaveScript,
                btnDeleteScript,
                btnRunScript,
                btnStopScript,
                btnKillScript,
                btnClearOutput,
                rtboxOutput,
                pnlEditorContainer);
        }

        public void Run(
          VgcApis.Models.IServices.IApiService api,
          Services.Settings settings,
          Services.LuaServer luaServer)
        {
            this.api = api;
            this.configMgr = api.GetConfigMgrService();
            this.vgcServers = api.GetServersService();

            this.settings = settings;
            this.luaServer = luaServer;
            this.luaCoreCtrl = CreateLuaCoreCtrl(
                settings, api);

            InitControls();
            BindEvents();

            ReloadScriptName();
            if (cboxScriptName.Items.Count > 0)
            {
                cboxScriptName.SelectedIndex = 0;
            }

            repaintCtrl = new VgcApis.Libs.Views.RepaintCtrl(rtboxOutput);

            updateOutputTimer.Tick += UpdateOutput;
            updateOutputTimer.Start();
        }


        #region properties 
        ConcurrentQueue<string> _logCache = new ConcurrentQueue<string>();
        long logCacheUpdateTimeStamp = DateTime.Now.Ticks;
        string logCache
        {
            get => string.Join(Environment.NewLine, _logCache);
            set
            {
                _logCache.Enqueue(value);
                logCacheUpdateTimeStamp = DateTime.Now.Ticks;
                VgcApis.Libs.Utils.TrimDownConcurrentQueue(
                    _logCache, 1000, 300);
            }
        }
        #endregion

        #region public methods
        public void KeyBoardShortcutHandler(KeyEventArgs keyEvent)
        {
            var keyCode = keyEvent.KeyCode;
            if (keyEvent.Control)
            {
                switch (keyCode)
                {
                    case Keys.F:
                        ShowFormSearch();
                        break;
                    case Keys.S:
                        OnBtnSaveScriptClickHandler(false);
                        break;
                    case Keys.N:
                        ClearEditor();
                        break;
                }
                return;
            }

            switch (keyCode)
            {
                case Keys.F5:
                    btnRunScript.PerformClick();
                    break;
                case Keys.F6:
                    btnStopScript.PerformClick();
                    break;
                case Keys.F7:
                    btnKillScript.PerformClick();
                    break;
                case Keys.F8:
                    btnClearOutput.PerformClick();
                    break;
            }
        }

        public bool IsChanged()
        {
            var script = luaEditor.Text;
            if (script == preScriptContent)
            {
                return false;
            }
            return true;
        }


        LuaCoreCtrl CreateLuaCoreCtrl(
            Services.Settings settings,
            VgcApis.Models.IServices.IApiService api)
        {
            var luaApis = new Models.Apis.LuaApis(settings, api);
            luaApis.SetRedirectLogWorker(Log);

            var coreSettings = new Models.Data.LuaCoreSetting();

            var ctrl = new LuaCoreCtrl();
            ctrl.Run(settings, coreSettings, luaApis);
            return ctrl;
        }

        public void Cleanup()
        {
            formSearch?.Close();

            updateOutputTimer.Stop();
            updateOutputTimer.Tick -= UpdateOutput;
            updateOutputTimer.Dispose();

            luaCoreCtrl?.Kill();
        }

        public string GetCurrentEditorContent() => luaEditor.Text;

        public void SetCurrentEditorContent(string content) =>
            VgcApis.Libs.UI.RunInUiThread(
                luaEditor, () => luaEditor.Text = content);

        public bool SaveScript()
        {
            var scriptName = cboxScriptName.Text;
            var content = luaEditor.Text;
            var success = luaServer.AddOrReplaceScript(scriptName, content);

            if (success)
            {
                preScriptContent = content;
            }

            return success;
        }
        #endregion

        #region private methods
        void ShowFormSearch()
        {
            if (formSearch != null)
            {
                formSearch.Activate();
                return;
            }

            formSearch = new VgcApis.WinForms.FormSearch(luaEditor);
            formSearch.FormClosed += (s, a) => formSearch = null;
        }

        void Log(string content)
        {
            logCache = content;
        }

        Timer updateOutputTimer = new Timer { Interval = 500 };
        readonly object updateOutputLocker = new object();
        bool isOutputUpdating = false;
        long updateOutputTimeStamp = 0;

        void UpdateOutput(object sender, EventArgs args)
        {
            lock (updateOutputLocker)
            {
                if (isOutputUpdating || updateOutputTimeStamp == logCacheUpdateTimeStamp)
                {
                    return;
                }
                isOutputUpdating = true;
            }

            // form maybe closed
            try
            {
                repaintCtrl.Disable();
                updateOutputTimeStamp = logCacheUpdateTimeStamp;
                rtboxOutput.Text = logCache;
                VgcApis.Libs.UI.ScrollToBottom(rtboxOutput);
                repaintCtrl.Enable();
            }
            catch { }

            lock (updateOutputLocker)
            {
                isOutputUpdating = false;
            }
        }

        private void BindEvents()
        {
            btnNewScript.Click += (s, a) => ClearEditor();

            btnKillScript.Click += (s, a) => luaCoreCtrl.Kill();

            btnStopScript.Click += (s, a) => luaCoreCtrl.Stop();

            btnRunScript.Click += (s, a) =>
            {
                var name = cboxScriptName.Text;

                luaCoreCtrl.Kill();

                luaCoreCtrl.SetScriptName(
                    string.IsNullOrEmpty(name)
                    ? $"({I18N.Empty})" : name);

                luaCoreCtrl.ReplaceScript(luaEditor.Text);
                luaCoreCtrl.Start();
            };

            btnClearOutput.Click += (s, a) =>
            {
                _logCache = new ConcurrentQueue<string>();
                logCacheUpdateTimeStamp = DateTime.Now.Ticks;
            };

            btnDeleteScript.Click += (s, a) =>
            {
                var scriptName = cboxScriptName.Text;
                if (string.IsNullOrEmpty(scriptName)
                || !VgcApis.Libs.UI.Confirm(I18N.ConfirmRemoveScript))
                {
                    return;
                }

                if (!luaServer.RemoveScriptByName(scriptName))
                {
                    VgcApis.Libs.UI.MsgBoxAsync("", I18N.ScriptNotFound);
                }
            };

            btnSaveScript.Click += (s, a) => OnBtnSaveScriptClickHandler(true);

            cboxScriptName.DropDown += (s, a) => ReloadScriptName();

            cboxScriptName.SelectedValueChanged += CboxScriptNameChangedHandler;

        }

        private void OnBtnSaveScriptClickHandler(bool showResult)
        {
            var scriptName = cboxScriptName.Text;
            if (string.IsNullOrEmpty(scriptName))
            {
                VgcApis.Libs.UI.MsgBoxAsync("", I18N.ScriptNameNotSet);
                return;
            }

            var success = SaveScript();
            if (showResult)
            {
                VgcApis.Libs.UI.MsgBoxAsync("", success ? I18N.Done : I18N.Fail);
            }
        }

        private void ClearEditor()
        {
            if (IsChanged() && !VgcApis.Libs.UI.Confirm(I18N.DiscardUnsavedChanges))
            {
                return;
            }
            preScriptName = "";
            preScriptContent = "";
            luaEditor.Text = "";
            cboxScriptName.Text = "";
        }

        void CboxScriptNameChangedHandler(object sender, EventArgs args)
        {
            var name = cboxScriptName.Text;

            if (name == preScriptName)
            {
                return;
            }

            if (IsChanged() && !VgcApis.Libs.UI.Confirm(I18N.DiscardUnsavedChanges))
            {
                cboxScriptName.Text = preScriptName;
                return;
            }

            preScriptName = name;
            preScriptContent = LoadScriptByName(name);
            luaEditor.Text = preScriptContent;
        }

        string LoadScriptByName(string name) =>
            settings.GetLuaCoreSettings()
                .Where(s => s.name == name)
                .FirstOrDefault()
                ?.script
                ?? string.Empty;


        void InitControls()
        {
            // script editor
            luaEditor = Libs.UI.CreateLuaEditor(pnlEditorContainer);
        }

        void ReloadScriptName()
        {
            cboxScriptName.Items.Clear();

            var cores = settings
                .GetLuaCoreSettings()
                .OrderBy(c => c.name)
                .ToList();

            foreach (var coreState in cores)
            {
                cboxScriptName.Items.Add(coreState.name);
            }
        }

        private void BindControls(
            ComboBox cboxScriptName,
            Button btnNewScript,
            Button btnSaveScript,
            Button btnDeleteScript,
            Button btnRunScript,
            Button btnStopScript,
            Button btnKillScript,
            Button btnClearOutput,
            RichTextBox rtboxOutput,
            Panel pnlEditorContainer)
        {
            this.cboxScriptName = cboxScriptName;
            this.btnNewScript = btnNewScript;
            this.btnSaveScript = btnSaveScript;
            this.btnDeleteScript = btnDeleteScript;
            this.btnRunScript = btnRunScript;
            this.btnStopScript = btnStopScript;
            this.btnKillScript = btnKillScript;
            this.btnClearOutput = btnClearOutput;
            this.rtboxOutput = rtboxOutput;
            this.pnlEditorContainer = pnlEditorContainer;
        }
        #endregion
    }
}
