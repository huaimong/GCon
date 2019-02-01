using System;
using System.Linq;
using System.Windows.Forms;

namespace V2RayGCon.Controller.FormMainComponent
{
    class MenuItemsSelect : FormMainComponentController
    {
        Service.Servers servers;
        public MenuItemsSelect(
            ToolStripMenuItem selectAllCurPage,
            ToolStripMenuItem invertSelectionCurPage,
            ToolStripMenuItem selectNoneCurPage,

            ToolStripMenuItem selectAllAllPages,
            ToolStripMenuItem invertSelectionAllPages,
            ToolStripMenuItem selectNoneAllPages,

            ToolStripMenuItem selectAutorunAllPages,
            ToolStripMenuItem selectNoMarkAllPages,
            ToolStripMenuItem selectNoSpeedTestAllPages,
            ToolStripMenuItem selectRunningAllPages,
            ToolStripMenuItem selectTimeoutAllPages,
            ToolStripMenuItem selectUntrackAllPages,

            ToolStripMenuItem selectAllAllServers,
            ToolStripMenuItem invertSelectionAllServers,
            ToolStripMenuItem selectNoneAllServers,

            ToolStripMenuItem selectAutorunAllServers,
            ToolStripMenuItem selectNoMarkAllServers,
            ToolStripMenuItem selectNoSpeedTestAllServers,
            ToolStripMenuItem selectRunningAllServers,
            ToolStripMenuItem selectTimeoutAllServers,
            ToolStripMenuItem selectUntrackAllServers)


        {
            servers = Service.Servers.Instance;

            InitAllPagesSelectors(selectNoneAllPages, invertSelectionAllPages, selectAllAllPages, selectAutorunAllPages, selectRunningAllPages, selectTimeoutAllPages, selectNoSpeedTestAllPages, selectNoMarkAllPages, selectUntrackAllPages);
            InitAllServersSelector(selectNoneAllServers, invertSelectionAllServers, selectAllAllServers, selectAutorunAllServers, selectRunningAllServers, selectTimeoutAllServers, selectNoSpeedTestAllServers, selectNoMarkAllServers, selectUntrackAllServers);
            InitCurPageSelectors(selectAllCurPage, selectNoneCurPage, invertSelectionCurPage);
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
        private void InitAllPagesSelectors(
            ToolStripMenuItem selectNoneAllPages,
            ToolStripMenuItem invertSelectionAllPages,
            ToolStripMenuItem selectAllAllPages,
            ToolStripMenuItem selectAutorunAllPages,
            ToolStripMenuItem selectRunningAllPages,
            ToolStripMenuItem selectTimeoutAllPages,
            ToolStripMenuItem selectNoSpeedTestAllPages,
            ToolStripMenuItem selectNoMarkAllPages,
            ToolStripMenuItem selectUntrackAllPages)
        {
            selectAllAllPages.Click +=
                            (s, a) => SelectAllPagesWhere(el => true);

            selectNoneAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => false);

            invertSelectionAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => !el.isSelected);

            selectAutorunAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => el.isAutoRun);

            selectRunningAllPages.Click +=
                 (s, a) => SelectAllPagesWhere(el => el.isServerOn);

            selectTimeoutAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => el.speedTestResult == long.MaxValue);

            selectNoSpeedTestAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => el.speedTestResult < 0);

            selectNoMarkAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => string.IsNullOrEmpty(el.mark));

            selectUntrackAllPages.Click +=
                (s, a) => SelectAllPagesWhere(el => el.isUntrack);

        }

        private void InitAllServersSelector(
            ToolStripMenuItem selectNoneAllServers,
            ToolStripMenuItem invertSelectionAllServers,
            ToolStripMenuItem selectAllAllServers,
            ToolStripMenuItem selectAutorunAllServers,
            ToolStripMenuItem selectRunningAllServers,
            ToolStripMenuItem selectTimeoutAllServers,
            ToolStripMenuItem selectNoSpeedTestAllServers,
            ToolStripMenuItem selectNoMarkAllServers,
            ToolStripMenuItem selectUntrackAllServers)
        {
            selectAllAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => true);

            invertSelectionAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => !el.isSelected);

            selectNoneAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => false);

            selectNoMarkAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => string.IsNullOrEmpty(el.mark));

            selectNoSpeedTestAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => el.speedTestResult < 0);

            selectTimeoutAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => el.speedTestResult == long.MaxValue);

            selectRunningAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => el.isServerOn);

            selectAutorunAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => el.isAutoRun);

            selectUntrackAllServers.Click +=
                (s, a) => SelectAllServersWhere(el => el.isUntrack);
        }

        private void InitCurPageSelectors(ToolStripMenuItem selectAllCurPage, ToolStripMenuItem selectNoneCurPage, ToolStripMenuItem invertSelectionCurPage)
        {
            selectAllCurPage.Click +=
                (s, a) => SelectCurPageWhere(el => true);

            selectNoneCurPage.Click +=
                (s, a) => SelectCurPageWhere(el => false);

            invertSelectionCurPage.Click +=
                (s, a) => SelectCurPageWhere(el => !el.isSelected);
        }

        void SelectCurPageWhere(Func<Views.UserControls.ServerUI, bool> condiction)
        {
            var panel = GetFlyPanel();
            var configs = panel.GetFilteredList().Select(s => s.config);

            // clear all not in current filtered list
            servers.GetServerList()
                .Where(s => !configs.Contains(s.config))
                .Select(s =>
                {
                    s.SetIsSelected(false);
                    return true;
                })
                .ToList();

            panel.LoopThroughAllServerUI(s =>
            {
                s.SetSelected(condiction(s));
            });
        }

        void SelectAllPagesWhere(Func<Controller.CoreServerCtrl, bool> condiction)
        {
            var configs = GetFlyPanel().GetFilteredList()
                .Select(s => s.config)
                .ToList();

            servers.GetServerList()
                .Select(s =>
                {
                    if (!configs.Contains(s.config))
                    {
                        s.SetIsSelected(false);
                        return false;
                    }
                    s.SetIsSelected(condiction(s));
                    return true;
                })
                .ToList();
        }


        void SelectAllServersWhere(Func<Controller.CoreServerCtrl, bool> condiction)
        {
            servers.GetServerList()
                .Select(s =>
                {
                    s.SetIsSelected(condiction(s));
                    return true;
                })
                .ToList();
        }

        Controller.FormMainComponent.FlyServer GetFlyPanel()
        {
            return this.GetContainer()
                .GetComponent<Controller.FormMainComponent.FlyServer>();
        }
        #endregion
    }
}
