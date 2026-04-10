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
    public partial class OrgSettingsForm : DockContent
    {
        private int currentColumnOrder;
        private List<ListViewItem> allSettingItems = new List<ListViewItem>();
        private const int MaxColumnWidth = 350;
        private const int TargetColumnWidth = 80;

        public event EventHandler RefreshRequested;
        public event EventHandler<SettingSyncRequestedEventArgs> SyncRequested;

        public OrgSettingsForm()
        {
            InitializeComponent();
        }

        public void DisplaySourceSettings(IOrganizationService service, ConnectionDetail detail)
        {
            lvSettings.Items.Clear();
            allSettingItems.Clear();
            cmbCategoryFilter.Items.Clear();
            cmbCategoryFilter.Items.Add("(All)");

            if (service == null) return;

            var definitions = RetrieveSettingDefinitions(service);
            var values = RetrieveSettingValues(service);

            if (definitions == null || definitions.Count == 0) return;

            // Build a lookup of setting values by settingdefinitionid
            var valueLookup = new Dictionary<Guid, string>();
            foreach (var val in values)
            {
                var defId = val.GetAttributeValue<EntityReference>("settingdefinitionid")?.Id ?? Guid.Empty;
                var settingValue = val.GetAttributeValue<string>("value") ?? "";
                if (defId != Guid.Empty)
                    valueLookup[defId] = settingValue;
            }

            var categories = new HashSet<string>();

            foreach (var def in definitions.OrderBy(d =>
                (d.GetAttributeValue<string>("groupname") ?? "Other") + "|" +
                (d.GetAttributeValue<string>("displayname") ?? d.GetAttributeValue<string>("uniquename") ?? "")))
            {
                var uniqueName = def.GetAttributeValue<string>("uniquename") ?? "";
                var displayName = def.GetAttributeValue<string>("displayname") ?? uniqueName;
                var description = def.GetAttributeValue<string>("description") ?? "";
                var groupName = def.GetAttributeValue<string>("groupname") ?? "Other";
                var dataType = def.GetAttributeValue<OptionSetValue>("datatype")?.Value ?? 0;
                var defaultValue = def.GetAttributeValue<string>("defaultvalue") ?? "";
                var overridableLevel = def.GetAttributeValue<OptionSetValue>("overridablelevel")?.Value ?? 0;
                var releaseLevel = def.GetAttributeValue<OptionSetValue>("releaselevel")?.Value ?? 0;

                // Get the actual value, fall back to default
                var actualValue = valueLookup.ContainsKey(def.Id) ? valueLookup[def.Id] : defaultValue;

                var item = new ListViewItem
                {
                    Tag = def,
                    Text = groupName
                };
                item.SubItems.Add(displayName);
                item.SubItems.Add(uniqueName);
                item.SubItems.Add(FormatValue(actualValue, dataType));

                // Color booleans
                item.UseItemStyleForSubItems = false;
                var valueSub = item.SubItems[3];
                ColorValueCell(valueSub, actualValue, defaultValue);

                if (!string.IsNullOrEmpty(groupName))
                    categories.Add(groupName);

                lvSettings.Items.Add(item);
            }

            allSettingItems = lvSettings.Items.Cast<ListViewItem>().ToList();

            foreach (var cat in categories.OrderBy(c => c))
            {
                cmbCategoryFilter.Items.Add(cat);
            }
            cmbCategoryFilter.SelectedIndex = 0;

            AutoSizeColumns();
        }

        public void DisplayTargetSettings(List<ConnectionDetail> connectionDetails, PluginControlBase parent)
        {
            // Remove existing target columns (keep Group, Display Name, Unique Name, Source Value)
            while (lvSettings.Columns.Count > 4)
            {
                lvSettings.Columns.RemoveAt(4);
                foreach (ListViewItem item in lvSettings.Items)
                {
                    if (item.SubItems.Count > 4)
                        item.SubItems.RemoveAt(4);
                }
                foreach (var item in allSettingItems)
                {
                    if (item.SubItems.Count > 4)
                        item.SubItems.RemoveAt(4);
                }
            }

            foreach (var cd in connectionDetails)
            {
                var localCd = cd;
                parent.WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (w, e) =>
                    {
                        var svc = localCd.GetCrmServiceClient();
                        var defs = RetrieveSettingDefinitions(svc);
                        var vals = RetrieveSettingValues(svc);
                        e.Result = new Tuple<List<Entity>, List<Entity>>(defs, vals);
                    },
                    PostWorkCallBack = (e) =>
                    {
                        if (e.Error != null) return;
                        var result = e.Result as Tuple<List<Entity>, List<Entity>>;
                        var targetDefs = result?.Item1 ?? new List<Entity>();
                        var targetVals = result?.Item2 ?? new List<Entity>();

                        // Build value lookup for target
                        var targetValueLookup = new Dictionary<string, string>();
                        var targetDefsByName = targetDefs.ToDictionary(
                            d => d.GetAttributeValue<string>("uniquename") ?? "",
                            d => d.Id);

                        foreach (var val in targetVals)
                        {
                            var defId = val.GetAttributeValue<EntityReference>("settingdefinitionid")?.Id ?? Guid.Empty;
                            var settingValue = val.GetAttributeValue<string>("value") ?? "";
                            var matchingDef = targetDefs.FirstOrDefault(d => d.Id == defId);
                            if (matchingDef != null)
                            {
                                var name = matchingDef.GetAttributeValue<string>("uniquename") ?? "";
                                targetValueLookup[name] = settingValue;
                            }
                        }

                        // Also get defaults for settings with no override
                        var targetDefaultLookup = targetDefs.ToDictionary(
                            d => d.GetAttributeValue<string>("uniquename") ?? "",
                            d => d.GetAttributeValue<string>("defaultvalue") ?? "");

                        var col = new ColumnHeader
                        {
                            Text = localCd.ConnectionName,
                            Tag = localCd,
                            Width = TargetColumnWidth
                        };
                        lvSettings.Columns.Add(col);

                        foreach (ListViewItem item in lvSettings.Items)
                        {
                            var def = item.Tag as Entity;
                            var uniqueName = def?.GetAttributeValue<string>("uniquename") ?? "";
                            var dataType = def?.GetAttributeValue<OptionSetValue>("datatype")?.Value ?? 0;
                            var sourceValue = item.SubItems[3].Text;

                            string targetValue;
                            if (targetValueLookup.ContainsKey(uniqueName))
                                targetValue = FormatValue(targetValueLookup[uniqueName], dataType);
                            else if (targetDefaultLookup.ContainsKey(uniqueName))
                                targetValue = FormatValue(targetDefaultLookup[uniqueName], dataType);
                            else
                                targetValue = "";

                            var sub = item.SubItems.Add(targetValue);

                            // Highlight differences from source
                            if (!string.IsNullOrEmpty(targetValue) && targetValue != sourceValue)
                            {
                                sub.BackColor = Color.LightYellow;
                                sub.ForeColor = Color.DarkRed;
                            }
                            else
                            {
                                ColorValueCell(sub, targetValue, "");
                            }
                        }

                        AutoSizeColumns();
                    }
                });
            }
        }

        public void RemoveTargetColumn(ConnectionDetail cd)
        {
            var col = lvSettings.Columns.Cast<ColumnHeader>()
                .FirstOrDefault(c => c.Tag == cd);
            if (col == null) return;

            var idx = col.Index;
            lvSettings.Columns.Remove(col);
            foreach (ListViewItem item in lvSettings.Items)
            {
                if (item.SubItems.Count > idx)
                    item.SubItems.RemoveAt(idx);
            }
        }

        private List<Entity> RetrieveSettingDefinitions(IOrganizationService service)
        {
            try
            {
                var query = new QueryExpression("settingdefinition")
                {
                    ColumnSet = new ColumnSet(true),
                    Orders = { new OrderExpression("displayname", OrderType.Ascending) }
                };

                var results = new List<Entity>();
                query.PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 };

                while (true)
                {
                    var response = service.RetrieveMultiple(query);
                    results.AddRange(response.Entities);
                    if (!response.MoreRecords) break;
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = response.PagingCookie;
                }

                return results;
            }
            catch
            {
                return new List<Entity>();
            }
        }

        private List<Entity> RetrieveSettingValues(IOrganizationService service)
        {
            try
            {
                var query = new QueryExpression("organizationsetting")
                {
                    ColumnSet = new ColumnSet(true),
                };

                var results = new List<Entity>();
                query.PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 };

                while (true)
                {
                    var response = service.RetrieveMultiple(query);
                    results.AddRange(response.Entities);
                    if (!response.MoreRecords) break;
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = response.PagingCookie;
                }

                return results;
            }
            catch
            {
                return new List<Entity>();
            }
        }

        private string FormatValue(string value, int dataType)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // dataType: 0=String, 1=Number, 2=Boolean, 3=JSON
            if (dataType == 2) // Boolean
            {
                if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                    return "True";
                if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0")
                    return "False";
            }

            return value;
        }

        private void ColorValueCell(ListViewItem.ListViewSubItem sub, string value, string defaultValue)
        {
            if (value == "True")
            {
                sub.BackColor = Color.LightGreen;
                sub.ForeColor = Color.DarkGreen;
            }
            else if (value == "False")
            {
                sub.BackColor = Color.MistyRose;
                sub.ForeColor = Color.DarkRed;
            }
        }

        private void AutoSizeColumns()
        {
            lvSettings.BeginUpdate();
            foreach (ColumnHeader col in lvSettings.Columns)
            {
                // Skip hidden Unique Name column
                if (col == colUniqueName && col.Width == 0) continue;

                // Target columns: keep compact
                if (col.Tag is ConnectionDetail)
                {
                    col.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    var hdrW = TextRenderer.MeasureText(col.Text, lvSettings.Font).Width + 20;
                    if (col.Width < hdrW) col.Width = hdrW;
                    if (col.Width > 120) col.Width = 120;
                    continue;
                }

                col.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                var headerWidth = TextRenderer.MeasureText(col.Text, lvSettings.Font).Width + 20;
                if (col.Width < headerWidth) col.Width = headerWidth;
                if (col.Width > MaxColumnWidth) col.Width = MaxColumnWidth;
                if (col.Width < 60) col.Width = 60;
            }
            lvSettings.EndUpdate();
        }

        private void ApplyCategoryFilter()
        {
            lvSettings.Items.Clear();
            var cat = cmbCategoryFilter.SelectedItem as string;

            IEnumerable<ListViewItem> filtered = allSettingItems;

            if (cat != null && cat != "(All)")
            {
                filtered = filtered.Where(i => i.Text == cat);
            }

            var searchText = txtSearch.Text;
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "Search...")
            {
                filtered = filtered.Where(i =>
                    i.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .Any(s => s.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (chkDifferencesOnly.Checked && lvSettings.Columns.Count > 4)
            {
                filtered = filtered.Where(item =>
                {
                    var sourceVal = item.SubItems[3].Text;
                    for (int i = 4; i < item.SubItems.Count; i++)
                    {
                        if (item.SubItems[i].Text != sourceVal && !string.IsNullOrEmpty(item.SubItems[i].Text))
                            return true;
                    }
                    return false;
                });
            }

            foreach (var item in filtered) lvSettings.Items.Add(item);
        }

        private void cmbCategoryFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyCategoryFilter();
        }

        private void chkDifferencesOnly_CheckedChanged(object sender, EventArgs e)
        {
            ApplyCategoryFilter();
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
            ApplyCategoryFilter();
        }

        private void lvSettings_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == currentColumnOrder)
            {
                lvSettings.Sorting = lvSettings.Sorting == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                currentColumnOrder = e.Column;
                lvSettings.Sorting = SortOrder.Ascending;
            }

            lvSettings.ListViewItemSorter = new ListViewItemComparer(e.Column, lvSettings.Sorting);
        }

        private void tslRefresh_Click(object sender, EventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public void InvokeSyncSelected()
        {
            var selected = lvSettings.SelectedItems.Cast<ListViewItem>()
                .Select(i => i.Tag as Entity)
                .Where(d => d != null)
                .ToList();

            if (!selected.Any())
            {
                MessageBox.Show("Select one or more settings to sync.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RaiseSyncEvent(selected);
        }

        public void InvokeSyncAll()
        {
            // Only sync settings that have differences
            var toSync = new List<Entity>();
            foreach (ListViewItem item in lvSettings.Items)
            {
                var sourceVal = item.SubItems[3].Text;
                for (int i = 4; i < item.SubItems.Count; i++)
                {
                    if (item.SubItems[i].Text != sourceVal && !string.IsNullOrEmpty(item.SubItems[i].Text))
                    {
                        var def = item.Tag as Entity;
                        if (def != null) toSync.Add(def);
                        break;
                    }
                }
            }

            if (!toSync.Any())
            {
                MessageBox.Show("All settings already match the source.", "No Differences",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RaiseSyncEvent(toSync);
        }

        private void RaiseSyncEvent(List<Entity> settingDefinitions)
        {
            // Get source values for the selected settings
            var items = new List<SettingSyncItem>();
            foreach (var def in settingDefinitions)
            {
                var matchingItem = allSettingItems.FirstOrDefault(i => i.Tag == def);
                if (matchingItem == null) continue;

                items.Add(new SettingSyncItem
                {
                    Definition = def,
                    UniqueName = def.GetAttributeValue<string>("uniquename") ?? "",
                    DisplayName = def.GetAttributeValue<string>("displayname") ?? "",
                    SourceValue = matchingItem.SubItems[3].Text
                });
            }

            SyncRequested?.Invoke(this, new SettingSyncRequestedEventArgs { Settings = items });
        }
    }

    public class SettingSyncRequestedEventArgs : EventArgs
    {
        public List<SettingSyncItem> Settings { get; set; } = new List<SettingSyncItem>();
    }

    public class SettingSyncItem
    {
        public Entity Definition { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string SourceValue { get; set; }
    }
}
