using Newtonsoft.Json.Linq;
using System;

namespace VgcApis.Models.Interfaces.CoreCtrlComponents
{
    public interface IConfig
    {
        bool IsSuitableToBeUsedAsSysProxy(
          bool isGlobal,
          out bool isSocks,
          out int port);

        void UpdateSummaryThen(Action next = null);
        void ChangeCoreConfig(string newConfig);

        void GetterInboundInfoFor(Action<string> next);

        bool OverwriteInboundSettings(
            ref JObject config,
            int inboundType,
            string ip,
            int port);

        string InjectGlobalImport(
            string config,
            bool isIncludeSpeedTest,
            bool isIncludeActivate);

        JObject GetDecodedConfig(
            bool isUseCache,
            bool isIncludeSpeedTest,
            bool isIncludeActivate);
    }
}
