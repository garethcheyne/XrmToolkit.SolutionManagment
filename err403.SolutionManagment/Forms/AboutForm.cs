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
