using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class VeeDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;
        Setting setting;

        public VeeDecoder(
            Cache cache,
            Setting setting)
        {
            this.cache = cache;
            this.setting = setting;
        }
        #region properties

        #endregion

        #region public methods
        public string Decode(string shareLink)
        {
            string message = null;
            try
            {
                var veeLink = new Model.Data.Vee(shareLink);
                var version = veeLink.version;
                switch (version)
                {
                    case 1:
                        return VeeToConfigVer1(veeLink);
                    default:
                        throw new NotSupportedException(
                            $"Not supported vee share link version ${version}");
                }

            }
            catch (Exception e)
            {
                message = e.Message;
            }

            if (!string.IsNullOrEmpty(message))
            {
                setting.SendLog(message);
            }
            return null;
        }

        public string Encode(string config)
        {
            string message = null;
            try
            {
                return ConfigToVeeVer1(config)?.ToShareLink();
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            if (!string.IsNullOrEmpty(message))
            {
                setting.SendLog(message);
            }
            return null;
        }

        public List<string> ExtractLinksFromText(string text) =>
            Lib.Utils.ExtractLinks(
                text,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        #endregion

        #region private methods
        Model.Data.Vee ConfigToVeeVer1(string config)
        {
            var version = 1;

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
                version = version,
                alias = GetStr("v2raygcon", "alias"),
                address = GetStr(mainPrefix, "address"),
                port = Lib.Utils.Str2Int(GetStr(mainPrefix, "port")),
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
                            json["outbounds"][0]["streamSettings"]["httpSettings"]["host"] :
                            json["outbound"]["streamSettings"]["httpSettings"]["host"];
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

        string VeeToConfigVer1(Model.Data.Vee vee)
        {
            if (vee == null)
            {
                return null;
            }

            var outVmess = cache.tpl.LoadTemplate("outbVee");
            outVmess["streamSettings"] = GenStreamSettingVer1(vee);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vee.address;
            node["port"] = vee.port;
            node["users"][0]["id"] = vee.uuid;

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2raygcon"]["alias"] = vee.alias;
            tpl["v2raygcon"]["description"] = vee.description;
            return GetContainer()?.FillDefConfig(ref tpl, outVmess);
        }

        JToken GenStreamSettingVer1(Model.Data.Vee vee)
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
