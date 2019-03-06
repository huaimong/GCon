using System.Collections.Generic;

namespace VgcApis.Models.Consts
{
    public static class Config
    {
        public const string ProtocolNameVmess = @"vmess";
        public const string ProtocolNameSs = @"shadowsocks";

        public const string JsonArray = @"[]";
        public const string JsonDict = @"{}";

        public const string ConfigDotJson = "config.json";

        public static Dictionary<string, string> GetDefCfgSections() =>
            new Dictionary<string, string>
            {
                { "config.json", JsonDict},
                { "v2raygcon", JsonDict},
                { "log", JsonDict},
                { "inbounds", JsonArray},
                { "outbounds", JsonArray},
                { "routing", JsonDict},
                { "policy", JsonDict},
                { "api", JsonDict},
                { "dns", JsonDict},
                { "stats", JsonDict},
                { "transport", JsonDict},
                { "reverse", JsonDict},
            };
    }
}
