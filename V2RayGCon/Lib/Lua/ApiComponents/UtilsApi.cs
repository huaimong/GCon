using System;
using System.Collections.Generic;
using System.Threading;

namespace V2RayGCon.Lib.Lua.ApiComponents
{
    public class UtilsApi :
        VgcApis.Models.BaseClasses.Disposable,
        VgcApis.Models.IServices.IUtilsService
    {
        public string ScanQrcode()
        {
            var shareLink = @"";
            AutoResetEvent are = new AutoResetEvent(false);

            void Success(string result)
            {
                shareLink = result;
                are.Set();
            }

            void Fail()
            {
                are.Set();
            }

            Lib.QRCode.QRCode.ScanQRCode(Success, Fail);
            are.WaitOne();
            return shareLink;
        }

        public void ExecuteInParallel<TParam>(
            IEnumerable<TParam> source, Action<TParam> worker) =>
            Lib.Utils.ExecuteInParallel(source, worker);

        public void ExecuteInParallel<TParam, TResult>(
            IEnumerable<TParam> source, Func<TParam, TResult> worker) =>
            Lib.Utils.ExecuteInParallel(source, worker);

    }
}
