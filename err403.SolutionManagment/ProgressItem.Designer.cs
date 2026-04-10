namespace err403.SolutionManagment
{
    partial class ProgressItem
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressItem));
            this.lblAction = new System.Windows.Forms.Label();
            this.lblDirection = new System.Windows.Forms.Label();
            this.ilProgress = new System.Windows.Forms.ImageList(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblPercentage = new System.Windows.Forms.Label();
            this.pbProgress = new System.Windows.Forms.PictureBox();
            this.pnlProgress = new System.Windows.Forms.Panel();
            this.llDownloadLog = new System.Windows.Forms.LinkLabel();
            this.lblProgress = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbProgress)).BeginInit();
            this.pnlProgress.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAction
            // 
            this.lblAction.AutoSize = true;
            this.lblAction.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAction.Location = new System.Drawing.Point(63, 6);
            this.lblAction.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(192, 28);
            this.lblAction.TabIndex = 1;
            this.lblAction.Tag = "Importing Solution {0}";
            this.lblAction.Text = "Importing Solution";
            // 
            // lblDirection
            // 
            this.lblDirection.AutoSize = true;
            this.lblDirection.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDirection.Location = new System.Drawing.Point(63, 35);
            this.lblDirection.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblDirection.Name = "lblDirection";
            this.lblDirection.Size = new System.Drawing.Size(278, 28);
            this.lblDirection.TabIndex = 2;
            this.lblDirection.Text = "To organization ITL PROD 365";
            // 
            // ilProgress
            // 
            this.ilProgress.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilProgress.ImageStream")));
            this.ilProgress.TransparentColor = System.Drawing.Color.Transparent;
            this.ilProgress.Images.SetKeyName(0, "hourglass.png");
            this.ilProgress.Images.SetKeyName(1, "play-rounded-button.png");
            this.ilProgress.Images.SetKeyName(2, "error.png");
            this.ilProgress.Images.SetKeyName(3, "checked.png");
            this.ilProgress.Images.SetKeyName(4, "Upgrade.png");
            this.ilProgress.Images.SetKeyName(5, "search.png");
            this.ilProgress.Images.SetKeyName(6, "crisis.png");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblPercentage);
            this.panel1.Controls.Add(this.pbProgress);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(60, 98);
            this.panel1.TabIndex = 6;
            // 
            // lblPercentage
            // 
            this.lblPercentage.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblPercentage.Font = new System.Drawing.Font("Segoe UI Light", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPercentage.Location = new System.Drawing.Point(0, 71);
            this.lblPercentage.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(60, 27);
            this.lblPercentage.TabIndex = 8;
            this.lblPercentage.Text = "0 %";
            this.lblPercentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPercentage.Visible = false;
            // 
            // pbProgress
            // 
            this.pbProgress.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pbProgress.Image = global::err403.SolutionManagment.Properties.Resources.progressbar;
            this.pbProgress.Location = new System.Drawing.Point(6, 14);
            this.pbProgress.Margin = new System.Windows.Forms.Padding(5);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(48, 49);
            this.pbProgress.TabIndex = 0;
            this.pbProgress.TabStop = false;
            // 
            // pnlProgress
            // 
            this.pnlProgress.Controls.Add(this.llDownloadLog);
            this.pnlProgress.Controls.Add(this.lblProgress);
            this.pnlProgress.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlProgress.Location = new System.Drawing.Point(60, 67);
            this.pnlProgress.Margin = new System.Windows.Forms.Padding(5);
            this.pnlProgress.Name = "pnlProgress";
            this.pnlProgress.Size = new System.Drawing.Size(597, 31);
            this.pnlProgress.TabIndex = 7;
            // 
            // llDownloadLog
            // 
            this.llDownloadLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.llDownloadLog.Location = new System.Drawing.Point(420, 7);
            this.llDownloadLog.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.llDownloadLog.Name = "llDownloadLog";
            this.llDownloadLog.Size = new System.Drawing.Size(177, 24);
            this.llDownloadLog.TabIndex = 8;
            this.llDownloadLog.TabStop = true;
            this.llDownloadLog.Text = "Download log file";
            this.llDownloadLog.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.llDownloadLog.Visible = false;
            this.llDownloadLog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llDownloadLog_LinkClicked);
            // 
            // lblProgress
            // 
            this.lblProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProgress.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblProgress.Location = new System.Drawing.Point(0, 0);
            this.lblProgress.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblProgress.Size = new System.Drawing.Size(597, 31);
            this.lblProgress.TabIndex = 4;
            this.lblProgress.Tag = "Started at {0}";
            this.lblProgress.Text = "Progress : 15% (Started at 04/10/2018 12:13:45)";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ProgressItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlProgress);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lblDirection);
            this.Controls.Add(this.lblAction);
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "ProgressItem";
            this.Size = new System.Drawing.Size(657, 98);
            this.Load += new System.EventHandler(this.ProgressItem_Load);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbProgress)).EndInit();
            this.pnlProgress.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.Label lblDirection;
        private System.Windows.Forms.ImageList ilProgress;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pbProgress;
        private System.Windows.Forms.Panel pnlProgress;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.LinkLabel llDownloadLog;
        private System.Windows.Forms.Label lblPercentage;
    }
}
