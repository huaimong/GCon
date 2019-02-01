using System;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.UserControls
{
    public partial class SubscriptionUI : UserControl
    {
        Action OnDeleted;

        public SubscriptionUI(Model.Data.SubscriptionItem subItem, Action OnDeleted)
        {
            InitializeComponent();

            lbIndex.Text = "";
            tboxUrl.Text = subItem.url;
            tboxAlias.Text = subItem.alias;
            chkIsUse.Checked = subItem.isUse;
            chkIsSetMark.Checked = subItem.isSetMark;

            this.OnDeleted = OnDeleted;
        }

        public Model.Data.SubscriptionItem GetValue()
        {
            return new Model.Data.SubscriptionItem
            {
                isUse = chkIsUse.Checked,
                isSetMark = chkIsSetMark.Checked,
                alias = tboxAlias.Text,
                url = tboxUrl.Text,
            };
        }

        #region public method
        public void SetIndex(int index)
        {
            lbIndex.Text = index.ToString();
        }
        #endregion

        #region UI event
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!Lib.UI.Confirm(I18N.ConfirmDeleteControl))
            {
                return;
            }

            var flyPanel = this.Parent as FlowLayoutPanel;
            flyPanel.Controls.Remove(this);

            this.OnDeleted?.Invoke();
        }
        #endregion

        #region private method
        private void UrlListItem_MouseDown(object sender, MouseEventArgs e)
        {
            Cursor.Current = Lib.UI.CreateCursorIconFromUserControl(this);
            DoDragDrop((SubscriptionUI)sender, DragDropEffects.Move);
        }
        #endregion
    }
}
