namespace err403.SolutionManagment.Forms
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabAbout = new System.Windows.Forms.TabPage();
            this.tabImprovements = new System.Windows.Forms.TabPage();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblOriginalDev = new System.Windows.Forms.Label();
            this.lnkOriginalRepo = new System.Windows.Forms.LinkLabel();
            this.lblForkBy = new System.Windows.Forms.Label();
            this.lnkForkRepo = new System.Windows.Forms.LinkLabel();
            this.lblThanks = new System.Windows.Forms.Label();
            this.lblDevelopedBy = new System.Windows.Forms.Label();
            this.wbChangelog = new System.Windows.Forms.WebBrowser();
            this.btnClose = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.tabControl.SuspendLayout();
            this.tabAbout.SuspendLayout();
            this.tabImprovements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabAbout);
            this.tabControl.Controls.Add(this.tabImprovements);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(696, 390);
            this.tabControl.TabIndex = 0;
            // 
            // tabAbout
            // 
            this.tabAbout.Controls.Add(this.pictureBox);
            this.tabAbout.Controls.Add(this.lblTitle);
            this.tabAbout.Controls.Add(this.lblVersion);
            this.tabAbout.Controls.Add(this.lblDescription);
            this.tabAbout.Controls.Add(this.lblThanks);
            this.tabAbout.Controls.Add(this.lblDevelopedBy);
            this.tabAbout.Controls.Add(this.lblOriginalDev);
            this.tabAbout.Controls.Add(this.lnkOriginalRepo);
            this.tabAbout.Controls.Add(this.lblForkBy);
            this.tabAbout.Controls.Add(this.lnkForkRepo);
            this.tabAbout.Location = new System.Drawing.Point(4, 25);
            this.tabAbout.Name = "tabAbout";
            this.tabAbout.Padding = new System.Windows.Forms.Padding(12);
            this.tabAbout.Size = new System.Drawing.Size(688, 361);
            this.tabAbout.TabIndex = 0;
            this.tabAbout.Text = "About";
            this.tabAbout.UseVisualStyleBackColor = true;
            // 
            // pictureBox
            // 
            this.pictureBox.Image = global::err403.SolutionManagment.Properties.Resources.icon;
            this.pictureBox.Location = new System.Drawing.Point(15, 15);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(64, 64);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(90, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(270, 36);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Solution Management";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblVersion.ForeColor = System.Drawing.Color.Gray;
            this.lblVersion.Location = new System.Drawing.Point(92, 52);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(130, 23);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "Version 1.2026.4.10";
            // 
            // lblDescription
            // 
            this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblDescription.Location = new System.Drawing.Point(15, 95);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(658, 46);
            this.lblDescription.TabIndex = 3;
            this.lblDescription.Text = "Transfer solutions and manage environment variables across Dataverse environments" +
                " with ease.";
            // 
            // lblThanks
            // 
            this.lblThanks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblThanks.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblThanks.Location = new System.Drawing.Point(15, 150);
            this.lblThanks.Name = "lblThanks";
            this.lblThanks.Size = new System.Drawing.Size(658, 50);
            this.lblThanks.TabIndex = 4;
            this.lblThanks.Text = "Special thanks to Tanguy Touzard (DamSim / MscrmTools) for creating the original " +
                "Solution Transfer Tool for XrmToolBox. This fork builds on that excellent foundation.";
            // 
            // lblDevelopedBy
            // 
            this.lblDevelopedBy.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDevelopedBy.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic);
            this.lblDevelopedBy.ForeColor = System.Drawing.Color.Gray;
            this.lblDevelopedBy.Location = new System.Drawing.Point(15, 205);
            this.lblDevelopedBy.Name = "lblDevelopedBy";
            this.lblDevelopedBy.Size = new System.Drawing.Size(658, 20);
            this.lblDevelopedBy.TabIndex = 20;
            this.lblDevelopedBy.Text = "Coded by Copilot (Claude Opus 4.6) \u2014 Gareth Cheyne mass-approved the PRs. Mostly.";
            // 
            // lblOriginalDev
            // 
            this.lblOriginalDev.AutoSize = true;
            this.lblOriginalDev.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblOriginalDev.Location = new System.Drawing.Point(15, 235);
            this.lblOriginalDev.Name = "lblOriginalDev";
            this.lblOriginalDev.Size = new System.Drawing.Size(131, 23);
            this.lblOriginalDev.TabIndex = 5;
            this.lblOriginalDev.Text = "Original Project:";
            // 
            // lnkOriginalRepo
            // 
            this.lnkOriginalRepo.AutoSize = true;
            this.lnkOriginalRepo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lnkOriginalRepo.Location = new System.Drawing.Point(150, 235);
            this.lnkOriginalRepo.Name = "lnkOriginalRepo";
            this.lnkOriginalRepo.Size = new System.Drawing.Size(281, 23);
            this.lnkOriginalRepo.TabIndex = 6;
            this.lnkOriginalRepo.TabStop = true;
            this.lnkOriginalRepo.Text = "MscrmTools/DamSim.SolutionTransferTool";
            this.lnkOriginalRepo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkOriginalRepo_LinkClicked);
            // 
            // lblForkBy
            // 
            this.lblForkBy.AutoSize = true;
            this.lblForkBy.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblForkBy.Location = new System.Drawing.Point(15, 270);
            this.lblForkBy.Name = "lblForkBy";
            this.lblForkBy.Size = new System.Drawing.Size(108, 23);
            this.lblForkBy.TabIndex = 7;
            this.lblForkBy.Text = "Forked By:";
            // 
            // lnkForkRepo
            // 
            this.lnkForkRepo.AutoSize = true;
            this.lnkForkRepo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lnkForkRepo.Location = new System.Drawing.Point(150, 270);
            this.lnkForkRepo.Name = "lnkForkRepo";
            this.lnkForkRepo.Size = new System.Drawing.Size(258, 23);
            this.lnkForkRepo.TabIndex = 8;
            this.lnkForkRepo.TabStop = true;
            this.lnkForkRepo.Text = "garethcheyne/SolutionTransferTool";
            this.lnkForkRepo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkForkRepo_LinkClicked);
            // 
            // tabImprovements
            // 
            this.tabImprovements.Controls.Add(this.wbChangelog);
            this.tabImprovements.Location = new System.Drawing.Point(4, 25);
            this.tabImprovements.Name = "tabImprovements";
            this.tabImprovements.Padding = new System.Windows.Forms.Padding(0);
            this.tabImprovements.Size = new System.Drawing.Size(688, 361);
            this.tabImprovements.TabIndex = 1;
            this.tabImprovements.Text = "Changelog";
            this.tabImprovements.UseVisualStyleBackColor = true;
            // 
            // wbChangelog
            // 
            this.wbChangelog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbChangelog.Location = new System.Drawing.Point(0, 0);
            this.wbChangelog.Name = "wbChangelog";
            this.wbChangelog.Size = new System.Drawing.Size(688, 361);
            this.wbChangelog.TabIndex = 0;
            this.wbChangelog.IsWebBrowserContextMenuEnabled = false;
            this.wbChangelog.AllowNavigation = false;
            this.wbChangelog.ScriptErrorsSuppressed = true;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnClose.Location = new System.Drawing.Point(608, 414);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 32);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // AboutForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(720, 458);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Solution Management";
            this.tabControl.ResumeLayout(false);
            this.tabAbout.ResumeLayout(false);
            this.tabAbout.PerformLayout();
            this.tabImprovements.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabAbout;
        private System.Windows.Forms.TabPage tabImprovements;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblOriginalDev;
        private System.Windows.Forms.LinkLabel lnkOriginalRepo;
        private System.Windows.Forms.Label lblForkBy;
        private System.Windows.Forms.LinkLabel lnkForkRepo;
        private System.Windows.Forms.Label lblThanks;
        private System.Windows.Forms.Label lblDevelopedBy;
        private System.Windows.Forms.WebBrowser wbChangelog;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.PictureBox pictureBox;
    }
}
