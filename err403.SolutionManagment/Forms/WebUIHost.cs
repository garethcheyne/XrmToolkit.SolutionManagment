using McTools.Xrm.Connection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using XrmToolBox.Extensibility;

namespace err403.SolutionManagment.Forms
{
    public class WebUIHost : UserControl
    {
        private WebView2 webView;
        private bool webViewReady;
        private readonly Queue<string> pendingScripts = new Queue<string>();
        private string sourceEnvironmentId;
        private readonly Dictionary<string, string> targetEnvironmentIds = new Dictionary<string, string>();
        private const string DefaultSolutionId = "fd140aaf-4df4-11dd-bd17-0019b9312238";

        // Flow data for bridge
        private List<FlowDataDto> currentFlows = new List<FlowDataDto>();

        // Solutions events
        public event EventHandler<StartTransferEventArgs> StartTransferRequested;
        public event EventHandler LoadSolutionsRequested;
        public event EventHandler<SolutionActionEventArgs> TransferSolutionsRequested;
        public event EventHandler<SolutionActionEventArgs> TransferWithSettingsRequested;
        public event EventHandler<SolutionActionEventArgs> ExportSolutionsRequested;
        public event EventHandler<SolutionActionEventArgs> RemoveFromSourceRequested;
        public event EventHandler ImportFromFileRequested;
        public event EventHandler<SolutionActionEventArgs> RemoveFromTargetsRequested;
        public event EventHandler SwitchOrgsRequested;
        public event EventHandler FindMissingDepsRequested;
        public event EventHandler<StringEventArgs> OpenSolutionInMakerRequested;

        // Environment Variables events
        public event EventHandler RefreshEnvVarsRequested;
        public event EventHandler<EnvVarTransferEventArgs> TransferEnvVarsRequested;
        public event EventHandler<EnvVarEditEventArgs> EditEnvVarRequested;
        public event EventHandler<EnvVarSaveEventArgs> SaveEnvVarRequested;

        // Cloud Flows events
        public event EventHandler RefreshRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> ActivateRequested;
        public event EventHandler<FlowActivateRequestedEventArgs> DeactivateRequested;

        // Platform Settings events
        public event EventHandler RefreshSettingsRequested;
        public event EventHandler<SettingSyncEventArgs> SyncSettingsRequested;

        // Tab change event
        public event EventHandler<TabChangedEventArgs> TabChanged;

        // Connection events
        public event EventHandler AddTargetRequested;
        public event EventHandler<RemoveTargetEventArgs> RemoveTargetRequested;

        // Progress events
        public event EventHandler<ProgressActionEventArgs> ProgressActionRequested;
        public event EventHandler RetryRequested;

        // Settings persistence events
        public event EventHandler<StringEventArgs> SavePluginSettingsRequested;

        // Auth events
        public event EventHandler RefreshTokenRequested;
        public event EventHandler AuthenticateGdsRequested;

