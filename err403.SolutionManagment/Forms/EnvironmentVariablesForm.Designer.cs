namespace err403.SolutionManagment.Forms
{
    partial class EnvironmentVariablesForm
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
            this.chkShowDefault = new System.Windows.Forms.CheckBox();
            this.chkShowSchema = new System.Windows.Forms.CheckBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lvEnvVars = new System.Windows.Forms.ListView();
            this.colDisplayName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSchemaName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDefaultValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colCurrentValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.pnlToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.Controls.Add(this.chkShowDefault);
            this.pnlToolbar.Controls.Add(this.chkShowSchema);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 0);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.pnlToolbar.Size = new System.Drawing.Size(1074, 30);
            this.pnlToolbar.TabIndex = 1;
            // 
            // chkShowSchema
            // 
            this.chkShowSchema.AutoSize = true;
            this.chkShowSchema.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkShowSchema.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.chkShowSchema.Location = new System.Drawing.Point(4, 2);
            this.chkShowSchema.Name = "chkShowSchema";
            this.chkShowSchema.Size = new System.Drawing.Size(130, 26);
            this.chkShowSchema.TabIndex = 0;
            this.chkShowSchema.Text = "Show Schema Name";
            this.chkShowSchema.UseVisualStyleBackColor = true;
            this.chkShowSchema.CheckedChanged += new System.EventHandler(this.chkShowSchema_CheckedChanged);
            // 
            // chkShowDefault
            // 
            this.chkShowDefault.AutoSize = true;
            this.chkShowDefault.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkShowDefault.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.chkShowDefault.Location = new System.Drawing.Point(134, 2);
            this.chkShowDefault.Margin = new System.Windows.Forms.Padding(10, 3, 3, 3);
            this.chkShowDefault.Name = "chkShowDefault";
            this.chkShowDefault.Size = new System.Drawing.Size(135, 26);
            this.chkShowDefault.TabIndex = 1;
            this.chkShowDefault.Text = "Show Default Value";
            this.chkShowDefault.UseVisualStyleBackColor = true;
            this.chkShowDefault.CheckedChanged += new System.EventHandler(this.chkShowDefault_CheckedChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Location = new System.Drawing.Point(0, 30);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(1074, 29);
            this.txtSearch.TabIndex = 3;
            this.txtSearch.Text = "Search...";
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lvEnvVars
            // 
            this.lvEnvVars.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDisplayName,
            this.colSchemaName,
            this.colType,
            this.colDefaultValue,
            this.colCurrentValue});
            this.lvEnvVars.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvEnvVars.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lvEnvVars.FullRowSelect = true;
            this.lvEnvVars.GridLines = true;
            this.lvEnvVars.HideSelection = false;
            this.lvEnvVars.Location = new System.Drawing.Point(0, 90);
            this.lvEnvVars.Margin = new System.Windows.Forms.Padding(4);
            this.lvEnvVars.Name = "lvEnvVars";
            this.lvEnvVars.Size = new System.Drawing.Size(1074, 464);
            this.lvEnvVars.ShowItemToolTips = false;
            this.lvEnvVars.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvEnvVars.TabIndex = 2;
            this.lvEnvVars.UseCompatibleStateImageBehavior = false;
            this.lvEnvVars.View = System.Windows.Forms.View.Details;
            this.lvEnvVars.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvEnvVars_ColumnClick);
            this.lvEnvVars.DoubleClick += new System.EventHandler(this.lvEnvVars_DoubleClick);
            this.lvEnvVars.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lvEnvVars_MouseMove);
            // 
            // colDisplayName
            // 
            this.colDisplayName.Text = "Display Name";
            this.colDisplayName.Width = 250;
            // 
            // colSchemaName
            // 
            this.colSchemaName.Text = "Schema Name";
            this.colSchemaName.Width = 0;
            // 
            // colType
            // 
            this.colType.Text = "Type";
            this.colType.Width = 80;
            // 
            // colDefaultValue
            // 
            this.colDefaultValue.Text = "Default Value";
            this.colDefaultValue.Width = 0;
            // 
            // colCurrentValue
            // 
            this.colCurrentValue.Text = "Current Value";
            this.colCurrentValue.Width = 250;
            // 
            // EnvironmentVariablesForm
            // 
            this.AllowEndUserDocking = false;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 554);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.lvEnvVars);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.pnlToolbar);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "EnvironmentVariablesForm";
            this.TabText = "Environment Variables";
            this.Text = "Environment Variables";
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.CheckBox chkShowSchema;
        private System.Windows.Forms.CheckBox chkShowDefault;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListView lvEnvVars;
        private System.Windows.Forms.ColumnHeader colDisplayName;
        private System.Windows.Forms.ColumnHeader colSchemaName;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colDefaultValue;
        private System.Windows.Forms.ColumnHeader colCurrentValue;
    }
}
