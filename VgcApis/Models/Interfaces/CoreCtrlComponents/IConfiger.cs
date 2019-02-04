using Newtonsoft.Json.Linq;
using System;

namespace VgcApis.Models.Interfaces.CoreCtrlComponents
{
    public interface IConfiger
    {
        string GetConfig();

        bool IsSuitableToBeUsedAsSysProxy(
          bool isGlobal,
          out bool isSocks,
          out int port);

        void UpdateSummaryThen(Action next = null);
        void SetConfig(string newConfig);

        void GetterInboundInfoThen(Action<string> next);
    }
}
