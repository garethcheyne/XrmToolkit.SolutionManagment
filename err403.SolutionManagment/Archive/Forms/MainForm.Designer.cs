namespace err403.SolutionManagment.Forms
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.grpSourceSolution = new System.Windows.Forms.GroupBox();
            this.lstSourceSolutions = new System.Windows.Forms.ListView();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.uniquename = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.friendlyname = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.version = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.installedon = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.publisher = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.description = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.grpSourceSolution.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // grpSourceSolution
            // 
            this.grpSourceSolution.Controls.Add(this.lstSourceSolutions);
            this.grpSourceSolution.Controls.Add(this.txtSearch);
            this.grpSourceSolution.Controls.Add(this.panel1);
            this.grpSourceSolution.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSourceSolution.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grpSourceSolution.Location = new System.Drawing.Point(0, 0);
            this.grpSourceSolution.Margin = new System.Windows.Forms.Padding(4);
            this.grpSourceSolution.Name = "grpSourceSolution";
            this.grpSourceSolution.Padding = new System.Windows.Forms.Padding(4);
            this.grpSourceSolution.Size = new System.Drawing.Size(1074, 554);
            this.grpSourceSolution.TabIndex = 9;
            this.grpSourceSolution.TabStop = false;
            this.grpSourceSolution.Text = "Solutions";
            // 
            // lstSourceSolutions
            // 
            this.lstSourceSolutions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.uniquename,
            this.friendlyname,
            this.version,
            this.installedon,
            this.publisher,
            this.description});
            this.lstSourceSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstSourceSolutions.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lstSourceSolutions.HideSelection = false;
            this.lstSourceSolutions.Location = new System.Drawing.Point(4, 52);
            this.lstSourceSolutions.Margin = new System.Windows.Forms.Padding(4);
            this.lstSourceSolutions.Name = "lstSourceSolutions";
            this.lstSourceSolutions.Size = new System.Drawing.Size(1066, 438);
            this.lstSourceSolutions.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lstSourceSolutions.TabIndex = 6;
            this.lstSourceSolutions.UseCompatibleStateImageBehavior = false;
            this.lstSourceSolutions.View = System.Windows.Forms.View.Details;
            this.lstSourceSolutions.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lstSourceSolutions_ColumnClick);
            this.lstSourceSolutions.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lstSourceSolutions_KeyDown);
            // 
            // txtSearch
            // 
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Location = new System.Drawing.Point(4, 52);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(1066, 29);
            this.txtSearch.TabIndex = 7;
            this.txtSearch.Text = "Search...";
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // uniquename
            // 
            this.uniquename.Text = "Name";
            this.uniquename.Width = 145;
            // 
            // friendlyname
            // 
            this.friendlyname.Text = "Friendly name";
            this.friendlyname.Width = 145;
            // 
            // version
            // 
            this.version.Text = "Version";
            this.version.Width = 80;
            // 
            // installedon
            // 
            this.installedon.Text = "Installed on";
            this.installedon.Width = 95;
            // 
            // publisher
            // 
            this.publisher.Text = "Publisher";
            this.publisher.Width = 120;
            // 
            // description
            // 
            this.description.Text = "Description";
            this.description.Width = 200;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Info;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(4, 24);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1066, 28);
            this.panel1.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label2.Location = new System.Drawing.Point(35, 5);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(460, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Click on \"Load Solutions\" to display and select solution(s) to transfer";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(4, 4);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(21, 20);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // MainForm
            // 
            this.AllowEndUserDocking = false;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 554);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.Controls.Add(this.grpSourceSolution);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.TabText = "Solutions";
            this.Text = "Solutions";
            this.grpSourceSolution.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox grpSourceSolution;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ListView lstSourceSolutions;
        private System.Windows.Forms.ColumnHeader uniquename;
        private System.Windows.Forms.ColumnHeader friendlyname;
        private System.Windows.Forms.ColumnHeader version;
        private System.Windows.Forms.ColumnHeader installedon;
        private System.Windows.Forms.ColumnHeader publisher;
        private System.Windows.Forms.ColumnHeader description;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}