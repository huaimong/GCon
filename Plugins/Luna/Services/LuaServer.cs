using System;
using System.Collections.Generic;
using System.Linq;

namespace Luna.Services
{
    public class LuaServer:
        VgcApis.Models.BaseClasses.Disposable
    {
        public EventHandler OnLuaCoreCtrlListChange;

        Settings settings;
        List<Controllers.LuaCoreCtrl> luaCoreCtrls;
        Models.Apis.LuaApis luaApis;

        public LuaServer() { }

        public void Run(
           Settings settings,
           VgcApis.Models.IServices.IApiService api)
        {
            this.settings = settings;
            this.luaApis = new Models.Apis.LuaApis(settings, api);

            luaCoreCtrls = InitLuaCores(settings, luaApis);
            WakeUpAutoRunScripts();
        }

        #region public methods
        public List<Controllers.LuaCoreCtrl> GetAllLuaCoreCtrls()
        {
            var list = luaCoreCtrls ?? new List<Controllers.LuaCoreCtrl>();
            return list.OrderBy(c => c.name).ToList();
        }

        public bool RemoveScriptByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var coreCtrl = luaCoreCtrls.FirstOrDefault(c => c.name == name);
            if (coreCtrl == null)
            {
                return false;
            }

            coreCtrl.Kill();
            luaCoreCtrls.Remove(coreCtrl);

            settings.GetLuaCoreSettings().RemoveAll(s => s.name == name);
            Save();
            InvokeOnLuaCoreCtrlListChangeIgnoreError();
            return true;
        }

        public bool AddOrReplaceScript(string name, string script)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var coreCtrl = luaCoreCtrls
                .FirstOrDefault(c => c.name == name);

            if (coreCtrl != null)
            {
                coreCtrl.ReplaceScript(script);
                return true;
            }

            var coreState = new Models.Data.LuaCoreSetting
            {
                name = name,
                script = script,
            };

            settings.GetLuaCoreSettings().Add(coreState);

            coreCtrl = new Controllers.LuaCoreCtrl();
            luaCoreCtrls.Add(coreCtrl);
            coreCtrl.Run(settings, coreState, luaApis);
            Save();
            InvokeOnLuaCoreCtrlListChangeIgnoreError();
            return true;
        }

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            if (luaCoreCtrls == null)
            {
                return;
            }

            foreach (var ctrl in luaCoreCtrls)
            {
                ctrl.Kill();
            }
        }
        #endregion

        #region private methods
        void InvokeOnLuaCoreCtrlListChangeIgnoreError()
        {
            try
            {
                OnLuaCoreCtrlListChange?.Invoke(null, null);
            }
            catch { }
        }

        void Save() => settings.SaveSettings();

        void WakeUpAutoRunScripts()
        {
            var list = luaCoreCtrls.Where(c => c.isAutoRun).ToList();
            if (list.Count() <= 0)
            {
                return;
            }
            foreach (var core in list)
            {
                core.Start();
            }
        }

        List<Controllers.LuaCoreCtrl> InitLuaCores(
            Settings settings,
            VgcApis.Models.Interfaces.ILuaApis luaApis)
        {
            var cores = new List<Controllers.LuaCoreCtrl>();
            foreach (var luaCoreState in settings.GetLuaCoreSettings())
            {
                var luaCtrl = new Controllers.LuaCoreCtrl();
                luaCtrl.Run(settings, luaCoreState, luaApis);
                cores.Add(luaCtrl);
            }
            return cores;
        }
        #endregion
    }
}
