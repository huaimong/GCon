using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class VeeDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        Cache cache;
        Setting setting;
        VeeCodecs.VeeCodecs veeCodecs;

        public VeeDecoder(
            Cache cache,
            Setting setting)
        {
            this.cache = cache;
            this.setting = setting;
            veeCodecs = new VeeCodecs.VeeCodecs(cache);
            veeCodecs.Run();
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            string message = null;
            try
            {
                return veeCodecs.Decode(shareLink);
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
                return veeCodecs.Encode(config);
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

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            veeCodecs?.Dispose();
        }
        #endregion
    }
}
