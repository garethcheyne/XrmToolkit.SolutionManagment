using err403.SolutionManagment.AppCode;
using WeifenLuo.WinFormsUI.Docking;

namespace err403.SolutionManagment.Forms
{
    public partial class SettingsForm : DockContent
    {
        public SettingsForm(bool isFromOneShot = false)
        {
            InitializeComponent();

            pnlBottom.Visible = isFromOneShot;
            pnlReviewWarning.Visible = !isFromOneShot;
        }

        public Settings Settings
        {
            set => SettingsPropertyPanel.SelectedObject = value;
            get => (Settings)SettingsPropertyPanel.SelectedObject;
        }
    }
}