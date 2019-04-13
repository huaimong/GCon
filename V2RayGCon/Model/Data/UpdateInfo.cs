namespace V2RayGCon.Model.Data
{
    internal sealed class UpdateInfo
    {
        public string version;
        public string md5;

        public UpdateInfo()
        {
            version = string.Empty;
            md5 = string.Empty;
        }
    }
}
