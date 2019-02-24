namespace V2RayGCon.Model.Data
{
    public sealed class VgcLink
    {
        string address, uid, streamType, streamParam, alias, description;
        bool isUseTls;
        int port;

        public VgcLink() { }

        #region public methods
        public string ToShareLink()
        {
            return "";
        }

        #endregion

        #region private methods


        #endregion
    }
}
