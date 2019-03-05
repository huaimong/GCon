using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class VmessDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public VmessDecoder(Cache cache)
        {
            this.cache = cache;
        }
        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            var vmess = Lib.Utils.VmessLink2Vmess(shareLink);
            return Vmess2Config(vmess);
        }

        public string Encode(string config) =>
            ConfigString2Vmess(config)?.ToVmessLink();

        public List<string> ExtractLinksFromText(string text) =>
            Lib.Utils.ExtractLinks(text, VgcApis.Models.Datas.Enum.LinkTypes.vmess);
        #endregion

        #region private methods
        Model.Data.Vmess ConfigString2Vmess(string config)
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

            var prefix = root + "." + "settings.vnext.0";
            Model.Data.Vmess vmess = new Model.Data.Vmess
            {
                v = "2",
                ps = GetStr("v2raygcon", "alias")
            };
            vmess.add = GetStr(prefix, "address");
            vmess.port = GetStr(prefix, "port");
            vmess.id = GetStr(prefix, "users.0.id");
            vmess.aid = GetStr(prefix, "users.0.alterId");

            prefix = root + "." + "streamSettings";
            vmess.net = GetStr(prefix, "network");
            vmess.type = GetStr(prefix, "kcpSettings.header.type");
            vmess.tls = GetStr(prefix, "security");

            switch (vmess.net)
            {
                case "ws":
                    vmess.path = GetStr(prefix, "wsSettings.path");
                    vmess.host = GetStr(prefix, "wsSettings.headers.Host");
                    break;
                case "h2":
                    try
                    {
                        vmess.path = GetStr(prefix, "httpSettings.path");
                        var hosts = isUseV4 ?
                            json["outbounds"][0]["streamSettings"]["httpSettings"]["host"] :
                            json["outbound"]["streamSettings"]["httpSettings"]["host"];
                        vmess.host = Lib.Utils.JArray2Str(hosts as JArray);
                    }
                    catch { }
                    break;
            }
            return vmess;
        }

        Tuple<JObject, JToken> Vmess2Config(Model.Data.Vmess vmess)
        {
            if (vmess == null)
            {
                return null;
            }

            var outVmess = cache.tpl.LoadTemplate("outbVmess");
            outVmess["streamSettings"] = GenStreamSetting(vmess);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vmess.add;
            node["port"] = Lib.Utils.Str2Int(vmess.port);
            node["users"][0]["id"] = vmess.id;
            node["users"][0]["alterId"] = Lib.Utils.Str2Int(vmess.aid);

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2raygcon"]["alias"] = vmess.ps;
            return new Tuple<JObject, JToken>(tpl, outVmess);
        }

        JToken GenStreamSetting(Model.Data.Vmess vmess)
        {
            // insert stream type
            string[] streamTypes = { "ws", "tcp", "kcp", "h2" };
            string streamType = vmess.net.ToLower();

            if (!streamTypes.Contains(streamType))
            {
                return JToken.Parse(@"{}");
            }

            var streamToken = cache.tpl.LoadTemplate(streamType);
            try
            {
                switch (streamType)
                {
                    case "kcp":
                        streamToken["kcpSettings"]["header"]["type"] = vmess.type;
                        break;
                    case "ws":
                        streamToken["wsSettings"]["path"] = string.IsNullOrEmpty(vmess.v) ? vmess.host : vmess.path;
                        if (vmess.v == "2" && !string.IsNullOrEmpty(vmess.host))
                        {
                            streamToken["wsSettings"]["headers"]["Host"] = vmess.host;
                        }
                        break;
                    case "h2":
                        streamToken["httpSettings"]["path"] = vmess.path;
                        streamToken["httpSettings"]["host"] = Lib.Utils.Str2JArray(vmess.host);
                        break;
                }
            }
            catch { }

            try
            {
                streamToken["security"] =
                    vmess.tls?.ToLower() == "tls" ?
                    "tls" : "none";
            }
            catch { }

            return streamToken;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
