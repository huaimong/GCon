using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    internal sealed class ShareLinkMgr :
        Model.BaseClass.SingletonService<ShareLinkMgr>,
        VgcApis.Models.IServices.IShareLinkMgrService
    {
        Setting setting;
        Servers servers;
        Cache cache;

        ShareLinkComponents.Codecs codecs;

        public ShareLinkMgr()
        {
            codecs = new ShareLinkComponents.Codecs();
        }

        #region properties

        #endregion

        #region IShareLinkMgrService methods
        public string DecodeVmessLink(string vmessLink) =>
            codecs.Decode<ShareLinkComponents.VmessDecoder>(vmessLink);

        public string EncodeVmessLink(string config) =>
            codecs.Encode<ShareLinkComponents.VmessDecoder>(config);

        public string EncodeVeeLink(string config) =>
            codecs.Encode<ShareLinkComponents.VeeDecoder>(config);

        #endregion

        #region public methods
        public int UpdateSubscriptions(int proxyPort)
        {
            var subs = setting.GetSubscriptionItems();

            var links = Lib.Utils.FetchLinksFromSubcriptions(
                    subs, proxyPort);

            var decoders = GenDecoderList(false);
            var results = ImportLinksBatchModeSync(links, decoders);
            return results
                .Where(r => r.Item1)
                .SelectMany(r => r.Item2)
                .Where(l => l[3] == "√")
                .Count();
        }

        public void ImportLinkWithOutV2cfgLinksBatchMode(
            IEnumerable<string[]> linkList)
        {
            var decoders = GenDecoderList(false);
            ImportLinksBatchModeThen(
                linkList,
                decoders,
                (result) => ShowImportResults(result));
        }

        public void ImportLinkWithOutV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(false);
            ImportLinksBatchModeThen(
                linkList,
                decoders,
                (result) => ShowImportResults(result));
        }

        public void ImportLinkWithV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };

            // var decoders = GenDecoderList(true);

            var decoders = new List<VgcApis.Models.Interfaces.IShareLinkDecoder>
            {
                codecs.GetComponent<ShareLinkComponents.V2cfgDecoder>(),
            };

            ImportLinksBatchModeThen(
                linkList,
                decoders,
                (result) => ShowImportResults(result));
        }

        public void Run(
           Setting setting,
           Servers servers,
           Cache cache)
        {
            this.setting = setting;
            this.servers = servers;
            this.cache = cache;

            codecs.Run(cache, setting);
        }

        #endregion

        #region private methods
        void ImportLinksBatchModeThen(
            IEnumerable<string[]> linkList,
            IEnumerable<VgcApis.Models.Interfaces.IShareLinkDecoder> decoders,
            Action<List<Tuple<bool, List<string[]>>>> next)
        {
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                var results = ImportLinksBatchModeSync(linkList, decoders);
                next(results);
            });
        }

        List<VgcApis.Models.Interfaces.IShareLinkDecoder> GenDecoderList(
            bool isIncludeV2cfgDecoder)
        {
            var decoders = new List<VgcApis.Models.Interfaces.IShareLinkDecoder>
            {
                codecs.GetComponent<ShareLinkComponents.SsDecoder>(),
                codecs.GetComponent<ShareLinkComponents.VmessDecoder>(),
                codecs.GetComponent<ShareLinkComponents.VeeDecoder>(),
            };

            if (isIncludeV2cfgDecoder)
            {
                decoders.Add(codecs.GetComponent<ShareLinkComponents.V2cfgDecoder>());
            }

            return decoders;
        }

        /// <summary>
        /// <para>linkList=List(string[]{0: text, 1: mark}>)</para>
        /// <para>decoders = List(IShareLinkDecoder)</para>
        /// </summary>
        List<Tuple<bool, List<string[]>>> ImportLinksBatchModeSync(
            IEnumerable<string[]> linkList,
            IEnumerable<VgcApis.Models.Interfaces.IShareLinkDecoder> decoders)
        {
            var jobs = new List<Tuple<string, string, VgcApis.Models.Interfaces.IShareLinkDecoder>>();

            foreach (var link in linkList)
            {
                foreach (var decoder in decoders)
                {
                    jobs.Add(new Tuple<string, string, VgcApis.Models.Interfaces.IShareLinkDecoder>(
                        link[0], link[1], decoder));
                }
            }

            Tuple<bool, List<string[]>> worker(Tuple<string, string, VgcApis.Models.Interfaces.IShareLinkDecoder> job)
            {
                return ImportShareLinks(job.Item1, job.Item2, job.Item3);
            }

            return Lib.Utils.ExecuteInParallel(jobs, worker);

        }

        void ShowImportResults(IEnumerable<Tuple<bool, List<string[]>>> results)
        {
            var isAddNewServer = false;
            var lines = new List<string[]>();

            // flatten results
            foreach (var result in results)
            {
                if (result.Item1)
                {
                    isAddNewServer = true;
                }
                foreach (var line in result.Item2)
                {
                    lines.Add(line);
                }
            }

            if (isAddNewServer)
            {
                servers.UpdateAllServersSummaryBg();
            }

            if (lines.Count <= 0)
            {
                MessageBox.Show(I18N.NoLinkFound);
                return;
            }

            var form = new Views.WinForms.FormImportLinksResult(lines);
            form.Show();
            Application.Run();
        }

        private Tuple<bool, List<string[]>> ImportShareLinks(
            string text,
            string mark,
            VgcApis.Models.Interfaces.IShareLinkDecoder decoder)
        {
            var links = decoder.ExtractLinksFromText(text);
            bool isAddNewServer = false;

            string[] worker(string link)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                var decodedConfig = codecs.Decode(link, decoder);
                watch.Stop();
                Debug.WriteLine("decode: ", watch.ElapsedTicks)
                var msg = AddLinkToServerList(mark, decodedConfig);
                return GenImportResult(link, msg.Item1, msg.Item2, mark);
            }

            var results = Lib.Utils.ExecuteInParallel(links, worker);
            foreach (var result in results)
            {
                if (result[3] == "√")
                {
                    isAddNewServer = true;
                }
            }
            return new Tuple<bool, List<string[]>>(isAddNewServer, results);
        }

        private Tuple<bool, string> AddLinkToServerList(
            string mark,
            string decodedConfig)
        {
            if (string.IsNullOrEmpty(decodedConfig))
            {
                return new Tuple<bool, string>(false, I18N.DecodeFail);
            }
            var isSuccess = servers.AddServer(decodedConfig, mark, true);
            var reason = isSuccess ? I18N.Success : I18N.DuplicateServer;
            return new Tuple<bool, string>(isSuccess, reason);
        }

        string[] GenImportResult(
            string link,
            bool success,
            string reason,
            string mark)
        {
            return new string[]
            {
                string.Empty,  // reserved for index
                link,
                mark,
                success?"√":"×",
                reason,
            };
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            codecs?.Dispose();
        }
        #endregion
    }
}
