using System;
using System.Collections.Generic;

namespace V2RayGCon.Model.VeeShareLinks
{
    public sealed class Ver0a
    {
        // ver 0a is optimized for vmess protocol 

        const string version = @"0a";
        static public string SupportedVersion() => version;

        public string alias, description; // 256 bytes each
        public bool isUseTls;
        public int port, alterId; // 16 bit each
        public Guid uuid;
        public string address; // 256 bytes
        public string streamType, streamParam1, streamParam2, streamParam3; // 256 bytes each

        public Ver0a()
        {
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

        #region string table for compression 
        const int tableLenInBits = 4;
        List<string> strTable = new List<string>{
            "ws", "tcp", "kcp", "h2", "quic",
            "none", "srtp", "utp", "wechat-video",
            "dtls", "wireguard", "",
        };

        #endregion

        #region public methods

        public Ver0a(byte[] bytes) :
            this()
        {
            var ver = VgcApis.Libs.Streams.BitStream.ReadVersion(bytes);
            if (ver != version)
            {
                throw new NotSupportedException(
                    $"Not supported version ${ver}");
            }

            using (var bs = new VgcApis.Libs.Streams.BitStream(bytes))
            {
                alias = bs.Read<string>();
                description = ReadString(bs);
                isUseTls = bs.Read<bool>();
                port = bs.Read<int>();
                alterId = bs.Read<int>();
                uuid = bs.Read<Guid>();
                address = bs.ReadAddress();
                streamType = ReadString(bs);
                streamParam1 = ReadString(bs);
                streamParam2 = ReadString(bs);
                streamParam3 = ReadString(bs);
            }
        }

        public byte[] ToBytes()
        {
            byte[] result;
            using (var bs = new VgcApis.Libs.Streams.BitStream())
            {
                bs.Clear();
                bs.Write(alias);
                WriteString(bs, description);
                bs.Write(isUseTls);
                bs.Write(port);
                bs.Write(alterId);
                bs.Write(uuid);
                bs.WriteAddress(address);
                WriteString(bs, streamType);
                WriteString(bs, streamParam1);
                WriteString(bs, streamParam2);
                WriteString(bs, streamParam3);
                result = bs.ToBytes();
            }
            VgcApis.Libs.Streams.BitStream.WriteVersion(version, result);
            return result;
        }

        public bool EqTo(Ver0a veeLink)
        {
            if (isUseTls != veeLink.isUseTls
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
        #endregion

        #region private methods
        string ReadString(VgcApis.Libs.Streams.BitStream bitStream)
        {
            if (bitStream.Read<bool>())
            {
                // using string table
                var index = bitStream.ReadTinyInt(tableLenInBits);
                return strTable[index];
            }
            else
            {
                return bitStream.Read<string>();
            }

        }

        void WriteString(
            VgcApis.Libs.Streams.BitStream bitStream,
            string str)
        {
            var index = strTable.IndexOf(str);
            if (index == -1)
            {
                bitStream.Write(false);
                bitStream.Write(str);
            }
            else
            {
                bitStream.Write(true);
                bitStream.WriteTinyInt(index, tableLenInBits);
            }
        }
        #endregion

    }
}
