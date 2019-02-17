using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.FormMainComponent
{
    class MenuItemsServer : FormMainComponentController
    {
        Service.Cache cache;
        Service.Servers servers;
        readonly Service.Setting setting;
        readonly MenuStrip menuContainer;

        public MenuItemsServer(
            // for invoke ui refresh
            MenuStrip menuContainer,

            // misc
            ToolStripMenuItem refreshSummary,
            ToolStripMenuItem deleteAllServers,
            ToolStripMenuItem deleteSelected,

            // copy
            ToolStripMenuItem copyAsV2rayLinks,
            ToolStripMenuItem copyAsVmessLinks,
            ToolStripMenuItem copyAsSubscriptions,

            // batch op
            ToolStripMenuItem speedTestOnSelected,

            ToolStripMenuItem modifySelected,
            ToolStripMenuItem packSelected,
            ToolStripMenuItem stopSelected,
            ToolStripMenuItem restartSelected,

            // view
            ToolStripMenuItem moveToTop,
            ToolStripMenuItem moveToBottom,
            ToolStripMenuItem foldPanel,
            ToolStripMenuItem expansePanel,
            ToolStripMenuItem sortBySpeed,
            ToolStripMenuItem sortBySummary)
        {
            cache = Service.Cache.Instance;
            servers = Service.Servers.Instance;
            setting = Service.Setting.Instance;

            this.menuContainer = menuContainer; // for invoke ui update

            InitCtrlSorting(sortBySpeed, sortBySummary);
            InitCtrlView(moveToTop, moveToBottom, foldPanel, expansePanel);
            InitCtrlCopyToClipboard(copyAsV2rayLinks, copyAsVmessLinks, copyAsSubscriptions);
            InitCtrlMisc(refreshSummary, deleteSelected, deleteAllServers);
            InitCtrlBatchOperation(
                stopSelected,
                restartSelected,
                speedTestOnSelected,
                modifySelected,
                packSelected);

        }

        #region public method
        public override bool RefreshUI()
        {
            return false;
        }

        public override void Cleanup()
        {
        }
        #endregion

        #region private method
        EventHandler ApplyActionOnSelectedServers(Action lambda)
        {
            return (s, a) =>
            {
                if (!servers.IsSelecteAnyServer())
                {
                    VgcApis.Libs.Utils.RunInBackground(() => MessageBox.Show(I18N.SelectServerFirst));
                    return;
                }
                lambda();
            };
        }

        private void InitCtrlBatchOperation(
            ToolStripMenuItem stopSelected,
            ToolStripMenuItem restartSelected,
            ToolStripMenuItem speedTestOnSelected,
            ToolStripMenuItem modifySelected,
            ToolStripMenuItem packSelected)
        {


            modifySelected.Click += ApplyActionOnSelectedServers(
                () => Views.WinForms.FormBatchModifyServerSetting.GetForm());

            packSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                var list = servers
                    .GetAllServersOrderByIndex()
                    .Where(s => s.GetCoreStates().IsSelected())
                    .Select(s => s as VgcApis.Models.Interfaces.ICoreServCtrl)
                    .ToList();

                if (setting.isUseV4)
                {
                    servers.PackServersIntoV4Package(list, null, null);
                }
                else
                {
                    list.Reverse();
                    servers.PackServersIntoV3Package(list);
                }
            });

            speedTestOnSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (!Lib.UI.Confirm(I18N.TestWillTakeALongTime))
                {
                    return;
                }

                servers.RunSpeedTestOnSelectedServersBg();
            });

            stopSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (Lib.UI.Confirm(I18N.ConfirmStopAllSelectedServers))
                {
                    servers.StopSelectedServersThen();
                }
            });

            restartSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (Lib.UI.Confirm(I18N.ConfirmRestartAllSelectedServers))
                {
                    servers.RestartSelectedServersThen();
                }
            });
        }

        private void InitCtrlMisc(
            ToolStripMenuItem refreshSummary,
            ToolStripMenuItem deleteSelected,
            ToolStripMenuItem deleteAllItems)
        {
            refreshSummary.Click += (s, a) =>
            {
                cache.html.Clear();
                servers.UpdateAllServersSummary();
            };

            deleteAllItems.Click += (s, a) =>
            {
                if (!Lib.UI.Confirm(I18N.ConfirmDeleteAllServers))
                {
                    return;
                }
                Service.Servers.Instance.DeleteAllServersThen();
                Service.Cache.Instance.core.Clear();
            };

            deleteSelected.Click += ApplyActionOnSelectedServers(() =>
            {
                if (!Lib.UI.Confirm(I18N.ConfirmDeleteSelectedServers))
                {
                    return;
                }
                servers.DeleteSelectedServersThen();
            });
        }

        private void InitCtrlCopyToClipboard(ToolStripMenuItem copyAsV2rayLinks, ToolStripMenuItem copyAsVmessLinks, ToolStripMenuItem copyAsSubscriptions)
        {
            copyAsSubscriptions.Click += ApplyActionOnSelectedServers(() =>
            {
                MessageBox.Show(
                Lib.Utils.CopyToClipboard(
                    Lib.Utils.Base64Encode(
                        EncodeAllServersIntoVmessLinks())) ?
                I18N.LinksCopied :
                I18N.CopyFail);
            });

            copyAsV2rayLinks.Click += ApplyActionOnSelectedServers(() =>
            {
                var list = servers.GetAllServersOrderByIndex()
                    .Where(s => s.GetCoreStates().IsSelected())
                    .Select(s => Lib.Utils.AddLinkPrefix(
                        Lib.Utils.Base64Encode(s.GetConfiger().GetConfig()),
                        VgcApis.Models.Datas.Enum.LinkTypes.v2ray))
                    .ToList();

                MessageBox.Show(
                    Lib.Utils.CopyToClipboard(
                        string.Join(Environment.NewLine, list)) ?
                    I18N.LinksCopied :
                    I18N.CopyFail);
            });

            copyAsVmessLinks.Click += ApplyActionOnSelectedServers(() =>
            {
                MessageBox.Show(
                   Lib.Utils.CopyToClipboard(
                       EncodeAllServersIntoVmessLinks()) ?
                   I18N.LinksCopied :
                   I18N.CopyFail);
            });
        }

        private void InitCtrlView(
            ToolStripMenuItem moveToTop,
            ToolStripMenuItem moveToBottom,
            ToolStripMenuItem collapsePanel,
            ToolStripMenuItem expansePanel)
        {
            expansePanel.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemPanelCollapseLevel(0);
            });

            collapsePanel.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemPanelCollapseLevel(1);
            });

            moveToTop.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemsIndex(0);
            });

            moveToBottom.Click += ApplyActionOnSelectedServers(() =>
            {
                SetServerItemsIndex(double.MaxValue);
            });
        }

        private void InitCtrlSorting(ToolStripMenuItem sortBySpeed, ToolStripMenuItem sortBySummary)
        {
            sortBySummary.Click += ApplyActionOnSelectedServers(
                SortServerListBySummary);

            sortBySpeed.Click += ApplyActionOnSelectedServers(
                SortServerListBySpeedTestResult);
        }

        void SortServerListBySummary()
        {
            var list = servers.GetAllServersOrderByIndex().Where(s => s.GetCoreStates().IsSelected()).ToList();
            if (list.Count < 2)
            {
                return;
            }

            SortServerItemList(
                ref list,
                (a, b) => a.GetCoreStates().GetSummary().CompareTo(b.GetCoreStates().GetSummary()));

            RemoveAllControlsAndRefreshFlyPanel();
        }

        static void SortServerItemList(
             ref List<VgcApis.Models.Interfaces.ICoreServCtrl> list,
             Comparison<VgcApis.Models.Interfaces.ICoreServCtrl> comparer)
        {
            if (list == null || list.Count < 2)
            {
                return;
            }

            list.Sort(comparer);
            var minIndex = list.First().GetCoreStates().GetIndex();
            var delta = 1.0 / 2 / list.Count;
            for (int i = 1; i < list.Count; i++)
            {
                list[i].GetCoreStates().SetIndexQuiet(minIndex + delta * i);
            }
        }

        private void SortServerListBySpeedTestResult()
        {
            var list = servers.GetAllServersOrderByIndex().Where(s => s.GetCoreStates().IsSelected()).ToList();
            if (list.Count < 2)
            {
                return;
            }

            SortServerItemList(
                ref list,
                (a, b) =>
                {
                    var spa = a.GetCoreStates().GetSpeedTestResult();
                    var spb = b.GetCoreStates().GetSpeedTestResult();
                    return spa.CompareTo(spb);
                });
            RemoveAllControlsAndRefreshFlyPanel();
        }

        void SetServerItemPanelCollapseLevel(int collapseLevel)
        {
            collapseLevel = Lib.Utils.Clamp(collapseLevel, 0, 3);
            servers
                .GetAllServersOrderByIndex()
                .Where(s => s.GetCoreStates().IsSelected())
                .Select(s =>
                {
                    s.GetCoreStates().SetFoldingLevel(collapseLevel);
                    return true;
                })
                .ToList(); // force linq to execute
        }

        void RemoveAllControlsAndRefreshFlyPanel()
        {
            var panel = GetFlyPanel();
            panel.RemoveAllServersConrol();
            panel.RefreshUI();
        }

        void SetServerItemsIndex(double index)
        {
            servers.GetAllServersOrderByIndex()
                .Where(s => s.GetCoreStates().IsSelected())
                .Select(s =>
                {
                    s.GetCoreStates().SetIndex(index);
                    return true;
                })
                .ToList(); // force linq to execute

            RemoveAllControlsAndRefreshFlyPanel();
        }

        string EncodeAllServersIntoVmessLinks()
        {
            var serverList = servers.GetAllServersOrderByIndex();
            string result = string.Empty;

            foreach (var server in serverList)
            {
                if (!server.GetCoreStates().IsSelected())
                {
                    continue;
                }
                var vmess = Lib.Utils.ConfigString2Vmess(server.GetConfiger().GetConfig());
                var vmessLink = Lib.Utils.Vmess2VmessLink(vmess);

                if (!string.IsNullOrEmpty(vmessLink))
                {
                    result += vmessLink + System.Environment.NewLine;
                }
            }

            return result;
        }

        Controller.FormMainComponent.FlyServer GetFlyPanel()
        {
            return this.GetContainer()
                .GetComponent<Controller.FormMainComponent.FlyServer>();
        }
        #endregion
    }
}
