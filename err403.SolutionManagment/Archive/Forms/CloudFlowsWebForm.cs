using McTools.Xrm.Connection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using XrmToolBox.Extensibility;

namespace err403.SolutionManagment.Forms
{
    public class CloudFlowsWebForm : DockContent
    {
        private WebView2 webView;
        private bool webViewReady;
        private readonly Queue<string> pendingScripts = new Queue<string>();
        private string sourceEnvironmentId;
        private readonly Dictionary<string, string> targetEnvironmentIds = new Dictionary<string, string>();
        private const string DefaultSolutionId = "fd140aaf-4df4-11dd-bd17-0019b9312238";

        // Keep flow data for the bridge
        private List<FlowData> currentFlows = new List<FlowData>();

        public event EventHandler RefreshRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> ActivateRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> DeactivateRequested;

        public CloudFlowsWebForm()
        {
            Text = "Cloud Flows";
            CloseButtonVisible = false;

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(webView);
            InitWebView();
        }

        private async void InitWebView()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "err403.SolutionManagment", "WebView2");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Load embedded HTML
                var html = LoadEmbeddedHtml();
                webView.CoreWebView2.NavigateToString(html);

                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    webViewReady = true;
                    while (pendingScripts.Count > 0)
                    {
                        webView.CoreWebView2.ExecuteScriptAsync(pendingScripts.Dequeue());
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"WebView2 initialization failed. Make sure WebView2 Runtime is installed.\n\n{ex.Message}",
                    "WebView2 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string LoadEmbeddedHtml()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "err403.SolutionManagment.Resources.CloudFlows.html";

            // Try embedded resource first
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

            // Fallback: load from file next to assembly
            var dir = Path.GetDirectoryName(assembly.Location);
            var filePath = Path.Combine(dir, "Resources", "CloudFlows.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            // Last resort: look relative to the project
            var projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "CloudFlows.html");
            if (File.Exists(projectPath))
                return File.ReadAllText(projectPath);

            return "<html><body><h3>CloudFlows.html not found</h3></body></html>";
        }

        private void ExecuteScript(string script)
        {
            if (webViewReady && webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                pendingScripts.Enqueue(script);
            }
        }

