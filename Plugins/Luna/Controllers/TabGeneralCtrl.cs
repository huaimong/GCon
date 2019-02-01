using System;
using System.Windows.Forms;

namespace Luna.Controllers
{
    public class TabGeneralCtrl
    {
        Button btnStopAll, btnKillAll;
        FlowLayoutPanel flyLuaUiPanel;

        public TabGeneralCtrl(
            FlowLayoutPanel flyLuaUiPanel,
            Button btnStopAll,
            Button btnKillAll)
        {
            BindControls(flyLuaUiPanel, btnStopAll, btnKillAll);
        }

        #region public methods
        Services.LuaServer luaServer;
        public void Run(Services.LuaServer luaServer)
        {
            this.luaServer = luaServer;
            BindEvents(luaServer);
            RefreshFlyPanel();
            luaServer.OnLuaCoreCtrlListChange += OnLuaCoreCtrlListChangeHandler;
        }

        private void BindEvents(Services.LuaServer luaServer)
        {
            btnStopAll.Click += (s, a) =>
            {
                var ctrls = luaServer.GetAllLuaCoreCtrls();
                foreach (var c in ctrls)
                {
                    c.Stop();
                }
            };

            btnKillAll.Click += (s, a) =>
            {
                var ctrls = luaServer.GetAllLuaCoreCtrls();
                foreach (var c in ctrls)
                {
                    c.Kill();
                }
            };
        }

        public void Cleanup()
        {
            luaServer.OnLuaCoreCtrlListChange -= OnLuaCoreCtrlListChangeHandler;
            RunInUiThread(() =>
            {
                ClearFlyPanel();
            });
        }
        #endregion

        #region private methods
        void OnLuaCoreCtrlListChangeHandler(object sender, EventArgs args)
        {
            RefreshFlyPanel();
        }

        void RunInUiThread(Action updater)
        {
            VgcApis.Libs.UI.RunInUiThread(flyLuaUiPanel, () =>
            {
                updater();
            });
        }

        void RefreshFlyPanel()
        {
            RunInUiThread(() =>
            {
                ClearFlyPanel();
                AddLuaCoreCtrlToPanel();
            });
        }

        void AddLuaCoreCtrlToPanel()
        {
            var ctrls = luaServer.GetAllLuaCoreCtrls();
            foreach (var c in ctrls)
            {
                var ui = new Views.UserControls.LuaUI(c);
                flyLuaUiPanel.Controls.Add(ui);
            }
        }

        void ClearFlyPanel()
        {
            var list = flyLuaUiPanel.Controls;
            foreach (Views.UserControls.LuaUI c in list)
            {
                c.Cleanup();
            }
            flyLuaUiPanel.Controls.Clear();
        }

        private void BindControls(FlowLayoutPanel flyLuaUiPanel, Button btnStopAll, Button btnKillAll)
        {
            this.btnStopAll = btnStopAll;
            this.btnKillAll = btnKillAll;
            this.flyLuaUiPanel = flyLuaUiPanel;
        }

        #endregion
    }
}