        public WebUIHost()
        {
            Dock = DockStyle.Fill;

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
                webView.CoreWebView2.Settings.AreDevToolsEnabled =
#if DEBUG
                    true;
#else
                    false;
#endif
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.ZoomFactor = 0.8;

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var html = LoadEmbeddedHtml("err403.SolutionManagment.Resources.WebUI.html");
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
                    $"WebView2 initialization failed. Ensure WebView2 Runtime is installed.\n\n{ex.Message}",
                    "WebView2 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string LoadEmbeddedHtml(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
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

            // Fallback: file next to assembly
            var dir = Path.GetDirectoryName(assembly.Location);
            var filePath = Path.Combine(dir, "Resources", "WebUI.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            return "<html><body><h3>WebUI.html not found</h3></body></html>";
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
                var raw = e.TryGetWebMessageAsString();
                var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw);
                if (msg == null || !msg.ContainsKey("action")) return;

                var action = msg["action"].ToString();

                switch (action)
                {
                    case "activateFlows":
                        RaiseFlowEvent(msg, true);
                        break;

                    case "deactivateFlows":
                        RaiseFlowEvent(msg, false);
                        break;

                    case "refreshFlows":
                        RefreshRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "openFlowInMaker":
                        HandleOpenFlowInMaker(msg);
                        break;

                    case "tabChanged":
                        if (msg.ContainsKey("tab"))
                            TabChanged?.Invoke(this, new TabChangedEventArgs { Tab = msg["tab"].ToString() });
                        break;

                    case "addTarget":
                        AddTargetRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "removeTarget":
                        if (msg.ContainsKey("connectionName"))
                            RemoveTargetRequested?.Invoke(this, new RemoveTargetEventArgs { ConnectionName = msg["connectionName"].ToString() });
                        break;

                    case "downloadLog":
                    case "viewMessage":
                    case "downloadSolution":
                        if (msg.ContainsKey("id"))
                            ProgressActionRequested?.Invoke(this, new ProgressActionEventArgs { Action = action, ItemId = msg["id"].ToString() });
                        break;

                    case "retryTransfer":
                        RetryRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "refreshToken":
                        RefreshTokenRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "savePluginSettings":
                        if (msg.ContainsKey("settings"))
                            SavePluginSettingsRequested?.Invoke(this, new StringEventArgs { Value = msg["settings"].ToString() });
                        break;

                    case "authenticateGds":
                        AuthenticateGdsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "openUrl":
                        if (msg.ContainsKey("url"))
                        {
                            var url = msg["url"].ToString();
                            System.Diagnostics.Trace.WriteLine($"[WebUIHost] openUrl: {url}");
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                            catch (System.Exception ex) { System.Diagnostics.Trace.WriteLine($"[WebUIHost] openUrl error: {ex.Message}"); }
                        }
                        break;

                    // Solutions
                    case "loadSolutions":
                        LoadSolutionsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "transferSolutions":
                        RaiseSolutionEvent(TransferSolutionsRequested, msg);
                        break;

                    case "startTransfer":
                        if (msg.ContainsKey("solutions") && msg.ContainsKey("settings"))
                        {
                            var solutions = JsonConvert.DeserializeObject<List<SolutionActionItem>>(msg["solutions"].ToString());
                            var transferSettings = JsonConvert.DeserializeObject<Services.SolutionTransferService.TransferSettings>(msg["settings"].ToString());
                            StartTransferRequested?.Invoke(this, new StartTransferEventArgs
                            {
                                Solutions = solutions,
                                Settings = transferSettings
                            });
                        }
                        break;

                    case "removeFromSource":
                        RaiseSolutionEvent(RemoveFromSourceRequested, msg);
                        break;

                    case "exportToFile":
                        RaiseSolutionEvent(ExportSolutionsRequested, msg);
                        break;

                    case "transferWithSettings":
                        RaiseSolutionEvent(TransferWithSettingsRequested, msg);
                        break;

                    case "exportSolutions":
                        RaiseSolutionEvent(ExportSolutionsRequested, msg);
                        break;

                    case "importFromFile":
                        ImportFromFileRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "removeFromTargets":
                        RaiseSolutionEvent(RemoveFromTargetsRequested, msg);
                        break;

                    case "switchOrgs":
                        SwitchOrgsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "findMissingDeps":
                        FindMissingDepsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "openSolutionInMaker":
                        if (msg.ContainsKey("solutionId"))
                            OpenSolutionInMakerRequested?.Invoke(this, new StringEventArgs { Value = msg["solutionId"].ToString() });
                        break;

                    // Environment Variables
                    case "refreshEnvVars":
                        RefreshEnvVarsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "transferEnvVars":
                        if (msg.ContainsKey("items"))
                        {
                            var items = JsonConvert.DeserializeObject<List<WebEnvVarTransferItem>>(msg["items"].ToString());
                            TransferEnvVarsRequested?.Invoke(this, new EnvVarTransferEventArgs { Items = items });
                        }
                        break;

                    case "editEnvVar":
                        EditEnvVarRequested?.Invoke(this, new EnvVarEditEventArgs
                        {
                            SchemaName = msg.ContainsKey("schemaName") ? msg["schemaName"].ToString() : "",
                            DisplayName = msg.ContainsKey("displayName") ? msg["displayName"].ToString() : "",
                            TypeName = msg.ContainsKey("typeName") ? msg["typeName"].ToString() : "",
                            SourceValue = msg.ContainsKey("sourceValue") ? msg["sourceValue"].ToString() : "",
                            DefinitionId = msg.ContainsKey("definitionId") ? msg["definitionId"].ToString() : ""
                        });
                        break;

                    case "saveEnvVar":
                        if (msg.ContainsKey("changedValues"))
                        {
                            var changed = JsonConvert.DeserializeObject<Dictionary<string, string>>(msg["changedValues"].ToString());
                            SaveEnvVarRequested?.Invoke(this, new EnvVarSaveEventArgs
                            {
                                SchemaName = msg.ContainsKey("schemaName") ? msg["schemaName"].ToString() : "",
                                DisplayName = msg.ContainsKey("displayName") ? msg["displayName"].ToString() : "",
                                ChangedValues = changed
                            });
                        }
                        break;

                    // Platform Settings
                    case "refreshSettings":
                        RefreshSettingsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case "syncSettings":
                        if (msg.ContainsKey("items"))
                        {
                            var syncItems = JsonConvert.DeserializeObject<List<WebSettingSyncItem>>(msg["items"].ToString());
                            var all = msg.ContainsKey("all") && bool.TryParse(msg["all"].ToString(), out var a) && a;
                            SyncSettingsRequested?.Invoke(this, new SettingSyncEventArgs { Items = syncItems, All = all });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"WebView message error: {ex.Message}");
            }
        }

