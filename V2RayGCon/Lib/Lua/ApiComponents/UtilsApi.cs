using System;
using System.Collections.Generic;

namespace V2RayGCon.Lib.Lua.ApiComponents
{
    public class UtilsApi :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.IServices.IUtilsService
    {
        public void ExecuteInParallel<TParam>(
            IEnumerable<TParam> source, Action<TParam> worker) =>
            Lib.Utils.ExecuteInParallel(source, worker);

        public void ExecuteInParallel<TParam, TResult>(
            IEnumerable<TParam> source, Func<TParam, TResult> worker) =>
            Lib.Utils.ExecuteInParallel(source, worker);

    }
}
