namespace V2RayGCon.Views.WinForms
{
    partial class FormOption
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormOption));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageImport = new System.Windows.Forms.TabPage();
            this.btnImportAdd = new System.Windows.Forms.Button();
            this.flyImportPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPageSubscribe = new System.Windows.Forms.TabPage();
            this.chkSubsIsUseProxy = new System.Windows.Forms.CheckBox();
            this.btnUpdateViaSubscription = new System.Windows.Forms.Button();
            this.btnAddSubsUrl = new System.Windows.Forms.Button();
            this.flySubsUrlContainer = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPageSetting = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkSetUpgradeUseProxy = new System.Windows.Forms.CheckBox();
            this.rbtnIdontCare = new System.Windows.Forms.RadioButton();
            this.rbtnSetUpgradeToVgcFull = new System.Windows.Forms.RadioButton();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.chkSetUseV4 = new System.Windows.Forms.CheckBox();
            this.chkSetSysPortable = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkSetServStatistics = new System.Windows.Forms.CheckBox();
            this.chkSetServAutotrack = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cboxSettingPageSize = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cboxSettingLanguage = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPagePlugins = new System.Windows.Forms.TabPage();
            this.flyPluginsItemsContainer = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBakBackup = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnOptionSave = new System.Windows.Forms.Button();
            this.btnBakRestore = new System.Windows.Forms.Button();
            this.btnOptionExit = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPageImport.SuspendLayout();
            this.tabPageSubscribe.SuspendLayout();
            this.tabPageSetting.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPagePlugins.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPageImport);
            this.tabControl1.Controls.Add(this.tabPageSubscribe);
            this.tabControl1.Controls.Add(this.tabPageSetting);
            this.tabControl1.Controls.Add(this.tabPagePlugins);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPageImport
            // 
            this.tabPageImport.Controls.Add(this.btnImportAdd);
            this.tabPageImport.Controls.Add(this.flyImportPanel);
            resources.ApplyResources(this.tabPageImport, "tabPageImport");
            this.tabPageImport.Name = "tabPageImport";
            this.tabPageImport.UseVisualStyleBackColor = true;
            // 
            // btnImportAdd
            // 
            resources.ApplyResources(this.btnImportAdd, "btnImportAdd");
            this.btnImportAdd.Name = "btnImportAdd";
            this.toolTip1.SetToolTip(this.btnImportAdd, resources.GetString("btnImportAdd.ToolTip"));
            this.btnImportAdd.UseVisualStyleBackColor = true;
            // 
            // flyImportPanel
            // 
            this.flyImportPanel.AllowDrop = true;
            resources.ApplyResources(this.flyImportPanel, "flyImportPanel");
            this.flyImportPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flyImportPanel.Name = "flyImportPanel";
            // 
            // tabPageSubscribe
            // 
            this.tabPageSubscribe.Controls.Add(this.chkSubsIsUseProxy);
            this.tabPageSubscribe.Controls.Add(this.btnUpdateViaSubscription);
            this.tabPageSubscribe.Controls.Add(this.btnAddSubsUrl);
            this.tabPageSubscribe.Controls.Add(this.flySubsUrlContainer);
            resources.ApplyResources(this.tabPageSubscribe, "tabPageSubscribe");
            this.tabPageSubscribe.Name = "tabPageSubscribe";
            this.tabPageSubscribe.UseVisualStyleBackColor = true;
            // 
            // chkSubsIsUseProxy
            // 
            resources.ApplyResources(this.chkSubsIsUseProxy, "chkSubsIsUseProxy");
            this.chkSubsIsUseProxy.Name = "chkSubsIsUseProxy";
            this.toolTip1.SetToolTip(this.chkSubsIsUseProxy, resources.GetString("chkSubsIsUseProxy.ToolTip"));
            this.chkSubsIsUseProxy.UseVisualStyleBackColor = true;
            // 
            // btnUpdateViaSubscription
            // 
            resources.ApplyResources(this.btnUpdateViaSubscription, "btnUpdateViaSubscription");
            this.btnUpdateViaSubscription.Name = "btnUpdateViaSubscription";
            this.toolTip1.SetToolTip(this.btnUpdateViaSubscription, resources.GetString("btnUpdateViaSubscription.ToolTip"));
            this.btnUpdateViaSubscription.UseVisualStyleBackColor = true;
            // 
            // btnAddSubsUrl
            // 
            resources.ApplyResources(this.btnAddSubsUrl, "btnAddSubsUrl");
            this.btnAddSubsUrl.Name = "btnAddSubsUrl";
            this.toolTip1.SetToolTip(this.btnAddSubsUrl, resources.GetString("btnAddSubsUrl.ToolTip"));
            this.btnAddSubsUrl.UseVisualStyleBackColor = true;
            // 
            // flySubsUrlContainer
            // 
            this.flySubsUrlContainer.AllowDrop = true;
            resources.ApplyResources(this.flySubsUrlContainer, "flySubsUrlContainer");
            this.flySubsUrlContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flySubsUrlContainer.Name = "flySubsUrlContainer";
            // 
            // tabPageSetting
            // 
            this.tabPageSetting.Controls.Add(this.groupBox2);
            this.tabPageSetting.Controls.Add(this.groupBox6);
            this.tabPageSetting.Controls.Add(this.groupBox5);
            this.tabPageSetting.Controls.Add(this.groupBox1);
            resources.ApplyResources(this.tabPageSetting, "tabPageSetting");
            this.tabPageSetting.Name = "tabPageSetting";
            this.tabPageSetting.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rbtnIdontCare);
            this.groupBox2.Controls.Add(this.rbtnSetUpgradeToVgcFull);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // chkSetUpgradeUseProxy
            // 
            resources.ApplyResources(this.chkSetUpgradeUseProxy, "chkSetUpgradeUseProxy");
            this.chkSetUpgradeUseProxy.Name = "chkSetUpgradeUseProxy";
            this.toolTip1.SetToolTip(this.chkSetUpgradeUseProxy, resources.GetString("chkSetUpgradeUseProxy.ToolTip"));
            this.chkSetUpgradeUseProxy.UseVisualStyleBackColor = true;
            // 
            // rbtnIdontCare
            // 
            resources.ApplyResources(this.rbtnIdontCare, "rbtnIdontCare");
            this.rbtnIdontCare.Checked = true;
            this.rbtnIdontCare.Name = "rbtnIdontCare";
            this.rbtnIdontCare.TabStop = true;
            this.toolTip1.SetToolTip(this.rbtnIdontCare, resources.GetString("rbtnIdontCare.ToolTip"));
            this.rbtnIdontCare.UseVisualStyleBackColor = true;
            // 
            // rbtnSetUpgradeToVgcFull
            // 
            resources.ApplyResources(this.rbtnSetUpgradeToVgcFull, "rbtnSetUpgradeToVgcFull");
            this.rbtnSetUpgradeToVgcFull.Name = "rbtnSetUpgradeToVgcFull";
            this.toolTip1.SetToolTip(this.rbtnSetUpgradeToVgcFull, resources.GetString("rbtnSetUpgradeToVgcFull.ToolTip"));
            this.rbtnSetUpgradeToVgcFull.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.chkSetUpgradeUseProxy);
            this.groupBox6.Controls.Add(this.chkSetUseV4);
            this.groupBox6.Controls.Add(this.chkSetSysPortable);
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // chkSetUseV4
            // 
            resources.ApplyResources(this.chkSetUseV4, "chkSetUseV4");
            this.chkSetUseV4.Name = "chkSetUseV4";
            this.toolTip1.SetToolTip(this.chkSetUseV4, resources.GetString("chkSetUseV4.ToolTip"));
            this.chkSetUseV4.UseVisualStyleBackColor = true;
            // 
            // chkSetSysPortable
            // 
            resources.ApplyResources(this.chkSetSysPortable, "chkSetSysPortable");
            this.chkSetSysPortable.Name = "chkSetSysPortable";
            this.toolTip1.SetToolTip(this.chkSetSysPortable, resources.GetString("chkSetSysPortable.ToolTip"));
            this.chkSetSysPortable.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.chkSetServStatistics);
            this.groupBox5.Controls.Add(this.chkSetServAutotrack);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // chkSetServStatistics
            // 
            resources.ApplyResources(this.chkSetServStatistics, "chkSetServStatistics");
            this.chkSetServStatistics.Name = "chkSetServStatistics";
            this.toolTip1.SetToolTip(this.chkSetServStatistics, resources.GetString("chkSetServStatistics.ToolTip"));
            this.chkSetServStatistics.UseVisualStyleBackColor = true;
            // 
            // chkSetServAutotrack
            // 
            resources.ApplyResources(this.chkSetServAutotrack, "chkSetServAutotrack");
            this.chkSetServAutotrack.Name = "chkSetServAutotrack";
            this.toolTip1.SetToolTip(this.chkSetServAutotrack, resources.GetString("chkSetServAutotrack.ToolTip"));
            this.chkSetServAutotrack.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.cboxSettingPageSize);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cboxSettingLanguage);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // cboxSettingPageSize
            // 
            this.cboxSettingPageSize.FormattingEnabled = true;
            this.cboxSettingPageSize.Items.AddRange(new object[] {
            resources.GetString("cboxSettingPageSize.Items"),
            resources.GetString("cboxSettingPageSize.Items1"),
            resources.GetString("cboxSettingPageSize.Items2"),
            resources.GetString("cboxSettingPageSize.Items3"),
            resources.GetString("cboxSettingPageSize.Items4")});
            resources.ApplyResources(this.cboxSettingPageSize, "cboxSettingPageSize");
            this.cboxSettingPageSize.Name = "cboxSettingPageSize";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // cboxSettingLanguage
            // 
            this.cboxSettingLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboxSettingLanguage.FormattingEnabled = true;
            this.cboxSettingLanguage.Items.AddRange(new object[] {
            resources.GetString("cboxSettingLanguage.Items"),
            resources.GetString("cboxSettingLanguage.Items1"),
            resources.GetString("cboxSettingLanguage.Items2")});
            resources.ApplyResources(this.cboxSettingLanguage, "cboxSettingLanguage");
            this.cboxSettingLanguage.Name = "cboxSettingLanguage";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tabPagePlugins
            // 
            this.tabPagePlugins.Controls.Add(this.flyPluginsItemsContainer);
            resources.ApplyResources(this.tabPagePlugins, "tabPagePlugins");
            this.tabPagePlugins.Name = "tabPagePlugins";
            this.tabPagePlugins.UseVisualStyleBackColor = true;
            // 
            // flyPluginsItemsContainer
            // 
            resources.ApplyResources(this.flyPluginsItemsContainer, "flyPluginsItemsContainer");
            this.flyPluginsItemsContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flyPluginsItemsContainer.Name = "flyPluginsItemsContainer";
            // 
            // btnBakBackup
            // 
            resources.ApplyResources(this.btnBakBackup, "btnBakBackup");
            this.btnBakBackup.Name = "btnBakBackup";
            this.toolTip1.SetToolTip(this.btnBakBackup, resources.GetString("btnBakBackup.ToolTip"));
            this.btnBakBackup.UseVisualStyleBackColor = true;
            this.btnBakBackup.Click += new System.EventHandler(this.btnBakBackup_Click);
            // 
            // btnOptionSave
            // 
            resources.ApplyResources(this.btnOptionSave, "btnOptionSave");
            this.btnOptionSave.Name = "btnOptionSave";
            this.toolTip1.SetToolTip(this.btnOptionSave, resources.GetString("btnOptionSave.ToolTip"));
            this.btnOptionSave.UseVisualStyleBackColor = true;
            this.btnOptionSave.Click += new System.EventHandler(this.btnOptionSave_Click);
            // 
            // btnBakRestore
            // 
            resources.ApplyResources(this.btnBakRestore, "btnBakRestore");
            this.btnBakRestore.Name = "btnBakRestore";
            this.toolTip1.SetToolTip(this.btnBakRestore, resources.GetString("btnBakRestore.ToolTip"));
            this.btnBakRestore.UseVisualStyleBackColor = true;
            this.btnBakRestore.Click += new System.EventHandler(this.btnBakRestore_Click);
            // 
            // btnOptionExit
            // 
            resources.ApplyResources(this.btnOptionExit, "btnOptionExit");
            this.btnOptionExit.Name = "btnOptionExit";
            this.btnOptionExit.UseVisualStyleBackColor = true;
            this.btnOptionExit.Click += new System.EventHandler(this.btnOptionExit_Click);
            // 
            // FormOption
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnBakRestore);
            this.Controls.Add(this.btnBakBackup);
            this.Controls.Add(this.btnOptionExit);
            this.Controls.Add(this.btnOptionSave);
            this.Controls.Add(this.tabControl1);
            this.Name = "FormOption";
            this.Shown += new System.EventHandler(this.FormOption_Shown);
            this.tabControl1.ResumeLayout(false);
            this.tabPageImport.ResumeLayout(false);
            this.tabPageSubscribe.ResumeLayout(false);
            this.tabPageSubscribe.PerformLayout();
            this.tabPageSetting.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPagePlugins.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSubscribe;
        private System.Windows.Forms.FlowLayoutPanel flySubsUrlContainer;
        private System.Windows.Forms.Button btnAddSubsUrl;
        private System.Windows.Forms.Button btnUpdateViaSubscription;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btnOptionSave;
        private System.Windows.Forms.Button btnOptionExit;
        private System.Windows.Forms.TabPage tabPageImport;
        private System.Windows.Forms.Button btnImportAdd;
        private System.Windows.Forms.FlowLayoutPanel flyImportPanel;
        private System.Windows.Forms.Button btnBakBackup;
        private System.Windows.Forms.Button btnBakRestore;
        private System.Windows.Forms.TabPage tabPageSetting;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cboxSettingLanguage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboxSettingPageSize;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox chkSetServAutotrack;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.CheckBox chkSetSysPortable;
        private System.Windows.Forms.CheckBox chkSetUseV4;
        private System.Windows.Forms.TabPage tabPagePlugins;
        private System.Windows.Forms.FlowLayoutPanel flyPluginsItemsContainer;
        private System.Windows.Forms.CheckBox chkSetServStatistics;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rbtnIdontCare;
        private System.Windows.Forms.RadioButton rbtnSetUpgradeToVgcFull;
        private System.Windows.Forms.CheckBox chkSetUpgradeUseProxy;
        private System.Windows.Forms.CheckBox chkSubsIsUseProxy;
    }
}
