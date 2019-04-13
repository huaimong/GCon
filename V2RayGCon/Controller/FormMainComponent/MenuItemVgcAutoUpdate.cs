using System.Windows.Forms;

namespace V2RayGCon.Controller.FormMainComponent
{
    class MenuItemVgcAutoUpdate : FormMainComponentController
    {
        Service.Updater updater;

        public MenuItemVgcAutoUpdate(
            ToolStripMenuItem miCheckVgcUpdate)
        {
            updater = Service.Updater.Instance;
            miCheckVgcUpdate.Click += (s, a) => updater.CheckForUpdate(true);
        }

        #region public method
        #endregion

        #region component things
        public override bool RefreshUI() => false;
        public override void Cleanup() { }
        #endregion

        #region private method

        #endregion
    }
}
