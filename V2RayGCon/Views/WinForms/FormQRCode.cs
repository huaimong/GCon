using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Views.WinForms
{
    public partial class FormQRCode : Form
    {
        #region Sigleton
        static FormQRCode _instant;
        public static FormQRCode GetForm()
        {
            if (_instant == null || _instant.IsDisposed)
            {
                _instant = new FormQRCode();
            }
            return _instant;
        }
        #endregion

        Service.Servers servers;
        Service.ShareLinkMgr slinkMgr;

        int servIndex, linkType;
        Dictionary<string, string> serverList;

        FormQRCode()
        {
            servers = Service.Servers.Instance;
            slinkMgr = Service.ShareLinkMgr.Instance;

            servIndex = 0;
            linkType = 0;

            InitializeComponent();

            VgcApis.Libs.UI.AutoSetFormIcon(this);
            this.Show();
        }

        private void FormQRCode_Shown(object sender, EventArgs e)
        {
            serverList = new Dictionary<string, string>();
            UpdateCboxServerNameList();
            cboxLinkType.SelectedIndex = linkType;

            this.FormClosed += (s, a) =>
            {
                servers.OnRequireMenuUpdate -= SettingChange;
            };

            servers.OnRequireMenuUpdate += SettingChange;
            SetPicZoomMode();
        }

        #region private methods
        void SetPicZoomMode()
        {
            picQRCode.SizeMode = rbtnIsCenterImage.Checked ?
                PictureBoxSizeMode.CenterImage :
                PictureBoxSizeMode.Zoom;
        }

        void SettingChange(object sender, EventArgs args)
        {
            try
            {
                VgcApis.Libs.UI.RunInUiThread(cboxServList, () =>
                {
                    UpdateCboxServerNameList();
                });
            }
            catch { }
        }

        void UpdateCboxServerNameList(int index = -1)
        {
            var oldIndex = index < 0 ? cboxServList.SelectedIndex : index;

            cboxServList.Items.Clear();

            var serverList = servers.GetAllServersOrderByIndex();

            if (serverList.Count <= 0)
            {
                cboxServList.SelectedIndex = -1;
                return;
            }

            this.serverList = new Dictionary<string, string>();
            foreach (var server in serverList)
            {
                var name = server.GetCoreStates().GetName();
                cboxServList.Items.Add(name);
                this.serverList[name] = server.GetConfiger().GetConfig();
            }

            servIndex = Lib.Utils.Clamp(oldIndex, 0, serverList.Count);
            cboxServList.SelectedIndex = servIndex;
            UpdateLink();
            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxServList);
        }

        void UpdateLink()
        {
            var key = cboxServList.Text;
            var config = string.Empty;
            if (serverList.ContainsKey(key))
            {
                config = serverList[key];
            }

            if (string.IsNullOrEmpty(config))
            {
                tboxLink.Text = string.Empty;
                return;
            }

            string link = linkType == 0 ?
                link = slinkMgr.EncodeVmessLink(config) :
                link = slinkMgr.EncodeV2cfgLink(config);

            tboxLink.Text = link ?? string.Empty;
        }

        void ShowQRCode()
        {
            picQRCode.InitialImage = null;

            var link = tboxLink.Text;

            if (string.IsNullOrEmpty(link))
            {
                return;
            }

            Tuple<Bitmap, Lib.QRCode.QRCode.WriteErrors> pair =
                Lib.QRCode.QRCode.GenQRCode(
                    link, linkType == 0 ? 180 : 320);

            switch (pair.Item2)
            {
                case Lib.QRCode.QRCode.WriteErrors.Success:
                    picQRCode.Image = pair.Item1;
                    break;
                case Lib.QRCode.QRCode.WriteErrors.DataEmpty:
                    picQRCode.Image = null;
                    MessageBox.Show(I18N.EmptyLink);
                    break;
                case Lib.QRCode.QRCode.WriteErrors.DataTooBig:
                    picQRCode.Image = null;
                    MessageBox.Show(I18N.DataTooBig);
                    break;
            }
        }
        #endregion

        #region UI event handler
        private void cboxLinkType_SelectedIndexChanged(object sender, EventArgs e)
        {
            linkType = cboxLinkType.SelectedIndex;
            UpdateLink();
        }

        private void btnSavePic_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = StrConst.ExtPng,
                FilterIndex = 1,
                RestoreDirectory = true,
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    picQRCode.Image.Save(myStream, System.Drawing.Imaging.ImageFormat.Png);
                    myStream.Close();
                }
            }
        }

        private void tboxLink_TextChanged(object sender, EventArgs e)
        {
            ShowQRCode();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Lib.Utils.CopyToClipboardAndPrompt(tboxLink.Text);
        }

        private void rbtnIsCenterImage_CheckedChanged(object sender, EventArgs e)
        {
            SetPicZoomMode();
        }

        private void cboxServList_SelectedIndexChanged(object sender, EventArgs e)
        {
            servIndex = cboxServList.SelectedIndex;
            UpdateLink();
        }
        #endregion
    }
}
