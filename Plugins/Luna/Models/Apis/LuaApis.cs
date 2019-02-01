using System;
using System.Collections.Generic;
using System.Linq;

namespace Luna.Models.Apis
{
    public class LuaApis : VgcApis.Models.Interfaces.ILuaApis
    {
        Services.Settings settings;
        VgcApis.Models.IServices.IServersService vgcServers;
        Action<string> redirectLogWorker;

        public LuaApis() { }

        #region ILuaApis
        public string PerdefinedFunctions() => @"
import = function () end
                
-- copy from NLua
function Each(o)
    local e = o:GetEnumerator()
    return function()
        if e:MoveNext() then
        return e.Current
        end
    end
end";

        public List<VgcApis.Models.IControllers.ICoreCtrl> GetAllServers() =>
            vgcServers.GetAllServersList().ToList();

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
            VgcApis.Models.IServices.IServersService vgcServers)
        {
            this.settings = settings;
            this.vgcServers = vgcServers;
            this.redirectLogWorker = settings.SendLog;
        }
        #endregion

        #region private methods
        #endregion
    }
}
