using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller
{
    class FormOptionCtrl : Model.BaseClass.FormController
    {
        Service.Setting setting;

        public FormOptionCtrl()
        {
            this.setting = Service.Setting.Instance;
        }

        public bool IsOptionsSaved()
        {
            foreach (var component in GetAllComponents())
            {
                var optCtrl = component.Value as OptionComponent.OptionComponentController;
                if (optCtrl.IsOptionsChanged())
                {
                    return false;
                }
            }
            return true;
        }

        public bool SaveAllOptions()
        {
            var changed = false;
            foreach (var kvPair in GetAllComponents())
            {
                var component = kvPair.Value as OptionComponent.OptionComponentController;
                if (component.SaveOptions())
                {
                    changed = true;
                }
            }
            return changed;
        }

        public void BackupOptions()
        {
            if (!IsOptionsSaved())
            {
                MessageBox.Show(I18N.SaveChangeFirst);
                return;
            }

            var serverString = string.Empty;
            foreach (var server in Service.Servers.Instance.GetServerList())
            {
                // insert a space in the front for regex matching
                serverString += " v2ray://"
                    + Lib.Utils.Base64Encode(server.GetStates().GetConfig())
                    + Environment.NewLine;
            }

            var data = new Dictionary<string, string> {
                    { "import", JsonConvert.SerializeObject(setting.GetGlobalImportItems())},
                    { "subscription",JsonConvert.SerializeObject(setting.GetSubscriptionItems()) },
                    { "servers" ,serverString},
                };

            VgcApis.Libs.UI.SaveToFile(
                VgcApis.Models.Consts.Files.TxtExt,
                JsonConvert.SerializeObject(data));
        }

        public void RestoreOptions()
        {
            string backup = VgcApis.Libs.UI.ReadFileContentFromDialog(
                VgcApis.Models.Consts.Files.TxtExt);

            if (backup == null)
            {
                return;
            }

            if (!Lib.UI.Confirm(I18N.ConfirmAllOptionWillBeReplaced))
            {
                return;
            }

            Dictionary<string, string> options;
            try
            {
                options = JsonConvert.DeserializeObject<Dictionary<string, string>>(backup);
            }
            catch
            {
                MessageBox.Show(I18N.DecodeFail);
                return;
            }

            if (options.ContainsKey("import"))
            {
                GetComponent<Controller.OptionComponent.Import>()
                    .Reload(options["import"]);
            }

            if (options.ContainsKey("subscription"))
            {
                GetComponent<Controller.OptionComponent.Subscription>()
                    .Reload(options["subscription"]);
            }

            if (options.ContainsKey("servers")
                && Lib.UI.Confirm(I18N.ConfirmImportServers))
            {
                Service.Servers.Instance.ImportLinksWithV2RayLinks(options["servers"]);
            }
            else
            {
                MessageBox.Show(I18N.Done);
            }

        }

    }
}
