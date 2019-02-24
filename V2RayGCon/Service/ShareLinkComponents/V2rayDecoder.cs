using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace V2RayGCon.Service.ShareLinkComponents
{
    internal sealed class V2rayDecoder :
        VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
        VgcApis.Models.Interfaces.IShareLinkDecoder
    {
        public V2rayDecoder() { }

        #region properties

        #endregion

        #region public methods
        public string Decode(string shareLink)
        {
            try
            {
                var config = JObject.Parse(
                    Lib.Utils.Base64Decode(
                        Lib.Utils.GetLinkBody(shareLink)));
                if (config != null)
                {
                    return Lib.Utils.Config2String(config);
                }
            }
            catch { }
            return null;
        }

        public string Encode(string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                return null;
            }

            var body = Lib.Utils.Base64Encode(config);

            return Lib.Utils.AddLinkPrefix(
                body,
                VgcApis.Models.Datas.Enum.LinkTypes.v2ray);
        }


        public List<string> ExtractLinksFromText(string text) =>
            Lib.Utils.ExtractLinks(text, VgcApis.Models.Datas.Enum.LinkTypes.v2ray);
        #endregion

        #region private methods

        #endregion

        #region protected methods

        #endregion
    }
}
