using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace V2RayGCon.Service.ShareLinkComponents.VeeCodecs
{
    internal sealed class Vee0a :
        VgcApis.Models.BaseClasses.Plugable<VeeDecoder>,
        IVeeDecoder
    {
        Cache cache;

        public Vee0a(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public string GetSupportedVersion() =>
           Model.VeeShareLinks.Ver0a.SupportedVersion();

        public byte[] Config2Bytes(JObject config)
        {
            var vee = Config2Vee(config);
            return vee?.ToBytes();
        }

        public Tuple<JObject, JToken> Bytes2Config(byte[] bytes)
        {
            var veeLink = new Model.VeeShareLinks.Ver0a(bytes);
            return VeeToConfig(veeLink);
        }

        #endregion

        #region private methods
        Model.VeeShareLinks.Ver0a Config2Vee(JObject config)
        {
            var GetStr = Lib.Utils.GetStringByPrefixAndKeyHelper(config);
            var isUseV4 = (GetStr("outbounds.0", "protocol")?.ToLower()) == "vmess";
            var root = isUseV4 ? "outbounds.0" : "outbound";
            if (!isUseV4)
            {
                var protocol = GetStr(root, "protocol")?.ToLower();
                if (protocol == null || protocol != "vmess")
                {
                    return null;
                }
            }

            var mainPrefix = root + "." + "settings.vnext.0";
            var vee = new Model.VeeShareLinks.Ver0a
            {
                alias = GetStr("v2raygcon", "alias"),
                address = GetStr(mainPrefix, "address"),
                port = Lib.Utils.Str2Int(GetStr(mainPrefix, "port")),
                alterId = Lib.Utils.Str2Int(GetStr(mainPrefix, "users.0.alterId")),
                uuid = Guid.Parse(GetStr(mainPrefix, "users.0.id")),
                description = GetStr("v2raygcon", "description"),
            };

            var subPrefix = root + "." + "streamSettings";
            vee.streamType = GetStr(subPrefix, "network");
            vee.isUseTls = GetStr(subPrefix, "security")?.ToLower() == "tls";
            var mainParam = "";
            switch (vee.streamType)
            {
                case "kcp":
                    mainParam = GetStr(subPrefix, "kcpSettings.header.type");
                    break;
                case "ws":
                    mainParam = GetStr(subPrefix, "wsSettings.path");
                    vee.streamParam2 = GetStr(subPrefix, "wsSettings.headers.Host");
                    break;
                case "h2":
                    mainParam = GetStr(subPrefix, "httpSettings.path");
                    try
                    {
                        var hosts = isUseV4 ?
                            config["outbounds"][0]["streamSettings"]["httpSettings"]["host"] :
                            config["outbound"]["streamSettings"]["httpSettings"]["host"];
                        vee.streamParam2 = Lib.Utils.JArray2Str(hosts as JArray);
                    }
                    catch { }
                    break;
                case "quic":
                    mainParam = GetStr(subPrefix, "quicSettings.header.type");
                    vee.streamParam2 = GetStr(subPrefix, "quicSettings.security");
                    vee.streamParam3 = GetStr(subPrefix, "quicSettings.key");
                    break;
                default:
                    break;
            }
            vee.streamParam1 = mainParam;
            return vee;
        }

        Tuple<JObject, JToken> VeeToConfig(Model.VeeShareLinks.Ver0a vee)
        {
            if (vee == null)
            {
                return null;
            }

            var outVmess = cache.tpl.LoadTemplate("outbVeeVmess");
            outVmess["streamSettings"] = GenStreamSetting(vee);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vee.address;
            node["port"] = vee.port;
            node["users"][0]["id"] = vee.uuid;

            if (vee.alterId > 0)
            {
                node["users"][0]["alterId"] = vee.alterId;
            }

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2raygcon"]["alias"] = vee.alias;
            tpl["v2raygcon"]["description"] = vee.description;
            return new Tuple<JObject, JToken>(tpl, outVmess);
        }

        JToken GenStreamSetting(Model.VeeShareLinks.Ver0a vee)
        {
            // insert stream type
            string[] streamTypes = { "ws", "tcp", "kcp", "h2", "quic" };
            string streamType = vee.streamType.ToLower();

            if (!streamTypes.Contains(streamType))
            {
                return JToken.Parse(@"{}");
            }

            string mainParam = vee.streamParam1;

            var streamToken = cache.tpl.LoadTemplate(streamType);
            try
            {
                switch (streamType)
                {
                    case "kcp":
                        streamToken["kcpSettings"]["header"]["type"] = mainParam;
                        break;
                    case "ws":
                        streamToken["wsSettings"]["path"] = mainParam;
                        streamToken["wsSettings"]["headers"]["Host"] = vee.streamParam2;
                        break;
                    case "h2":
                        streamToken["httpSettings"]["path"] = mainParam;
                        streamToken["httpSettings"]["host"] = Lib.Utils.Str2JArray(vee.streamParam2);
                        break;
                    case "quic":
                        streamToken["quicSettings"]["header"]["type"] = mainParam;
                        streamToken["quicSettings"]["security"] = vee.streamParam2;
                        streamToken["quicSettings"]["key"] = vee.streamParam3;
                        break;
                    default:
                        break;
                }
            }
            catch { }

            try
            {
                streamToken["security"] = vee.isUseTls ? "tls" : "none";
            }
            catch { }

            return streamToken;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
