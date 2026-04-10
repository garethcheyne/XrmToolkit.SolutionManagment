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
    public class EnvVarTransferRequestedEventArgs : EventArgs
    {
        public List<EnvVarTransferItem> Items { get; set; } = new List<EnvVarTransferItem>();
    }

    public class EnvVarTransferItem
    {
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public string TypeName { get; set; }
        public string SourceValue { get; set; }
        public Entity Definition { get; set; }
        public ListViewItem Item { get; set; }
    }

    public class EnvVarEditRequestedEventArgs : EventArgs
    {
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public string TypeName { get; set; }
        public string SourceValue { get; set; }
        public Entity Definition { get; set; }
        public ListViewItem Item { get; set; }
    }

    public partial class EnvironmentVariablesForm : DockContent
    {
        private int currentColumnOrder;
        private readonly ToolTip cellToolTip = new ToolTip();
        private ListViewItem.ListViewSubItem lastToolTipSubItem;
        private const int MaxColumnWidth = 350;
        private List<ListViewItem> allEnvVarItems = new List<ListViewItem>();

        public event EventHandler<EnvVarEditRequestedEventArgs> EditRequested;
        public event EventHandler<EnvVarTransferRequestedEventArgs> TransferRequested;
        public event EventHandler RefreshRequested;
        public ColumnHeader ColCurrentValue => colCurrentValue;
        public ListView LvEnvVars => lvEnvVars;

        public EnvironmentVariablesForm()
        {
            InitializeComponent();
            cellToolTip.InitialDelay = 400;
            cellToolTip.ReshowDelay = 100;
            cellToolTip.AutoPopDelay = 15000;
        }

        public void DisplayEnvironmentVariables(List<Entity> definitions, List<Entity> values)
        {
            lvEnvVars.Items.Clear();

            if (definitions == null) return;

            foreach (var def in definitions)
            {
                var defId = def.Id;
                var currentValue = values?.FirstOrDefault(v =>
                    v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid")?.Id == defId);

                var item = new ListViewItem
                {
                    Tag = def,
                    Text = def.GetAttributeValue<string>("displayname") ?? ""
                };
                item.SubItems.Add(def.GetAttributeValue<string>("schemaname") ?? "");
                item.SubItems.Add(GetTypeName(def.GetAttributeValue<OptionSetValue>("type")?.Value ?? 0));
                item.SubItems.Add(def.GetAttributeValue<string>("defaultvalue") ?? "");
                item.SubItems.Add(currentValue?.GetAttributeValue<string>("value") ?? "(default)");

                lvEnvVars.Items.Add(item);
            }

            allEnvVarItems = lvEnvVars.Items.Cast<ListViewItem>().ToList();

            AutoSizeVisibleColumns();
        }

        public void DisplayTargetEnvironmentValues(List<ConnectionDetail> connectionDetails, PluginControlBase parent)
        {
            foreach (var cd in connectionDetails)
            {
                parent.WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (w, e) =>
                    {
                        var svc = cd.GetCrmServiceClient();

                        var defQuery = new QueryExpression("environmentvariabledefinition")
                        {
                            ColumnSet = new ColumnSet("schemaname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                }
                            }
                        };
                        var defs = svc.RetrieveMultiple(defQuery).Entities.ToList();

                        var valQuery = new QueryExpression("environmentvariablevalue")
                        {
                            ColumnSet = new ColumnSet("value", "environmentvariabledefinitionid", "schemaname"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                }
                            }
                        };
                        var vals = svc.RetrieveMultiple(valQuery).Entities.ToList();

                        e.Result = new Tuple<ConnectionDetail, List<Entity>, List<Entity>>(cd, defs, vals);
                    },
                    PostWorkCallBack = (e) =>
                    {
                        if (e.Error != null) return;

                        var result = (Tuple<ConnectionDetail, List<Entity>, List<Entity>>)e.Result;
                        var tcd = result.Item1;
                        var targetDefs = result.Item2;
                        var targetVals = result.Item3;

                        var column = lvEnvVars.Columns.Cast<ColumnHeader>().FirstOrDefault(c => c.Text == tcd.ConnectionName);
                        if (column == null)
                        {
                            var newCol = new ColumnHeader
                            {
                                Text = tcd.ConnectionName,
                                Tag = tcd
                            };

                            // Insert after "Current Value" column
                            var insertIndex = colCurrentValue.Index + 1;
                            foreach (var c in lvEnvVars.Columns.Cast<ColumnHeader>().Where(c => c.Tag is ConnectionDetail))
                            {
                                var idx = c.Index + 1;
                                if (idx > insertIndex) insertIndex = idx;
                            }

                            lvEnvVars.Columns.Insert(insertIndex, newCol);
                            column = lvEnvVars.Columns.Cast<ColumnHeader>().First(c => c.Text == tcd.ConnectionName);
                        }

                        foreach (ListViewItem item in lvEnvVars.Items)
                        {
                            item.UseItemStyleForSubItems = false;
                            var sourceDef = (Entity)item.Tag;
                            var sourceSchema = sourceDef.GetAttributeValue<string>("schemaname");

                            var targetDef = targetDefs.FirstOrDefault(d =>
                                d.GetAttributeValue<string>("schemaname") == sourceSchema);

                            // Pad subitems so we can safely access column.Index
                            while (item.SubItems.Count <= column.Index)
                            {
                                item.SubItems.Add(new ListViewItem.ListViewSubItem());
                            }

                            var subItem = item.SubItems[column.Index];

                            if (targetDef != null)
                            {
                                var targetVal = targetVals.FirstOrDefault(v =>
                                    v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid")?.Id == targetDef.Id);

                                var targetValue = targetVal?.GetAttributeValue<string>("value") ?? "(default)";
                                var sourceValue = item.SubItems[colCurrentValue.Index].Text;
                                subItem.Text = targetValue;

                                if (targetValue == sourceValue)
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

                        lvEnvVars.AutoResizeColumn(column.Index, ColumnHeaderAutoResizeStyle.ColumnContent);
                        if (column.Width > MaxColumnWidth) column.Width = MaxColumnWidth;
                    }
                });
            }
        }

        private static string GetTypeName(int type)
        {
            switch (type)
            {
                case 100000000: return "String";
                case 100000001: return "Number";
                case 100000002: return "Boolean";
                case 100000003: return "JSON";
                case 100000004: return "Data Source";
                case 100000005: return "Secret";
                default: return type.ToString();
            }
        }

        private void chkShowSchema_CheckedChanged(object sender, EventArgs e)
        {
            colSchemaName.Width = chkShowSchema.Checked ? -2 : 0;
        }

        private void chkShowDefault_CheckedChanged(object sender, EventArgs e)
        {
            colDefaultValue.Width = chkShowDefault.Checked ? -2 : 0;
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
            var filter = txtSearch.Text;
            if (string.IsNullOrWhiteSpace(filter) || filter == "Search...")
            {
                lvEnvVars.BeginUpdate();
                lvEnvVars.Items.Clear();
                lvEnvVars.Items.AddRange(allEnvVarItems.ToArray());
                lvEnvVars.EndUpdate();
                return;
            }

            filter = filter.ToLowerInvariant();
            var matched = allEnvVarItems.Where(item =>
                item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                    .Any(sub => sub.Text.ToLowerInvariant().Contains(filter)))
                .ToArray();

            lvEnvVars.BeginUpdate();
            lvEnvVars.Items.Clear();
            lvEnvVars.Items.AddRange(matched);
            lvEnvVars.EndUpdate();
        }

        private void AutoSizeVisibleColumns()
        {
            foreach (ColumnHeader col in lvEnvVars.Columns)
            {
                if (col == colSchemaName && !chkShowSchema.Checked) continue;
                if (col == colDefaultValue && !chkShowDefault.Checked) continue;
                col.Width = -2;
                if (col.Width > MaxColumnWidth) col.Width = MaxColumnWidth;
            }
        }

        private void lvEnvVars_MouseMove(object sender, MouseEventArgs e)
        {
            var hit = lvEnvVars.HitTest(e.Location);
            if (hit.SubItem != null && hit.SubItem != lastToolTipSubItem)
            {
                lastToolTipSubItem = hit.SubItem;
                var text = hit.SubItem.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    cellToolTip.Hide(lvEnvVars);
                    cellToolTip.Show(text, lvEnvVars, e.X + 15, e.Y + 15, 10000);
                }
                else
                {
                    cellToolTip.Hide(lvEnvVars);
                }
            }
            else if (hit.SubItem == null)
            {
                cellToolTip.Hide(lvEnvVars);
                lastToolTipSubItem = null;
            }
        }

        private void lvEnvVars_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != currentColumnOrder)
            {
                currentColumnOrder = e.Column;
                lvEnvVars.Sorting = SortOrder.Descending;
            }

            lvEnvVars.Sorting = lvEnvVars.Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            lvEnvVars.ListViewItemSorter = new ListViewItemComparer(e.Column, lvEnvVars.Sorting);
            lvEnvVars.Sort();
        }

        private void lvEnvVars_DoubleClick(object sender, EventArgs e)
        {
            if (lvEnvVars.SelectedItems.Count == 0) return;

            var item = lvEnvVars.SelectedItems[0];
            var def = (Entity)item.Tag;

            var args = new EnvVarEditRequestedEventArgs
            {
                DisplayName = item.Text,
                SchemaName = item.SubItems[colSchemaName.Index].Text,
                TypeName = item.SubItems[colType.Index].Text,
                SourceValue = item.SubItems[colCurrentValue.Index].Text,
                Definition = def,
                Item = item
            };

            EditRequested?.Invoke(this, args);
        }

        private void btnTransfer_Click(object sender, EventArgs e)
        {
            if (lvEnvVars.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select one or more environment variables to transfer.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var transferArgs = new EnvVarTransferRequestedEventArgs();

            foreach (ListViewItem item in lvEnvVars.SelectedItems)
            {
                var def = (Entity)item.Tag;
                transferArgs.Items.Add(new EnvVarTransferItem
                {
                    DisplayName = item.Text,
                    SchemaName = item.SubItems[colSchemaName.Index].Text,
                    TypeName = item.SubItems[colType.Index].Text,
                    SourceValue = item.SubItems[colCurrentValue.Index].Text,
                    Definition = def,
                    Item = item
                });
            }

            TransferRequested?.Invoke(this, transferArgs);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public void InvokeRefresh()
        {
            btnRefresh_Click(this, EventArgs.Empty);
        }

        public void InvokeTransfer()
        {
            btnTransfer_Click(this, EventArgs.Empty);
        }
    }
}
