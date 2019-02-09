using System;
using System.Collections.Generic;
using System.Linq;

namespace VgcApis.Libs.Sys
{
    sealed public class QueueLogger :
        Models.BaseClasses.Disposable
    {
        long updateTimestamp = DateTime.Now.Ticks;
        Queue<string> logCache = new Queue<string>();
        const int maxLogLineNumber = Models.Consts.Libs.MaxCacheLoggerLineNumber;
        Tasks.LazyGuy logChopper;

        public QueueLogger()
        {
            logChopper = new Tasks.LazyGuy(
                TrimLogCache,
                Models.Consts.Libs.TrimdownLogCacheDelay);
        }

        #region public methods
        public long GetTimestamp() => updateTimestamp;

        public void Log(string message)
        {
            lock (logCache)
            {
                logCache.Enqueue(message);
                updateTimestamp = DateTime.Now.Ticks;
                if (logCache.Count() > 2 * maxLogLineNumber)
                {
                    logChopper.DoItNow();
                }
                else
                {
                    logChopper.DoItLater();
                }
            }
        }

        public bool IsHasNewLog(long timestamp) =>
            updateTimestamp != timestamp;

        long listUpdateTimestamp = -1;
        List<string> listCache = new List<string>();
        public IReadOnlyCollection<string> GetLogContent()
        {
            lock (logCache)
            {
                if (listUpdateTimestamp != updateTimestamp)
                {
                    listCache = logCache.ToList();
                    listUpdateTimestamp = updateTimestamp;
                }

                return listCache.AsReadOnly();
            }
        }

        #endregion

        #region private methods
        void TrimLogCache()
        {
            const int minLogLineNumber = Models.Consts.Libs.MinCacheLoggerLineNumber;
            lock (logCache)
            {
                var count = logCache.Count();
                if (count < maxLogLineNumber)
                {
                    return;
                }

                for (int i = 0; i < count - minLogLineNumber; i++)
                {
                    logCache.Dequeue();
                }
            }
        }

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            logChopper.Quit();
            lock (logCache) { }
        }
        #endregion

    }
}
