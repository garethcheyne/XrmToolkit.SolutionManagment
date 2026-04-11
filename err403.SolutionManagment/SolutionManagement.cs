using err403.SolutionManagment.AppCode;
using err403.SolutionManagment.Forms;
using err403.SolutionManagment.Services;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Toolkit.Uwp.Notifications;
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
            cfForm.StartTransferRequested += CfForm_StartTransferRequested;
            cfForm.ImportFromFileRequested += CfForm_ImportFromFileRequested;
            cfForm.ExportSolutionsRequested += CfForm_ExportRequested;
            cfForm.RemoveFromTargetsRequested += CfForm_RemoveFromTargetsRequested;
            cfForm.RemoveFromSourceRequested += CfForm_RemoveFromSourceRequested;
            cfForm.SwitchOrgsRequested += CfForm_SwitchOrgsRequested;
            cfForm.FindMissingDepsRequested += CfForm_FindMissingDepsRequested;

            // Settings persistence
            cfForm.SavePluginSettingsRequested += CfForm_SavePluginSettingsRequested;

            Controls.Add(cfForm);
        }

        private void SendSettingsToReact()
        {
            if (settings == null) return;
            var json = JsonConvert.SerializeObject(new
            {
                managed = settings.Managed,
                exportAsync = settings.ExportAsynchronously,
                autoNumbering = settings.ExportAutoNumberingSettings,
                calendarSettings = settings.ExportCalendarSettings,
                customizationSettings = settings.ExportCustomizationSettings,
                emailTracking = settings.ExportEmailTrackingSettings,
                externalApps = settings.ExportExternalApplications,
                generalSettings = settings.ExportGeneralSettings,
                isvConfig = settings.ExportIsvConfig,
                marketingSettings = settings.ExportMarketingSettings,
                outlookSync = settings.ExportOutlookSynchronizationSettings,
                relationshipRoles = settings.ExportRelationshipRoles,
                sales = settings.ExportSales,
                importMode = settings.ImportMode.ToString(),
                checkDependencies = settings.CheckForMissingDependencies,
                convertToManaged = settings.ConvertToManaged,
                deployMissingPackages = settings.DeployMissingPackagesBeforeSolutionImport,
                overwriteUnmanaged = settings.OverwriteUnmanagedCustomizations,
                publishWorkflows = settings.PublishWorkflows,
                skipProductUpdateDeps = settings.SkipProductUpdateDependencies,
                publishCustomizations = settings.Publish,
                autoSave = settings.AutoExportSolutionsToDisk,
                autoSavePath = settings.AutoExportSolutionsFolderPath ?? "",
                preImportSummary = settings.ShowPreImportSummary,
                useToastNotifications = settings.UseWindowsToastNotification,
                updateVersion = settings.UpdateSourceSolutionVersionNew.ToString(),
                versionPolicy = settings.VersionSchema.ToString(),
                dateVersionMask = settings.VersionDateMask ?? "yyyy.MM.dd.x"
            });
            cfForm.SendPluginSettings(json);
        }

        private void CfForm_SavePluginSettingsRequested(object sender, StringEventArgs e)
        {
            if (settings == null || string.IsNullOrEmpty(e.Value)) return;
            try
            {
                var s = JsonConvert.DeserializeObject<dynamic>(e.Value);
                settings.Managed = (bool)(s.managed ?? settings.Managed);
                settings.ExportAsynchronously = (bool)(s.exportAsync ?? settings.ExportAsynchronously);
                settings.ExportAutoNumberingSettings = (bool)(s.autoNumbering ?? false);
                settings.ExportCalendarSettings = (bool)(s.calendarSettings ?? false);
                settings.ExportCustomizationSettings = (bool)(s.customizationSettings ?? false);
                settings.ExportEmailTrackingSettings = (bool)(s.emailTracking ?? false);
                settings.ExportExternalApplications = (bool)(s.externalApps ?? false);
                settings.ExportGeneralSettings = (bool)(s.generalSettings ?? false);
                settings.ExportIsvConfig = (bool)(s.isvConfig ?? false);
                settings.ExportMarketingSettings = (bool)(s.marketingSettings ?? false);
                settings.ExportOutlookSynchronizationSettings = (bool)(s.outlookSync ?? false);
                settings.ExportRelationshipRoles = (bool)(s.relationshipRoles ?? false);
                settings.ExportSales = (bool)(s.sales ?? false);
                settings.CheckForMissingDependencies = (bool)(s.checkDependencies ?? false);
                settings.ConvertToManaged = (bool)(s.convertToManaged ?? false);
                settings.DeployMissingPackagesBeforeSolutionImport = (bool)(s.deployMissingPackages ?? true);
                settings.OverwriteUnmanagedCustomizations = (bool)(s.overwriteUnmanaged ?? true);
                settings.PublishWorkflows = (bool)(s.publishWorkflows ?? true);
                settings.SkipProductUpdateDependencies = (bool)(s.skipProductUpdateDeps ?? false);
                settings.Publish = (bool)(s.publishCustomizations ?? true);
                settings.AutoExportSolutionsToDisk = (bool)(s.autoSave ?? false);
                settings.AutoExportSolutionsFolderPath = (string)(s.autoSavePath ?? "");
                settings.ShowPreImportSummary = (bool)(s.preImportSummary ?? true);
                settings.UseWindowsToastNotification = (bool)(s.useToastNotifications ?? true);

                string importMode = (string)(s.importMode ?? "Update");
                if (System.Enum.TryParse<ImportModeEnum>(importMode, out var im)) settings.ImportMode = im;

                string updateVer = (string)(s.updateVersion ?? "Prompt");
                if (System.Enum.TryParse<UpdateVersionEnum>(updateVer, out var uv)) settings.UpdateSourceSolutionVersionNew = uv;

                string verPolicy = (string)(s.versionPolicy ?? "Date");
                if (System.Enum.TryParse<VersionType>(verPolicy, out var vp)) settings.VersionSchema = vp;

                settings.VersionDateMask = (string)(s.dateVersionMask ?? "yyyy.MM.dd.x");

                settings.Save(ConnectionDetail?.ConnectionName);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[SaveSettings] Error: {ex.Message}");
            }
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

                    // Fetch target data via C# (target tokens may not work for direct React API calls)
                    FetchTargetSolutions(detail);
                    FetchTargetFlows(detail);
                    FetchTargetEnvVars(detail);
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

                // Send saved settings to React
                SendSettingsToReact();

                base.UpdateConnection(newService, detail, actionName, parameter);
            }
        }

        protected override void ConnectionDetailsUpdated(NotifyCollectionChangedEventArgs e)
        {
            cfForm.SetTargets(AdditionalConnectionDetails.Select(c => c.ConnectionName).ToList());
        }

        // ── Target data fetching (C# does this because targets have separate auth) ──

        private void FetchTargetSolutions(ConnectionDetail target)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    // Get source solution names to look up on target
                    // Ask React for current solution list via the source Web API response
                    // For now, query target for ALL visible unmanaged solutions
                    var svc = target.GetCrmServiceClient();
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("uniquename", "version", "ismanaged"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true)
                            }
                        }
                    };
                    var solutions = svc.RetrieveMultiple(query).Entities;
                    we.Result = new System.Tuple<ConnectionDetail, List<Entity>>(target, solutions.ToList());
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null) return;
                    var result = (System.Tuple<ConnectionDetail, List<Entity>>)we.Result;
                    var tcd = result.Item1;
                    var solutions = result.Item2;

                    var json = JsonConvert.SerializeObject(solutions.Select(s => new
                    {
                        uniquename = s.GetAttributeValue<string>("uniquename"),
                        version = s.GetAttributeValue<string>("version"),
                        ismanaged = s.GetAttributeValue<bool>("ismanaged")
                    }));

                    cfForm.SendTargetSolutions(tcd.ConnectionName, json);
                }
            });
        }

        private void FetchTargetFlows(ConnectionDetail target)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    var svc = target.GetCrmServiceClient();
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
                    var flows = svc.RetrieveMultiple(query).Entities;
                    we.Result = new System.Tuple<ConnectionDetail, List<Entity>>(target, flows.ToList());
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null) return;
                    var result = (System.Tuple<ConnectionDetail, List<Entity>>)we.Result;
                    var tcd = result.Item1;
                    var flows = result.Item2;

                    var json = JsonConvert.SerializeObject(flows.Select(f => new
                    {
                        name = f.GetAttributeValue<string>("name"),
                        statecode = f.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                        statuscode = f.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0
                    }));

                    cfForm.SendTargetFlows(tcd.ConnectionName, json);
                }
            });
        }

        private void FetchTargetEnvVars(ConnectionDetail target)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    var svc = target.GetCrmServiceClient();
                    var defQuery = new QueryExpression("environmentvariabledefinition")
                    {
                        ColumnSet = new ColumnSet("schemaname"),
                        Criteria = new FilterExpression { Conditions = { new ConditionExpression("statecode", ConditionOperator.Equal, 0) } }
                    };
                    var defs = svc.RetrieveMultiple(defQuery).Entities.ToList();

                    var valQuery = new QueryExpression("environmentvariablevalue")
                    {
                        ColumnSet = new ColumnSet("value", "environmentvariabledefinitionid"),
                        Criteria = new FilterExpression { Conditions = { new ConditionExpression("statecode", ConditionOperator.Equal, 0) } }
                    };
                    var vals = svc.RetrieveMultiple(valQuery).Entities.ToList();

                    we.Result = new System.Tuple<ConnectionDetail, List<Entity>, List<Entity>>(target, defs, vals);
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null) return;
                    var result = (System.Tuple<ConnectionDetail, List<Entity>, List<Entity>>)we.Result;
                    var tcd = result.Item1;
                    var defs = result.Item2;
                    var vals = result.Item3;

                    var json = JsonConvert.SerializeObject(defs.Select(d =>
                    {
                        var val = vals.FirstOrDefault(v =>
                            v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid")?.Id == d.Id);
                        return new
                        {
                            schemaname = d.GetAttributeValue<string>("schemaname"),
                            value = val?.GetAttributeValue<string>("value") ?? "",
                            exists = true
                        };
                    }));

                    cfForm.SendTargetEnvVars(tcd.ConnectionName, json);
                }
            });
        }

        // ── Solution transfer with settings from React dialog ──

        private void CfForm_StartTransferRequested(object sender, StartTransferEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            // Show progress panel in React
            cfForm.ShowProgress(true);

            // Version bump if configured
            if (settings != null && settings.UpdateSourceSolutionVersionNew == UpdateVersionEnum.Yes)
            {
                foreach (var sol in e.Solutions)
                {
                    var newVersion = SolutionTransferService.BumpVersion(
                        sol.Version, settings.VersionSchema.ToString(), settings.VersionDateMask ?? "yyyy.MM.dd.x");
                    SolutionTransferService.UpdateSolutionVersion(sourceService, System.Guid.Parse(sol.SolutionId), newVersion);
                    sol.Version = newVersion;
                }
            }

            foreach (var sol in e.Solutions)
            {
                foreach (var cd in AdditionalConnectionDetails)
                {
                    // Send initial progress to React
                    SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Exporting...", 0);

                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null, // Suppress XrmToolBox dialog — React shows progress
                        Work = (bw, we) =>
                        {
                            // Export
                            var exportResult = SolutionTransferService.ExportSolution(
                                sourceService, sol.UniqueName, e.Settings.Managed);
                            if (!exportResult.Success)
                                throw new Exception($"Export failed: {exportResult.ErrorMessage}");

                            // Auto-save to disk if configured
                            if (settings != null && settings.AutoExportSolutionsToDisk
                                && !string.IsNullOrEmpty(settings.AutoExportSolutionsFolderPath))
                            {
                                SolutionTransferService.SaveSolutionToDisk(
                                    exportResult.SolutionContent, sol.UniqueName, sol.Version,
                                    e.Settings.Managed, settings.AutoExportSolutionsFolderPath);
                            }

                            // Update progress on UI thread
                            Invoke(new System.Action(() =>
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Importing...", 33)));

                            // Import
                            var importResult = SolutionTransferService.ImportSolution(
                                cd, exportResult.SolutionContent, e.Settings);
                            if (!importResult.Success)
                                throw new Exception($"Import failed: {importResult.ErrorMessage}");

                            Invoke(new System.Action(() =>
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Publishing...", 66)));

                            // Publish
                            SolutionTransferService.PublishCustomizations(cd);
                            we.Result = true;
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                            {
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "error", we.Error.Message, 100);
                            }
                            else
                            {
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "success", "Complete", 100);
                            }

                            var json = JsonConvert.SerializeObject(new
                            {
                                solution = sol.FriendlyName,
                                target = cd.ConnectionName,
                                success = we.Error == null,
                                error = we.Error?.Message ?? ""
                            });
                            cfForm.SendTransferResult(json);

                            // Toast notification
                            if (settings?.UseWindowsToastNotification == true)
                            {
                                try
                                {
                                    new ToastContentBuilder()
                                        .AddText(we.Error == null ? "Transfer Complete" : "Transfer Failed")
                                        .AddText($"{sol.FriendlyName} → {cd.ConnectionName}: {(we.Error == null ? "Success" : we.Error.Message)}")
                                        .Show();
                                }
                                catch { /* toast may not be available */ }
                            }

                            // Refresh target versions after transfer
                            FetchTargetSolutions(cd);
                        }
                    });
                }
            }
        }

        private void SendProgress(string solution, string target, string status, string message, int percentage)
        {
            var json = JsonConvert.SerializeObject(new
            {
                id = $"{solution}|{target}",
                action = $"{solution}",
                direction = $"→ {target}",
                status,
                percentage,
                elapsed = message,
            });
            cfForm.UpdateProgressItem(json);
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
                    Message = null, // React shows progress
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
                        Message = null, // React shows progress
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
                        Message = null, // React shows progress
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
                        Message = null, // React shows progress
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
                        Message = null, // React shows progress
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

        // ── Remove from source ──

        private void CfForm_RemoveFromSourceRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0) return;

            var confirm = MessageBox.Show(this,
                $"Remove {e.Solutions.Count} solution(s) from SOURCE environment? This cannot be undone.",
                "Confirm Remove from Source", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            foreach (var sol in e.Solutions)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        we.Result = SolutionRemovalService.RemoveFromSource(
                            sol.UniqueName, sol.FriendlyName,
                            System.Guid.Parse(sol.SolutionId), sourceService);
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                            MessageBox.Show(this, we.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        }

        // ── Find missing dependencies ──

        private void CfForm_FindMissingDepsRequested(object sender, EventArgs e)
        {
            // TODO: React sends importJobId, C# calls MissingDependencyService, returns results
        }
    }
}
