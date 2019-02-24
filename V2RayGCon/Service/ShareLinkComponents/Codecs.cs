using Newtonsoft.Json.Linq;

namespace V2RayGCon.Service.ShareLinkComponents
{
    public sealed class Codecs :
        VgcApis.Models.BaseClasses.ContainerTpl<Codecs>
    {
        Setting setting;
        Cache cache;

        public Codecs() { }

        #region public methods
        public string Encode<TDecoder>(string config)
            where TDecoder :
                VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
                VgcApis.Models.Interfaces.IShareLinkDecoder
            => GetComponent<TDecoder>()?.Encode(config);

        public string Decode<TDecoder>(string shareLink)
            where TDecoder :
                VgcApis.Models.BaseClasses.ComponentOf<Codecs>,
                VgcApis.Models.Interfaces.IShareLinkDecoder
            => GetComponent<TDecoder>()?.Decode(shareLink);

        public string FillDefConfig(ref JObject template, JToken outbound)
        {
            var isV4 = setting.isUseV4;

            var inb = Lib.Utils.CreateJObject(
                (isV4 ? "inbounds.0" : "inbound"),
                cache.tpl.LoadTemplate("inbSimSock"));

            var outb = Lib.Utils.CreateJObject(
                (isV4 ? "outbounds.0" : "outbound"),
                outbound);

            Lib.Utils.MergeJson(ref template, inb);
            Lib.Utils.MergeJson(ref template, outb);
            return Lib.Utils.Config2String(template as JObject);
        }

        public void Run(
            Cache cache,
            Setting setting)
        {
            this.setting = setting;
            this.cache = cache;

            var ssDecoder = new SsDecoder(cache);
            var v2rayDecoder = new V2rayDecoder();
            var vmessDecoder = new VmessDecoder(cache);

            Plug(ssDecoder);
            Plug(v2rayDecoder);
            Plug(vmessDecoder);

        }
        #endregion

        #region private methods
        void Plug(VgcApis.Models.Interfaces.IComponent<Codecs> component)
            => Plug(this, component);
        #endregion
    }
}
