using System;
using System.Collections.Generic;

namespace VgcApis.Models.IServices
{
    public interface IUtilsService
    {
        void ExecuteInParallel<TParam>(
            IEnumerable<TParam> source, Action<TParam> worker);

        void ExecuteInParallel<TParam, TResult>(
            IEnumerable<TParam> source, Func<TParam, TResult> worker);
    }
}
