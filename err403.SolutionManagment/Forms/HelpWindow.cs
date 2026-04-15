using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    /// <summary>
    /// Standalone pop-out window that shows only the Help / Documentation viewer.
    /// </summary>
    public class HelpWindow : Form
    {
        private WebView2 webView;

        public HelpWindow()
        {
            Text = "Solution Management — Help";
            Width = 1000;
            Height = 750;
            StartPosition = FormStartPosition.CenterParent;
            Icon = null;

            webView = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(webView);

            Load += (s, e) => InitWebView();
        }

        private async void InitWebView()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "err403.SolutionManagment", "WebView2Help");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.ZoomFactor = 0.8;

                var html = LoadHtml();
                // Inject help-only flag before the closing </head> so it's set before React boots
                html = html.Replace("</head>",
                    "<script>window.__helpOnly=true;</script></head>");

                webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Failed to open help window.\n\n{ex.Message}",
                    "Help Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private string LoadHtml()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "err403.SolutionManagment.Resources.WebUI.html";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            var dir = Path.GetDirectoryName(assembly.Location);
            var filePath = Path.Combine(dir, "Resources", "WebUI.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            return "<html><body><h3>WebUI.html not found</h3></body></html>";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
