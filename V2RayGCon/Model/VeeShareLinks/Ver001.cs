using System;

namespace V2RayGCon.Model.VeeShareLinks
{
    public sealed class Ver001
    {
        public int version = 1; // 16 bit
        public string alias, description; // 256 bytes max
        public bool isUseTls;
        public int port; // 16 bit
        public Guid uuid;
        public string address; // 256 bytes max
        public string streamType, streamParam1, streamParam2, streamParam3; // 256 bytes max

        public Ver001()
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

        public Ver001(
            VgcApis.Libs.Streams.BitStream bitStream) :
            this()
        {
            version = bitStream.Read<int>();
            alias = bitStream.Read<string>();
            description = bitStream.Read<string>();
            isUseTls = bitStream.Read<bool>();
            port = bitStream.Read<int>();
            uuid = bitStream.Read<Guid>();
            address = bitStream.ReadAddress();
            streamType = bitStream.Read<string>();
            streamParam1 = bitStream.Read<string>();
            streamParam2 = bitStream.Read<string>();
            streamParam3 = bitStream.Read<string>();
        }

        public byte[] ToBytes()
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

        public bool EqTo(Ver001 veeLink)
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
    }
}
