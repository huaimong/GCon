using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class VgcDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public VgcDecoder(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public string Decode(string shareLink) =>
            SsLink2Config(shareLink);

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
        string SsLink2Config(string ssLink)
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

            var tpl = cache.tpl.LoadTemplate("tplImportSS") as JObject;
            return GetContainer()?.FillDefConfig(ref tpl, outbSs);
        }
        #endregion

        #region protected methods

        #endregion
    }
}
