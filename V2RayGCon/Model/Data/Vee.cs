namespace V2RayGCon.Model.Data
{
    public sealed class Vee
    {
        VgcApis.Libs.Streams.BitStream bitStream;
        VgcApis.Libs.Streams.BitStreamComponents.Numbers numbers;
        VgcApis.Libs.Streams.BitStreamComponents.Uuids uuids;
        VgcApis.Libs.Streams.BitStreamComponents.AsciiString asciiString;
        VgcApis.Libs.Streams.BitStreamComponents.Utf16String utf16String;
        VgcApis.Libs.Streams.BitStreamComponents.Address addressWriter;

        public bool isUseTls; // direct write
        public int port; // numbers
        public string uuid; // uuids
        public string address; // address
        public string streamType, streamParam; // asciiString
        public string alias, description; // utf16String

        public Vee()
        {
            uuid = string.Empty;
            address = string.Empty;
            streamType = string.Empty;
            streamParam = string.Empty;
            alias = string.Empty;
            description = string.Empty;

            bitStream = new VgcApis.Libs.Streams.BitStream();
            bitStream.Run();

            numbers = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Numbers>();
            uuids = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Uuids>();
            asciiString = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.AsciiString>();
            utf16String = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Utf16String>();
            addressWriter = bitStream.GetComponent<VgcApis.Libs.Streams.BitStreamComponents.Address>();
        }

        public Vee(string veeLink) :
            this()
        {
            bitStream.Clear();
            string b64Body = Lib.Utils.GetLinkBody(veeLink);
            string plainBody = Lib.Utils.Base64Decode(b64Body);
            bitStream.FromString(plainBody);
            isUseTls = bitStream.Read() == true;
            port = numbers.ReadPortNum();
            uuid = uuids.Read().ToString();
            address = addressWriter.Read();
            streamType = asciiString.Read();
            streamParam = asciiString.Read();
            alias = utf16String.Read();
            description = utf16String.Read();
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

        public string ToShareLink()
        {
            bitStream.Clear();
            bitStream.Write(isUseTls);
            numbers.WritePortNum(port);
            uuids.Write(uuid);
            addressWriter.Write(address);
            asciiString.Write(streamType);
            asciiString.Write(streamParam);
            utf16String.Write(alias);
            utf16String.Write(description);
            var str = bitStream.ToString();
            var b64Str = Lib.Utils.Base64Encode(str);
            return Lib.Utils.AddLinkPrefix(
                b64Str,
                VgcApis.Models.Datas.Enum.LinkTypes.v);
        }

        #endregion

        #region private methods


        #endregion
    }
}
