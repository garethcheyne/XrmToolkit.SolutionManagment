using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            LoadChangelog();
            LoadPowerPlatformInfo();
        }

        private void LoadChangelog()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CHANGELOG.md"))
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var markdown = reader.ReadToEnd();
                        wbChangelog.DocumentText = MarkdownToHtml(markdown);
                    }
                }
            }
            catch
            {
                // If resource not found, leave the browser blank
            }
        }

        private static string MarkdownToHtml(string md)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><style>");
            sb.Append("body{font-family:'Segoe UI',sans-serif;font-size:13px;margin:12px;color:#222}");
            sb.Append("h1{font-size:18px;border-bottom:1px solid #ccc;padding-bottom:4px}");
            sb.Append("h2{font-size:15px;margin-top:16px;color:#333}");
            sb.Append("h3{font-size:13px;margin-top:12px;color:#555}");
            sb.Append("ul{margin:4px 0 4px 20px;padding:0}");
            sb.Append("li{margin:2px 0}");
            sb.Append("hr{border:none;border-top:1px solid #ddd;margin:16px 0}");
            sb.Append("</style></head><body>");

            var lines = md.Split('\n');
            bool inList = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                // Horizontal rule
                if (Regex.IsMatch(line, @"^---+\s*$"))
                {
                    if (inList) { sb.Append("</ul>"); inList = false; }
                    sb.Append("<hr/>");
                    continue;
                }

                // Headers
                if (line.StartsWith("### "))
                {
                    if (inList) { sb.Append("</ul>"); inList = false; }
                    sb.Append("<h3>").Append(Inline(line.Substring(4))).Append("</h3>");
                    continue;
                }
                if (line.StartsWith("## "))
                {
                    if (inList) { sb.Append("</ul>"); inList = false; }
                    sb.Append("<h2>").Append(Inline(line.Substring(3))).Append("</h2>");
                    continue;
                }
                if (line.StartsWith("# ") || Regex.IsMatch(line, @"^=+\s*$"))
                {
                    if (inList) { sb.Append("</ul>"); inList = false; }
                    if (line.StartsWith("# "))
                        sb.Append("<h1>").Append(Inline(line.Substring(2))).Append("</h1>");
                    // Skip === underline headers (the previous line was already text)
                    continue;
                }

                // List items
                var listMatch = Regex.Match(line, @"^(\s*)- (.*)");
                if (listMatch.Success)
                {
                    if (!inList) { sb.Append("<ul>"); inList = true; }
                    sb.Append("<li>").Append(Inline(listMatch.Groups[2].Value)).Append("</li>");
                    continue;
                }

                // Blank line
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (inList) { sb.Append("</ul>"); inList = false; }
                    continue;
                }

                // Plain text
                if (inList) { sb.Append("</ul>"); inList = false; }
                sb.Append("<p>").Append(Inline(line)).Append("</p>");
            }

            if (inList) sb.Append("</ul>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static string Inline(string text)
        {
            // Bold **text**
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            // Italic *text*
            text = Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
            return text;
        }

        private void LoadPowerPlatformInfo()
        {
            var hasToken = AppCode.EnvironmentIdResolver.HasGdsToken;
            var status = hasToken
                ? "<span style='color:green;font-weight:bold'>✓ Authenticated</span>"
                : "<span style='color:orange;font-weight:bold'>⚠ Not authenticated</span>";

            var html = new StringBuilder();
            html.Append("<html><head><style>");
            html.Append("body{font-family:'Segoe UI',sans-serif;font-size:13px;margin:16px;color:#222}");
            html.Append("h2{font-size:16px;color:#742774;margin-top:0}");
            html.Append("h3{font-size:14px;margin-top:16px;color:#333}");
            html.Append("ul{margin:6px 0 6px 20px;padding:0}");
            html.Append("li{margin:4px 0}");
            html.Append(".status{font-size:14px;margin:12px 0;padding:8px 12px;background:#f5f5f5;border-radius:4px}");
            html.Append(".note{font-size:12px;color:#666;margin-top:16px;padding:8px;background:#fefce8;border-left:3px solid #d97706;border-radius:2px}");
            html.Append("</style></head><body>");

            html.Append("<h2>Power Platform Authentication</h2>");

            html.Append("<div class='status'>Current status: ").Append(status).Append("</div>");

            html.Append("<h3>Why is a separate sign-in needed?</h3>");
            html.Append("<p>XrmToolBox connects to the <strong>Dataverse API</strong> for solution management. ");
            html.Append("However, features like opening solutions or flows in the Power Platform Maker Portal ");
            html.Append("require the <strong>Environment ID</strong>, which is only available from the ");
            html.Append("<strong>Global Discovery Service</strong> — a separate Microsoft API that requires its own authentication token.</p>");

            html.Append("<p>For federated/SSO accounts (like yours), the Dataverse credentials cannot be reused ");
            html.Append("for the Discovery Service, so a one-time browser sign-in is required.</p>");

            html.Append("<h3>What does this enable?</h3>");
            html.Append("<ul>");
            html.Append("<li><strong>Solutions tab:</strong> Right-click any solution → <em>Open in Maker Portal</em> — ");
            html.Append("opens the solution directly in make.powerapps.com</li>");
            html.Append("<li><strong>Cloud Flows tab:</strong> Right-click any flow → <em>Open in Power Automate</em> — ");
            html.Append("opens the flow in the Power Automate designer</li>");
            html.Append("<li><strong>Environment ID resolution:</strong> Maps your Dataverse org URL to the ");
            html.Append("Power Platform Environment ID via the Global Discovery Service</li>");
            html.Append("</ul>");

            html.Append("<h3>Security</h3>");
            html.Append("<ul>");
            html.Append("<li>The token is encrypted using <strong>Windows DPAPI</strong> (CurrentUser scope) and stored locally</li>");
            html.Append("<li>Only <em>your</em> Windows account can decrypt it — other users on the machine cannot</li>");
            html.Append("<li>The token is scoped to the Global Discovery Service only — it cannot modify your Dataverse data</li>");
            html.Append("<li>Tokens expire automatically (typically ~1 hour) and are refreshed silently when possible</li>");
            html.Append("</ul>");

            html.Append("<div class='note'>💡 Click the <strong>Power Platform Auth</strong> button in the toolbar to sign in. ");
            html.Append("Once authenticated, the button changes to <strong>Authenticated ✓</strong>. ");
            html.Append("The token persists across sessions so you usually only need to sign in once.</div>");

            html.Append("</body></html>");

            wbPowerPlatform.DocumentText = html.ToString();
        }

        private void lnkOriginalRepo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/MscrmTools/DamSim.SolutionTransferTool",
                UseShellExecute = true
            });
        }

        private void lnkForkRepo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/garethcheyne/SolutionTransferTool",
                UseShellExecute = true
            });
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