        private void HandleOpenFlowInMaker(Dictionary<string, object> msg)
        {
            string connectionName = null;
            if (msg.ContainsKey("connectionName") && msg["connectionName"] != null)
                connectionName = msg["connectionName"].ToString();

            string envId = null;
            if (connectionName != null)
                targetEnvironmentIds.TryGetValue(connectionName, out envId);
            else
                envId = sourceEnvironmentId;

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
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Could not open browser:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void RaiseSolutionEvent(EventHandler<SolutionActionEventArgs> handler, Dictionary<string, object> msg)
        {
            if (handler == null || !msg.ContainsKey("solutions")) return;
            var solutions = JsonConvert.DeserializeObject<List<SolutionActionItem>>(msg["solutions"].ToString());
            handler.Invoke(this, new SolutionActionEventArgs { Solutions = solutions });
        }

        public async System.Threading.Tasks.Task<string> GetSelectedSolutions()
        {
            var json = await webView.CoreWebView2.ExecuteScriptAsync("window.bridge.getSelectedSolutions?.() ?? '[]'");
            return JsonConvert.DeserializeObject<string>(json) ?? "[]";
        }

        private void RaiseFlowEvent(Dictionary<string, object> msg, bool activate)
        {
            if (!msg.ContainsKey("flows")) return;
            var flows = JsonConvert.DeserializeObject<List<SelectedFlowDto>>(msg["flows"].ToString());
            if (flows == null || flows.Count == 0) return;

            var args = new FlowActivateRequestedEventArgs { Activate = activate };
            foreach (var sf in flows)
            {
                var flowData = currentFlows.FirstOrDefault(f => f.WorkflowId == sf.WorkflowId);
                args.Flows.Add(new FlowActionItem
                {
                    FlowName = sf.Name,
                    WorkflowId = Guid.Parse(sf.WorkflowId),
                    Workflow = flowData?.Entity,
                    Item = null
                });
            }

            if (activate)
                ActivateRequested?.Invoke(this, args);
            else
                DeactivateRequested?.Invoke(this, args);
        }

        private async void RaiseFlowActivateEvent(bool activate)
        {
            try
            {
                var json = await webView.CoreWebView2.ExecuteScriptAsync("window.bridge.getSelectedFlows()");
                var inner = JsonConvert.DeserializeObject<string>(json);
                var selected = JsonConvert.DeserializeObject<List<SelectedFlowDto>>(inner);

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
                        Item = null
                    });
                }

