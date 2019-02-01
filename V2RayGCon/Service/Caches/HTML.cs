using System.Net;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service.Caches
{
    public class HTML : Model.BaseClass.CacheComponent<string, string>
    {
        #region public method
        public new string this[string url]
        {
            get => GetCache(url);
        }
        #endregion

        #region private method
        string GetCache(string url)
        {
            lock (writeLock)
            {
                if (!ContainsKey(url))
                {
                    data[url] = new Model.Data.LockValuePair<string>();
                }
            }

            var c = data[url];
            lock (c.rwLock)
            {
                var timeout = Lib.Utils.Str2Int(
                    StrConst.ParseImportTimeOut);

                var retry = Lib.Utils.Str2Int(
                    StrConst.ParseImportRetry);

                for (var i = 0;
                    i < retry && string.IsNullOrEmpty(c.content);
                    i++)
                {
                    c.content = Lib.Utils.Fetch(url, timeout * 1000);
                }
            }

            if (string.IsNullOrEmpty(c.content))
            {
                throw new WebException("Download fail!");
            }
            return c.content;
        }

        #endregion
    }
}