        // ── Bridge: JS → C# ──
        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.WebMessageAsJson);
                if (msg == null || !msg.ContainsKey("action")) return;

                var action = msg["action"].ToString();

                switch (action)
                {
                    case "activate":
                        RaiseActivateEvent(true);
                        break;

                    case "deactivate":
                        RaiseActivateEvent(false);
                        break;

                    case "openInMaker":
                        HandleOpenInMaker(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"WebView message error: {ex.Message}");
            }
        }

        private void HandleOpenInMaker(Dictionary<string, object> msg)
        {
            string connectionName = null;
            if (msg.ContainsKey("connectionName") && msg["connectionName"] != null)
                connectionName = msg["connectionName"].ToString();

            string envId = null;
            if (connectionName != null)
            {
                targetEnvironmentIds.TryGetValue(connectionName, out envId);
            }
            else
            {
                envId = sourceEnvironmentId;
            }

            if (string.IsNullOrEmpty(envId))
            {
                MessageBox.Show(this, "Environment ID not available. Click 'Power Platform Auth' first.",
                    "Authentication Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (msg.ContainsKey("flowIds"))
            {
                var flowIds = JsonConvert.DeserializeObject<List<string>>(msg["flowIds"].ToString());
                foreach (var id in flowIds)
                {
                    var url = $"https://make.powerapps.com/environments/{envId}/solutions/{DefaultSolutionId}/objects/cloudflows/{id}/view";
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Could not open browser:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private async void RaiseActivateEvent(bool activate)
        {
            try
            {
                var json = await webView.CoreWebView2.ExecuteScriptAsync("window.getSelectedFlows()");
                // ExecuteScriptAsync returns a JSON-encoded string, so we need to unwrap it
                var inner = JsonConvert.DeserializeObject<string>(json);
                var selected = JsonConvert.DeserializeObject<List<SelectedFlowInfo>>(inner);

                if (selected == null || selected.Count == 0)
                {
                    MessageBox.Show(this,
                        $"Select one or more cloud flows to {(activate ? "activate" : "deactivate")}.",
                        "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var args = new FlowActivateRequestedEventArgs { Activate = activate };
                foreach (var sf in selected)
                {
                    var flowData = currentFlows.FirstOrDefault(f => f.WorkflowId == sf.WorkflowId);
                    args.Flows.Add(new FlowActionItem
                    {
                        FlowName = sf.Name,
                        WorkflowId = Guid.Parse(sf.WorkflowId),
                        Workflow = flowData?.Entity,
                        Item = null // No ListViewItem in WebView2 mode
                    });
                }

                if (activate)
                    ActivateRequested?.Invoke(this, args);
                else
                    DeactivateRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"RaiseActivateEvent error: {ex.Message}");
            }
        }

        // ── Public API (same surface as CloudFlowsForm) ──

        public void SetSourceEnvironment(string environmentId)
        {
            sourceEnvironmentId = environmentId;
        }

        public void DisplayCloudFlows(List<Entity> workflows)
        {
            currentFlows.Clear();

            if (workflows == null)
            {
                ExecuteScript("window.loadFlows('[]')");
                return;
            }

            foreach (var wf in workflows)
            {
                currentFlows.Add(new FlowData
                {
                    Name = wf.GetAttributeValue<string>("name") ?? "(unnamed)",
                    WorkflowId = wf.Id.ToString(),
                    Type = GetCategoryText(wf.GetAttributeValue<OptionSetValue>("category")?.Value ?? 0),
                    Status = GetStatusText(wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0),
                    StateCode = wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                    StatusCode = wf.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0,
                    Solution = wf.GetAttributeValue<AliasedValue>("solution.friendlyname")?.Value as string ?? "",
                    Owner = wf.GetAttributeValue<EntityReference>("ownerid")?.Name ?? "",
                    ModifiedOn = wf.GetAttributeValue<DateTime?>("modifiedon")?.ToString("yy-MM-dd HH:mm") ?? "",
                    Entity = wf
                });
            }

            var json = JsonConvert.SerializeObject(currentFlows.Select(f => new
            {
                name = f.Name,
                workflowId = f.WorkflowId,
                type = f.Type,
                status = f.Status,
                stateCode = f.StateCode,
                solution = f.Solution,
                owner = f.Owner,
                modifiedOn = f.ModifiedOn
            }));

            ExecuteScript($"window.loadFlows({JsonConvert.SerializeObject(json)})");
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
                        string envId = AppCode.EnvironmentIdResolver.Resolve(cd);

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
                        e.Result = new Tuple<ConnectionDetail, List<Entity>, string>(cd, flows, envId);
                    },
                    PostWorkCallBack = (e) =>
                    {
                        if (e.Error != null) return;

                        var result = (Tuple<ConnectionDetail, List<Entity>, string>)e.Result;
                        var tcd = result.Item1;
                        var targetFlows = result.Item2;
                        var resolvedEnvId = result.Item3;

                        if (!string.IsNullOrEmpty(resolvedEnvId))
                            targetEnvironmentIds[tcd.ConnectionName] = resolvedEnvId;

                        ExecuteScript($"window.addTargetColumn({JsonConvert.SerializeObject(tcd.ConnectionName)})");

                        var targetData = targetFlows.Select(f => new
                        {
                            name = f.GetAttributeValue<string>("name"),
                            status = GetStatusText(f.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0),
                            stateCode = f.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                            statusCode = f.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0
                        });

                        var json = JsonConvert.SerializeObject(targetData);
                        ExecuteScript($"window.setTargetFlowStatus({JsonConvert.SerializeObject(tcd.ConnectionName)}, {JsonConvert.SerializeObject(json)})");
                    }
                });
            }
        }

        public void RemoveTargetColumn(ConnectionDetail detail)
        {
            targetEnvironmentIds.Remove(detail.ConnectionName);
            ExecuteScript($"window.removeTargetColumn({JsonConvert.SerializeObject(detail.ConnectionName)})");
        }

        /// <summary>
        /// Update a single flow's cell in a target column after activate/deactivate.
        /// Replaces direct ListView manipulation from SolutionTransferTool.cs.
        /// </summary>
        public void UpdateFlowCellStatus(string connectionName, string flowName, bool activated, bool isMatch, bool isError = false, string errorWarning = null)
        {
            var status = activated ? "On" : "Off";
            ExecuteScript($"window.updateFlowCellStatus(" +
                $"{JsonConvert.SerializeObject(connectionName)}, " +
                $"{JsonConvert.SerializeObject(flowName)}, " +
                $"{JsonConvert.SerializeObject(status)}, " +
                $"{(isMatch ? "true" : "false")}, " +
                $"{(isError ? "true" : "false")}, " +
                $"{JsonConvert.SerializeObject(errorWarning ?? "")})");
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

        // ── Helpers ──

        private static string GetStatusText(int stateCode)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                webView?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ── Internal DTOs ──

        private class FlowData
        {
            public string Name { get; set; }
            public string WorkflowId { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public int StateCode { get; set; }
            public int StatusCode { get; set; }
            public string Solution { get; set; }
            public string Owner { get; set; }
            public string ModifiedOn { get; set; }
            [JsonIgnore]
            public Entity Entity { get; set; }
        }

        private class SelectedFlowInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("workflowId")]
            public string WorkflowId { get; set; }
            [JsonProperty("stateCode")]
            public int StateCode { get; set; }
        }
    }
}
