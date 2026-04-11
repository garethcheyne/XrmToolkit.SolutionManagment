namespace err403.SolutionManagment
{
    partial class SolutionManagement
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cfForm?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // SolutionManagement
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "SolutionManagement";
            this.Size = new System.Drawing.Size(1372, 836);
            this.ResumeLayout(false);
        }
    }
}
