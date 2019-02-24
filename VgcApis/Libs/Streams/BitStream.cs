using System.Collections.Concurrent;

namespace VgcApis.Libs.Streams
{
    public sealed class BitStream :
        Models.BaseClasses.ContainerTpl<BitStream>
    {
        ConcurrentQueue<bool> bitStream = new ConcurrentQueue<bool>();

        public BitStream()
        {
            Plug(new BitStreamComponents.Numbers());
        }

        #region public methods
        public ConcurrentQueue<bool> GetBitStream() => bitStream;
        #endregion

        #region private methods
        void Plug(Models.BaseClasses.ComponentOf<BitStream> component) =>
            Plug(this, component);
        #endregion

    }
}
