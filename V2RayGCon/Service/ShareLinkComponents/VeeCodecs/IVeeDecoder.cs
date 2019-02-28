using Newtonsoft.Json.Linq;
using System;

namespace V2RayGCon.Service.ShareLinkComponents.VeeCodecs
{
    internal interface IVeeDecoder
    {
        Tuple<JObject, JToken> Bytes2Config(byte[] bytes);
        string GetSupportedVersion();
    }
}
