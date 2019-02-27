using System;

namespace V2RayGCon.Model.Data
{
    public sealed class Vee
    {
        public int version = 1; // 16 bit
        public string alias, description; // 256 bytes max
        public bool isUseTls;
        public int port; // 16 bit
        public Guid uuid;
        public string address; // 256 bytes max
        public string streamType, streamParam1, streamParam2, streamParam3; // 256 bytes max

        public Vee()
        {
            version = 1;
            alias = string.Empty;
            description = string.Empty;
            isUseTls = false;
            port = 0;
            uuid = new Guid(); // zeros
            address = string.Empty;
            streamType = string.Empty;
            streamParam1 = string.Empty;
            streamParam2 = string.Empty;
            streamParam3 = string.Empty;
        }

        public Vee(string veeLink) : this()
        {
            var bytes = VeeLink2Bytes(veeLink);
            var version = ReadVersion(bytes);
            switch (version)
            {
                case 1:
                    DecoderVer1(bytes);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Not support vee share link version {version}");
            }
        }

        #region static methods
        public static int ReadVersion(string veeLink)
        {
            var bytes = VeeLink2Bytes(veeLink);
            return ReadVersion(bytes);
        }

        static byte[] VeeLink2Bytes(string veeLink)
        {
            // Do not use Lib.Utils.Base64Decode() 
            // Unicode encoder can not handle all possible byte values.

            string b64Body = Lib.Utils.GetLinkBody(veeLink);
            string b64Padded = Lib.Utils.Base64PadRight(b64Body);
            return Convert.FromBase64String(b64Padded);
        }

        static int ReadVersion(byte[] bytes)
        {
            if (bytes.Length < 2)
            {
                return -1;
            }
            var version = 0;
            for (int i = 0; i < 2; i++)
            {
                int ascii = bytes[i];
                var pow = 1;
                for (int j = 0; j < VgcApis.Models.Consts.BitStream.BitsPerByte; j++)
                {
                    version += pow * (ascii % 2);
                    pow *= 2;
                    ascii /= 2;
                }
            }
            return version;

        }
        #endregion

        #region public methods
        public bool EqTo(Vee veeLink)
        {
            if (version != veeLink.version
                || isUseTls != veeLink.isUseTls
                || port != veeLink.port
                || uuid != veeLink.uuid
                || address != veeLink.address
                || streamType != veeLink.streamType
                || streamParam1 != veeLink.streamParam1
                || streamParam2 != veeLink.streamParam2
                || streamParam3 != veeLink.streamParam3
                || alias != veeLink.alias
                || description != veeLink.description)
            {
                return false;
            }
            return true;
        }

        public string ToShareLink()
        {
            // Do not use Lib.Utils.Base64Encode() 
            // Unicode encoder can not handle all possible byte values.
            switch (version)
            {
                case 1:
                    return GenVeeShareLink(EncoderVer1);
                default:
                    throw new NotSupportedException(
                        $"Not support vee share link version {version}");
            }
        }
        #endregion

        #region private methods
        string GenVeeShareLink(Func<byte[]> encoder)
        {
            if (encoder == null)
            {
                throw new NullReferenceException(
                    @"Encoder is null!");
            }

            var bytes = encoder.Invoke();
            var b64Str = Convert.ToBase64String(bytes);
            return Lib.Utils.AddLinkPrefix(
                b64Str,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        }

        private void DecoderVer1(byte[] bytes)
        {
            using (var bs = new VgcApis.Libs.Streams.BitStream(bytes))
            {
                version = bs.Read<int>();
                alias = bs.Read();
                description = bs.Read();
                isUseTls = bs.Read<bool>();
                port = bs.Read<int>();
                uuid = bs.Read<Guid>();
                address = bs.ReadAddress();
                streamType = bs.Read();
                streamParam1 = bs.Read();
                streamParam2 = bs.Read();
                streamParam3 = bs.Read();

            }
        }

        private byte[] EncoderVer1()
        {
            var version = 1;
            byte[] result;
            using (var bs = new VgcApis.Libs.Streams.BitStream())
            {
                bs.Clear();
                bs.Write(version);
                bs.Write(alias);
                bs.Write(description);
                bs.Write(isUseTls);
                bs.Write(port);
                bs.Write(uuid);
                bs.WriteAddress(address);
                bs.Write(streamType);
                bs.Write(streamParam1);
                bs.Write(streamParam2);
                bs.Write(streamParam3);
                result = bs.ToBytes();
            }
            return result;
        }

        #endregion
    }
}
