using System.Windows.Forms;

namespace ProxySetter.Controllers.VGCPluginComponents
{
    class TabPacCustomList : ComponentCtrl
    {
        Services.PsSettings setting;

        string[] oldCustomPacList;
        RichTextBox rtboxWhiteList, rtboxBlackList;

        public TabPacCustomList(
            Services.PsSettings setting,

            RichTextBox rtboxWhiteList,
            RichTextBox rtboxBlackList)
        {
            this.setting = setting;

            // oldCustomPacList != customPacList
            oldCustomPacList = setting.GetCustomPacSetting();

            this.rtboxBlackList = rtboxBlackList;
            this.rtboxWhiteList = rtboxWhiteList;

            InitControls();
        }

        #region private methods
        private void InitControls()
        {
            rtboxWhiteList.Text = oldCustomPacList[0];
            rtboxBlackList.Text = oldCustomPacList[1];
        }
        #endregion

        #region public method
        public void Reload()
        {
            oldCustomPacList = setting.GetCustomPacSetting();
            VgcApis.Libs.UI.RunInUiThread(rtboxWhiteList, () =>
            {
                InitControls();
            });
        }

        public override void Cleanup()
        {
            // do nothing
        }

        public override bool IsOptionsChanged()
        {
            if (oldCustomPacList[0] != rtboxWhiteList.Text
                || oldCustomPacList[1] != rtboxBlackList.Text)
            {
                return true;
            }

            return false;
        }

        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            oldCustomPacList[0] = rtboxWhiteList.Text;
            oldCustomPacList[1] = rtboxBlackList.Text;

            setting.SaveCustomPacSetting(oldCustomPacList);
            return true;
        }
        #endregion
    }
}
