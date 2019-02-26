using System;
using System.Text;

namespace VgcApis.Libs.Streams
{
    public sealed class BitStream :
        VgcApis.Models.BaseClasses.Disposable
    {
        const int BitsPerInt = VgcApis.Models.Consts.BitStream.BitsPerInt;

        readonly object rbsWriteLock = new object();

        RawBitStream.RawBitStream rawBitStream;
        RawBitStream.Numbers numberWriter;
        RawBitStream.Bytes bytesWriter;
        RawBitStream.Uuids uuidsWriter;
        RawBitStream.Address addressWriter;

        public BitStream(string str) : this()
        {
            rawBitStream.FromString(str);
        }

        public BitStream()
        {
            rawBitStream = new RawBitStream.RawBitStream();
            rawBitStream.Run();
            numberWriter = rawBitStream.GetComponent<RawBitStream.Numbers>();
            bytesWriter = rawBitStream.GetComponent<RawBitStream.Bytes>();
            uuidsWriter = rawBitStream.GetComponent<RawBitStream.Uuids>();
            addressWriter = rawBitStream.GetComponent<RawBitStream.Address>();
        }

        #region properties

        #endregion

        #region public methods
        public override string ToString() =>
            rawBitStream.ToString();

        public void Clear()
        {
            lock (rbsWriteLock)
            {
                rawBitStream.Clear();
            }
        }

        public string ReadAddress()
        {
            lock (rbsWriteLock)
            {
                return addressWriter.Read();
            }
        }

        public string Read()
        {
            lock (rbsWriteLock)
            {
                var cache = bytesWriter.Read();
                return Encoding.UTF8.GetString(cache);
            }
        }

        public T Read<T>()
            where T : struct
        {
            lock (rbsWriteLock)
            {
                var typeName = typeof(T).Name;
                switch (typeof(T).Name)
                {
                    case nameof(Int32):
                        return (T)(numberWriter.Read(BitsPerInt) as object);
                    case nameof(Boolean):
                        return (T)((rawBitStream.Read() == true) as object);
                    case nameof(Guid):
                        return (T)(uuidsWriter.Read() as object);
                    default:
                        throw new NotSupportedException($"Not support type {typeName}");
                }
            }
        }

        public void WriteAddress(string address)
        {
            lock (rbsWriteLock)
            {
                addressWriter.Write(address);
            }
        }

        public void Write(string str)
        {
            lock (rbsWriteLock)
            {
                var cache = Encoding.UTF8.GetBytes(str);
                bytesWriter.Write(cache);
            }
        }

        public void Write<T>(T val)
            where T : struct
        {
            lock (rbsWriteLock)
            {
                switch (val)
                {
                    case int number:
                        numberWriter.Write(number, BitsPerInt);
                        break;
                    case bool flag:
                        rawBitStream.Write(flag);
                        break;
                    case Guid uuid:
                        uuidsWriter.Write(uuid);
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Not supported type {typeof(T)}");
                }
            }
        }
        #endregion

        #region private methods

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            rawBitStream?.Dispose();
        }
        #endregion
    }
}
