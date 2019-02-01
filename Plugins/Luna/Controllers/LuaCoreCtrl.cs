using Luna.Resources.Langs;
using NLua;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Luna.Controllers
{
    public class LuaCoreCtrl
    {
        public EventHandler OnStateChange;

        Services.Settings settings;
        Models.Data.LuaCoreSetting coreSetting;
        VgcApis.Models.Interfaces.ILuaApis luaApis;
        VgcApis.Models.BaseClasses.LuaSignal luaSignal;

        Thread luaCoreThread;
        Task luaCoreTask;

        readonly object coreStateLocker = new object();

        public LuaCoreCtrl() { }

        #region properties 
        public string name => coreSetting.name;

        public bool isAutoRun
        {
            get => coreSetting.isAutorun;
            set
            {
                if (coreSetting.isAutorun == value)
                {
                    return;
                }

                coreSetting.isAutorun = value;
                Save();
            }
        }

        bool _isRunning = false;
        public bool isRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;
                if (_isRunning == false)
                {
                    SendLog($"{coreSetting.name} {I18N.Stopped}");
                    luaCoreTask = null;
                    luaCoreThread = null;
                }
                InvokeOnStateChangeIgnoreError();
            }
        }

        void InvokeOnStateChangeIgnoreError()
        {
            try
            {
                OnStateChange?.Invoke(null, null);
            }
            catch { }
        }
        #endregion

        #region public methods
        public void SetScriptName(string name)
        {
            coreSetting.name = name;
        }

        public void ReplaceScript(string script)
        {
            coreSetting.script = script;
            Save();
        }

        public void Stop()
        {
            lock (coreStateLocker)
            {
                if (!isRunning)
                {
                    return;
                }
            }

            SendLog($"{I18N.SendStopSignalTo} {coreSetting.name}");
            luaSignal.SetStopSignal(true);
        }

        public void Kill()
        {
            if (luaCoreTask == null)
            {
                return;
            }

            SendLog($"{I18N.Terminate} {coreSetting.name}");

            luaSignal.SetStopSignal(true);
            if (luaCoreTask.Wait(2000))
            {
                return;
            }

            try
            {
                luaCoreThread?.Abort();
            }
            catch { }
            isRunning = false;
        }

        public void Start()
        {
            lock (coreStateLocker)
            {
                if (isRunning)
                {
                    return;
                }
                isRunning = true;
            }

            SendLog($"{I18N.Start} {coreSetting.name}");
            luaCoreTask = Task.Factory.StartNew(
                RunLuaScript,
                TaskCreationOptions.LongRunning);
        }

        public void Cleanup()
        {
            Kill();
        }

        public void Run(
            Services.Settings settings,
            Models.Data.LuaCoreSetting luaCoreState,
            VgcApis.Models.Interfaces.ILuaApis luaApis)
        {
            this.settings = settings;
            this.coreSetting = luaCoreState;
            this.luaApis = luaApis;
            this.luaSignal = new VgcApis.Models.BaseClasses.LuaSignal();
        }
        #endregion

        #region private methods
        void SendLog(string content)
            => luaApis.Print(content);

        void RunLuaScript()
        {
            luaSignal.ResetAllSignals();
            luaCoreThread = Thread.CurrentThread;

            try
            {
                var core = CreateLuaCore();
                core.DoString(coreSetting.script);
            }
            catch (Exception e)
            {
                SendLog($"[{coreSetting.name}] {e.ToString()}");
            }
            isRunning = false;
        }

        Lua CreateLuaCore()
        {
            var state = new Lua();
            state["Api"] = luaApis;
            state["Signal"] = luaSignal;
            state.DoString(luaApis.PerdefinedFunctions());
            return state;
        }

        void Save() => settings.SaveSettings();

        #endregion
    }
}
