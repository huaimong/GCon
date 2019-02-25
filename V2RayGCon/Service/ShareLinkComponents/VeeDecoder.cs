using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class VeeDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public VeeDecoder(Cache cache)
        {
            this.cache = cache;
        }
        #region properties

        #endregion

        #region public methods
        public string Decode(string shareLink)
        {
            try
            {
                var veeLink = new Model.Data.Vee(shareLink);
                return Vee2Config(veeLink);
            }
            catch { }
            return null;
        }

        public string Encode(string config)
        {
            try
            {
                return ConfigString2Vee(config)?.ToShareLink();
            }
            catch { }
            return null;
        }

        public List<string> ExtractLinksFromText(string text) =>
            Lib.Utils.ExtractLinks(
                text,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        #endregion

        #region private methods
        Model.Data.Vee ConfigString2Vee(string config)
        {
            JObject json;
            try
            {
                json = JObject.Parse(config);
            }
            catch
            {
                return null;
            }

            var GetStr = Lib.Utils.GetStringByPrefixAndKeyHelper(json);
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
            var vee = new Model.Data.Vee
            {
                alias = GetStr("v2raygcon", "alias"),
                address = GetStr(mainPrefix, "address"),
                port = Lib.Utils.Str2Int(GetStr(mainPrefix, "port")),
                uuid = GetStr(mainPrefix, "users.0.id"),
                description = GetStr("v2raygcon", "description"),
            };

            var subPrefix = root + "." + "streamSettings";
            vee.streamType = GetStr(subPrefix, "network");
            vee.isUseTls = GetStr(subPrefix, "security")?.ToLower() == "tls";
            var streamParam = "";
            switch (vee.streamType)
            {
                case "kcp":
                    streamParam = GetStr(subPrefix, "kcpSettings.header.type");
                    break;
                case "ws":
                    streamParam = GetStr(subPrefix, "wsSettings.path");
                    break;
                case "h2":
                    streamParam = GetStr(subPrefix, "httpSettings.path");
                    break;
            }
            vee.streamParam = streamParam;
            return vee;
        }

        string Vee2Config(Model.Data.Vee vee)
        {
            if (vee == null)
            {
                return null;
            }

            var outVmess = cache.tpl.LoadTemplate("outbVee");
            outVmess["streamSettings"] = GenStreamSetting(vee);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vee.address;
            node["port"] = vee.port;
            node["users"][0]["id"] = vee.uuid;

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2raygcon"]["alias"] = vee.alias;
            tpl["v2raygcon"]["description"] = vee.description;
            return GetContainer()?.FillDefConfig(ref tpl, outVmess);
        }

        JToken GenStreamSetting(Model.Data.Vee vee)
        {
            // insert stream type
            string[] streamTypes = { "ws", "tcp", "kcp", "h2" };
            string streamType = vee.streamType.ToLower();

            if (!streamTypes.Contains(streamType))
            {
                return JToken.Parse(@"{}");
            }

            string streamParam = vee.streamParam;

            var streamToken = cache.tpl.LoadTemplate(streamType);
            try
            {
                switch (streamType)
                {
                    case "kcp":
                        streamToken["kcpSettings"]["header"]["type"] = streamParam;
                        break;
                    case "ws":
                        streamToken["wsSettings"]["path"] = streamParam;
                        break;
                    case "h2":
                        streamToken["httpSettings"]["path"] = streamParam;
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
