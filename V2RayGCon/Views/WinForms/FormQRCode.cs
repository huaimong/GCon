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
        List<string> serverList;

        FormQRCode()
        {
            servers = Service.Servers.Instance;
            slinkMgr = Service.ShareLinkMgr.Instance;

            servIndex = -1;
            linkType = 0;

            InitializeComponent();

            VgcApis.Libs.UI.AutoSetFormIcon(this);
            this.Show();
        }

        private void FormQRCode_Shown(object sender, EventArgs e)
        {
            ClearServerList();
            RefreshServerList();
            cboxLinkType.SelectedIndex = linkType;
            picQRCode.InitialImage = null;

            this.FormClosed += (s, a) =>
            {
                servers.OnRequireMenuUpdate -= OnSettingChangeHandler;
            };

            servers.OnRequireMenuUpdate += OnSettingChangeHandler;
            SetPicZoomMode();
            if (cboxServList.Items.Count > 0)
            {
                cboxServList.SelectedIndex = 0;
            }
        }

        #region private methods
        void ClearServerList()
        {
            serverList = new List<string>();
        }

        void SetPicZoomMode()
        {
            picQRCode.SizeMode = rbtnIsCenterImage.Checked ?
                PictureBoxSizeMode.CenterImage :
                PictureBoxSizeMode.Zoom;
        }

        void OnSettingChangeHandler(object sender, EventArgs args)
        {
            try
            {
                VgcApis.Libs.UI.RunInUiThread(cboxServList, () =>
                {
                    RefreshServerList();
                });
            }
            catch { }
        }

        void RefreshServerList(int index = -1)
        {
            cboxServList.Items.Clear();

            var allServers = servers.GetAllServersOrderByIndex();
            ClearServerList();
            foreach (var serv in allServers)
            {
                var summary = serv.GetCoreStates().GetTitle();
                var config = serv.GetConfiger().GetConfig();
                cboxServList.Items.Add(summary);
                this.serverList.Add(config);
            }

            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxServList);

            servIndex = -2;
        }

        void UpdateTboxLink()
        {
            var config = string.Empty;

            if (servIndex >= 0
                && serverList != null
                && servIndex < serverList.Count)
            {
                config = serverList[servIndex];
            }


            if (string.IsNullOrEmpty(config))
            {
                tboxLink.Text = string.Empty;
                return;
            }

            string link = linkType == 0 ?
                link = slinkMgr.EncodeVmessLink(config) :
                link = slinkMgr.EncodeVeeLink(config);

            tboxLink.Text = link ?? string.Empty;
        }

        void SetQRCodeImage(Image img)
        {
            var oldImage = picQRCode.Image;

            picQRCode.Image = img;

            if (oldImage != img)
            {
                oldImage?.Dispose();
            }
        }

        void ShowQRCode()
        {
            var link = tboxLink.Text;

            if (string.IsNullOrEmpty(link))
            {
                SetQRCodeImage(null);
                return;
            }

            Tuple<Bitmap, Lib.QRCode.QRCode.WriteErrors> pair =
                Lib.QRCode.QRCode.GenQRCode(link, 320);

            switch (pair.Item2)
            {
                case Lib.QRCode.QRCode.WriteErrors.Success:
                    SetQRCodeImage( pair.Item1);
                    break;
                case Lib.QRCode.QRCode.WriteErrors.DataEmpty:
                    SetQRCodeImage(null);
                    MessageBox.Show(I18N.EmptyLink);
                    break;
                case Lib.QRCode.QRCode.WriteErrors.DataTooBig:
                    SetQRCodeImage(null);
                    MessageBox.Show(I18N.DataTooBig);
                    break;
            }
        }
        #endregion

        #region UI event handler
        private void cboxLinkType_SelectedIndexChanged(object sender, EventArgs e)
        {
            linkType = cboxLinkType.SelectedIndex;
            UpdateTboxLink();
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
            var newIndex = cboxServList.SelectedIndex;
            if (servIndex == newIndex)
            {
                return;
            }
            servIndex = newIndex;
            UpdateTboxLink();
        }
        #endregion
    }
}
