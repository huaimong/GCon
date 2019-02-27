using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.OptionComponent
{
    class Subscription : OptionComponentController
    {
        FlowLayoutPanel flyPanel;
        Button btnAdd, btnUpdate;
        CheckBox chkSubsIsUseProxy;

        Service.Setting setting;
        Service.Servers servers;
        Service.ShareLinkMgr slinkMgr;

        string oldOptions;

        public Subscription(
            FlowLayoutPanel flyPanel,
            Button btnAdd,
            Button btnUpdate,
            CheckBox chkSubsIsUseProxy)
        {
            this.setting = Service.Setting.Instance;
            this.servers = Service.Servers.Instance;
            this.slinkMgr = Service.ShareLinkMgr.Instance;

            this.flyPanel = flyPanel;
            this.btnAdd = btnAdd;
            this.btnUpdate = btnUpdate;
            this.chkSubsIsUseProxy = chkSubsIsUseProxy;

            chkSubsIsUseProxy.Checked = setting.isUpdateUseProxy;
            InitPanel();
            BindEvent();
        }

        #region public method
        public override bool SaveOptions()
        {
            string curOptions = GetCurOptions();

            if (curOptions != oldOptions)
            {
                setting.SaveSubscriptionItems(curOptions);
                oldOptions = curOptions;
                return true;
            }
            return false;
        }

        public override bool IsOptionsChanged()
        {
            return GetCurOptions() != oldOptions;
        }

        public void Reload(string rawSetting)
        {
            setting.SaveSubscriptionItems(rawSetting);
            Lib.UI.ClearFlowLayoutPanel(this.flyPanel);
            InitPanel();
        }
        #endregion

        #region private method
        string GetCurOptions()
        {
            return JsonConvert.SerializeObject(
                CollectSubscriptionItems());
        }

        List<Model.Data.SubscriptionItem> CollectSubscriptionItems()
        {
            var itemList = new List<Model.Data.SubscriptionItem>();
            foreach (Views.UserControls.SubscriptionUI item in this.flyPanel.Controls)
            {
                var v = item.GetValue();
                if (!string.IsNullOrEmpty(v.alias)
                    || !string.IsNullOrEmpty(v.url))
                {
                    itemList.Add(v);
                }
            }
            return itemList;
        }

        void InitPanel()
        {
            var subItemList = setting.GetSubscriptionItems();

            this.oldOptions = JsonConvert.SerializeObject(subItemList);

            if (subItemList.Count <= 0)
            {
                subItemList.Add(new Model.Data.SubscriptionItem());
            }

            foreach (var item in subItemList)
            {
                this.flyPanel.Controls.Add(new Views.UserControls.SubscriptionUI(item, UpdatePanelItemsIndex));
            }

            UpdatePanelItemsIndex();
        }

        void BindEventBtnAddClick()
        {
            this.btnAdd.Click += (s, a) =>
            {
                this.flyPanel.Controls.Add(
                    new Views.UserControls.SubscriptionUI(
                        new Model.Data.SubscriptionItem(),
                        UpdatePanelItemsIndex));
                UpdatePanelItemsIndex();
            };
        }

        void BindEventBtnUpdateClick()
        {
            this.btnUpdate.Click += (s, a) =>
            {
                this.btnUpdate.Enabled = false;

                var subs = new Dictionary<string, string>();
                foreach (Views.UserControls.SubscriptionUI item in this.flyPanel.Controls)
                {
                    var value = item.GetValue();
                    if (value.isUse
                        && !string.IsNullOrEmpty(value.url)
                        && !subs.ContainsKey(value.url))
                    {
                        subs[value.url] = value.isSetMark ? value.alias : null;
                    }
                }

                if (subs.Count <= 0)
                {
                    this.btnUpdate.Enabled = true;
                    MessageBox.Show(I18N.NoSubsUrlAvailable);
                    return;
                }

                ImportFromSubscriptionUrls(subs);
            };
        }

        void BindEventFlyPanelDragDrop()
        {
            this.flyPanel.DragDrop += (s, a) =>
            {
                // https://www.codeproject.com/Articles/48411/Using-the-FlowLayoutPanel-and-Reordering-with-Drag

                var data = a.Data.GetData(typeof(Views.UserControls.SubscriptionUI))
                    as Views.UserControls.SubscriptionUI;

                var dest = s as FlowLayoutPanel;
                Point p = dest.PointToClient(new Point(a.X, a.Y));
                var item = dest.GetChildAtPoint(p);
                int index = dest.Controls.GetChildIndex(item, false);
                dest.Controls.SetChildIndex(data, index);
                dest.Invalidate();
            };
        }

        void BindEvent()
        {
            BindEventBtnAddClick();
            BindEventBtnUpdateClick();
            BindEventFlyPanelDragDrop();

            this.flyPanel.DragEnter += (s, a) =>
            {
                a.Effect = DragDropEffects.Move;
            };
        }

        void UpdatePanelItemsIndex()
        {
            var index = 1;
            foreach (Views.UserControls.SubscriptionUI item in this.flyPanel.Controls)
            {
                item.SetIndex(index++);
            }
        }

        private void ImportFromSubscriptionUrls(
            Dictionary<string, string> subscriptions)
        {
            VgcApis.Libs.Utils.RunInBackground(() =>
            {
                // dict( [url]=>mark ) to list(url,mark) mark maybe null
                var subsUrl = subscriptions.Select(s => s).ToList();
                List<string[]> links = BatchGetLinksFromSubsUrl(subsUrl);
                slinkMgr.ImportLinkWithOutV2cfgLinksBatchMode(links);
                EnableBtnUpdate();
            });
        }

        int GetAvailableHttpProxyPort()
        {
            if (!chkSubsIsUseProxy.Checked)
            {
                return -1;
            }

            var port = servers.GetAvailableHttpProxyPort();
            if (port > 0)
            {
                return port;
            }

            VgcApis.Libs.Utils.RunInBackground(
                () => MessageBox.Show(
                    I18N.NoQualifyProxyServer));

            return -1;
        }

        private List<string[]> BatchGetLinksFromSubsUrl(
            List<KeyValuePair<string, string>> subscriptionInfos)
        {
            var proxyPort = GetAvailableHttpProxyPort();

            Func<KeyValuePair<string, string>, string[]> worker = (item) =>
            {
                // item[url]=mark
                var subsString = Lib.Utils.Fetch(
                    item.Key,
                    proxyPort,
                    VgcApis.Models.Consts.Import.ParseImportTimeout);

                if (string.IsNullOrEmpty(subsString))
                {
                    setting.SendLog(I18N.DownloadFail + "\n" + item.Key);
                    return new string[] { string.Empty, item.Value };
                }

                var links = new List<string>();
                var matches = Regex.Matches(
                    subsString,
                    VgcApis.Models.Consts.Patterns.Base64NonStandard);
                foreach (Match match in matches)
                {
                    try
                    {
                        links.Add(Lib.Utils.Base64Decode(match.Value));
                    }
                    catch { }
                }

                return new string[] { string.Join("\n", links), item.Value };
            };

            return Lib.Utils.ExecuteInParallel(subscriptionInfos, worker);
        }

        private void EnableBtnUpdate()
        {
            try
            {
                VgcApis.Libs.UI.RunInUiThread(btnUpdate, () =>
                {
                    this.btnUpdate.Enabled = true;
                });
            }
            catch { }
        }
        #endregion
    }
}
