using System;

namespace V2RayGCon.Model.Data
{
    public sealed class Vee
    {
        public bool isUseTls; // direct write
        public int port; // numbers
        public Guid uuid; // uuids
        public string address; // address
        public string streamType, streamParam; // asciiString
        public string alias, description; // utf16String

        public Vee()
        {
            isUseTls = false;
            port = 0;
            uuid = new Guid(); // all zero
            address = string.Empty;
            streamType = string.Empty;
            streamParam = string.Empty;
            alias = string.Empty;
            description = string.Empty;
        }

        public Vee(string veeLink) : this()
        {
            string b64Body = Lib.Utils.GetLinkBody(veeLink);
            string plainBody = Lib.Utils.Base64Decode(b64Body);
            DecodeProperties(plainBody);
        }

        #region public methods
        public bool EqTo(Vee veeLink)
        {
            if (isUseTls != veeLink.isUseTls
                || port != veeLink.port
                || uuid != veeLink.uuid
                || address != veeLink.address
                || streamType != veeLink.streamType
                || streamParam != veeLink.streamParam
                || alias != veeLink.alias
                || description != veeLink.description)
            {
                return false;
            }
            return true;
        }

        public override string ToString() =>
            EncodeProperties();

        public string ToShareLink()
        {
            var str = EncodeProperties();
            var b64Str = Lib.Utils.Base64Encode(str);

            // debug
            // var decoded = Lib.Utils.Base64Decode(b64Str);
            // var iseq = str == decoded;

            return Lib.Utils.AddLinkPrefix(
                b64Str,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        }
        #endregion

        #region private methods
        private void DecodeProperties(string plainBody)
        {
            using (var bs = new VgcApis.Libs.Streams.BitStream(plainBody))
            {
                isUseTls = bs.Read<bool>();
                port = bs.Read<int>();
                uuid = bs.Read<Guid>();
                address = bs.ReadAddress();
                streamType = bs.Read();
                streamParam = bs.Read();
                alias = bs.Read();
                description = bs.Read();
            }
        }

        private string EncodeProperties()
        {
            using (var bs = new VgcApis.Libs.Streams.BitStream())
            {
                bs.Clear();
                bs.Write(isUseTls);
                bs.Write(port);
                bs.Write(uuid);
                bs.WriteAddress(address);
                bs.Write(streamType);
                bs.Write(streamParam);
                bs.Write(alias);
                bs.Write(description);
                var result = bs.ToString();
                return result;
            }
        }

        #endregion
    }
}
