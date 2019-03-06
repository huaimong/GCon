using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.ConfigerComponet
{
    class MenuUpdater : ConfigerComponentController
    {
        Service.Servers servers;

        Views.WinForms.FormConfiger formConfiger;
        ToolStripMenuItem miReplaceServer, miLoadServer;

        VgcApis.Libs.Tasks.LazyGuy menuUpdater;

        public MenuUpdater(
            Views.WinForms.FormConfiger formConfiger,
            ToolStripMenuItem miReplaceServer,
            ToolStripMenuItem miLoadServer)
        {
            servers = Service.Servers.Instance;

            this.formConfiger = formConfiger;
            this.miReplaceServer = miReplaceServer;
            this.miLoadServer = miLoadServer;

            menuUpdater = new VgcApis.Libs.Tasks.LazyGuy(
               () =>
               {
                   try
                   {
                       VgcApis.Libs.Utils.RunInBackground(UpdateServerMenus);
                   }
                   catch { }
               },
                VgcApis.Models.Consts.Intervals.FormConfigerMenuUpdateDelay);
        }

        #region properties
        #endregion

        #region private method
        void UpdateServerMenus()
        {
            var serverList = servers.GetAllServersOrderByIndex();

            var loadServMiList = new List<ToolStripMenuItem>();
            var replaceServMiList = new List<ToolStripMenuItem>();

            for (int i = 0; i < serverList.Count; i++)
            {
                var name = string.Format(
                    "{0}.{1}",
                    i + 1,
                    serverList[i].GetCoreStates().GetName());

                var org = serverList[i].GetConfiger().GetConfig();
                loadServMiList.Add(GenMenuItemLoad(name, org));
                replaceServMiList.Add(GenMenuItemReplace(name, org));
            }

            VgcApis.Libs.UI.RunInUiThread(
                formConfiger,
                () =>
                {
                    try
                    {
                        ReplaceOldMenus(loadServMiList, replaceServMiList);
                    }
                    catch { }
                });
        }

        private void ReplaceOldMenus(List<ToolStripMenuItem> loadServMiList, List<ToolStripMenuItem> replaceServMiList)
        {
            var misReplaceServer = miReplaceServer.DropDownItems;
            var misLoadServer = miLoadServer.DropDownItems;

            misReplaceServer.Clear();
            misLoadServer.Clear();

            if (loadServMiList.Count > 0)
            {
                misLoadServer.AddRange(loadServMiList.ToArray());
                miLoadServer.Enabled = true;
            }
            else
            {
                miLoadServer.Enabled = false;
            }

            if (replaceServMiList.Count > 0)
            {
                misReplaceServer.AddRange(replaceServMiList.ToArray());
                miReplaceServer.Enabled = true;
            }
            else
            {
                miReplaceServer.Enabled = false;
            }
        }

        private ToolStripMenuItem GenMenuItemLoad(string name, string orgConfig)
        {
            var configer = container;
            var config = orgConfig;

            return new ToolStripMenuItem(name, null, (s, a) =>
            {
                if (!configer.IsConfigSaved()
                && !Lib.UI.Confirm(I18N.ConfirmLoadNewServer))
                {
                    return;
                }
                configer.LoadServer(config);
                formConfiger.SetTitle(configer.GetAlias());
            });
        }

        private ToolStripMenuItem GenMenuItemReplace(string name, string orgConfig)
        {
            var configer = container;
            var config = orgConfig;

            return new ToolStripMenuItem(name, null, (s, a) =>
            {
                if (Lib.UI.Confirm(I18N.ReplaceServer))
                {
                    if (configer.ReplaceServer(config))
                    {
                        formConfiger.SetTitle(configer.GetAlias());
                    }
                }
            });
        }
        #endregion

        #region public method
        public void Cleanup()
        {
            menuUpdater?.ForgetIt();
            menuUpdater?.Quit();
        }

        public void UpdateMenusLater() =>
            menuUpdater?.DoItLater();

        public override void Update(JObject config) { }
        #endregion
    }
}
