namespace V2RayGCon.Service.ShareLinkComponents.VeeCodecs
{
    internal interface IVeeDecoder
    {
        string BitStream2Config(VgcApis.Libs.Streams.BitStream bitStream);
    }
}
