using System;
using System.Collections.Generic;
using System.Linq;

namespace Luna.Models.Apis
{
    public class LuaApis :
        VgcApis.Models.Interfaces.ILuaApis
    {
        Services.Settings settings;
        VgcApis.Models.IServices.IServersService vgcServers;
        VgcApis.Models.IServices.IConfigMgrService configMgr;
        Action<string> redirectLogWorker;

        public LuaApis() { }

        #region ILuaApis

        public long RunSpeedTest(string rawConfig) =>
            configMgr.RunSpeedTest(rawConfig);

        public List<VgcApis.Models.Interfaces.ICoreServCtrl> GetAllServers() =>
            vgcServers.GetAllServersOrderByIndex().ToList();

        public void Sleep(int milliseconds) =>
            System.Threading.Thread.Sleep(milliseconds);

        public void Print(params object[] contents)
        {
            var text = "";
            foreach (var c in contents)
            {
                text += c.ToString();
            }
            redirectLogWorker(text);
        }
        #endregion

        #region public methods
        public void SetRedirectLogWorker(Action<string> worker)
        {
            if (worker != null)
            {
                redirectLogWorker = worker;
            }
        }

        public void Run(
            Services.Settings settings,
            VgcApis.Models.IServices.IServersService vgcServers,
            VgcApis.Models.IServices.IConfigMgrService configMgr)
        {
            this.settings = settings;
            this.configMgr = configMgr;
            this.vgcServers = vgcServers;
            this.redirectLogWorker = settings.SendLog;
        }

        public string PerdefinedFunctions() =>
            VgcApis.Models.Consts.Libs.LuaPerdefinedFunctions;
        #endregion

        #region private methods
        #endregion
    }
}
