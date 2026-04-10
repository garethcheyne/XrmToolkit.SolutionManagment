using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Label = System.Windows.Forms.Label;

namespace err403.SolutionManagment.Forms
{
    public class FlowActionRequestedEventArgs : EventArgs
    {
        public List<FlowActionItem> Flows { get; set; }
        public ConnectionDetail TargetDetail { get; set; }
        public bool Activate { get; set; }
    }

    public class FlowActionPanel : DockContent
    {
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblFlowCount;
        private Panel pnlFlows;
        private ListView lvSelectedFlows;
        private Panel pnlTargets;
        private Panel pnlEmpty;
        private Label lblEmpty;

        private List<FlowActionItem> currentFlows = new List<FlowActionItem>();

        public event EventHandler<FlowActionRequestedEventArgs> ActionRequested;

        public FlowActionPanel()
        {
            InitializeControls();
            ShowEmptyState();
        }

        protected override string GetPersistString() => "FlowActionPanel";

        private void InitializeControls()
        {
            Text = "Flow Actions";
            TabText = "Flow Actions";
            CloseButton = false;
            CloseButtonVisible = false;
            DockAreas = DockAreas.DockRight | DockAreas.DockBottom | DockAreas.Float;

            // Empty state
            pnlEmpty = new Panel { Dock = DockStyle.Fill };
            lblEmpty = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Light", 12F),
                Text = "Select cloud flows then click\nActivate or Deactivate",
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlEmpty.Controls.Add(lblEmpty);

            // Header
            pnlHeader = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(12, 8, 12, 8)
            };
            lblTitle = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Flow Actions"
            };
            lblFlowCount = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Color.LightGray
            };
            pnlHeader.Controls.Add(lblFlowCount);
            pnlHeader.Controls.Add(lblTitle);

            // Selected flows list
            pnlFlows = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(12, 6, 12, 6)
            };

            var lblFlowsHeader = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.Green,
                Padding = new Padding(0, 0, 0, 4),
                Text = "Selected Flows"
            };

            lvSelectedFlows = new ListView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.25F),
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                View = View.Details,
                GridLines = true
            };
            lvSelectedFlows.Columns.Add("Flow Name", -2);

            pnlFlows.Controls.Add(lvSelectedFlows);
            pnlFlows.Controls.Add(lblFlowsHeader);

            // Targets (scrollable)
            pnlTargets = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };

            Controls.Add(pnlEmpty);
        }

        public void ShowEmptyState()
        {
            pnlHeader.Visible = false;
            pnlFlows.Visible = false;
            pnlTargets.Visible = false;

            if (!Controls.Contains(pnlEmpty)) Controls.Add(pnlEmpty);
            pnlEmpty.Visible = true;
        }

        public void LoadFlows(List<FlowActionItem> flows, List<ConnectionDetail> targets)
        {
            currentFlows = flows;
            pnlTargets.Controls.Clear();

            Controls.Clear();
            pnlEmpty.Visible = false;

            // Header
            lblTitle.Text = "Flow Actions";
            lblFlowCount.Text = $"{flows.Count} flow(s) selected";

            // Populate the selected flows list
            lvSelectedFlows.Items.Clear();
            foreach (var flow in flows)
            {
                var item = new ListViewItem { Text = flow.FlowName };
                var wf = flow.Workflow;
                var stateCode = wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;
                item.ForeColor = stateCode == 1 ? Color.DarkGreen : Color.Gray;
                lvSelectedFlows.Items.Add(item);
            }
            lvSelectedFlows.Columns[0].Width = -2;

            // Adjust flows panel height based on count
            pnlFlows.Height = Math.Min(30 + flows.Count * 20, 180);

            // Build target environment rows
            if (targets.Count == 0)
            {
                var lbl = new Label
                {
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.Gray,
                    Height = 40,
                    Padding = new Padding(12, 12, 12, 0),
                    Text = "No target environments connected."
                };
                pnlTargets.Controls.Add(lbl);
            }
            else
            {
                foreach (var target in targets)
                {
                    var targetPanel = CreateTargetRow(target);
                    pnlTargets.Controls.Add(targetPanel);
                    pnlTargets.Controls.SetChildIndex(targetPanel, 0);
                }

                // Reorder so first target is at top
                var panels = pnlTargets.Controls.OfType<Panel>().Reverse().ToList();
                for (int i = 0; i < panels.Count; i++)
                {
                    pnlTargets.Controls.SetChildIndex(panels[i], i);
                }
            }

            // Build layout
            Controls.Add(pnlTargets);
            Controls.Add(pnlFlows);
            Controls.Add(pnlHeader);

            pnlHeader.Visible = true;
            pnlFlows.Visible = true;
            pnlTargets.Visible = true;

            Activate();
        }

        private Panel CreateTargetRow(ConnectionDetail target)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(12, 8, 12, 8),
                Tag = target
            };

            var lbl = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Padding = new Padding(0, 0, 0, 6),
                Text = target.ConnectionName
            };

            var btnFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0),
                WrapContents = false
            };

            var btnActivate = new RoundedButton
            {
                Font = new Font("Segoe UI", 8.25F),
                Size = new Size(90, 32),
                Text = "Activate",
                BackColor = Color.FromArgb(220, 245, 220),
                ForeColor = Color.FromArgb(30, 100, 30),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Radius = 6,
                Tag = target
            };
            btnActivate.FlatAppearance.BorderSize = 0;
            btnActivate.Click += (s, e) => RaiseActionEvent(target, true);

            var btnDeactivate = new RoundedButton
            {
                Font = new Font("Segoe UI", 8.25F),
                Size = new Size(100, 32),
                Text = "Deactivate",
                BackColor = Color.FromArgb(255, 230, 230),
                ForeColor = Color.FromArgb(150, 30, 30),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 0),
                Radius = 6,
                Tag = target
            };
            btnDeactivate.FlatAppearance.BorderSize = 0;
            btnDeactivate.Click += (s, e) => RaiseActionEvent(target, false);

            btnFlow.Controls.Add(btnActivate);
            btnFlow.Controls.Add(btnDeactivate);

            panel.Controls.Add(btnFlow);
            panel.Controls.Add(lbl);

            return panel;
        }

        private void RaiseActionEvent(ConnectionDetail target, bool activate)
        {
            if (currentFlows.Count == 0) return;

            ActionRequested?.Invoke(this, new FlowActionRequestedEventArgs
            {
                Flows = currentFlows,
                TargetDetail = target,
                Activate = activate
            });
        }

        public void SetTargetResult(ConnectionDetail target, bool success, string message = null)
        {
            foreach (var panel in pnlTargets.Controls.OfType<Panel>())
            {
                if (panel.Tag as ConnectionDetail != target) continue;

                var lbl = panel.Controls.OfType<Label>().FirstOrDefault();
                if (lbl == null) continue;

                if (success)
                {
                    lbl.ForeColor = Color.DarkGreen;
                    lbl.Text = target.ConnectionName + "  ✓";
                }
                else
                {
                    lbl.ForeColor = Color.DarkRed;
                    lbl.Text = target.ConnectionName + "  ✗ " + (message ?? "Failed");
                }
            }
        }
    }

    public class RoundedButton : Button
    {
        public int Radius { get; set; } = 6;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GetRoundedRect(rect, Radius))
            {
                Region = new Region(path);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
