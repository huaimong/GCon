using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.FormMainComponent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    class FlyServer : FormMainComponentController
    {
        Form formMain;
        FlowLayoutPanel flyPanel;
        Service.Servers servers;
        Service.Setting setting;
        ToolStripComboBox cboxMarkFilter;
        ToolStripStatusLabel tslbTotal, tslbPrePage, tslbNextPage;
        ToolStripDropDownButton tsdbtnPager;
        Lib.Sys.CancelableTimeout lazyStatusBarUpdateTimer = null;
        readonly Views.UserControls.WelcomeUI welcomeItem = null;
        readonly int[] paging = new int[] { 0, 1 }; // 0: current page 1: total page

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FlyServer(
            Form formMain,
            FlowLayoutPanel panel,
            ToolStripLabel lbMarkFilter,
            ToolStripComboBox cboxMarkeFilter,
            ToolStripStatusLabel tslbTotal,
            ToolStripDropDownButton tsdbtnPager,
            ToolStripStatusLabel tslbPrePage,
            ToolStripStatusLabel tslbNextPage)
        {
            this.servers = Service.Servers.Instance;
            this.setting = Service.Setting.Instance;

            this.formMain = formMain;
            this.flyPanel = panel;
            this.cboxMarkFilter = cboxMarkeFilter;
            this.tsdbtnPager = tsdbtnPager;
            this.tslbTotal = tslbTotal;
            this.tslbPrePage = tslbPrePage;
            this.tslbNextPage = tslbNextPage;
            this.welcomeItem = new Views.UserControls.WelcomeUI();

            InitFormControls(lbMarkFilter);
            BindDragDropEvent();
            RefreshUI();
            servers.OnRequireFlyPanelUpdate += OnRequireFlyPanelUpdateHandler;
            servers.OnRequireFlyPanelReload += OnRequireFlyPanelReloadHandler;
            servers.OnRequireStatusBarUpdate += OnRequireStatusBarUpdateHandler;
        }


        #region public method
        public List<Controller.CoreServerCtrl> GetFilteredList()
        {
            var list = servers.GetServerList();
            var keywords = (searchKeywords ?? "").Split(
                new char[] { ' ', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            if (keywords.Length < 1)
            {
                return list.ToList();
            }

            return list
                .Where(serv => serv.GetterInfoFor(
                    infos => keywords.All(
                        kw => infos.Any(
                            info => Lib.Utils.PartialMatch(info, kw)))))
                .ToList();
        }

        public void LoopThroughAllServerUI(Action<Views.UserControls.ServerUI> operation)
        {
            var list = flyPanel.Controls
                .OfType<Views.UserControls.ServerUI>()
                .Select(e =>
                {
                    VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
                    {
                        operation(e);
                    });
                    return true;
                })
                .ToList();
        }

        public override void Cleanup()
        {
            servers.OnRequireFlyPanelReload -= OnRequireFlyPanelReloadHandler;
            servers.OnRequireFlyPanelUpdate -= OnRequireFlyPanelUpdateHandler;
            servers.OnRequireStatusBarUpdate -= OnRequireStatusBarUpdateHandler;
            lazyStatusBarUpdateTimer?.Release();
            lazyShowSearchResultTimer?.Release();
            RemoveAllServersConrol(true);
        }

        public void RemoveAllServersConrol(bool blocking = false)
        {
            var controlList = GetAllServersControl();

            VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
            {
                flyPanel.SuspendLayout();
                flyPanel.Controls.Clear();
                flyPanel.ResumeLayout();
            });

            if (blocking)
            {
                DisposeFlyPanelControlByList(controlList);
            }
            else
            {
                Task.Factory.StartNew(
                    () => DisposeFlyPanelControlByList(
                        controlList));
            }
        }

        public void LazyStatusBarUpdater()
        {
            // create on demand
            if (lazyStatusBarUpdateTimer == null)
            {
                lazyStatusBarUpdateTimer =
                    new Lib.Sys.CancelableTimeout(
                        UpdateStatusBar,
                        300);
            }

            lazyStatusBarUpdateTimer.Start();
        }

        public override bool RefreshUI()
        {
            ResetIndex();
            var list = this.GetFilteredList();
            var pagedList = GenPagedServerList(list);

            VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
            {
                if (pagedList.Count > 0)
                {
                    RemoveWelcomeItem();
                }
                else
                {
                    RemoveAllServersConrol();
                    if (string.IsNullOrEmpty(this.searchKeywords))
                    {
                        LoadWelcomeItem();
                    }
                    return;
                }

                RemoveDeletedServerItems(ref pagedList);
                AddNewServerItems(pagedList);
            });
            LazyStatusBarUpdater();
            return true;
        }
        #endregion

        #region private method
        List<Controller.CoreServerCtrl> GenPagedServerList(List<Controller.CoreServerCtrl> serverList)
        {
            var count = serverList.Count;
            var pageSize = setting.serverPanelPageSize;
            paging[1] = (int)Math.Ceiling(1.0 * count / pageSize);
            paging[0] = Lib.Utils.Clamp(paging[0], 0, paging[1]);

            if (serverList.Count <= 0)
            {
                return serverList;
            }

            var begin = paging[0] * pageSize;
            var num = Math.Min(pageSize, count - begin);
            return serverList.GetRange(begin, num);
        }

        void OnRequireStatusBarUpdateHandler(object sender, EventArgs args)
        {
            LazyStatusBarUpdater();
        }

        void UpdateStatusBar()
        {
            var text = string.Format(
                I18N.StatusBarServerCountTpl,
                    GetFilteredList().Count,
                    servers.GetTotalServerCount())
                + " "
                + string.Format(
                    I18N.StatusBarTplSelectedItem,
                    servers.GetTotalSelectedServerCount(),
                    GetAllServersControl().Count());

            var showPager = paging[1] > 1;

            VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
            {
                if (showPager)
                {
                    if (paging[1] != tsdbtnPager.DropDownItems.Count)
                    {
                        UpdateStatusBarPagerMenu();
                    }

                    UpdateStatusBarPagerCheckStatus();

                    tsdbtnPager.Text = string.Format(
                        I18N.StatusBarPagerInfoTpl,
                        paging[0] + 1,
                        paging[1]);
                }

                if (tsdbtnPager.Visible != showPager)
                {
                    tsdbtnPager.Visible = showPager;
                    tslbNextPage.Visible = showPager;
                    tslbPrePage.Visible = showPager;
                }

                if (text != tslbTotal.Text)
                {
                    tslbTotal.Text = text;
                }
            });

            LoopThroughAllServerUI(sui => sui.SetKeywords(searchKeywords));
        }

        private void UpdateStatusBarPagerCheckStatus()
        {
            for (int i = 0; i < tsdbtnPager.DropDownItems.Count; i++)
            {
                (tsdbtnPager.DropDownItems[i] as ToolStripMenuItem)
                    .Checked = paging[0] == i;
            }
        }

        private void UpdateStatusBarPagerMenu()
        {
            tsdbtnPager.DropDownItems.Clear();
            for (int i = 0; i < paging[1]; i++)
            {
                var index = i;
                var item = new ToolStripMenuItem(
                    string.Format(I18N.StatusBarPagerMenuTpl, (index + 1)),
                    null,
                    (s, a) =>
                    {
                        paging[0] = index;

                        // 切换分页的时候应保留原选定状态
                        // 否则无法批量对大量服务进行测速排序
                        // servers.ClearSelection();

                        RefreshUI();
                        formMain.Activate();
                    });
                tsdbtnPager.DropDownItems.Add(item);
            }
        }

        string searchKeywords = "";
        Lib.Sys.CancelableTimeout lazyShowSearchResultTimer = null;
        void LazyShowSearchResult()
        {
            // create on demand
            if (lazyShowSearchResultTimer == null)
            {
                var delay = Lib.Utils.Str2Int(StrConst.LazySaveServerListDelay);

                lazyShowSearchResultTimer =
                    new Lib.Sys.CancelableTimeout(
                        () =>
                        {
                            // 如果不RemoveAll会乱序
                            RemoveAllServersConrol();

                            // 修改搜索项时应该清除选择,否则会有可显示列表外的选中项
                            servers.SetAllServerIsSelected(false);

                            RefreshUI();
                        },
                        600);
            }

            lazyShowSearchResultTimer.Start();
        }

        private void InitFormControls(ToolStripLabel lbMarkFilter)
        {
            InitComboBoxMarkFilter();
            tslbPrePage.Click += (s, a) =>
            {
                paging[0]--;
                RefreshUI();
                formMain.Activate();
            };

            tslbNextPage.Click += (s, a) =>
            {
                paging[0]++;
                RefreshUI();
                formMain.Activate();
            };

            lbMarkFilter.Click +=
                (s, a) => this.cboxMarkFilter.Text = string.Empty;
        }

        private void InitComboBoxMarkFilter()
        {
            UpdateMarkFilterItemList(cboxMarkFilter);

            cboxMarkFilter.DropDown += (s, e) =>
            {
                // cboxMarkFilter has no Invoke method.
                VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
                {
                    UpdateMarkFilterItemList(cboxMarkFilter);
                    Lib.UI.ResetComboBoxDropdownMenuWidth(cboxMarkFilter);
                });
            };

            cboxMarkFilter.TextChanged += (s, a) =>
            {
                this.searchKeywords = cboxMarkFilter.Text;
                LazyShowSearchResult();
            };
        }

        void UpdateMarkFilterItemList(ToolStripComboBox marker)
        {
            marker.Items.Clear();
            marker.Items.AddRange(
                servers.GetMarkList().ToArray());
        }

        void AddNewServerItems(List<Controller.CoreServerCtrl> serverList)
        {
            flyPanel.Controls.AddRange(
                serverList
                    .Select(s => new Views.UserControls.ServerUI(s))
                    .ToArray());
        }

        void DisposeFlyPanelControlByList(List<Views.UserControls.ServerUI> controlList)
        {
            foreach (var control in controlList)
            {
                control.Cleanup();
            }
            VgcApis.Libs.UI.RunInUiThread(flyPanel, () =>
            {
                foreach (var control in controlList)
                {
                    control.Dispose();
                }
            });
        }

        void RemoveDeletedServerItems(ref List<Controller.CoreServerCtrl> serverList)
        {
            var deletedControlList = GetDeletedControlList(serverList);

            flyPanel.SuspendLayout();
            foreach (var control in deletedControlList)
            {
                flyPanel.Controls.Remove(control);
            }

            flyPanel.ResumeLayout();
            Task.Factory.StartNew(() => DisposeFlyPanelControlByList(deletedControlList));
        }

        List<Views.UserControls.ServerUI> GetDeletedControlList(List<Controller.CoreServerCtrl> serverList)
        {
            var result = new List<Views.UserControls.ServerUI>();

            foreach (var control in GetAllServersControl())
            {
                var config = control.GetConfig();
                if (serverList.Where(s => s.config == config)
                    .FirstOrDefault() == null)
                {
                    result.Add(control);
                }
                serverList.RemoveAll(s => s.config == config);
            }

            return result;
        }

        void RemoveWelcomeItem()
        {
            var list = flyPanel.Controls
                .OfType<Views.UserControls.WelcomeUI>()
                .Select(e =>
                {
                    flyPanel.Controls.Remove(e);
                    return true;
                })
                .ToList();
        }

        List<Views.UserControls.ServerUI> GetAllServersControl()
        {
            return flyPanel.Controls
                .OfType<Views.UserControls.ServerUI>()
                .ToList();
        }

        void OnRequireFlyPanelReloadHandler(object sender, EventArgs args)
        {
            RemoveAllServersConrol();
            RefreshUI();
        }

        void OnRequireFlyPanelUpdateHandler(object sender, EventArgs args)
        {
            RefreshUI();
        }

        private void LoadWelcomeItem()
        {
            var welcome = flyPanel.Controls
                .OfType<Views.UserControls.WelcomeUI>()
                .FirstOrDefault();

            if (welcome == null)
            {
                flyPanel.Controls.Add(welcomeItem);
            }
        }

        private void ResetIndex()
        {
            var list = servers.GetServerList().ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var index = i + 1.0; // closure
                var item = list[i];
                item.ChangeIndex(index);
            }
        }

        private void BindDragDropEvent()
        {
            flyPanel.DragEnter += (s, a) =>
            {
                a.Effect = DragDropEffects.Move;
            };

            flyPanel.DragDrop += (s, a) =>
            {
                // https://www.codeproject.com/Articles/48411/Using-the-FlowLayoutPanel-and-Reordering-with-Drag


                if (!(a.Data.GetData(typeof(Views.UserControls.ServerUI)) is Views.UserControls.ServerUI serverItemMoving))
                {
                    return;
                }

                var panel = s as FlowLayoutPanel;
                Point p = panel.PointToClient(new Point(a.X, a.Y));
                var controlDest = panel.GetChildAtPoint(p);
                int index = panel.Controls.GetChildIndex(controlDest, false);
                panel.Controls.SetChildIndex(serverItemMoving, index);
                var serverItemDest = controlDest as Views.UserControls.ServerUI;
                MoveServerListItem(ref serverItemMoving, ref serverItemDest);
            };
        }

        void MoveServerListItem(ref Views.UserControls.ServerUI moving, ref Views.UserControls.ServerUI destination)
        {
            var indexDest = destination.GetIndex();
            var indexMoving = moving.GetIndex();

            if (indexDest == indexMoving)
            {
                return;
            }

            moving.SetIndex(
                indexDest < indexMoving ?
                indexDest - 0.5 :
                indexDest + 0.5);

            RefreshUI();
        }
        #endregion
    }
}
