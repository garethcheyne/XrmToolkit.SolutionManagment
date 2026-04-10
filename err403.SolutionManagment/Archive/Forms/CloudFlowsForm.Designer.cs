namespace err403.SolutionManagment.Forms
{
    partial class CloudFlowsForm
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
            this.pnlToolbar = new System.Windows.Forms.Panel();
            this.chkActiveOnly = new System.Windows.Forms.CheckBox();
            this.cmbSolutionFilter = new System.Windows.Forms.ComboBox();
            this.lblSolutionFilter = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lvFlows = new System.Windows.Forms.ListView();
            this.colFlowName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSolution = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colOwner = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colModifiedOn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.Controls.Add(this.cmbSolutionFilter);
            this.pnlToolbar.Controls.Add(this.lblSolutionFilter);
            this.pnlToolbar.Controls.Add(this.chkActiveOnly);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 0);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.pnlToolbar.Size = new System.Drawing.Size(1074, 30);
            this.pnlToolbar.TabIndex = 0;
            // 
            // chkActiveOnly
            // 
            this.chkActiveOnly.AutoSize = true;
            this.chkActiveOnly.Checked = true;
            this.chkActiveOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkActiveOnly.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkActiveOnly.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.chkActiveOnly.Location = new System.Drawing.Point(4, 2);
            this.chkActiveOnly.Name = "chkActiveOnly";
            this.chkActiveOnly.Size = new System.Drawing.Size(105, 26);
            this.chkActiveOnly.TabIndex = 0;
            this.chkActiveOnly.Text = "Active Only";
            this.chkActiveOnly.UseVisualStyleBackColor = true;
            this.chkActiveOnly.CheckedChanged += new System.EventHandler(this.chkActiveOnly_CheckedChanged);
            // 
            // lblSolutionFilter
            // 
            this.lblSolutionFilter.AutoSize = true;
            this.lblSolutionFilter.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblSolutionFilter.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblSolutionFilter.Location = new System.Drawing.Point(109, 2);
            this.lblSolutionFilter.Padding = new System.Windows.Forms.Padding(10, 5, 0, 0);
            this.lblSolutionFilter.Name = "lblSolutionFilter";
            this.lblSolutionFilter.Size = new System.Drawing.Size(78, 21);
            this.lblSolutionFilter.TabIndex = 1;
            this.lblSolutionFilter.Text = "Solution:";
            // 
            // cmbSolutionFilter
            // 
            this.cmbSolutionFilter.Dock = System.Windows.Forms.DockStyle.Left;
            this.cmbSolutionFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSolutionFilter.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.cmbSolutionFilter.Location = new System.Drawing.Point(187, 2);
            this.cmbSolutionFilter.Name = "cmbSolutionFilter";
            this.cmbSolutionFilter.Size = new System.Drawing.Size(220, 25);
            this.cmbSolutionFilter.TabIndex = 2;
            this.cmbSolutionFilter.SelectedIndexChanged += new System.EventHandler(this.cmbSolutionFilter_SelectedIndexChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Location = new System.Drawing.Point(0, 30);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(1074, 29);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.Text = "Search...";
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lvFlows
            // 
            this.lvFlows.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFlowName,
            this.colType,
            this.colStatus,
            this.colSolution,
            this.colOwner,
            this.colModifiedOn});
            this.lvFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFlows.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lvFlows.FullRowSelect = true;
            this.lvFlows.GridLines = true;
            this.lvFlows.HideSelection = false;
            this.lvFlows.Location = new System.Drawing.Point(0, 59);
            this.lvFlows.Margin = new System.Windows.Forms.Padding(4);
            this.lvFlows.Name = "lvFlows";
            this.lvFlows.Size = new System.Drawing.Size(1074, 495);
            this.lvFlows.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvFlows.TabIndex = 2;
            this.lvFlows.UseCompatibleStateImageBehavior = false;
            this.lvFlows.View = System.Windows.Forms.View.Details;
            this.lvFlows.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvFlows_ColumnClick);
            // 
            // colFlowName
            // 
            this.colFlowName.Text = "Flow Name";
            this.colFlowName.Width = 280;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = 90;
            // 
            // colStatus
            // 
            this.colStatus.Text = "Status";
            this.colStatus.Width = 80;
            // 
            // colSolution
            // 
            this.colSolution.Text = "Solution";
            this.colSolution.Width = 150;
            // 
            // colOwner
            // 
            this.colOwner.Text = "Owner";
            this.colOwner.Width = 130;
            // 
            // colModifiedOn
            // 
            this.colModifiedOn.Text = "Modified On";
            this.colModifiedOn.Width = 120;
            // 
            // CloudFlowsForm
            // 
            this.AllowEndUserDocking = false;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 554);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.lvFlows);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.pnlToolbar);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "CloudFlowsForm";
            this.TabText = "Cloud Flows";
            this.Text = "Cloud Flows";
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.CheckBox chkActiveOnly;
        private System.Windows.Forms.Label lblSolutionFilter;
        private System.Windows.Forms.ComboBox cmbSolutionFilter;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListView lvFlows;
        private System.Windows.Forms.ColumnHeader colFlowName;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colSolution;
        private System.Windows.Forms.ColumnHeader colOwner;
        private System.Windows.Forms.ColumnHeader colModifiedOn;
    }
}
