using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class SsDecoder :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;
        Setting setting;
        public SsDecoder() { }

        #region properties

        #endregion

        #region public methods
        public void Run(
            Setting setting,
            Cache cache)
        {
            this.cache = cache;
            this.setting = setting;
        }

        public string DecodeLink(string shareLink) =>
            SsLink2Config(shareLink);

        public string EncodeLink(string config)
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

            var config = cache.tpl.LoadTemplate("tplImportSS");

            var setting = config["outbound"]["settings"]["servers"][0];
            setting["address"] = ip;
            setting["port"] = port;
            setting["method"] = ss.method;
            setting["password"] = ss.pass;

            return Lib.Utils.Config2String(config as JObject);
        }

        #endregion

        #region protected methods

        #endregion
    }
}
