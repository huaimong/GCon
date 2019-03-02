using Newtonsoft.Json.Linq;
using System;

namespace V2RayGCon.Service.ShareLinkComponents.VeeCodecs
{
    internal sealed class VeeCodecs :
        VgcApis.Models.BaseClasses.ContainerOf<VeeCodecs>

    {
        Cache cache;

        public VeeCodecs(Cache cache)
        {
            this.cache = cache;
        }

        public void Run()
        {
            var v0a = new Vee0a(cache);
            var v0b = new Vee0b(cache);

            Plug(this, v0a);
            Plug(this, v0b);
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            var bytes = VeeLink2Bytes(shareLink);
            var linkVersion = VgcApis.Libs.Streams.BitStream.ReadVersion(bytes);

            foreach (var component in GetAllComponents())
            {
                var decoder = component as IVeeDecoder;
                if (decoder.GetSupportedVersion() == linkVersion)
                {
                    return decoder.Bytes2Config(bytes);
                }
            }

            throw new NotSupportedException(
                $"Not supported vee share link version {linkVersion}");
        }

        public string Encode(string config)
        {
            if (!VgcApis.Libs.Utils.TryParseJObject(
                config, out JObject json))
            {
                return null;
            }

            IVeeDecoder encoder;
            var protocol = Lib.Utils.GetProtocolFromConfig(json);
            switch (protocol)
            {
                case VgcApis.Models.Consts.Config.ProtocolNameVmess:
                    encoder = GetComponent<Vee0a>();
                    break;
                case VgcApis.Models.Consts.Config.ProtocolNameSs:
                    encoder = GetComponent<Vee0b>();
                    break;
                default:
                    return null;
            }

            var bytes = encoder?.Config2Bytes(json);
            return Bytes2VeeLink(bytes);
        }
        #endregion

        #region private methods
        static byte[] VeeLink2Bytes(string veeLink)
        {
            // Do not use Lib.Utils.Base64Decode() 
            // Unicode encoder can not handle all possible byte values.

            string b64Body = Lib.Utils.GetLinkBody(veeLink);
            string b64Padded = Lib.Utils.Base64PadRight(b64Body);
            return Convert.FromBase64String(b64Padded);
        }

        string Bytes2VeeLink(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new NullReferenceException(
                    @"Bytes is null!");
            }

            var b64Str = Convert.ToBase64String(bytes);
            return Lib.Utils.AddLinkPrefix(
                b64Str,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        }

        #endregion

        #region protected methods

        #endregion
    }
}
