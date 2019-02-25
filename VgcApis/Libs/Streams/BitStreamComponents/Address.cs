using System;
using System.Net;

namespace VgcApis.Libs.Streams.BitStreamComponents
{
    public sealed class Address :
        Models.BaseClasses.ComponentOf<BitStream>
    {
        const int BitsPerByte = Models.Consts.BitStream.BitsPerByte;
        const int BytesPerIpv4 = Models.Consts.BitStream.BytesPerIpv4;
        const int BytesPerIpv6 = Models.Consts.BitStream.BytesPerIpv6;

        AsciiString asciiString;
        Numbers numbers;
        BitStream bitStream;

        public Address() { }

        public void Run(
            Numbers numbers,
            AsciiString asciiString)
        {
            this.asciiString = asciiString;
            this.numbers = numbers;
            bitStream = GetContainer();
        }

        #region properties

        #endregion

        #region public methods
        public void Write(string address)
        {
            var ip = ParseIpAddress(address);
            bitStream.Write(ip != null);
            if (ip == null)
            {
                asciiString.Write(address);
            }
            else
            {
                WriteIp(ip);
            }
        }

        public string Read()
        {
            var isIp = ReadOneBit();
            if (!isIp)
            {
                return asciiString.Read();
            }
            else
            {
                var isIpV4 = ReadOneBit();
                var bytes = ReadBytes(isIpV4 ? BytesPerIpv4 : BytesPerIpv6);
                return new IPAddress(bytes).ToString();
            }
        }

        #endregion

        #region private methods
        byte[] ReadBytes(int len)
        {
            var result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(numbers.Read(BitsPerByte));
            }
            return result;
        }

        bool ReadOneBit()
        {
            var val = bitStream.Read();
            if (val == null)
            {
                throw new NullReferenceException("Read overflow.");
            }
            return (bool)val;
        }

        void WriteIp(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();

            // ipv4
            bitStream.Write(bytes.Length == BytesPerIpv4);

            foreach (var b in bytes)
            {
                numbers.Write(b, BitsPerByte);
            }
        }

        IPAddress ParseIpAddress(string input)
        {
            // https://stackoverflow.com/questions/799060/how-to-determine-if-a-string-is-a-valid-ipv4-or-ipv6-address-in-c
            IPAddress address;
            if (IPAddress.TryParse(input, out address))
            {
                switch (address.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        return address;
                    default:
                        // umm... yeah... I'm going to need to take your red packet and...
                        break;
                }
            }
            return null;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
