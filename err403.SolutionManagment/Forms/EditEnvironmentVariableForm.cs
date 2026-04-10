using McTools.Xrm.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public partial class EditEnvironmentVariableForm : Form
    {
        private readonly string schemaName;
        private readonly string typeName;
        private readonly Dictionary<ConnectionDetail, TextBox> targetTextBoxes = new Dictionary<ConnectionDetail, TextBox>();
        private readonly Dictionary<ConnectionDetail, string> originalValues = new Dictionary<ConnectionDetail, string>();

        public EditEnvironmentVariableForm(
            string displayName,
            string schemaName,
            string typeName,
            string sourceValue,
            List<TargetVariableInfo> targets)
        {
            InitializeComponent();

            this.schemaName = schemaName;
            this.typeName = typeName;

            lblVariableName.Text = displayName;
            lblType.Text = $"Type: {typeName}  |  Schema: {schemaName}";
            lblSourceHeader.Text = $"Source (Read-only)";

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
                txtSourceValue.Text = sourceValue;
            }

            BuildTargetEditors(targets, isMultiline);
        }

        public Dictionary<ConnectionDetail, string> ChangedValues { get; } = new Dictionary<ConnectionDetail, string>();

        private void BuildTargetEditors(List<TargetVariableInfo> targets, bool isMultiline)
        {
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

            // Resize form height based on target count
            var targetHeight = targets.Count * panelHeight;
            Height = 55 + (isMultiline ? 150 : 62) + targetHeight + 44 + 40;
            if (Height > 800) Height = 800;

            if (isMultiline)
            {
                Width = 800;
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimumSize = new Size(650, 400);
            }
        }

        private void btnCopySourceToAll_Click(object sender, EventArgs e)
        {
            foreach (var kvp in targetTextBoxes)
            {
                if (kvp.Value.Enabled)
                {
                    kvp.Value.Text = txtSourceValue.Text;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate all changed target values
            foreach (var kvp in targetTextBoxes)
            {
                if (!kvp.Value.Enabled) continue;

                var rawValue = kvp.Value.Text;
                // For comparison, minify JSON so whitespace-only changes aren't flagged
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

            ChangedValues.Clear();

            foreach (var kvp in targetTextBoxes)
            {
                if (!kvp.Value.Enabled) continue;

                var rawValue = kvp.Value.Text;
                var saveValue = typeName == "JSON" ? MinifyJson(rawValue) : rawValue;
                var saveOriginal = typeName == "JSON" ? MinifyJson(originalValues[kvp.Key]) : originalValues[kvp.Key];

                if (saveValue != saveOriginal)
                {
                    ChangedValues[kvp.Key] = saveValue;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string ValidateValue(string value, string type)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            switch (type)
            {
                case "JSON":
                    try
                    {
                        JToken.Parse(value);
                    }
                    catch (JsonReaderException ex)
                    {
                        return $"Invalid JSON: {ex.Message}";
                    }
                    return null;

                case "Number":
                    if (!decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        return $"\"{value}\" is not a valid number.";
                    }
                    return null;

                case "Boolean":
                    var lower = value.Trim().ToLowerInvariant();
                    if (lower != "true" && lower != "false" && lower != "yes" && lower != "no"
                        && lower != "1" && lower != "0")
                    {
                        return $"\"{value}\" is not a valid boolean. Expected: true, false, yes, no, 1, or 0.";
                    }
                    return null;

                default:
                    return null;
            }
        }

        private static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            try
            {
                var obj = JToken.Parse(json);
                return obj.ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private static string MinifyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            try
            {
                var obj = JToken.Parse(json);
                return obj.ToString(Formatting.None);
            }
            catch
            {
                return json;
            }
        }
    }

    public class TargetVariableInfo
    {
        public ConnectionDetail Detail { get; set; }
        public string Value { get; set; }
        public bool Exists { get; set; }
        public Guid? DefinitionId { get; set; }
        public Guid? ValueId { get; set; }
    }
}
