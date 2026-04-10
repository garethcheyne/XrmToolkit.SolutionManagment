using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using XrmToolBox.Extensibility;

namespace err403.SolutionManagment.Forms
{
    public class FlowActivateRequestedEventArgs : EventArgs
    {
        public List<FlowActionItem> Flows { get; set; } = new List<FlowActionItem>();
        public bool Activate { get; set; }
    }

    public class FlowActionItem
    {
        public string FlowName { get; set; }
        public Guid WorkflowId { get; set; }
        public Entity Workflow { get; set; }
        public ListViewItem Item { get; set; }
    }

    public partial class CloudFlowsForm : DockContent
    {
        private int currentColumnOrder;
        private List<ListViewItem> allFlowItems = new List<ListViewItem>();
        private List<ListViewItem> filteredByActiveItems = new List<ListViewItem>();
        private const int MaxColumnWidth = 350;

        public event EventHandler RefreshRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> ActivateRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> DeactivateRequested;

        public ListView LvFlows => lvFlows;
        public ColumnHeader ColStatus => colStatus;

        public CloudFlowsForm()
        {
            InitializeComponent();
        }

        public void DisplayCloudFlows(List<Entity> workflows)
        {
            lvFlows.Items.Clear();
            cmbSolutionFilter.Items.Clear();
            cmbSolutionFilter.Items.Add("(All)");

            if (workflows == null) return;

            var solutionNames = new HashSet<string>();

            foreach (var wf in workflows)
            {
                var name = wf.GetAttributeValue<string>("name") ?? "(unnamed)";
                var category = wf.GetAttributeValue<OptionSetValue>("category")?.Value ?? 0;
                var stateCode = wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;
                var statusCode = wf.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0;
                var owner = wf.GetAttributeValue<EntityReference>("ownerid");
                var modifiedOn = wf.GetAttributeValue<DateTime?>("modifiedon");
                var solutionName = wf.GetAttributeValue<AliasedValue>("solution.friendlyname")?.Value as string ?? "";

                var statusText = GetStatusText(stateCode, statusCode);
                var typeText = GetCategoryText(category);

                var item = new ListViewItem
                {
                    Tag = wf,
                    Text = name
                };
                item.SubItems.Add(typeText);
                item.SubItems.Add(statusText);
                item.SubItems.Add(solutionName);
                item.SubItems.Add(owner?.Name ?? "");
                item.SubItems.Add(modifiedOn?.ToString("yy-MM-dd HH:mm") ?? "");

                // Color the status cell
                item.UseItemStyleForSubItems = false;
                var statusSub = item.SubItems[2];
                switch (stateCode)
                {
                    case 1: // Activated
                        statusSub.BackColor = Color.LightGreen;
                        statusSub.ForeColor = Color.DarkGreen;
                        break;
                    case 0: // Draft/Off
                        statusSub.BackColor = Color.LightGray;
                        statusSub.ForeColor = Color.Black;
                        break;
                    case 2: // Suspended
                        statusSub.BackColor = SystemColors.Info;
                        statusSub.ForeColor = Color.DarkRed;
                        break;
                }

                if (!string.IsNullOrEmpty(solutionName))
                    solutionNames.Add(solutionName);

                lvFlows.Items.Add(item);
            }

            allFlowItems = lvFlows.Items.Cast<ListViewItem>().ToList();

            foreach (var sol in solutionNames.OrderBy(s => s))
            {
                cmbSolutionFilter.Items.Add(sol);
            }
            cmbSolutionFilter.SelectedIndex = 0;

            AutoSizeColumns();
            ApplyFilters();
        }

