namespace VgcApis.Models.Consts
{
    static public class Libs
    {
        #region VgcApis.Libs.Sys.CacheLogger
        public const int TrimdownLogCacheDelay = 5000;
        public const int MaxCacheLoggerLineNumber = 1000;
        public const int MinCacheLoggerLineNumber = 1000;

        public const string LuaPerdefinedFunctions =
@"
import = function () end
                
-- copy from NLua
function Each(o)
    local e = o:GetEnumerator()
    return function()
        if e:MoveNext() then
        return e.Current
        end
    end
end";
        #endregion
    }
}
