using err403.SolutionManagment.AppCode;
using err403.SolutionManagment.Forms;
using err403.SolutionManagment.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using Settings = err403.SolutionManagment.AppCode.Settings;

namespace err403.SolutionManagment
{
    public partial class SolutionManagement : MultipleConnectionsPluginControlBase, IGitHubPlugin, IHelpPlugin
    {
        private readonly WebUIHost cfForm;
        private Settings settings;
        private ConnectionDetail sourceDetail;
        private IOrganizationService sourceService;

        public SolutionManagement()
        {
            InitializeComponent();

            cfForm = new WebUIHost();

            // Connection management
            cfForm.TabChanged += (s, e) => { /* React handles tab UI, no C# action needed */ };
            cfForm.AddTargetRequested += (s, e) => AddAdditionalOrganization();
            cfForm.RemoveTargetRequested += (s, e) =>
            {
                var toRemove = AdditionalConnectionDetails.FirstOrDefault(c => c.ConnectionName == e.ConnectionName);
                if (toRemove == null) return;
                cfForm.RemoveTargetColumn(toRemove);
                RemoveAdditionalOrganization(toRemove);
            };

            // Token refresh
            cfForm.RefreshTokenRequested += (s, e) =>
            {
                if (sourceDetail == null) return;
                var auth = TokenService.GetAuthContext(sourceDetail);
                cfForm.SetAuthContext(auth.OrgUrl, auth.Token, auth.EnvironmentId);
            };

            // SDK write operations — route to services
            cfForm.ActivateRequested += CfForm_ActivateRequested;
            cfForm.DeactivateRequested += CfForm_DeactivateRequested;

            // Solutions SDK operations
            cfForm.LoadSolutionsRequested += (s, e) => { /* React fetches via Web API */ };
            cfForm.TransferSolutionsRequested += CfForm_TransferRequested;
            cfForm.ImportFromFileRequested += CfForm_ImportFromFileRequested;
            cfForm.ExportSolutionsRequested += CfForm_ExportRequested;
            cfForm.RemoveFromTargetsRequested += CfForm_RemoveFromTargetsRequested;
            cfForm.SwitchOrgsRequested += CfForm_SwitchOrgsRequested;
            cfForm.FindMissingDepsRequested += CfForm_FindMissingDepsRequested;

            Controls.Add(cfForm);
        }

        // ── XrmToolBox interface ──

        public string HelpUrl => "https://github.com/garethcheyne/SolutionTransferTool/wiki";
        public string RepositoryName => "SolutionTransferTool";
        public string UserName => "garethcheyne";

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (ConnectionDetail != null && settings != null)
                settings.Save(ConnectionDetail.ConnectionName);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            if (actionName == "AdditionalOrganization")
            {
                if (!AdditionalConnectionDetails.Any(c => c.ConnectionId == detail.ConnectionId))
                {
                    AdditionalConnectionDetails.Add(detail);
                    var auth = TokenService.GetAuthContext(detail);
                    cfForm.AddTargetContext(detail.ConnectionName, auth.OrgUrl, auth.Token, auth.EnvironmentId);
                }

                if (newService is OrganizationServiceProxy proxy)
                    proxy.Timeout = detail.Timeout;
                else if (newService is OrganizationWebProxyClient client)
                    client.InnerChannel.OperationTimeout = detail.Timeout;
            }
            else
            {
                settings?.Save(ConnectionDetail?.ConnectionName);
                ConnectionDetail = detail;
                sourceDetail = detail;
                sourceService = newService;

                if (!SettingsManager.Instance.TryLoad(GetType(), out settings, ConnectionDetail.ConnectionName))
                    settings = new Settings();

                // Pass auth context to React
                var auth = TokenService.GetAuthContext(detail);
                cfForm.SetSource(detail.ConnectionName);
                cfForm.SetAuthContext(auth.OrgUrl, auth.Token, auth.EnvironmentId);
                cfForm.SetSourceEnvironment(auth.EnvironmentId);

                base.UpdateConnection(newService, detail, actionName, parameter);
            }
        }

        protected override void ConnectionDetailsUpdated(NotifyCollectionChangedEventArgs e)
        {
            cfForm.SetTargets(AdditionalConnectionDetails.Select(c => c.ConnectionName).ToList());
        }

        // ── Flow activation (SDK: SetStateRequest) ──

        private void CfForm_ActivateRequested(object sender, FlowActivateRequestedEventArgs e) => ToggleFlows(e, true);
        private void CfForm_DeactivateRequested(object sender, FlowActivateRequestedEventArgs e) => ToggleFlows(e, false);