        public void DisplayTargetFlowStatus(List<ConnectionDetail> connectionDetails, PluginControlBase parent)
        {
            foreach (var cd in connectionDetails)
            {
                parent.WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (w, e) =>
                    {
                        var svc = cd.GetCrmServiceClient();

                        var query = new QueryExpression("workflow")
                        {
                            ColumnSet = new ColumnSet("name", "statecode", "statuscode", "category"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("category", ConditionOperator.Equal, 5),
                                    new ConditionExpression("type", ConditionOperator.Equal, 1)
                                }
                            }
                        };
                        var flows = svc.RetrieveMultiple(query).Entities.ToList();
                        e.Result = new Tuple<ConnectionDetail, List<Entity>>(cd, flows);
                    },
                    PostWorkCallBack = (e) =>
                    {
                        if (e.Error != null) return;

                        var result = (Tuple<ConnectionDetail, List<Entity>>)e.Result;
                        var tcd = result.Item1;
                        var targetFlows = result.Item2;

                        var column = lvFlows.Columns.Cast<ColumnHeader>().FirstOrDefault(c => c.Text == tcd.ConnectionName);
                        if (column == null)
                        {
                            var newCol = new ColumnHeader
                            {
                                Text = tcd.ConnectionName,
                                Tag = tcd
                            };

                            var insertIndex = colModifiedOn.Index + 1;
                            foreach (var c in lvFlows.Columns.Cast<ColumnHeader>().Where(c => c.Tag is ConnectionDetail))
                            {
                                var idx = c.Index + 1;
                                if (idx > insertIndex) insertIndex = idx;
                            }

                            lvFlows.Columns.Insert(insertIndex, newCol);
                            column = lvFlows.Columns.Cast<ColumnHeader>().First(c => c.Text == tcd.ConnectionName);
                        }

                        foreach (ListViewItem item in lvFlows.Items)
                        {
                            item.UseItemStyleForSubItems = false;
                            var sourceWf = (Entity)item.Tag;
                            var sourceName = sourceWf.GetAttributeValue<string>("name");

                            var targetWf = targetFlows.FirstOrDefault(f =>
                                f.GetAttributeValue<string>("name") == sourceName);

                            while (item.SubItems.Count <= column.Index)
                            {
                                item.SubItems.Add(new ListViewItem.ListViewSubItem());
                            }

                            var subItem = item.SubItems[column.Index];

                            if (targetWf != null)
                            {
                                var targetState = targetWf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;
                                var targetStatusCode = targetWf.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0;
                                var sourceState = sourceWf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;

                                subItem.Text = GetStatusText(targetState, targetStatusCode);

                                if (targetState == sourceState)
                                {
                                    subItem.BackColor = Color.LightGreen;
                                    subItem.ForeColor = Color.DarkGreen;
                                }
                                else
                                {
                                    subItem.BackColor = SystemColors.Info;
                                    subItem.ForeColor = Color.DarkRed;
                                }
                            }
                            else
                            {
                                subItem.BackColor = Color.LightGray;
                                subItem.ForeColor = Color.Black;
                                subItem.Text = "(not found)";
                            }
                        }

                        lvFlows.AutoResizeColumn(column.Index, ColumnHeaderAutoResizeStyle.ColumnContent);
                        if (column.Width > MaxColumnWidth) column.Width = MaxColumnWidth;

                        // Refresh the allFlowItems cache
                        allFlowItems = lvFlows.Items.Cast<ListViewItem>().ToList();
                    }
                });
            }
        }

        public void RemoveTargetColumn(ConnectionDetail detail)
        {
            var col = lvFlows.Columns.Cast<ColumnHeader>()
                .FirstOrDefault(c => c.Text == detail.ConnectionName);
            if (col == null) return;

            var idx = col.Index;
            lvFlows.Columns.Remove(col);
            foreach (ListViewItem item in lvFlows.Items)
            {
                if (item.SubItems.Count > idx)
                    item.SubItems.RemoveAt(idx);
            }

            allFlowItems = lvFlows.Items.Cast<ListViewItem>().ToList();
        }

        public void InvokeRefresh()
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public void InvokeActivateSelected()
        {
            RaiseActivateEvent(true);
        }

        public void InvokeDeactivateSelected()
        {
            RaiseActivateEvent(false);
        }

        private void RaiseActivateEvent(bool activate)
        {
            if (lvFlows.SelectedItems.Count == 0)
            {
                MessageBox.Show($"Select one or more cloud flows to {(activate ? "activate" : "deactivate")}.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var args = new FlowActivateRequestedEventArgs { Activate = activate };

            foreach (ListViewItem item in lvFlows.SelectedItems)
            {
                var wf = (Entity)item.Tag;
                args.Flows.Add(new FlowActionItem
                {
                    FlowName = item.Text,
                    WorkflowId = wf.Id,
                    Workflow = wf,
                    Item = item
                });
            }

            if (activate)
                ActivateRequested?.Invoke(this, args);
            else
                DeactivateRequested?.Invoke(this, args);
        }

        private void ApplyFilters()
        {
            var solutionFilter = cmbSolutionFilter.SelectedItem?.ToString();
            var activeOnly = chkActiveOnly.Checked;

            var source = allFlowItems.AsEnumerable();

            if (activeOnly)
            {
                source = source.Where(item =>
                {
                    var wf = (Entity)item.Tag;
                    return (wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0) == 1;
                });
            }

            if (!string.IsNullOrEmpty(solutionFilter) && solutionFilter != "(All)")
            {
                source = source.Where(item => item.SubItems[3].Text == solutionFilter);
            }

            // Also apply text search
            var searchText = txtSearch.Text;
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "Search...")
            {
                var filter = searchText.ToLowerInvariant();
                source = source.Where(item =>
                    item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .Any(sub => sub.Text.ToLowerInvariant().Contains(filter)));
            }

            filteredByActiveItems = source.ToList();

            lvFlows.BeginUpdate();
            lvFlows.Items.Clear();
            lvFlows.Items.AddRange(filteredByActiveItems.ToArray());
            lvFlows.EndUpdate();
        }

        private void AutoSizeColumns()
        {
            foreach (ColumnHeader col in lvFlows.Columns)
            {
                col.Width = -2;
                if (col.Width > MaxColumnWidth) col.Width = MaxColumnWidth;
            }
        }

        private static string GetStatusText(int stateCode, int statusCode)
        {
            switch (stateCode)
            {
                case 0: return "Off";
                case 1: return "On";
                case 2: return "Suspended";
                default: return $"Unknown ({stateCode})";
            }
        }

        private static string GetCategoryText(int category)
        {
            switch (category)
            {
                case 0: return "Classic";
                case 1: return "Dialog";
                case 2: return "Business Rule";
                case 3: return "Action";
                case 4: return "BPF";
                case 5: return "Cloud Flow";
                case 6: return "Desktop Flow";
                default: return $"Other ({category})";
            }
        }

        #region Event handlers

        private void chkActiveOnly_CheckedChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cmbSolutionFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Search...")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Black;
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search...";
                txtSearch.ForeColor = Color.Gray;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void lvFlows_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != currentColumnOrder)
            {
                currentColumnOrder = e.Column;
                lvFlows.Sorting = SortOrder.Descending;
            }

            lvFlows.Sorting = lvFlows.Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            lvFlows.ListViewItemSorter = new ListViewItemComparer(e.Column, lvFlows.Sorting);
            lvFlows.Sort();
        }

        #endregion
    }
}
