using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public class FlowToggleResult
    {
        public string FlowName { get; set; }
        public string TargetName { get; set; }
        public string TargetOrgUrl { get; set; }
        public Guid? TargetFlowId { get; set; }
        public bool Success { get; set; }
        public bool IsConnectionRefError { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FlowResultsForm : Form
    {
        private ListView lvResults;
        private Button btnClose;
        private Button btnOpenSelected;
        private Label lblSummary;

        public FlowResultsForm(string action, List<FlowToggleResult> results)
        {
            InitControls();

            Text = $"Cloud Flow {action} — Results";

            var succeeded = results.Count(r => r.Success);
            var failed = results.Count - succeeded;

            lblSummary.Text = $"{succeeded} succeeded, {failed} failed out of {results.Count} operation(s).";
            if (failed > 0)
                lblSummary.ForeColor = Color.DarkRed;

            foreach (var result in results)
            {
                var item = new ListViewItem
                {
                    Tag = result,
                    Text = result.Success ? "✓" : "✗"
                };
                item.SubItems.Add(result.FlowName);
                item.SubItems.Add(result.TargetName);
                item.SubItems.Add(result.Success ? "OK" : GetShortError(result));

                item.UseItemStyleForSubItems = false;
                if (result.Success)
                {
                    item.SubItems[0].ForeColor = Color.DarkGreen;
                }
                else
                {
                    item.SubItems[0].ForeColor = Color.DarkRed;
                    item.SubItems[3].ForeColor = Color.DarkRed;
                    if (result.IsConnectionRefError)
                    {
                        item.SubItems[3].BackColor = Color.LemonChiffon;
                    }
                }

                lvResults.Items.Add(item);
            }

            foreach (ColumnHeader col in lvResults.Columns)
            {
                col.Width = -2;
                if (col.Width > 400) col.Width = 400;
            }

            UpdateOpenButtonState();
        }

        private void InitControls()
        {
            Width = 780;
            Height = 420;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 300);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                Height = 30,
                Padding = new Padding(8, 6, 8, 0)
            };

            lvResults = new ListView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.25F),
                FullRowSelect = true,
                GridLines = true,
                View = View.Details,
                HideSelection = false
            };

            lvResults.Columns.Add("", 30);
            lvResults.Columns.Add("Flow Name", 250);
            lvResults.Columns.Add("Target", 150);
            lvResults.Columns.Add("Result", 300);

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(8, 6, 8, 6)
            };

            btnClose = new Button
            {
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(100, 32),
                Text = "Close",
                DialogResult = DialogResult.Cancel
            };

            btnOpenSelected = new Button
            {
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(180, 32),
                Text = "Open Flow in Browser",
                Margin = new Padding(0, 0, 6, 0),
                Enabled = false
            };
            btnOpenSelected.Click += BtnOpenSelected_Click;

            lvResults.SelectedIndexChanged += (s, ev) => UpdateOpenButtonState();

            var spacer = new Panel { Dock = DockStyle.Right, Width = 6 };
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Controls.Add(spacer);
            pnlButtons.Controls.Add(btnOpenSelected);

            Controls.Add(lvResults);
            Controls.Add(lblSummary);
            Controls.Add(pnlButtons);

            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private void UpdateOpenButtonState()
        {
            btnOpenSelected.Enabled = lvResults.SelectedItems.Count > 0
                && lvResults.SelectedItems.Cast<ListViewItem>()
                    .Any(i => ((FlowToggleResult)i.Tag).TargetFlowId.HasValue);
        }

        private void BtnOpenSelected_Click(object sender, EventArgs e)
        {
            var notFound = new List<string>();

            foreach (ListViewItem item in lvResults.SelectedItems)
            {
                var result = (FlowToggleResult)item.Tag;
                if (result.TargetFlowId.HasValue && !string.IsNullOrEmpty(result.TargetOrgUrl))
                {
                    try
                    {
                        var url = $"{result.TargetOrgUrl.TrimEnd('/')}/main.aspx?pagetype=entityrecord&etn=workflow&id={result.TargetFlowId.Value}";
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open browser:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    notFound.Add(result.FlowName);
                }
            }

            if (notFound.Count > 0)
            {
                MessageBox.Show($"The following flow(s) were not found on the target:\n\n{string.Join("\n", notFound)}",
                    "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string GetShortError(FlowToggleResult result)
        {
            if (result.IsConnectionRefError)
                return "Connection reference not accessible — open flow to fix";
            return result.ErrorMessage?.Length > 200
                ? result.ErrorMessage.Substring(0, 200) + "..."
                : result.ErrorMessage ?? "Unknown error";
        }
    }
}
