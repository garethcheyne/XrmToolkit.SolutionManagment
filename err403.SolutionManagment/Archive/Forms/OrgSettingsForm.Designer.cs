namespace err403.SolutionManagment.Forms
{
    partial class OrgSettingsForm
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
            this.chkDifferencesOnly = new System.Windows.Forms.CheckBox();
            this.cmbCategoryFilter = new System.Windows.Forms.ComboBox();
            this.lblCategoryFilter = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lvSettings = new System.Windows.Forms.ListView();
            this.colCategory = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSetting = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUniqueName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSourceValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.Controls.Add(this.cmbCategoryFilter);
            this.pnlToolbar.Controls.Add(this.lblCategoryFilter);
            this.pnlToolbar.Controls.Add(this.chkDifferencesOnly);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 0);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.pnlToolbar.Size = new System.Drawing.Size(1074, 30);
            this.pnlToolbar.TabIndex = 0;
            // 
            // chkDifferencesOnly
            // 
            this.chkDifferencesOnly.AutoSize = true;
            this.chkDifferencesOnly.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkDifferencesOnly.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.chkDifferencesOnly.Location = new System.Drawing.Point(4, 2);
            this.chkDifferencesOnly.Name = "chkDifferencesOnly";
            this.chkDifferencesOnly.Size = new System.Drawing.Size(130, 26);
            this.chkDifferencesOnly.TabIndex = 0;
            this.chkDifferencesOnly.Text = "Differences Only";
            this.chkDifferencesOnly.UseVisualStyleBackColor = true;
            this.chkDifferencesOnly.CheckedChanged += new System.EventHandler(this.chkDifferencesOnly_CheckedChanged);
            // 
            // lblCategoryFilter
            // 
            this.lblCategoryFilter.AutoSize = true;
            this.lblCategoryFilter.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblCategoryFilter.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblCategoryFilter.Location = new System.Drawing.Point(134, 2);
            this.lblCategoryFilter.Padding = new System.Windows.Forms.Padding(10, 5, 0, 0);
            this.lblCategoryFilter.Name = "lblCategoryFilter";
            this.lblCategoryFilter.Size = new System.Drawing.Size(78, 21);
            this.lblCategoryFilter.TabIndex = 1;
            this.lblCategoryFilter.Text = "Category:";
            // 
            // cmbCategoryFilter
            // 
            this.cmbCategoryFilter.Dock = System.Windows.Forms.DockStyle.Left;
            this.cmbCategoryFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategoryFilter.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.cmbCategoryFilter.Location = new System.Drawing.Point(212, 2);
            this.cmbCategoryFilter.Name = "cmbCategoryFilter";
            this.cmbCategoryFilter.Size = new System.Drawing.Size(180, 25);
            this.cmbCategoryFilter.TabIndex = 2;
            this.cmbCategoryFilter.SelectedIndexChanged += new System.EventHandler(this.cmbCategoryFilter_SelectedIndexChanged);
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
            // lvSettings
            // 
            this.lvSettings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colCategory,
            this.colSetting,
            this.colUniqueName,
            this.colSourceValue});
            this.lvSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSettings.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lvSettings.FullRowSelect = true;
            this.lvSettings.GridLines = true;
            this.lvSettings.HideSelection = false;
            this.lvSettings.Location = new System.Drawing.Point(0, 59);
            this.lvSettings.Margin = new System.Windows.Forms.Padding(4);
            this.lvSettings.Name = "lvSettings";
            this.lvSettings.Size = new System.Drawing.Size(1074, 495);
            this.lvSettings.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvSettings.TabIndex = 2;
            this.lvSettings.UseCompatibleStateImageBehavior = false;
            this.lvSettings.View = System.Windows.Forms.View.Details;
            this.lvSettings.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvSettings_ColumnClick);
            // 
            // colCategory
            // 
            this.colCategory.Text = "Category";
            this.colCategory.Width = 120;
            // 
            // colSetting
            // 
            this.colSetting.Text = "Display Name";
            this.colSetting.Width = 200;
            // 
            // colUniqueName
            // 
            this.colUniqueName.Text = "Unique Name";
            this.colUniqueName.Width = 0;
            // 
            // colSourceValue
            // 
            this.colSourceValue.Text = "Source Value";
            this.colSourceValue.Width = 150;
            // 
            // OrgSettingsForm
            // 
            this.AllowEndUserDocking = false;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 554);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.lvSettings);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.pnlToolbar);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "OrgSettingsForm";
            this.TabText = "Platform Settings";
            this.Text = "Platform Settings";
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.CheckBox chkDifferencesOnly;
        private System.Windows.Forms.Label lblCategoryFilter;
        private System.Windows.Forms.ComboBox cmbCategoryFilter;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListView lvSettings;
        private System.Windows.Forms.ColumnHeader colCategory;
        private System.Windows.Forms.ColumnHeader colSetting;
        private System.Windows.Forms.ColumnHeader colUniqueName;
        private System.Windows.Forms.ColumnHeader colSourceValue;
    }
}