        private void ToggleFlows(FlowActivateRequestedEventArgs e, bool activate)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, "No target environments connected.", "No Targets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var cd in AdditionalConnectionDetails)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = $"{(activate ? "Activating" : "Deactivating")} flows on {cd.ConnectionName}...",
                    Work = (bw, we) =>
                    {
                        var requests = e.Flows.Select(f => new FlowActivationService.FlowActionRequest
                        {
                            Name = f.FlowName,
                            WorkflowId = f.WorkflowId.ToString()
                        }).ToList();
                        we.Result = FlowActivationService.Execute(requests, cd, activate);
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            MessageBox.Show(this, we.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        var results = (List<FlowActivationService.FlowActionResult>)we.Result;
                        var json = JsonConvert.SerializeObject(results);
                        cfForm.SendFlowResults(json);
                    }
                });
            }
        }

        // ── Solution transfer (SDK: Export + Import + Publish) ──

        private void CfForm_TransferRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;
            // TODO: React sends settings with the transfer request
            // For now use default settings
            var transferSettings = new SolutionTransferService.TransferSettings
            {
                Managed = settings?.Managed ?? true,
            };

            foreach (var sol in e.Solutions)
            {
                foreach (var cd in AdditionalConnectionDetails)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = $"Transferring {sol.FriendlyName} to {cd.ConnectionName}...",
                        Work = (bw, we) =>
                        {
                            // Export from source
                            var exportResult = SolutionTransferService.ExportSolution(
                                sourceService, sol.UniqueName, transferSettings.Managed);
                            if (!exportResult.Success)
                                throw new Exception($"Export failed: {exportResult.ErrorMessage}");

                            // Import to target
                            var importResult = SolutionTransferService.ImportSolution(
                                cd, exportResult.SolutionContent, transferSettings);
                            if (!importResult.Success)
                                throw new Exception($"Import failed: {importResult.ErrorMessage}");

                            // Publish
                            SolutionTransferService.PublishCustomizations(cd);

                            we.Result = new { Solution = sol.FriendlyName, Target = cd.ConnectionName };
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                            {
                                var errorJson = JsonConvert.SerializeObject(new
                                {
                                    solution = sol.FriendlyName,
                                    target = cd.ConnectionName,
                                    success = false,
                                    error = we.Error.Message
                                });
                                cfForm.SendTransferResult(errorJson);
                            }
                            else
                            {
                                var successJson = JsonConvert.SerializeObject(new
                                {
                                    solution = sol.FriendlyName,
                                    target = cd.ConnectionName,
                                    success = true,
                                    error = ""
                                });
                                cfForm.SendTransferResult(successJson);
                            }
                        }
                    });
                }
            }
        }

        // ── Export to file ──

        private void CfForm_ExportRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0) return;

            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog(this) != DialogResult.OK) return;

                foreach (var sol in e.Solutions)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = $"Exporting {sol.FriendlyName}...",
                        Work = (bw, we) =>
                        {
                            var result = SolutionTransferService.ExportSolution(
                                sourceService, sol.UniqueName, settings?.Managed ?? true);
                            if (!result.Success)
                                throw new Exception(result.ErrorMessage);

                            var path = SolutionTransferService.SaveSolutionToDisk(
                                result.SolutionContent, sol.UniqueName, sol.Version,
                                settings?.Managed ?? true, fbd.SelectedPath);
                            we.Result = path;
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                                MessageBox.Show(this, we.Error.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            else
                                MessageBox.Show(this, $"Saved to {we.Result}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    });
                }
            }
        }

        // ── Import from file ──

        private void CfForm_ImportFromFileRequested(object sender, EventArgs e)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, "No target environments connected.", "No Targets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ofd = new OpenFileDialog { Filter = "Solution files (*.zip)|*.zip", Multiselect = false })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                var content = System.IO.File.ReadAllBytes(ofd.FileName);
                var transferSettings = new SolutionTransferService.TransferSettings();

                foreach (var cd in AdditionalConnectionDetails)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = $"Importing to {cd.ConnectionName}...",
                        Work = (bw, we) =>
                        {
                            var result = SolutionTransferService.ImportSolution(cd, content, transferSettings);
                            if (!result.Success) throw new Exception(result.ErrorMessage);
                            SolutionTransferService.PublishCustomizations(cd);
                            we.Result = cd.ConnectionName;
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                                MessageBox.Show(this, we.Error.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            else
                                MessageBox.Show(this, $"Imported to {we.Result}", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    });
                }
            }
        }

        // ── Remove solutions from targets ──

        private void CfForm_RemoveFromTargetsRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            var confirm = MessageBox.Show(this,
                $"Remove {e.Solutions.Count} solution(s) from {AdditionalConnectionDetails.Count} target(s)?",
                "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            foreach (var sol in e.Solutions)
            {
                foreach (var cd in AdditionalConnectionDetails)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = $"Removing {sol.FriendlyName} from {cd.ConnectionName}...",
                        Work = (bw, we) =>
                        {
                            we.Result = SolutionRemovalService.RemoveFromTarget(sol.UniqueName, sol.FriendlyName, cd);
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                                MessageBox.Show(this, we.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                }
            }
        }

        // ── Switch source/target ──

        private void CfForm_SwitchOrgsRequested(object sender, EventArgs e)
        {
            if (AdditionalConnectionDetails.Count != 1)
            {
                MessageBox.Show(this, "Switch requires exactly one target.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tempDetail = sourceDetail;
            sourceDetail = AdditionalConnectionDetails.First();
            ConnectionDetail = sourceDetail;
            AdditionalConnectionDetails.Clear();
            if (tempDetail != null) AdditionalConnectionDetails.Add(tempDetail);

            sourceService = sourceDetail.GetCrmServiceClient();
            var auth = TokenService.GetAuthContext(sourceDetail);
            cfForm.SetSource(sourceDetail.ConnectionName);
            cfForm.SetAuthContext(auth.OrgUrl, auth.Token, auth.EnvironmentId);
            cfForm.SetSourceEnvironment(auth.EnvironmentId);

            if (tempDetail != null)
            {
                var targetAuth = TokenService.GetAuthContext(tempDetail);
                cfForm.AddTargetContext(tempDetail.ConnectionName, targetAuth.OrgUrl, targetAuth.Token, targetAuth.EnvironmentId);
            }

            base.UpdateConnection(sourceService, sourceDetail, "", null);
        }

        // ── Find missing dependencies ──

        private void CfForm_FindMissingDepsRequested(object sender, EventArgs e)
        {
            // TODO: React sends importJobId, C# calls MissingDependencyService, returns results
        }
    }
}
