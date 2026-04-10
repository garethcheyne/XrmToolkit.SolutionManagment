namespace err403.SolutionManagment.Forms
{
    partial class EditEnvironmentVariableForm
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblVariableName = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.pnlSource = new System.Windows.Forms.Panel();
            this.lblSourceHeader = new System.Windows.Forms.Label();
            this.txtSourceValue = new System.Windows.Forms.TextBox();
            this.pnlTargets = new System.Windows.Forms.Panel();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCopySourceToAll = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlSource.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlHeader.Controls.Add(this.lblType);
            this.pnlHeader.Controls.Add(this.lblVariableName);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.pnlHeader.Size = new System.Drawing.Size(550, 55);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblVariableName
            // 
            this.lblVariableName.AutoSize = true;
            this.lblVariableName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVariableName.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblVariableName.ForeColor = System.Drawing.Color.White;
            this.lblVariableName.Location = new System.Drawing.Point(12, 8);
            this.lblVariableName.Name = "lblVariableName";
            this.lblVariableName.Size = new System.Drawing.Size(120, 25);
            this.lblVariableName.TabIndex = 0;
            this.lblVariableName.Text = "Variable Name";
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblType.ForeColor = System.Drawing.Color.LightGray;
            this.lblType.Location = new System.Drawing.Point(12, 33);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(60, 14);
            this.lblType.TabIndex = 1;
            this.lblType.Text = "Type: String";
            // 
            // pnlSource
            // 
            this.pnlSource.Controls.Add(this.txtSourceValue);
            this.pnlSource.Controls.Add(this.lblSourceHeader);
            this.pnlSource.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSource.Location = new System.Drawing.Point(0, 55);
            this.pnlSource.Name = "pnlSource";
            this.pnlSource.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlSource.Size = new System.Drawing.Size(550, 62);
            this.pnlSource.TabIndex = 1;
            // 
            // lblSourceHeader
            // 
            this.lblSourceHeader.AutoSize = true;
            this.lblSourceHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSourceHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblSourceHeader.ForeColor = System.Drawing.Color.Green;
            this.lblSourceHeader.Location = new System.Drawing.Point(12, 6);
            this.lblSourceHeader.Name = "lblSourceHeader";
            this.lblSourceHeader.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblSourceHeader.Size = new System.Drawing.Size(120, 24);
            this.lblSourceHeader.TabIndex = 0;
            this.lblSourceHeader.Text = "Source (Read-only)";
            // 
            // txtSourceValue
            // 
            this.txtSourceValue.BackColor = System.Drawing.SystemColors.Control;
            this.txtSourceValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSourceValue.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtSourceValue.Location = new System.Drawing.Point(12, 30);
            this.txtSourceValue.Name = "txtSourceValue";
            this.txtSourceValue.ReadOnly = true;
            this.txtSourceValue.Size = new System.Drawing.Size(526, 27);
            this.txtSourceValue.TabIndex = 1;
            // 
            // pnlTargets
            // 
            this.pnlTargets.AutoScroll = true;
            this.pnlTargets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTargets.Location = new System.Drawing.Point(0, 117);
            this.pnlTargets.Name = "pnlTargets";
            this.pnlTargets.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.pnlTargets.Size = new System.Drawing.Size(550, 250);
            this.pnlTargets.TabIndex = 2;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Controls.Add(this.btnCopySourceToAll);
            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Location = new System.Drawing.Point(0, 367);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.pnlButtons.Size = new System.Drawing.Size(550, 44);
            this.pnlButtons.TabIndex = 3;
            // 
            // btnSave
            // 
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnSave.Location = new System.Drawing.Point(438, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 32);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.Location = new System.Drawing.Point(12, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 32);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnCopySourceToAll
            // 
            this.btnCopySourceToAll.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCopySourceToAll.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCopySourceToAll.Location = new System.Drawing.Point(288, 6);
            this.btnCopySourceToAll.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.btnCopySourceToAll.Name = "btnCopySourceToAll";
            this.btnCopySourceToAll.Size = new System.Drawing.Size(150, 32);
            this.btnCopySourceToAll.TabIndex = 1;
            this.btnCopySourceToAll.Text = "Copy source to all";
            this.btnCopySourceToAll.UseVisualStyleBackColor = true;
            this.btnCopySourceToAll.Click += new System.EventHandler(this.btnCopySourceToAll_Click);
            // 
            // EditEnvironmentVariableForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(750, 411);
            this.Controls.Add(this.pnlTargets);
            this.Controls.Add(this.pnlButtons);
            this.Controls.Add(this.pnlSource);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditEnvironmentVariableForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Environment Variable";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlSource.ResumeLayout(false);
            this.pnlSource.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblVariableName;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Panel pnlSource;
        private System.Windows.Forms.Label lblSourceHeader;
        private System.Windows.Forms.TextBox txtSourceValue;
        private System.Windows.Forms.Panel pnlTargets;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnCopySourceToAll;
    }
}
