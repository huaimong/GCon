using System;
using System.Collections.Generic;
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

        public string EncodeV2cfgLink(string config) =>
            codecs.Encode<ShareLinkComponents.V2cfgDecoder>(config);

        #endregion

        #region public methods
        public void ImportLinkWithOutV2cfgLinksBatchMode(
            IEnumerable<string[]> linkList)
        {
            var decoders = GenDecoderList(false);
            ImportLinksBatchModeBg(linkList, decoders);
        }

        public void ImportLinkWithOutV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(false);
            ImportLinksBatchModeBg(linkList, decoders);
        }

        public void ImportLinkWithV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(true);
            ImportLinksBatchModeBg(linkList, decoders);
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
        void ImportLinksBatchModeBg(
            IEnumerable<string[]> linkList,
            IEnumerable<VgcApis.Models.Interfaces.IShareLinkDecoder> decoders)
        {
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                ImportLinksBatchModeSync(linkList, decoders);
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
        void ImportLinksBatchModeSync(
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

            var results = Lib.Utils.ExecuteInParallel(jobs, worker);
            ShowImportResults(results);
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
                servers.UpdateAllServersSummary();
            }

            if (lines.Count > 0)
            {
                new Views.WinForms.FormImportLinksResult(lines);
                Application.Run();
            }
            else
            {
                MessageBox.Show(I18N.NoLinkFound);
            }
        }

        private Tuple<bool, List<string[]>> ImportShareLinks(
            string text,
            string mark,
            VgcApis.Models.Interfaces.IShareLinkDecoder decoder)
        {
            var links = decoder.ExtractLinksFromText(text);
            bool isAddNewServer = false;
            List<string[]> results = new List<string[]>();
            foreach (var link in links)
            {
                var decodedConfig = decoder.Decode(link);
                var result = AddLinkToServerList(mark, decodedConfig);
                if (result.Item1)
                {
                    isAddNewServer = true;
                }
                results.Add(GenImportResult(link, result.Item1, result.Item2, mark));
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
