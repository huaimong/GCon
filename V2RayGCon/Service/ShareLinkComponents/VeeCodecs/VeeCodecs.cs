using Newtonsoft.Json.Linq;
using System;

namespace V2RayGCon.Service.ShareLinkComponents.VeeCodecs
{
    internal sealed class VeeCodecs :
        VgcApis.Models.BaseClasses.ContainerOf<VeeCodecs>

    {
        VeeDecoder container;
        Cache cache;

        public VeeCodecs(
            Cache cache,
            VeeDecoder container)
        {
            this.cache = cache;
            this.container = container;
        }

        public void Run()
        {
            var vee001 = new Ver001Decoder(cache);
            Plug(this, vee001);
        }

        #region properties

        #endregion

        #region public methods
        public string FillDefConfig(ref JObject template, JToken outbound) =>
            container.FillDefConfig(ref template, outbound);

        public string Decode(string shareLink)
        {
            var bytes = VeeLink2Bytes(shareLink);
            var bitStream = new VgcApis.Libs.Streams.BitStream(bytes);
            var version = bitStream.Read<int>();
            bitStream.Rewind();

            IVeeDecoder decoder;
            switch (version)
            {
                case 1:
                    decoder = GetComponent<Ver001Decoder>();
                    break;
                default:
                    throw new NotSupportedException(
                        $"Not supported vee share link version ${version}");
            }

            return decoder.BitStream2Config(bitStream);
        }

        public string Encode(string config)
        {
            var encoder = GetComponent<Ver001Decoder>();
            var bytes = encoder.Config2Bytes(config);
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
