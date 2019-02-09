using System;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormMain : Form
    {

        #region single instance thing
        static readonly VgcApis.Models.BaseClasses.AuxSiWinForm<FormMain> auxSiForm =
            new VgcApis.Models.BaseClasses.AuxSiWinForm<FormMain>();
        static public FormMain GetForm() => auxSiForm.GetForm();
        static public void ShowForm() => auxSiForm.ShowForm();
        #endregion

        Controller.FormMainCtrl formMainCtrl;
        Service.Setting setting;
        Service.Servers servers;
        Timer updateTitleTimer = null;
        string formTitle = "";

        public FormMain()
        {
            setting = Service.Setting.Instance;
            servers = Service.Servers.Instance;

            InitializeComponent();
            VgcApis.Libs.UI.AutoSetFormIcon(this);
            Lib.UI.AutoScaleToolStripControls(this, 16);
            GenFormTitle();
        }

        public void FormMain_Shown(object sender, EventArgs e)
        {
            UpdateFormTitle(this, EventArgs.Empty);
            setting.RestoreFormRect(this);

            // https://alexpkent.wordpress.com/2011/05/11/25/
            // 添加新控件的时候会有bug,不显示新控件
            // ToolStripManager.LoadSettings(this); 

            this.FormClosing += (s, a) =>
            {
                if (updateTitleTimer != null)
                {
                    updateTitleTimer.Stop();
                    updateTitleTimer.Tick -= UpdateFormTitle;
                    updateTitleTimer.Dispose();
                }
            };

            this.FormClosed += (s, a) =>
            {
                setting.SaveFormRect(this);
                // ToolStripManager.SaveSettings(this);
                formMainCtrl.Cleanup();
                setting.LazyGC();
            };

            formMainCtrl = InitFormMainCtrl();
            BindToolStripButtonToMenuItem();

            updateTitleTimer = new Timer
            {
                Interval = 2000,
            };
            updateTitleTimer.Tick += UpdateFormTitle;
            updateTitleTimer.Start();
        }

        #region private method
        private void GenFormTitle()
        {
            var version = Lib.Utils.GetAssemblyVersion();
            formTitle = string.Format(
                "{0} v{1}",
                Properties.Resources.AppName,
                Lib.Utils.TrimVersionString(version));
        }

        private void UpdateFormTitle(object sender, EventArgs args)
        {
            var title = formTitle;
            if (setting.isPortable)
            {
                title += " - " + I18N.Portable;
            }

            this.Invoke((MethodInvoker)delegate
            {
                if (this.Text != title)
                {
                    this.Text = title;
                }
            });
        }

        void BindToolStripButtonToMenuItem()
        {
            void bind(ToolStripButton button, ToolStripMenuItem menu, bool activate = true)
            {
                if (activate)
                {
                    button.Click += (s, a) =>
                    {
                        menu.PerformClick();

                        // Do not know why, form main will lost focus sometimes.
                        this.Activate();
                    };
                }
                else
                {
                    button.Click += (s, a) => menu.PerformClick();
                }
            }

            bind(toolStripButtonSelectAllCurPage, selectAllCurPageToolStripMenuItem);
            bind(toolStripButtonInverseSelectionCurPage, invertSelectionCurPageToolStripMenuItem);
            bind(toolStripButtonSelectNoneCurPage, selectNoneCurPageToolStripMenuItem1);

            bind(toolStripButtonAllServerSelectAll, selectAllAllServersToolStripMenuItem);
            bind(toolStripButtonAllServerSelectNone, selectNoneAllServersToolStripMenuItem);

            bind(toolStripButtonRestartSelected, toolStripMenuItemRestartSelected);
            bind(toolStripButtonStopSelected, toolStripMenuItemStopSelected);

            bind(toolStripButtonModifySelected, toolStripMenuItemModifySettings);
            bind(toolStripButtonRunSpeedTest, toolStripMenuItemSpeedTestOnSelected);
            bind(toolStripButtonSortSelectedBySpeedTestResult, toolStripMenuItemSortBySpeedTest);

            bind(toolStripButtonFormOption, toolMenuItemOptions, false);
        }

        private Controller.FormMainCtrl InitFormMainCtrl()
        {
            var ctrl = new Controller.FormMainCtrl();

            ctrl.Plug(new Controller.FormMainComponent.FlyServer(
                this,
                flyServerListContainer,
                toolStripLabelMarkFilter,
                toolStripComboBoxMarkFilter,
                toolStripStatusLabelTotal,
                toolStripDropDownButtonPager,
                toolStripStatusLabelPrePage,
                toolStripStatusLabelNextPage));

            ctrl.Plug(new Controller.FormMainComponent.MenuItemsBasic(
                toolMenuItemSimAddVmessServer,
                toolMenuItemImportLinkFromClipboard,
                toolMenuItemExportAllServerToFile,
                toolMenuItemImportFromFile,
                toolMenuItemAbout,
                toolMenuItemHelp,
                toolMenuItemConfigEditor,
                toolMenuItemQRCode,
                toolMenuItemLog,
                toolMenuItemOptions,
                toolStripMenuItemDownLoadV2rayCore,
                toolStripMenuItemRemoveV2rayCore));

            ctrl.Plug(new Controller.FormMainComponent.MenuItemVgcAutoUpdate(
                toolMenuItemCheckUpdate
                ));

            ctrl.Plug(new Controller.FormMainComponent.MenuItemsSelect(
                /*
                ToolStripMenuItem selectAllCurPage,
                ToolStripMenuItem invertSelectionCurPage,
                ToolStripMenuItem selectNoneCurPage,
                */
                selectAllCurPageToolStripMenuItem,
                invertSelectionCurPageToolStripMenuItem,
                selectNoneCurPageToolStripMenuItem1,

                /*
                ToolStripMenuItem selectAllAllPages,
                ToolStripMenuItem invertSelectionAllPages,
                ToolStripMenuItem selectNoneAllPages,       
                */
                selectAllAllPagesToolStripMenuItem,
                invertSelectionAllPagesToolStripMenuItem,
                selectNoneAllPagesToolStripMenuItem,

                /*
                ToolStripMenuItem selectAutorunAllPages,
                ToolStripMenuItem selectNoMarkAllPages,
                ToolStripMenuItem selectNoSpeedTestAllPages,
                ToolStripMenuItem selectRunningAllPages,
                ToolStripMenuItem selectTimeoutAllPages,
                ToolStripMenuItem selectUntrackAllPages,
                */
                selectAutorunAllPagesToolStripMenuItem,
                selectNoMarkAllPagesToolStripMenuItem,
                selectNoSpeedTestAllPagesToolStripMenuItem,
                selectRunningAllPagesToolStripMenuItem,
                selectTimeoutAllPagesToolStripMenuItem,
                selectUntrackAllPagesToolStripMenuItem,

                /*
                ToolStripMenuItem selectAllAllServers,
                ToolStripMenuItem invertSelectionAllServers,
                ToolStripMenuItem selectNoneAllServers,
                */
                selectAllAllServersToolStripMenuItem,
                invertSelectionAllServersToolStripMenuItem,
                selectNoneAllServersToolStripMenuItem,

                /*
                ToolStripMenuItem selectAutorunAllServers,
                ToolStripMenuItem selectNoMarkAllServers,
                ToolStripMenuItem selectNoSpeedTestAllServers,
                ToolStripMenuItem selectRunningAllServers,
                ToolStripMenuItem selectTimeoutAllServers,
                ToolStripMenuItem selectUntrackAllServers,
                */
                selectAutorunAllServersToolStripMenuItem,
                selectNoMarkAllServersToolStripMenuItem,
                selectNoSpeedTestAllServersToolStripMenuItem,
                selectRunningAllServersToolStripMenuItem,
                selectTimeoutAllServersToolStripMenuItem,
                selectUntrackAllServersToolStripMenuItem));

            ctrl.Plug(new Controller.FormMainComponent.MenuItemsServer(
                // for invoke ui refresh
                //MenuStrip menuContainer,
                mainMneuStrip,

                //// misc
                //ToolStripMenuItem refreshSummary,
                //ToolStripMenuItem deleteAllServers,
                //ToolStripMenuItem deleteSelected,
                refreshSummaryToolStripMenuItem,
                toolStripMenuItemDeleteAllServer,
                toolStripMenuItemDeleteSelectedServers,

                //// copy
                //ToolStripMenuItem copyAsV2rayLinks,
                //ToolStripMenuItem copyAsVmessLinks,
                //ToolStripMenuItem copyAsSubscriptions,
                toolStripMenuItemCopyAsV2rayLink,
                toolStripMenuItemCopyAsVmessLink,
                toolStripMenuItemCopyAsSubscription,

                //// batch op
                //ToolStripMenuItem speedTestOnSelected,
                //ToolStripMenuItem modifySelected,
                //ToolStripMenuItem packSelected,
                //ToolStripMenuItem stopSelected,
                //ToolStripMenuItem restartSelected,
                toolStripMenuItemSpeedTestOnSelected,
                toolStripMenuItemModifySettings,
                toolStripMenuItemPackSelectedServers,
                toolStripMenuItemStopSelected,
                toolStripMenuItemRestartSelected,

                //// view
                //ToolStripMenuItem moveToTop,
                //ToolStripMenuItem moveToBottom,
                //ToolStripMenuItem foldPanel,
                //ToolStripMenuItem expansePanel,
                //ToolStripMenuItem sortBySpeed,
                //ToolStripMenuItem sortBySummary)
                toolStripMenuItemMoveToTop,
                toolStripMenuItemMoveToBottom,
                toolStripMenuItemFoldingPanel,
                toolStripMenuItemExpansePanel,
                toolStripMenuItemSortBySpeedTest,
                toolStripMenuItemSortBySummary));

            return ctrl;
        }

        #endregion

        #region UI event handler
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
