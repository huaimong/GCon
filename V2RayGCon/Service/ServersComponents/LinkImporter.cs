using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service.ServersComponents
{
    class LinkImporter
    {
        Servers servers;
        ConfigMgr configMgr;
        Setting setting;

        VgcApis.Libs.Tasks.LazyGuy serverSaver;

        public LinkImporter(
            Setting setting,
            Servers servers,
            ConfigMgr configMgr,
            VgcApis.Libs.Tasks.LazyGuy serverSaver)
        {
            this.serverSaver = serverSaver;
            this.setting = setting;
            this.servers = servers;
            this.configMgr = configMgr;
        }

        #region public methods
        /// <summary>
        /// linkList=List(string[]{0: text, 1: mark}>)
        /// </summary>
        /// <param name="linkList"></param>
        /// <param name="includingV2rayLinks"></param>
        public void ImportLinksBatchMode(
            IEnumerable<string[]> linkList, // list((text,mark))
            bool includingV2rayLinks)
        {
            // linkList:[ linksString, mark] 
            var taskList = new List<Task<Tuple<bool, List<string[]>>>>();

            foreach (var link in linkList)
            {
                taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                    () => ImportVmessLinks(link[0], link[1]),
                    TaskCreationOptions.LongRunning));
                taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                    () => ImportSSLinks(link[0], link[1]),
                    TaskCreationOptions.LongRunning));

                if (includingV2rayLinks)
                {
                    taskList.Add(new Task<Tuple<bool, List<string[]>>>(
                        () => ImportV2RayLinks(link[0], link[1]),
                        TaskCreationOptions.LongRunning));
                }
            }

            var tasks = taskList.ToArray();
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                foreach (var task in tasks)
                {
                    task.Start();
                }
                Task.WaitAll(tasks);

                var results = GetterImportLinksResult(tasks);
                servers.UpdateMarkList();
                ShowImportResult(results);
            });
        }

        public void ImportLinkWithOutV2RayLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            ImportLinksBatchMode(linkList, false);
        }

        public void ImportLinkWithV2RayLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            ImportLinksBatchMode(linkList, true);
        }
        #endregion

        #region private methods
        string[] GenImportResult(
         string link,
         bool success,
         string reason,
         string mark)
        {
            return new string[]
            {
                string.Empty,  // reserve for index
                link,
                mark,
                success?"√":"×",
                reason,
            };
        }

        void ShowImportResult(Tuple<bool, List<string[]>> results)
        {
            var isAddNewServer = results.Item1;
            var allResults = results.Item2;

            if (isAddNewServer)
            {
                servers.UpdateAllServersSummary();
                serverSaver.DoItLater();
            }

            setting.LazyGC();

            if (allResults.Count > 0)
            {
                new Views.WinForms.FormImportLinksResult(allResults);
                Application.Run();
            }
            else
            {
                MessageBox.Show(I18N.NoLinkFound);
            }
        }

        Tuple<bool, List<string[]>> GetterImportLinksResult(Task<Tuple<bool, List<string[]>>>[] tasks)
        {
            var allResults = new List<string[]>();
            var isAddNewServer = false;
            foreach (var task in tasks)
            {
                isAddNewServer = isAddNewServer || task.Result.Item1;
                allResults.AddRange(task.Result.Item2);
                task.Dispose();
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, allResults);
        }


        Tuple<bool, List<string[]>> ImportSSLinks(string text, string mark = "")
        {
            var isAddNewServer = false;
            var links = Lib.Utils.ExtractLinks(text, VgcApis.Models.Datas.Enum.LinkTypes.ss);
            List<string[]> result = new List<string[]>();

            foreach (var link in links)
            {
                var config = configMgr.SsLink2Config(link);

                if (config == null)
                {
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                    continue;
                }

                if (servers.AddServer(Lib.Utils.Config2String(config), mark, true))
                {
                    isAddNewServer = true;
                    result.Add(GenImportResult(link, true, I18N.Success, mark));
                }
                else
                {
                    result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        Tuple<bool, List<string[]>> ImportV2RayLinks(string text, string mark = "")
        {
            bool isAddNewServer = false;
            var links = Lib.Utils.ExtractLinks(text, VgcApis.Models.Datas.Enum.LinkTypes.v2ray);
            List<string[]> result = new List<string[]>();

            foreach (var link in links)
            {
                try
                {
                    var config = JObject.Parse(
                        Lib.Utils.Base64Decode(
                            Lib.Utils.GetLinkBody(link)));

                    if (config != null)
                    {
                        if (servers.AddServer(Lib.Utils.Config2String(config), mark, true))
                        {
                            isAddNewServer = true;
                            result.Add(GenImportResult(link, true, I18N.Success, mark));
                        }
                        else
                        {
                            result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                        }
                    }
                }
                catch
                {
                    // skip if error occured
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        Tuple<bool, List<string[]>> ImportVmessLinks(string text, string mark = "")
        {
            var links = Lib.Utils.ExtractLinks(text, VgcApis.Models.Datas.Enum.LinkTypes.vmess);
            var result = new List<string[]>();
            var isAddNewServer = false;

            foreach (var link in links)
            {
                var vmess = Lib.Utils.VmessLink2Vmess(link);
                var config = configMgr.Vmess2Config(vmess);

                if (config == null)
                {
                    result.Add(GenImportResult(link, false, I18N.DecodeFail, mark));
                    continue;
                }

                if (servers.AddServer(Lib.Utils.Config2String(config), mark, true))
                {
                    result.Add(GenImportResult(link, true, I18N.Success, mark));
                    isAddNewServer = true;
                }
                else
                {
                    result.Add(GenImportResult(link, false, I18N.DuplicateServer, mark));
                }
            }

            return new Tuple<bool, List<string[]>>(isAddNewServer, result);
        }

        #endregion
    }

}
