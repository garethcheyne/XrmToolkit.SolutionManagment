using McTools.Xrm.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace err403.SolutionManagment.Forms
{
    public class EnvVarEditPanel : DockContent
    {
        private Panel pnlHeader;
        private Label lblVariableName;
        private Label lblType;
        private Panel pnlSource;
        private Label lblSourceHeader;
        private TextBox txtSourceValue;
        private Panel pnlTargets;
        private Panel pnlButtons;
        private Button btnSave;
        private Button btnCopySourceToAll;
        private Panel pnlEmpty;
        private Label lblEmpty;

        private string typeName;
        private readonly Dictionary<ConnectionDetail, TextBox> targetTextBoxes = new Dictionary<ConnectionDetail, TextBox>();
        private readonly Dictionary<ConnectionDetail, string> originalValues = new Dictionary<ConnectionDetail, string>();

        public event EventHandler<EnvVarEditSaveEventArgs> SaveRequested;

        public EnvVarEditPanel()
        {
            InitializeControls();
            ShowEmptyState();
        }

        protected override string GetPersistString() => "EnvVarEditPanel";

        private void InitializeControls()
        {
            Text = "Edit Variable";
            TabText = "Edit Variable";
            CloseButton = false;
            CloseButtonVisible = false;
            DockAreas = DockAreas.DockRight | DockAreas.DockBottom | DockAreas.Float;

            // Empty state
            pnlEmpty = new Panel { Dock = DockStyle.Fill };
            lblEmpty = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Light", 12F),
                Text = "Double-click an environment variable to edit",
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
            lblVariableName = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.White
            };
            lblType = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Color.LightGray
            };
            pnlHeader.Controls.Add(lblType);
            pnlHeader.Controls.Add(lblVariableName);

            // Source
            pnlSource = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(12, 6, 12, 6)
            };
            lblSourceHeader = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.Green,
                Padding = new Padding(0, 0, 0, 4),
                Text = "Source (Read-only)"
            };
            txtSourceValue = new TextBox
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.75F),
                ReadOnly = true
            };
            pnlSource.Controls.Add(txtSourceValue);
            pnlSource.Controls.Add(lblSourceHeader);

            // Targets (scrollable)
            pnlTargets = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };

            // Buttons
            pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(12, 6, 12, 6)
            };
            btnSave = new Button
            {
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(100, 32),
                Text = "Save"
            };
            btnSave.Click += BtnSave_Click;

            btnCopySourceToAll = new Button
            {
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(150, 32),
                Text = "Copy source to all"
            };
            btnCopySourceToAll.Click += BtnCopySourceToAll_Click;

            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnCopySourceToAll);

            Controls.Add(pnlEmpty);
        }

        public void ShowEmptyState()
        {
            pnlHeader.Visible = false;
            pnlSource.Visible = false;
            pnlTargets.Visible = false;
            pnlButtons.Visible = false;

            if (!Controls.Contains(pnlEmpty)) Controls.Add(pnlEmpty);
            pnlEmpty.Visible = true;
        }

        public void LoadVariable(string displayName, string schemaName, string typeName,
            string sourceValue, List<TargetVariableInfo> targets, ListViewItem item)
        {
            this.typeName = typeName;
            targetTextBoxes.Clear();
            originalValues.Clear();
            pnlTargets.Controls.Clear();

            // Remove all, rebuild layout
            Controls.Clear();
            pnlEmpty.Visible = false;

            lblVariableName.Text = displayName;
            lblType.Text = $"Type: {typeName}  |  Schema: {schemaName}";

            bool isMultiline = typeName == "JSON";
            if (isMultiline)
            {
                txtSourceValue.Multiline = true;
                txtSourceValue.ScrollBars = ScrollBars.Both;
                txtSourceValue.WordWrap = false;
                txtSourceValue.AcceptsReturn = true;
                pnlSource.Height = 150;
                txtSourceValue.Text = FormatJson(sourceValue);
            }
            else
            {
                txtSourceValue.Multiline = false;
                txtSourceValue.ScrollBars = ScrollBars.None;
                pnlSource.Height = 62;
                txtSourceValue.Text = sourceValue;
            }

            int panelHeight = isMultiline ? 160 : 62;

            foreach (var target in targets)
            {
                var panel = new Panel
                {
                    Dock = DockStyle.Top,
                    Padding = new Padding(12, 6, 12, 6),
                    Height = panelHeight
                };

                var lbl = new Label
                {
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    ForeColor = target.Exists ? Color.DarkBlue : Color.Gray,
                    Padding = new Padding(0, 0, 0, 4),
                    Text = target.Detail.ConnectionName + (target.Exists ? "" : "  (not found on target)")
                };

                var txt = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9.75F),
                    Text = isMultiline ? FormatJson(target.Value ?? "") : (target.Value ?? ""),
                    Enabled = target.Exists,
                    Multiline = isMultiline,
                    ScrollBars = isMultiline ? ScrollBars.Both : ScrollBars.None,
                    WordWrap = false,
                    AcceptsReturn = isMultiline
                };

                panel.Controls.Add(txt);
                panel.Controls.Add(lbl);
                pnlTargets.Controls.Add(panel);
                pnlTargets.Controls.SetChildIndex(panel, 0);

                targetTextBoxes[target.Detail] = txt;
                originalValues[target.Detail] = target.Value ?? "";
            }

            // Reorder so first target is at top
            var panels = pnlTargets.Controls.OfType<Panel>().Reverse().ToList();
            for (int i = 0; i < panels.Count; i++)
            {
                pnlTargets.Controls.SetChildIndex(panels[i], i);
            }

            // Show edit layout
            Controls.Add(pnlTargets);
            Controls.Add(pnlButtons);
            Controls.Add(pnlSource);
            Controls.Add(pnlHeader);

            pnlHeader.Visible = true;
            pnlSource.Visible = true;
            pnlTargets.Visible = true;
            pnlButtons.Visible = true;

            // Store the list view item for the save callback
            Tag = item;

            Activate();
        }

        private void BtnCopySourceToAll_Click(object sender, EventArgs e)
        {
            foreach (var kvp in targetTextBoxes)
            {
                if (kvp.Value.Enabled)
                {
                    kvp.Value.Text = txtSourceValue.Text;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate
            foreach (var kvp in targetTextBoxes)
            {
                if (!kvp.Value.Enabled) continue;

                var rawValue = kvp.Value.Text;
                var compareValue = typeName == "JSON" ? MinifyJson(rawValue) : rawValue;
                var compareOriginal = typeName == "JSON" ? MinifyJson(originalValues[kvp.Key]) : originalValues[kvp.Key];

                if (compareValue == compareOriginal) continue;

                string error = ValidateValue(rawValue, typeName);
                if (error != null)
                {
                    MessageBox.Show(
                        $"Invalid value for target \"{kvp.Key.ConnectionName}\":\n\n{error}",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    kvp.Value.Focus();
                    return;
                }
            }

            var changedValues = new Dictionary<ConnectionDetail, string>();

            foreach (var kvp in targetTextBoxes)
            {
                if (!kvp.Value.Enabled) continue;

                var rawValue = kvp.Value.Text;
                var saveValue = typeName == "JSON" ? MinifyJson(rawValue) : rawValue;
                var saveOriginal = typeName == "JSON" ? MinifyJson(originalValues[kvp.Key]) : originalValues[kvp.Key];

                if (saveValue != saveOriginal)
                {
                    changedValues[kvp.Key] = saveValue;
                }
            }

            if (!changedValues.Any())
            {
                ShowEmptyState();
                return;
            }

            SaveRequested?.Invoke(this, new EnvVarEditSaveEventArgs
            {
                ChangedValues = changedValues,
                Item = Tag as ListViewItem
            });

            ShowEmptyState();
        }

        private static string ValidateValue(string value, string type)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            switch (type)
            {
                case "JSON":
                    try { JToken.Parse(value); }
                    catch (JsonReaderException ex) { return $"Invalid JSON: {ex.Message}"; }
                    return null;
                case "Number":
                    if (!decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                        return $"\"{value}\" is not a valid number.";
                    return null;
                case "Boolean":
                    var lower = value.Trim().ToLowerInvariant();
                    if (lower != "true" && lower != "false" && lower != "yes" && lower != "no"
                        && lower != "1" && lower != "0")
                        return $"\"{value}\" is not a valid boolean. Expected: true, false, yes, no, 1, or 0.";
                    return null;
                default:
                    return null;
            }
        }

        private static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            try { return JToken.Parse(json).ToString(Formatting.Indented); }
            catch { return json; }
        }

        private static string MinifyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            try { return JToken.Parse(json).ToString(Formatting.None); }
            catch { return json; }
        }
    }

    public class EnvVarEditSaveEventArgs : EventArgs
    {
        public Dictionary<ConnectionDetail, string> ChangedValues { get; set; }
        public ListViewItem Item { get; set; }
    }
}
