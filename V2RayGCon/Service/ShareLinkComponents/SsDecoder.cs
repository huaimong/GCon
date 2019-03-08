using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class SsDecoder :
        VgcApis.Models.BaseClasses.Plugable<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public SsDecoder(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            var outbound = SsLink2Outbound(shareLink);
            if (outbound == null)
            {
                return null;
            }
            var tpl = cache.tpl.LoadTemplate("tplImportSS") as JObject;

            return new Tuple<JObject, JToken>(tpl, outbound);
        }


        public string Encode(string config)
        {
            throw new NotImplementedException();
        }

        public List<string> ExtractLinksFromText(string text) =>
            Lib.Utils.ExtractLinks(
                text,
                VgcApis.Models.Datas.Enum.LinkTypes.ss);
        #endregion

        #region private methods
        JToken SsLink2Outbound(string ssLink)
        {
            Model.Data.Shadowsocks ss = Lib.Utils.SsLink2Ss(ssLink);
            if (ss == null)
            {
                return null;
            }

            Lib.Utils.TryParseIPAddr(ss.addr, out string ip, out int port);
            var outbSs = cache.tpl.LoadTemplate("outbSs");
            var node = outbSs["settings"]["servers"][0];
            node["address"] = ip;
            node["port"] = port;
            node["method"] = ss.method;
            node["password"] = ss.pass;

            return outbSs;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
