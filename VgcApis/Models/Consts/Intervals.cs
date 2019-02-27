namespace VgcApis.Models.Consts
{
    static public class Intervals
    {
        // Service.Setting 
        public const int LazyGCDelay = 180 * 1000;
        public const int LazySaveUserSettingsDelay = 30 * 1000;

        public const int SpeedTestTimeout = 20 * 1000;
        public const int FetchDefaultTimeout = 30 * 1000;

        public const int SaveServerListIntreval = 30 * 1000;
        public const int NotifierTextUpdateIntreval = 3 * 1000;

        public const int SiFormLogRefreshInterval = 500;
        public const int LuaPluginLogRefreshInterval = 500;
    }
}
