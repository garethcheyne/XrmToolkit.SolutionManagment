using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace err403.SolutionManagment.Forms
{
    public class ImportLogViewerForm : Form
    {
        private TabControl tabControl;
        private RichTextBox rtbRawXml;
        private ListView lvComponents;
        private Label lblSummary;
        private Button btnClose;
        private Button btnCopy;

        // Extra controls for mixed-content (error message) mode
        private RichTextBox rtbMessage;
        private ListView lvDependencies;

        public ImportLogViewerForm(string formattedResults, string solutionName)
        {
            InitControls(false);

            Text = string.IsNullOrEmpty(solutionName)
                ? "Import Log Viewer"
                : $"Import Log — {solutionName}";

            PopulateResults(formattedResults);
        }

        /// <summary>
        /// Constructor for displaying a raw async operation error message (mixed text + XML).
        /// </summary>
        public ImportLogViewerForm(string errorMessage, string solutionName, bool isErrorMessage)
        {
            InitControls(isErrorMessage);

            Text = string.IsNullOrEmpty(solutionName)
                ? "Import Error Details"
                : $"Import Error — {solutionName}";

            if (isErrorMessage)
                PopulateErrorMessage(errorMessage);
            else
                PopulateResults(errorMessage);
        }

        private void InitControls(bool errorMessageMode)
        {
            Size = new Size(1000, 650);
            MinimumSize = new Size(750, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowIcon = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F);

            lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 8, 8, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            tabControl = new TabControl { Dock = DockStyle.Fill };

            if (errorMessageMode)
            {
                // --- Message tab ---
                var tabMessage = new TabPage("Message");
                rtbMessage = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    WordWrap = true,
                    Font = new Font("Segoe UI", 9.5F),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Padding = new Padding(8)
                };
                tabMessage.Controls.Add(rtbMessage);
                tabControl.TabPages.Add(tabMessage);

                // --- Missing Dependencies tab ---
                var tabDeps = new TabPage("Missing Dependencies");
                lvDependencies = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Segoe UI", 9F)
                };
                lvDependencies.Columns.Add("Required Component", 250);
                lvDependencies.Columns.Add("Schema Name", 200);
                lvDependencies.Columns.Add("Solution", 140);
                lvDependencies.Columns.Add("Dependent Component", 220);
                lvDependencies.Columns.Add("Resolvable", 80);
                tabDeps.Controls.Add(lvDependencies);
                tabControl.TabPages.Add(tabDeps);

                // --- Raw tab ---
                var tabRaw = new TabPage("Raw Content");
                rtbRawXml = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    WordWrap = false,
                    Font = new Font("Consolas", 9F),
                    BackColor = Color.White
                };
                tabRaw.Controls.Add(rtbRawXml);
                tabControl.TabPages.Add(tabRaw);
            }
            else
            {
                // --- Components tab ---
                var tabComponents = new TabPage("Components");
                lvComponents = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Segoe UI", 9F)
                };
                lvComponents.Columns.Add("Component", 200);
                lvComponents.Columns.Add("Type", 120);
                lvComponents.Columns.Add("Result", 80);
                lvComponents.Columns.Add("Error", 480);
                tabComponents.Controls.Add(lvComponents);
                tabControl.TabPages.Add(tabComponents);

                // --- Raw XML tab ---
                var tabRaw = new TabPage("Raw XML");
                rtbRawXml = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    WordWrap = false,
                    Font = new Font("Consolas", 9F),
                    BackColor = Color.White
                };
                tabRaw.Controls.Add(rtbRawXml);
                tabControl.TabPages.Add(tabRaw);
            }

            // --- Bottom panel ---
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(8)
            };

            btnClose = new Button
            {
                Text = "Close",
                Width = 90,
                Height = 30,
                Dock = DockStyle.Right,
                DialogResult = DialogResult.OK
            };

            btnCopy = new Button
            {
                Text = "Copy All",
                Width = 100,
                Height = 30,
                Dock = DockStyle.Left
            };
            btnCopy.Click += (s, e) =>
            {
                var text = rtbRawXml?.Text ?? rtbMessage?.Text ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    btnCopy.Text = "Copied!";
                    var timer = new Timer { Interval = 1500 };
                    timer.Tick += (s2, e2) => { btnCopy.Text = "Copy All"; timer.Stop(); timer.Dispose(); };
                    timer.Start();
                }
            };

            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(btnCopy);

            Controls.Add(tabControl);
            Controls.Add(lblSummary);
            Controls.Add(pnlBottom);

            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        private void PopulateErrorMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                lblSummary.Text = "No error message available.";
                return;
            }

            // Store full raw content
            rtbRawXml.Text = rawMessage;

            lblSummary.Text = "Import Failed";
            lblSummary.ForeColor = Color.DarkRed;

            // Extract <MissingDependencies>...</MissingDependencies> specifically
            // The error message embeds XML inline with trailing text, so we must
            // extract the exact fragment rather than taking everything from the first <.
            string missingDepsXml = null;
            var mdMatch = Regex.Match(rawMessage, @"<MissingDependencies\b[^>]*>.*?</MissingDependencies>", RegexOptions.Singleline);
            if (mdMatch.Success)
            {
                missingDepsXml = mdMatch.Value;
            }

            // Build the text portion: strip the XML fragment from the message
            string textPart = rawMessage;
            if (missingDepsXml != null)
            {
                textPart = rawMessage.Replace(missingDepsXml, "").Trim();
                // Clean up double-spaces and trailing " , "
                textPart = Regex.Replace(textPart, @"\s*,\s*$", "");
                textPart = Regex.Replace(textPart, @"\s{2,}", " ");
            }

            // Also extract <OrganizationServiceFault> block if present (for Raw Content tab)
            string faultXml = null;
            var faultMatch = Regex.Match(rawMessage, @"<OrganizationServiceFault\b.*?</OrganizationServiceFault>", RegexOptions.Singleline);
            if (faultMatch.Success)
            {
                faultXml = faultMatch.Value;
                // Remove it from the text portion too
                textPart = textPart.Replace(faultXml, "").Trim();
                textPart = Regex.Replace(textPart, @"Detail:\s*$", "").Trim();
            }

            rtbMessage.Text = textPart;

            // Try to parse the MissingDependencies XML
            if (!string.IsNullOrEmpty(missingDepsXml))
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(missingDepsXml);

                    // Pretty-print the XML in the raw tab
                    using (var sw = new System.IO.StringWriter())
                    using (var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented, Indentation = 2 })
                    {
                        doc.WriteTo(xw);
                        xw.Flush();
                        rtbRawXml.Text = textPart + "\n\n" + sw.ToString();
                    }

                    ParseMissingDependencies(doc);
                }
                catch
                {
                    // XML didn't parse — just show everything as text
                }
            }
            else
            {
                // No XML found, hide the dependencies tab
                if (lvDependencies != null)
                {
                    var tabDeps = lvDependencies.Parent as TabPage;
                    if (tabDeps != null) tabControl.TabPages.Remove(tabDeps);
                }
            }
        }

        private void ParseMissingDependencies(XmlDocument doc)
        {
            var depNodes = doc.GetElementsByTagName("MissingDependency");
            if (depNodes.Count == 0)
            {
                var tabDeps = lvDependencies?.Parent as TabPage;
                if (tabDeps != null) tabControl.TabPages.Remove(tabDeps);
                return;
            }

            int resolvable = 0;
            int unresolvable = 0;
            var items = new List<ListViewItem>();

            foreach (XmlNode dep in depNodes)
            {
                var canResolve = dep.Attributes?["canResolveMissingDependency"]?.Value;
                var reqNode = dep.SelectSingleNode("Required");
                var depNode = dep.SelectSingleNode("Dependent");

                var reqDisplay = reqNode?.Attributes?["displayName"]?.Value ?? "(unknown)";
                var reqSchema = reqNode?.Attributes?["schemaName"]?.Value ?? "";
                var reqSolution = reqNode?.Attributes?["solution"]?.Value ?? "";

                // Check for package resolution info
                var packageNode = reqNode?.SelectSingleNode("package");
                if (packageNode != null)
                {
                    var appName = packageNode.Attributes?["appName"]?.Value;
                    var action = packageNode.Attributes?["resolutionAction"]?.Value;
                    if (!string.IsNullOrEmpty(appName))
                        reqSolution += $" → {action}: {appName}";
                }

                var depDisplay = depNode?.Attributes?["displayName"]?.Value ?? "(unknown)";
                var depParent = depNode?.Attributes?["parentDisplayName"]?.Value;
                if (!string.IsNullOrEmpty(depParent))
                    depDisplay = $"{depDisplay} ({depParent})";

                var isResolvable = string.Equals(canResolve, "True", StringComparison.OrdinalIgnoreCase);
                if (isResolvable) resolvable++;
                else unresolvable++;

                var item = new ListViewItem(reqDisplay);
                item.SubItems.Add(reqSchema);
                item.SubItems.Add(reqSolution);
                item.SubItems.Add(depDisplay);
                item.SubItems.Add(isResolvable ? "Yes" : "No");

                item.ForeColor = isResolvable ? Color.DarkGoldenrod : Color.DarkRed;
                items.Add(item);
            }

            lvDependencies.BeginUpdate();
            lvDependencies.Items.AddRange(items.ToArray());
            lvDependencies.EndUpdate();

            var parts = new List<string>();
            parts.Add($"{depNodes.Count} missing dependencies");
            if (unresolvable > 0) parts.Add($"{unresolvable} unresolvable");
            if (resolvable > 0) parts.Add($"{resolvable} resolvable");
            lblSummary.Text = $"Import Failed — {string.Join(", ", parts)}";

            // Auto-select the dependencies tab since that's the most useful
            var depsTab = lvDependencies.Parent as TabPage;
            if (depsTab != null) tabControl.SelectedTab = depsTab;
        }

        private void PopulateResults(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                lblSummary.Text = "No import log data available.";
                return;
            }

            // Show raw XML (pretty-printed)
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                using (var sw = new System.IO.StringWriter())
                using (var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented, Indentation = 2 })
                {
                    doc.WriteTo(xw);
                    xw.Flush();
                    rtbRawXml.Text = sw.ToString();
                }

                ParseComponents(doc);
            }
            catch
            {
                // If it's not valid XML, just show the raw text
                rtbRawXml.Text = xml;
                lblSummary.Text = "Could not parse XML — showing raw content.";
            }
        }

        private void ParseComponents(XmlDocument doc)
        {
            int successCount = 0;
            int failureCount = 0;
            int warningCount = 0;
            var items = new List<ListViewItem>();

            var resultNodes = doc.GetElementsByTagName("result");

            foreach (XmlNode node in resultNodes)
            {
                var name = node.Attributes?["localizedname"]?.Value
                    ?? node.Attributes?["name"]?.Value
                    ?? "(unknown)";
                var typeName = node.Attributes?["typename"]?.Value
                    ?? node.ParentNode?.Name
                    ?? "";
                var result = node.Attributes?["result"]?.Value ?? "";
                var errorText = node.Attributes?["errortext"]?.Value ?? "";
                var errorCode = node.Attributes?["errorcode"]?.Value ?? "";

                var isSuccess = result.Equals("success", StringComparison.OrdinalIgnoreCase);
                var isWarning = result.Equals("warning", StringComparison.OrdinalIgnoreCase);
                var isFailure = result.Equals("failure", StringComparison.OrdinalIgnoreCase);

                if (isSuccess) successCount++;
                else if (isWarning) warningCount++;
                else if (isFailure) failureCount++;

                if (isSuccess && string.IsNullOrEmpty(errorText)) continue;

                var displayResult = isSuccess ? "✓" : isWarning ? "⚠" : isFailure ? "✗" : result;
                var errorDisplay = string.IsNullOrEmpty(errorText) ? "" : errorText;
                if (!string.IsNullOrEmpty(errorCode) && errorCode != "0x0")
                    errorDisplay = $"[{errorCode}] {errorDisplay}";

                var item = new ListViewItem(name);
                item.SubItems.Add(typeName);
                item.SubItems.Add(displayResult);
                item.SubItems.Add(errorDisplay);

                if (isFailure) item.ForeColor = Color.DarkRed;
                else if (isWarning) item.ForeColor = Color.DarkGoldenrod;

                items.Add(item);
            }

            lvComponents.BeginUpdate();
            lvComponents.Items.AddRange(items.ToArray());
            lvComponents.EndUpdate();

            var totalProcessed = successCount + failureCount + warningCount;
            var parts = new List<string>();
            if (successCount > 0) parts.Add($"{successCount} succeeded");
            if (warningCount > 0) parts.Add($"{warningCount} warnings");
            if (failureCount > 0) parts.Add($"{failureCount} failed");

            lblSummary.Text = totalProcessed > 0
                ? $"{string.Join(", ", parts)} — {totalProcessed} components processed"
                : "No component results found in log.";

            if (failureCount > 0) lblSummary.ForeColor = Color.DarkRed;
            else if (warningCount > 0) lblSummary.ForeColor = Color.DarkGoldenrod;
        }
    }
}