                if (activate)
                    ActivateRequested?.Invoke(this, args);
                else
                    DeactivateRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"RaiseFlowActivateEvent error: {ex.Message}");
            }
        }

        // ── Public API: Progress ──

        public void SetProgressItems(string json)
        {
            ExecuteScript($"window.bridge.setProgressItems({JsonConvert.SerializeObject(json)})");
        }

        public void UpdateProgressItem(string json)
        {
            ExecuteScript($"window.bridge.updateProgressItem({JsonConvert.SerializeObject(json)})");
        }

        public void ShowProgress(bool visible)
        {
            ExecuteScript($"window.bridge.showProgress({(visible ? "true" : "false")})");
        }

        public void ShowRetryButton(bool show)
        {
            ExecuteScript($"window.bridge.showRetryButton({(show ? "true" : "false")})");
        }

        // ── Public API: Connections ──

        public void SetSource(string connectionName)
        {
            var isConnected = !string.IsNullOrEmpty(connectionName);
            ExecuteScript($"window.bridge.setSource({JsonConvert.SerializeObject(connectionName ?? "")}, {(isConnected ? "true" : "false")})");
        }

        /// <summary>
        /// Pass auth context to React so it can make direct Dataverse Web API calls.
        /// </summary>
        public void SetAuthContext(string orgUrl, string token, string environmentId)
        {
            ExecuteScript($"window.bridge.setAuthContext(" +
                $"{JsonConvert.SerializeObject(orgUrl ?? "")}, " +
                $"{JsonConvert.SerializeObject(token ?? "")}, " +
                $"{JsonConvert.SerializeObject(environmentId ?? "")})");
        }

        /// <summary>
        /// Pass target connection auth context to React.
        /// </summary>
        public void AddTargetContext(string connectionName, string orgUrl, string token, string environmentId)
        {
            ExecuteScript($"window.bridge.addTargetContext(" +
                $"{JsonConvert.SerializeObject(connectionName)}, " +
                $"{JsonConvert.SerializeObject(orgUrl ?? "")}, " +
                $"{JsonConvert.SerializeObject(token ?? "")}, " +
                $"{JsonConvert.SerializeObject(environmentId ?? "")})");
        }

        public void SetTargets(List<string> connectionNames)
        {
            var json = JsonConvert.SerializeObject(connectionNames.Select(n => new { name = n }));
            ExecuteScript($"window.bridge.setTargets({JsonConvert.SerializeObject(json)})");
        }

        // ── Public API: Solutions ──

        public void DisplaySolutions(string json)
        {
            ExecuteScript($"window.bridge.loadSolutions({JsonConvert.SerializeObject(json)})");
        }

        public void AddTargetSolutionColumn(string connectionName)
        {
            ExecuteScript($"window.bridge.addTargetSolutionColumn({JsonConvert.SerializeObject(connectionName)})");
        }

        public void SetTargetSolutions(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.setTargetSolutions({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        public void UpdateSolutionVersion(string uniqueName, string newVersion)
        {
            ExecuteScript($"window.bridge.updateSolutionVersion({JsonConvert.SerializeObject(uniqueName)}, {JsonConvert.SerializeObject(newVersion)})");
        }

        // ── Public API: Environment Variables ──

        public void DisplayEnvVars(string json)
        {
            ExecuteScript($"window.bridge.loadEnvVars({JsonConvert.SerializeObject(json)})");
        }

        public void AddTargetEnvVarColumn(string connectionName)
        {
            ExecuteScript($"window.bridge.addTargetEnvVarColumn({JsonConvert.SerializeObject(connectionName)})");
        }

        public void SetTargetEnvVarValues(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.setTargetEnvVarValues({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        // ── Public API: Platform Settings ──

        public void DisplaySettings(string json)
        {
            ExecuteScript($"window.bridge.loadSettings({JsonConvert.SerializeObject(json)})");
        }

        public void AddTargetSettingColumn(string connectionName)
        {
            ExecuteScript($"window.bridge.addTargetSettingColumn({JsonConvert.SerializeObject(connectionName)})");
        }

        public void SetTargetSettingValues(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.setTargetSettingValues({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        // ── Public API: Cloud Flows ──

        public void SetSourceEnvironment(string environmentId)
        {
            sourceEnvironmentId = environmentId;
        }

        public void DisplayCloudFlows(List<Entity> workflows)
        {
            currentFlows.Clear();

            if (workflows == null)
            {
                ExecuteScript("window.bridge.loadFlows('[]')");
                return;
            }

            foreach (var wf in workflows)
            {
                currentFlows.Add(new FlowDataDto
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

            ExecuteScript($"window.bridge.loadFlows({JsonConvert.SerializeObject(json)})");
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

                        ExecuteScript($"window.bridge.addTargetColumn({JsonConvert.SerializeObject(tcd.ConnectionName)})");

                        var targetData = targetFlows.Select(f => new
                        {
                            name = f.GetAttributeValue<string>("name"),
                            status = GetStatusText(f.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0),
                            stateCode = f.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                            statusCode = f.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0
                        });

                        var json = JsonConvert.SerializeObject(targetData);
                        ExecuteScript($"window.bridge.setTargetFlowStatus({JsonConvert.SerializeObject(tcd.ConnectionName)}, {JsonConvert.SerializeObject(json)})");
                    }
                });
            }
        }

        public void RemoveTargetColumn(ConnectionDetail detail)
        {
            targetEnvironmentIds.Remove(detail.ConnectionName);
            ExecuteScript($"window.bridge.removeTargetColumn({JsonConvert.SerializeObject(detail.ConnectionName)})");
        }

        public void UpdateFlowCellStatus(string connectionName, string flowName, bool activated, bool isMatch, bool isError = false, string errorWarning = null)
        {
            var status = activated ? "On" : "Off";
            ExecuteScript($"window.bridge.updateFlowCellStatus(" +
                $"{JsonConvert.SerializeObject(connectionName)}, " +
                $"{JsonConvert.SerializeObject(flowName)}, " +
                $"{JsonConvert.SerializeObject(status)}, " +
                $"{(isMatch ? "true" : "false")}, " +
                $"{(isError ? "true" : "false")}, " +
                $"{JsonConvert.SerializeObject(errorWarning ?? "")})");
        }

        // ── Public API: Results from C# services ──

        public void SendPluginSettings(string json)
        {
            ExecuteScript($"window.bridge.loadPluginSettings({JsonConvert.SerializeObject(json)})");
        }

        public void SendTargetSolutions(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.targetSolutions?.({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        public void SendTargetFlows(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.targetFlows?.({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        public void SendTargetEnvVars(string connectionName, string json)
        {
            ExecuteScript($"window.bridge.targetEnvVars?.({JsonConvert.SerializeObject(connectionName)}, {JsonConvert.SerializeObject(json)})");
        }

        public void SendFlowResults(string json)
        {
            ExecuteScript($"window.bridge.flowResults?.({JsonConvert.SerializeObject(json)})");
        }

        public void SendTransferResult(string json)
        {
            ExecuteScript($"window.bridge.transferResult?.({JsonConvert.SerializeObject(json)})");
        }

        public void SendMissingDeps(string json)
        {
            ExecuteScript($"window.bridge.missingDeps?.({JsonConvert.SerializeObject(json)})");
        }

        public void SetActiveTab(string tab)
        {
            ExecuteScript($"window.bridge.setActiveTab({JsonConvert.SerializeObject(tab)})");
        }

        public void InvokeRefresh()
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        public void InvokeActivateSelected()
        {
            RaiseFlowActivateEvent(true);
        }

        public void InvokeDeactivateSelected()
        {
            RaiseFlowActivateEvent(false);
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

        // ── DTOs ──

        private class FlowDataDto
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

        private class SelectedFlowDto
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("workflowId")]
            public string WorkflowId { get; set; }
            [JsonProperty("stateCode")]
            public int StateCode { get; set; }
        }
    }

    public class TabChangedEventArgs : EventArgs
    {
        public string Tab { get; set; }
    }

    public class RemoveTargetEventArgs : EventArgs
    {
        public string ConnectionName { get; set; }
    }

    public class ProgressActionEventArgs : EventArgs
    {
        public string Action { get; set; }
        public string ItemId { get; set; }
    }

    public class StringEventArgs : EventArgs
    {
        public string Value { get; set; }
    }

    public class SolutionActionEventArgs : EventArgs
    {
        public List<SolutionActionItem> Solutions { get; set; } = new List<SolutionActionItem>();
    }

    public class SolutionActionItem
    {
        [JsonProperty("solutionId")] public string SolutionId { get; set; }
        [JsonProperty("uniqueName")] public string UniqueName { get; set; }
        [JsonProperty("friendlyName")] public string FriendlyName { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
    }

    public class EnvVarTransferEventArgs : EventArgs
    {
        public List<WebEnvVarTransferItem> Items { get; set; } = new List<WebEnvVarTransferItem>();
    }

    public class WebEnvVarTransferItem
    {
        [JsonProperty("schemaName")] public string SchemaName { get; set; }
        [JsonProperty("displayName")] public string DisplayName { get; set; }
        [JsonProperty("sourceValue")] public string SourceValue { get; set; }
        [JsonProperty("definitionId")] public string DefinitionId { get; set; }
    }

    public class EnvVarEditEventArgs : EventArgs
    {
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public string TypeName { get; set; }
        public string SourceValue { get; set; }
        public string DefinitionId { get; set; }
    }

    public class EnvVarSaveEventArgs : EventArgs
    {
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public Dictionary<string, string> ChangedValues { get; set; } = new Dictionary<string, string>();
    }

    public class SettingSyncEventArgs : EventArgs
    {
        public List<WebSettingSyncItem> Items { get; set; } = new List<WebSettingSyncItem>();
        public bool All { get; set; }
    }

    public class WebSettingSyncItem
    {
        [JsonProperty("uniqueName")] public string UniqueName { get; set; }
        [JsonProperty("displayName")] public string DisplayName { get; set; }
        [JsonProperty("sourceValue")] public string SourceValue { get; set; }
    }

    public class StartTransferEventArgs : EventArgs
    {
        public List<SolutionActionItem> Solutions { get; set; } = new List<SolutionActionItem>();
        public Services.SolutionTransferService.TransferSettings Settings { get; set; }
    }
}
