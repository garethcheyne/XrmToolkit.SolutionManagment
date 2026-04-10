using McTools.Xrm.Connection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public class TransferEnvVarSummaryForm : Form
    {
        private ListView lvSummary;
        private Panel pnlButtons;
        private Button btnTransfer;
        private Button btnCancel;
        private Label lblInfo;

        public TransferEnvVarSummaryForm(
            List<EnvVarTransferItem> items,
            List<ConnectionDetail> targets)
        {
            Text = "Confirm Environment Variable Transfer";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(850, 500);
            MinimumSize = new Size(650, 350);
            Font = new Font("Segoe UI", 9F);

            lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40,
                Padding = new Padding(10, 10, 10, 4),
                Text = $"The following {items.Count} variable(s) will have their source value copied to {targets.Count} target(s):\n" +
                       string.Join(", ", targets.Select(t => t.ConnectionName)),
                Font = new Font("Segoe UI", 9F)
            };

            lvSummary = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 8.25F),
                CheckBoxes = true,
                HideSelection = false
            };

            lvSummary.Columns.Add("Variable", 220);
            lvSummary.Columns.Add("Type", 70);
            lvSummary.Columns.Add("Source Value", 500);

            foreach (var item in items)
            {
                var lvi = new ListViewItem
                {
                    Text = item.DisplayName,
                    Checked = true,
                    Tag = item
                };
                lvi.SubItems.Add(item.TypeName);

                var displayValue = item.SourceValue;
                if (displayValue != null && displayValue.Length > 120)
                    displayValue = displayValue.Substring(0, 120) + "...";
                lvi.SubItems.Add(displayValue ?? "(default)");

                lvSummary.Items.Add(lvi);
            }

            pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10, 8, 10, 8)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Left,
                Width = 100,
                DialogResult = DialogResult.Cancel
            };

            btnTransfer = new Button
            {
                Text = $"Transfer to {targets.Count} target(s)",
                Dock = DockStyle.Right,
                Width = 180,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnTransfer.Click += (s, e) =>
            {
                var checkedItems = lvSummary.CheckedItems.Cast<ListViewItem>().ToList();
                if (checkedItems.Count == 0)
                {
                    MessageBox.Show("No variables are checked for transfer.", "Nothing Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ConfirmedItems = checkedItems.Select(i => (EnvVarTransferItem)i.Tag).ToList();
                DialogResult = DialogResult.OK;
                Close();
            };

            pnlButtons.Controls.Add(btnTransfer);
            pnlButtons.Controls.Add(btnCancel);

            Controls.Add(lvSummary);
            Controls.Add(pnlButtons);
            Controls.Add(lblInfo);

            AcceptButton = btnTransfer;
            CancelButton = btnCancel;
        }

        public List<EnvVarTransferItem> ConfirmedItems { get; private set; }
    }
}
