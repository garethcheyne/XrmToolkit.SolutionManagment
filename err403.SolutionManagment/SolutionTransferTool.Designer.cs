using System.Windows.Forms;

namespace err403.SolutionManagment
{
    partial class SolutionTransferTool
    {
        /// <summary> 
        /// Required variable for the designer.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SolutionTransferTool));
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsbLoadSolutions = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tssbTransfer = new System.Windows.Forms.ToolStripSplitButton();
            this.tsmiTransferWithOneTimeSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.tsbCancel = new System.Windows.Forms.ToolStripButton();
            this.tsbDownload = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSwitchOrgs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbFindMissingDependencies = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExportSolutions = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbImportFromFile = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbRemoveFromTargets = new System.Windows.Forms.ToolStripButton();
            this.tsbEnvSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.tsbRefreshEnvVars = new System.Windows.Forms.ToolStripButton();
            this.tsbTransferEnvVars = new System.Windows.Forms.ToolStripButton();
            this.tsbFlowSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.tsbRefreshFlows = new System.Windows.Forms.ToolStripButton();
            this.tsbActivateFlows = new System.Windows.Forms.ToolStripButton();
            this.tsbDeactivateFlows = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorAbout = new System.Windows.Forms.ToolStripSeparator();
            this.tsbAbout = new System.Windows.Forms.ToolStripButton();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.dpMain = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.scOrganizations = new System.Windows.Forms.SplitContainer();
            this.gbSource = new System.Windows.Forms.GroupBox();
            this.lblSource = new System.Windows.Forms.Label();
            this.gbTargetOrgs = new System.Windows.Forms.GroupBox();
            this.btnAddTarget = new System.Windows.Forms.Button();
            this.tsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scOrganizations)).BeginInit();
            this.scOrganizations.Panel1.SuspendLayout();
            this.scOrganizations.Panel2.SuspendLayout();
            this.scOrganizations.SuspendLayout();
            this.gbSource.SuspendLayout();
            this.gbTargetOrgs.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsMain
            // 
            this.tsMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbLoadSolutions,
            this.toolStripSeparator1,
            this.tssbTransfer,
            this.tsbCancel,
            this.tsbImportFromFile,
            this.toolStripSeparator2,
            this.tsbDownload,
            this.tsbExportSolutions,
            this.toolStripSeparator3,
            this.tsbRemoveFromTargets,
            this.toolStripSeparator4,
            this.tsbSwitchOrgs,
            this.tsbFindMissingDependencies,
            this.tsbEnvSeparator,
            this.tsbRefreshEnvVars,
            this.tsbTransferEnvVars,
            this.tsbFlowSeparator,
            this.tsbRefreshFlows,
            this.tsbActivateFlows,
            this.tsbDeactivateFlows,
            this.toolStripSeparatorAbout,
            this.tsbAbout});
            this.tsMain.Location = new System.Drawing.Point(0, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(1372, 39);
            this.tsMain.TabIndex = 0;
            this.tsMain.Text = "tsMain";
            // 
            // tsbLoadSolutions
            // 
            this.tsbLoadSolutions.Image = global::err403.SolutionManagment.Properties.Resources.Solutions32;
            this.tsbLoadSolutions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbLoadSolutions.Name = "tsbLoadSolutions";
            this.tsbLoadSolutions.Size = new System.Drawing.Size(143, 36);
            this.tsbLoadSolutions.Text = "Load Solutions";
            this.tsbLoadSolutions.Click += new System.EventHandler(this.TsbLoadSolutionsClick);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 39);
            // 
            // tssbTransfer
            // 
            this.tssbTransfer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiTransferWithOneTimeSettings});
            this.tssbTransfer.Image = global::err403.SolutionManagment.Properties.Resources.Startup32;
            this.tssbTransfer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tssbTransfer.Name = "tssbTransfer";
            this.tssbTransfer.Size = new System.Drawing.Size(169, 36);
            this.tssbTransfer.Text = "Transfer solution";
            this.tssbTransfer.ButtonClick += new System.EventHandler(this.TsbTransfertSolutionClick);
            this.tssbTransfer.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tssbTransfer_DropDownItemClicked);
            // 
            // tsmiTransferWithOneTimeSettings
            // 
            this.tsmiTransferWithOneTimeSettings.Image = global::err403.SolutionManagment.Properties.Resources.Connect32;
            this.tsmiTransferWithOneTimeSettings.Name = "tsmiTransferWithOneTimeSettings";
            this.tsmiTransferWithOneTimeSettings.Size = new System.Drawing.Size(294, 26);
            this.tsmiTransferWithOneTimeSettings.Text = "Transfer with one time settings";
            // 
            // tsbCancel
            // 
            this.tsbCancel.Image = global::err403.SolutionManagment.Properties.Resources.Error32;
            this.tsbCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbCancel.Name = "tsbCancel";
            this.tsbCancel.Size = new System.Drawing.Size(89, 36);
            this.tsbCancel.Text = "Cancel";
            this.tsbCancel.Visible = false;
            this.tsbCancel.Click += new System.EventHandler(this.tsbCancel_Click);
            // 
            // tsbDownload
            // 
            this.tsbDownload.Image = global::err403.SolutionManagment.Properties.Resources.download1;
            this.tsbDownload.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbDownload.Name = "tsbDownload";
            this.tsbDownload.Size = new System.Drawing.Size(171, 36);
            this.tsbDownload.Text = "Download solution";
            this.tsbDownload.Click += new System.EventHandler(this.tsbDownload_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbSwitchOrgs
            // 
            this.tsbSwitchOrgs.Image = global::err403.SolutionManagment.Properties.Resources.arrow_switch;
            this.tsbSwitchOrgs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSwitchOrgs.Name = "tsbSwitchOrgs";
            this.tsbSwitchOrgs.Size = new System.Drawing.Size(181, 36);
            this.tsbSwitchOrgs.Text = "Switch environments";
            this.tsbSwitchOrgs.ToolTipText = "Switch source and target environments";
            this.tsbSwitchOrgs.Click += new System.EventHandler(this.tsbSwitchOrgs_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbFindMissingDependencies
            // 
            this.tsbFindMissingDependencies.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.tsbFindMissingDependencies.Enabled = false;
            this.tsbFindMissingDependencies.Image = global::err403.SolutionManagment.Properties.Resources.Connect32;
            this.tsbFindMissingDependencies.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFindMissingDependencies.Name = "tsbFindMissingDependencies";
            this.tsbFindMissingDependencies.Size = new System.Drawing.Size(193, 36);
            this.tsbFindMissingDependencies.Text = "Find Missing Dependencies";
            this.tsbFindMissingDependencies.ToolTipText = "Use this button to detect what component were missing for the previous failed sol" +
    "ution import";
            this.tsbFindMissingDependencies.Click += new System.EventHandler(this.tsbFindMissingDependencies_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbExportSolutions
            // 
            this.tsbExportSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            this.tsbExportSolutions.Enabled = false;
            this.tsbExportSolutions.Image = global::err403.SolutionManagment.Properties.Resources.download;
            this.tsbExportSolutions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExportSolutions.Name = "tsbExportSolutions";
            this.tsbExportSolutions.Size = new System.Drawing.Size(209, 36);
            this.tsbExportSolutions.Text = "Download exported solutions";
            this.tsbExportSolutions.Click += new System.EventHandler(this.tsbExportSolutions_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbImportFromFile
            // 
            this.tsbImportFromFile.Image = global::err403.SolutionManagment.Properties.Resources.inbox_download;
            this.tsbImportFromFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImportFromFile.Name = "tsbImportFromFile";
            this.tsbImportFromFile.Size = new System.Drawing.Size(159, 36);
            this.tsbImportFromFile.Text = "Import from file";
            this.tsbImportFromFile.Click += new System.EventHandler(this.tsbImportFromFile_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbRemoveFromTargets
            // 
            this.tsbRemoveFromTargets.Image = global::err403.SolutionManagment.Properties.Resources.icons8_cancel;
            this.tsbRemoveFromTargets.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRemoveFromTargets.Name = "tsbRemoveFromTargets";
            this.tsbRemoveFromTargets.Size = new System.Drawing.Size(195, 36);
            this.tsbRemoveFromTargets.Text = "Remove from targets";
            this.tsbRemoveFromTargets.Click += new System.EventHandler(this.tsbRemoveFromTargets_Click);
            // 
            // tsbEnvSeparator
            // 
            this.tsbEnvSeparator.Name = "tsbEnvSeparator";
            this.tsbEnvSeparator.Size = new System.Drawing.Size(6, 39);
            this.tsbEnvSeparator.Visible = false;
            // 
            // tsbRefreshEnvVars
            // 
            this.tsbRefreshEnvVars.Image = global::err403.SolutionManagment.Properties.Resources.Solutions32;
            this.tsbRefreshEnvVars.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefreshEnvVars.Name = "tsbRefreshEnvVars";
            this.tsbRefreshEnvVars.Size = new System.Drawing.Size(143, 36);
            this.tsbRefreshEnvVars.Text = "Refresh";
            this.tsbRefreshEnvVars.Visible = false;
            this.tsbRefreshEnvVars.Click += new System.EventHandler(this.tsbRefreshEnvVars_Click);
            // 
            // tsbTransferEnvVars
            // 
            this.tsbTransferEnvVars.Image = global::err403.SolutionManagment.Properties.Resources.Startup32;
            this.tsbTransferEnvVars.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbTransferEnvVars.Name = "tsbTransferEnvVars";
            this.tsbTransferEnvVars.Size = new System.Drawing.Size(169, 36);
            this.tsbTransferEnvVars.Text = "Transfer Selected";
            this.tsbTransferEnvVars.Visible = false;
            this.tsbTransferEnvVars.Click += new System.EventHandler(this.tsbTransferEnvVars_Click);
            // 
            // tsbFlowSeparator
            // 
            this.tsbFlowSeparator.Name = "tsbFlowSeparator";
            this.tsbFlowSeparator.Size = new System.Drawing.Size(6, 39);
            this.tsbFlowSeparator.Visible = false;
            // 
            // tsbRefreshFlows
            // 
            this.tsbRefreshFlows.Image = global::err403.SolutionManagment.Properties.Resources.Solutions32;
            this.tsbRefreshFlows.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefreshFlows.Name = "tsbRefreshFlows";
            this.tsbRefreshFlows.Size = new System.Drawing.Size(143, 36);
            this.tsbRefreshFlows.Text = "Refresh";
            this.tsbRefreshFlows.Visible = false;
            this.tsbRefreshFlows.Click += new System.EventHandler(this.tsbRefreshFlows_Click);
            // 
            // tsbActivateFlows
            // 
            this.tsbActivateFlows.Image = global::err403.SolutionManagment.Properties.Resources.Startup32;
            this.tsbActivateFlows.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbActivateFlows.Name = "tsbActivateFlows";
            this.tsbActivateFlows.Size = new System.Drawing.Size(169, 36);
            this.tsbActivateFlows.Text = "Activate Selected";
            this.tsbActivateFlows.Visible = false;
            this.tsbActivateFlows.Click += new System.EventHandler(this.tsbActivateFlows_Click);
            // 
            // tsbDeactivateFlows
            // 
            this.tsbDeactivateFlows.Image = global::err403.SolutionManagment.Properties.Resources.icons8_cancel;
            this.tsbDeactivateFlows.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbDeactivateFlows.Name = "tsbDeactivateFlows";
            this.tsbDeactivateFlows.Size = new System.Drawing.Size(185, 36);
            this.tsbDeactivateFlows.Text = "Deactivate Selected";
            this.tsbDeactivateFlows.Visible = false;
            this.tsbDeactivateFlows.Click += new System.EventHandler(this.tsbDeactivateFlows_Click);
            // 
            // toolStripSeparatorAbout
            // 
            this.toolStripSeparatorAbout.Name = "toolStripSeparatorAbout";
            this.toolStripSeparatorAbout.Size = new System.Drawing.Size(6, 39);
            // 
            // tsbAbout
            // 
            this.tsbAbout.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbAbout.Image = global::err403.SolutionManagment.Properties.Resources.icon;
            this.tsbAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbAbout.Name = "tsbAbout";
            this.tsbAbout.Size = new System.Drawing.Size(78, 36);
            this.tsbAbout.Text = "About";
            this.tsbAbout.Click += new System.EventHandler(this.tsbAbout_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Icon.png");
            // 
            // scOrganizations
            // 
            this.scOrganizations.Dock = System.Windows.Forms.DockStyle.Top;
            this.scOrganizations.Location = new System.Drawing.Point(0, 39);
            this.scOrganizations.Margin = new System.Windows.Forms.Padding(2);
            this.scOrganizations.Name = "scOrganizations";
            // 
            // scOrganizations.Panel1
            // 
            this.scOrganizations.Panel1.Controls.Add(this.gbSource);
            // 
            // scOrganizations.Panel2
            // 
            this.scOrganizations.Panel2.Controls.Add(this.gbTargetOrgs);
            this.scOrganizations.Size = new System.Drawing.Size(1372, 60);
            this.scOrganizations.SplitterDistance = 358;
            this.scOrganizations.SplitterWidth = 3;
            this.scOrganizations.TabIndex = 2;
            // 
            // gbSource
            // 
            this.gbSource.Controls.Add(this.lblSource);
            this.gbSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbSource.Location = new System.Drawing.Point(0, 0);
            this.gbSource.Margin = new System.Windows.Forms.Padding(2);
            this.gbSource.Name = "gbSource";
            this.gbSource.Padding = new System.Windows.Forms.Padding(2);
            this.gbSource.Size = new System.Drawing.Size(358, 60);
            this.gbSource.TabIndex = 0;
            this.gbSource.TabStop = false;
            this.gbSource.Text = "Source";
            // 
            // lblSource
            // 
            this.lblSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSource.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblSource.ForeColor = System.Drawing.Color.Red;
            this.lblSource.Location = new System.Drawing.Point(2, 17);
            this.lblSource.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(354, 41);
            this.lblSource.TabIndex = 3;
            this.lblSource.Text = "Not selected yet (use XrmToolBox connect button)";
            this.lblSource.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbTargetOrgs
            // 
            this.gbTargetOrgs.Controls.Add(this.btnAddTarget);
            this.gbTargetOrgs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbTargetOrgs.Location = new System.Drawing.Point(0, 0);
            this.gbTargetOrgs.Margin = new System.Windows.Forms.Padding(2);
            this.gbTargetOrgs.Name = "gbTargetOrgs";
            this.gbTargetOrgs.Padding = new System.Windows.Forms.Padding(2);
            this.gbTargetOrgs.Size = new System.Drawing.Size(1011, 60);
            this.gbTargetOrgs.TabIndex = 0;
            this.gbTargetOrgs.TabStop = false;
            this.gbTargetOrgs.Text = "Target environment(s)";
            // 
            // btnAddTarget
            // 
            this.btnAddTarget.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAddTarget.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.btnAddTarget.Image = global::err403.SolutionManagment.Properties.Resources.plus;
            this.btnAddTarget.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAddTarget.Location = new System.Drawing.Point(2, 17);
            this.btnAddTarget.Margin = new System.Windows.Forms.Padding(4);
            this.btnAddTarget.Name = "btnAddTarget";
            this.btnAddTarget.Size = new System.Drawing.Size(69, 41);
            this.btnAddTarget.TabIndex = 3;
            this.btnAddTarget.Text = "Add";
            this.btnAddTarget.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnAddTarget.UseVisualStyleBackColor = true;
            this.btnAddTarget.Click += new System.EventHandler(this.btnAddTarget_Click);
            // 
            // dpMain
            // 
            this.dpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dpMain.DocumentStyle = WeifenLuo.WinFormsUI.Docking.DocumentStyle.DockingWindow;
            this.dpMain.Location = new System.Drawing.Point(0, 99);
            this.dpMain.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dpMain.Name = "dpMain";
            this.dpMain.Size = new System.Drawing.Size(1372, 737);
            this.dpMain.TabIndex = 1;
            // 
            // SolutionTransferTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dpMain);
            this.Controls.Add(this.scOrganizations);
            this.Controls.Add(this.tsMain);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "SolutionTransferTool";
            this.Size = new System.Drawing.Size(1372, 836);
            this.Resize += new System.EventHandler(this.SolutionTransferTool_Resize);
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            this.scOrganizations.Panel1.ResumeLayout(false);
            this.scOrganizations.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scOrganizations)).EndInit();
            this.scOrganizations.ResumeLayout(false);
            this.gbSource.ResumeLayout(false);
            this.gbTargetOrgs.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

 
        #endregion

        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripButton tsbLoadSolutions;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private ToolStripButton tsbFindMissingDependencies;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton tsbSwitchOrgs;
        private WeifenLuo.WinFormsUI.Docking.DockPanel dpMain;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsbExportSolutions;
        private ToolStripButton tsbDownload;
        private ToolStripButton tsbCancel;
        private ToolStripSplitButton tssbTransfer;
        private ToolStripMenuItem tsmiTransferWithOneTimeSettings;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripButton tsbImportFromFile;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripButton tsbRemoveFromTargets;
        private ToolStripSeparator tsbEnvSeparator;
        private ToolStripButton tsbRefreshEnvVars;
        private ToolStripButton tsbTransferEnvVars;
        private ToolStripSeparator tsbFlowSeparator;
        private ToolStripButton tsbRefreshFlows;
        private ToolStripButton tsbActivateFlows;
        private ToolStripButton tsbDeactivateFlows;
        private ToolStripSeparator toolStripSeparatorAbout;
        private ToolStripButton tsbAbout;
        internal System.Windows.Forms.SplitContainer scOrganizations;
        internal System.Windows.Forms.GroupBox gbSource;
        internal System.Windows.Forms.Label lblSource;
        internal System.Windows.Forms.GroupBox gbTargetOrgs;
        internal System.Windows.Forms.Button btnAddTarget;
    }
}
