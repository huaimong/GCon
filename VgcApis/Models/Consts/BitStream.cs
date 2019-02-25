namespace VgcApis.Models.Consts
{
    public static class BitStream
    {
        public const int BitsPerPort = 16;
        public const int BytesPerIpv4 = 4;
        public const int BytesPerIpv6 = 16;
        public const int BytesPerUuid = 16;
        public const int BitsPerByte = 8;
        public const int BitsPerChar = 7;
        public const int BitsPerUtf16 = 16;
        public const int MaxStringLenInBits = 8;
        public const int MaxStringLen = 1 << (MaxStringLenInBits - 1);
    }
}
