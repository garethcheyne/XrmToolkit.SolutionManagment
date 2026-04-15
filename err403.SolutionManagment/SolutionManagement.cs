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
using System.Diagnostics;
using System.IO;
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
        private static TextWriterTraceListener _fileListener;
        private static readonly string TraceLogPath = Path.Combine(
            @"c:\Apps\Projects\XrmToolBox\Solutions\DamSim.SolutionTransferTool",
            "trace.log");

        // Async import polling
        private readonly System.Windows.Forms.Timer _importTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        private readonly List<ActiveImport> _activeImports = new List<ActiveImport>();

        // Timing: tracks when each progress item started (keyed by id: "solution|target|phase")
        private readonly Dictionary<string, DateTime> _phaseStartTimes = new Dictionary<string, DateTime>();

        private class ActiveImport
        {
            public string SolutionName { get; set; }
            public string TargetName { get; set; }
            public ConnectionDetail Target { get; set; }
            public Guid AsyncOperationId { get; set; }
            public Guid ImportJobId { get; set; }
            public SolutionTransferService.TransferSettings Settings { get; set; }
            public DateTime StartedAt { get; set; }
        }

        public SolutionManagement()
        {
            InitializeComponent();

            // Set up file-based trace listener
            InitTraceListener();

            Trace.WriteLine($"[SolutionManagement] Plugin loaded at {DateTime.Now:u}");

            cfForm = new WebUIHost();

            // Import progress polling timer
            _importTimer.Tick += ImportTimer_Tick;

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
            cfForm.ActiveImportsResponseReceived += (s, e) => HandleActiveImportsResponse(e.SkipTargets, e.WaitTargets);
            cfForm.RetryRequested += CfForm_RetryRequested;
            cfForm.ImportFromFileRequested += CfForm_ImportFromFileRequested;
            cfForm.ExportSolutionsRequested += CfForm_ExportRequested;
            cfForm.RemoveFromTargetsRequested += CfForm_RemoveFromTargetsRequested;
            cfForm.RemoveFromSourceRequested += CfForm_RemoveFromSourceRequested;
            cfForm.SwitchOrgsRequested += CfForm_SwitchOrgsRequested;
            cfForm.FindMissingDepsRequested += CfForm_FindMissingDepsRequested;

            // Settings persistence
            cfForm.SavePluginSettingsRequested += CfForm_SavePluginSettingsRequested;

            // Platform Settings sync
            cfForm.SyncSettingsRequested += CfForm_SyncSettingsRequested;

            // Environment Variable write operations
            cfForm.TransferEnvVarsRequested += CfForm_TransferEnvVarsRequested;
            cfForm.SaveEnvVarRequested += CfForm_SaveEnvVarRequested;

            // GDS auth
            cfForm.AuthenticateGdsRequested += (s, ev) =>
            {
                if (sourceDetail == null) return;
                AppCode.EnvironmentIdResolver.AuthenticateInteractively(sourceDetail, this);
                var auth = TokenService.GetAuthContext(sourceDetail);
                cfForm.SetAuthContext(auth.OrgUrl, auth.Token, auth.EnvironmentId);
                cfForm.SetSourceEnvironment(auth.EnvironmentId);
            };

            // Pop-out help window
            cfForm.PopOutHelpRequested += (s, ev) =>
            {
                var helpWindow = new Forms.HelpWindow();
                helpWindow.Show();
            };

            Controls.Add(cfForm);
        }

        private static void InitTraceListener()
        {
            if (_fileListener != null) return;

            try
            {
                var dir = Path.GetDirectoryName(TraceLogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Rotate: if existing log > 5MB, move to .prev
                if (File.Exists(TraceLogPath) && new FileInfo(TraceLogPath).Length > 5 * 1024 * 1024)
                {
                    var prev = TraceLogPath + ".prev";
                    if (File.Exists(prev)) File.Delete(prev);
                    File.Move(TraceLogPath, prev);
                }

                _fileListener = new TextWriterTraceListener(TraceLogPath)
                {
                    TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId
                };
                Trace.Listeners.Add(_fileListener);
                Trace.AutoFlush = true;

                Trace.WriteLine($"=== Trace session started at {DateTime.Now:u} ===");
                Trace.WriteLine($"=== Log file: {TraceLogPath} ===");
            }
            catch
            {
                // Tracing should never crash the plugin
            }
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
                dateVersionMask = settings.VersionDateMask ?? "yyyy.MM.dd.x",
                refreshInterval = settings.RefreshIntervalProp.ToString(@"hh\:mm\:ss"),
                solutionProfiles = settings.SolutionProfiles.Count > 0
                    ? settings.SolutionProfiles.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            managed = kvp.Value.Managed,
                            exportAsync = kvp.Value.ExportAsync,
                            autoNumbering = kvp.Value.ExportAutoNumberingSettings,
                            calendarSettings = kvp.Value.ExportCalendarSettings,
                            customizationSettings = kvp.Value.ExportCustomizationSettings,
                            emailTracking = kvp.Value.ExportEmailTrackingSettings,
                            externalApps = kvp.Value.ExportExternalApplications,
                            generalSettings = kvp.Value.ExportGeneralSettings,
                            isvConfig = kvp.Value.ExportIsvConfig,
                            marketingSettings = kvp.Value.ExportMarketingSettings,
                            outlookSync = kvp.Value.ExportOutlookSynchronizationSettings,
                            relationshipRoles = kvp.Value.ExportRelationshipRoles,
                            sales = kvp.Value.ExportSales,
                            importMode = kvp.Value.ImportMode.ToString(),
                            checkDependencies = kvp.Value.CheckForMissingDependencies,
                            convertToManaged = kvp.Value.ConvertToManaged,
                            deployMissingPackages = kvp.Value.DeployMissingPackages,
                            overwriteUnmanaged = kvp.Value.OverwriteUnmanagedCustomizations,
                            publishWorkflows = kvp.Value.PublishWorkflows,
                            skipProductUpdateDeps = kvp.Value.SkipProductUpdateDependencies,
                            publishCustomizations = kvp.Value.PublishCustomizations,
                            updateVersion = kvp.Value.UpdateVersion.ToString(),
                            versionPolicy = kvp.Value.VersionPolicy.ToString(),
                            dateVersionMask = kvp.Value.VersionDateMask ?? "yyyy.MM.dd.x"
                        })
                    : null
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

                string refreshStr = (string)(s.refreshInterval ?? "00:00:10");
                if (TimeSpan.TryParse(refreshStr, out var ri)) settings.RefreshIntervalProp = ri;

                // Per-solution profiles
                if (s.solutionProfiles != null)
                {
                    settings.SolutionProfiles.Clear();
                    var profiles = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(
                        JsonConvert.SerializeObject(s.solutionProfiles));
                    if (profiles != null)
                    {
                        foreach (var kvp in profiles)
                        {
                            var p = kvp.Value;
                            var profile = new SolutionProfile
                            {
                                Managed = (bool)(p.managed ?? true),
                                ExportAsync = (bool)(p.exportAsync ?? true),
                                ExportAutoNumberingSettings = (bool)(p.autoNumbering ?? false),
                                ExportCalendarSettings = (bool)(p.calendarSettings ?? false),
                                ExportCustomizationSettings = (bool)(p.customizationSettings ?? false),
                                ExportEmailTrackingSettings = (bool)(p.emailTracking ?? false),
                                ExportExternalApplications = (bool)(p.externalApps ?? false),
                                ExportGeneralSettings = (bool)(p.generalSettings ?? false),
                                ExportIsvConfig = (bool)(p.isvConfig ?? false),
                                ExportMarketingSettings = (bool)(p.marketingSettings ?? false),
                                ExportOutlookSynchronizationSettings = (bool)(p.outlookSync ?? false),
                                ExportRelationshipRoles = (bool)(p.relationshipRoles ?? false),
                                ExportSales = (bool)(p.sales ?? false),
                                CheckForMissingDependencies = (bool)(p.checkDependencies ?? false),
                                ConvertToManaged = (bool)(p.convertToManaged ?? false),
                                DeployMissingPackages = (bool)(p.deployMissingPackages ?? true),
                                OverwriteUnmanagedCustomizations = (bool)(p.overwriteUnmanaged ?? true),
                                PublishWorkflows = (bool)(p.publishWorkflows ?? true),
                                SkipProductUpdateDependencies = (bool)(p.skipProductUpdateDeps ?? false),
                                PublishCustomizations = (bool)(p.publishCustomizations ?? false),
                            };
                            string pImportMode = (string)(p.importMode ?? "Update");
                            if (System.Enum.TryParse<ImportModeEnum>(pImportMode, out var pim)) profile.ImportMode = pim;
                            string pUpdateVer = (string)(p.updateVersion ?? "Prompt");
                            if (System.Enum.TryParse<UpdateVersionEnum>(pUpdateVer, out var puv)) profile.UpdateVersion = puv;
                            string pVerPolicy = (string)(p.versionPolicy ?? "Date");
                            if (System.Enum.TryParse<VersionType>(pVerPolicy, out var pvp)) profile.VersionPolicy = pvp;
                            profile.VersionDateMask = (string)(p.dateVersionMask ?? "yyyy.MM.dd.x");
                            settings.SolutionProfiles[kvp.Key] = profile;
                        }
                    }
                }

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
            Trace.WriteLine($"[SolutionManagement] Plugin closing at {DateTime.Now:u}");
            if (ConnectionDetail != null && settings != null)
                settings.Save(ConnectionDetail.ConnectionName);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            Trace.WriteLine($"[UpdateConnection] action={actionName}, connection={detail?.ConnectionName}");
            if (actionName == "AdditionalOrganization")
            {
                if (!AdditionalConnectionDetails.Any(c => c.ConnectionId == detail.ConnectionId))
                {
                    AdditionalConnectionDetails.Add(detail);
                    Trace.WriteLine($"[UpdateConnection] Added target: {detail.ConnectionName}");
                    var auth = TokenService.GetAuthContext(detail);
                    Trace.WriteLine($"[UpdateConnection] Target auth: orgUrl={auth.OrgUrl}, envId={auth.EnvironmentId}, hasToken={!string.IsNullOrEmpty(auth.Token)}");
                    cfForm.AddTargetContext(detail.ConnectionName, auth.OrgUrl, auth.Token, auth.EnvironmentId);

                    // Fetch target data via C# (target tokens may not work for direct React API calls)
                    FetchTargetSolutions(detail);
                    FetchTargetFlows(detail);
                    FetchTargetEnvVars(detail);
                    FetchTargetOrgSettings(detail);
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

        private void FetchTargetOrgSettings(ConnectionDetail target)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    var svc = target.GetCrmServiceClient();
                    var query = new QueryExpression("organization")
                    {
                        ColumnSet = new ColumnSet(true),
                        TopCount = 1
                    };
                    var org = svc.RetrieveMultiple(query).Entities.FirstOrDefault();
                    we.Result = new System.Tuple<ConnectionDetail, Entity>(target, org);
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null) return;
                    var result = (System.Tuple<ConnectionDetail, Entity>)we.Result;
                    var tcd = result.Item1;
                    var org = result.Item2;
                    if (org == null) return;

                    var dict = new Dictionary<string, object>();
                    foreach (var attr in org.Attributes)
                    {
                        if (attr.Value == null || attr.Value is EntityReference || attr.Value is OptionSetValue
                            || attr.Value is Money || attr.Value is EntityCollection) continue;
                        dict[attr.Key] = attr.Value;
                    }

                    var json = JsonConvert.SerializeObject(dict);
                    cfForm.SendTargetOrgSettings(tcd.ConnectionName, json);
                }
            });
        }

        // ── Sync org settings to targets ──

        private void CfForm_SyncSettingsRequested(object sender, SettingSyncEventArgs e)
        {
            Trace.WriteLine($"[SyncSettings] Requested: {e.Items?.Count ?? 0} items, all={e.All}, targets={AdditionalConnectionDetails.Count}");
            if (e.Items == null || e.Items.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            // Confirmation is handled in React before this event fires
            foreach (var cd in AdditionalConnectionDetails)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        var svc = cd.GetCrmServiceClient();
                        var orgQuery = new QueryExpression("organization")
                        {
                            ColumnSet = new ColumnSet("organizationid"),
                            TopCount = 1
                        };
                        var orgEntity = svc.RetrieveMultiple(orgQuery).Entities.FirstOrDefault();
                        if (orgEntity == null)
                            throw new Exception("Could not retrieve organization entity on target.");

                        var update = new Entity("organization", orgEntity.Id);
                        foreach (var item in e.Items)
                        {
                            // Try parse back to correct types
                            if (bool.TryParse(item.SourceValue, out var bVal))
                                update[item.UniqueName] = bVal;
                            else if (int.TryParse(item.SourceValue, out var iVal))
                                update[item.UniqueName] = iVal;
                            else if (long.TryParse(item.SourceValue, out var lVal))
                                update[item.UniqueName] = lVal;
                            else if (double.TryParse(item.SourceValue, out var dVal))
                                update[item.UniqueName] = dVal;
                            else
                                update[item.UniqueName] = item.SourceValue;
                        }
                        svc.Update(update);
                        we.Result = cd.ConnectionName;
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            cfForm.ShowAlert("Sync Error", $"Sync to {cd.ConnectionName} failed:\n{we.Error.Message}", "error");
                        }
                        else
                        {
                            // Refresh target settings after sync
                            FetchTargetOrgSettings(cd);
                        }
                    }
                });
            }
        }

        // ── Environment Variable write operations ──

        private void CfForm_TransferEnvVarsRequested(object sender, EnvVarTransferEventArgs e)
        {
            Trace.WriteLine($"[TransferEnvVars] Requested: {e.Items?.Count ?? 0} items, targets={AdditionalConnectionDetails.Count}");
            if (e.Items == null || e.Items.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            foreach (var cd in AdditionalConnectionDetails)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        var svc = cd.GetCrmServiceClient();
                        var errors = new List<string>();
                        foreach (var item in e.Items)
                        {
                            try
                            {
                                // Find definition on target by schema name
                                var defQuery = new QueryExpression("environmentvariabledefinition")
                                {
                                    ColumnSet = new ColumnSet("environmentvariabledefinitionid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions = { new ConditionExpression("schemaname", ConditionOperator.Equal, item.SchemaName) }
                                    }
                                };
                                var def = svc.RetrieveMultiple(defQuery).Entities.FirstOrDefault();
                                if (def == null)
                                {
                                    errors.Add($"{item.DisplayName}: definition not found on target");
                                    continue;
                                }

                                // Check for existing value
                                var valQuery = new QueryExpression("environmentvariablevalue")
                                {
                                    ColumnSet = new ColumnSet("environmentvariablevalueid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions = { new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.Equal, def.Id) }
                                    }
                                };
                                var existingVal = svc.RetrieveMultiple(valQuery).Entities.FirstOrDefault();

                                if (existingVal != null)
                                {
                                    var update = new Entity("environmentvariablevalue", existingVal.Id);
                                    update["value"] = item.SourceValue;
                                    svc.Update(update);
                                }
                                else
                                {
                                    var create = new Entity("environmentvariablevalue");
                                    create["environmentvariabledefinitionid"] = new EntityReference("environmentvariabledefinition", def.Id);
                                    create["value"] = item.SourceValue;
                                    svc.Create(create);
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"{item.DisplayName}: {ex.Message}");
                            }
                        }
                        we.Result = errors;
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            cfForm.ShowAlert("Error", $"Transfer to {cd.ConnectionName} failed:\n{we.Error.Message}", "error");
                            return;
                        }
                        var errors = (List<string>)we.Result;
                        if (errors.Count > 0)
                        {
                            cfForm.ShowAlert("Transfer Warnings", $"Partial errors on {cd.ConnectionName}:\n{string.Join("\n", errors)}", "warning");
                        }
                        FetchTargetEnvVars(cd);
                    }
                });
            }
        }

        private void CfForm_SaveEnvVarRequested(object sender, EnvVarSaveEventArgs e)
        {
            Trace.WriteLine($"[SaveEnvVar] schema={e.SchemaName}, changedTargets={e.ChangedValues?.Count ?? 0}");
            if (e.ChangedValues == null || e.ChangedValues.Count == 0) return;

            foreach (var kvp in e.ChangedValues)
            {
                var target = AdditionalConnectionDetails.FirstOrDefault(c => c.ConnectionName == kvp.Key);
                if (target == null) continue;

                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        var svc = target.GetCrmServiceClient();
                        var defQuery = new QueryExpression("environmentvariabledefinition")
                        {
                            ColumnSet = new ColumnSet("environmentvariabledefinitionid"),
                            Criteria = new FilterExpression
                            {
                                Conditions = { new ConditionExpression("schemaname", ConditionOperator.Equal, e.SchemaName) }
                            }
                        };
                        var def = svc.RetrieveMultiple(defQuery).Entities.FirstOrDefault();
                        if (def == null) throw new Exception($"Variable '{e.SchemaName}' not found on target.");

                        var valQuery = new QueryExpression("environmentvariablevalue")
                        {
                            ColumnSet = new ColumnSet("environmentvariablevalueid"),
                            Criteria = new FilterExpression
                            {
                                Conditions = { new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.Equal, def.Id) }
                            }
                        };
                        var existingVal = svc.RetrieveMultiple(valQuery).Entities.FirstOrDefault();

                        if (existingVal != null)
                        {
                            var update = new Entity("environmentvariablevalue", existingVal.Id);
                            update["value"] = kvp.Value;
                            svc.Update(update);
                        }
                        else
                        {
                            var create = new Entity("environmentvariablevalue");
                            create["environmentvariabledefinitionid"] = new EntityReference("environmentvariabledefinition", def.Id);
                            create["value"] = kvp.Value;
                            svc.Create(create);
                        }
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            cfForm.ShowAlert("Error", $"Save to {target.ConnectionName} failed:\n{we.Error.Message}", "error");
                            return;
                        }
                        FetchTargetEnvVars(target);
                    }
                });
            }
        }

        // ── Solution transfer with settings from React dialog ──

        // Pending transfer state for pre-flight wait
        private StartTransferEventArgs _pendingTransfer;
        private HashSet<string> _waitTargets;
        private List<string> _pendingSkipTargets;
        private readonly System.Windows.Forms.Timer _waitTimer = new System.Windows.Forms.Timer { Interval = 5000 };

        // Retry state: stores the last failed transfer args so the user can retry
        private StartTransferEventArgs _lastTransferArgs;
        private List<string> _lastSkipTargets;

        private void CfForm_StartTransferRequested(object sender, StartTransferEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            // Pre-flight: check each target for active imports
            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    var activeByTarget = new Dictionary<string, List<SolutionTransferService.ActiveImportInfo>>();
                    foreach (var cd in AdditionalConnectionDetails)
                    {
                        var active = SolutionTransferService.GetActiveImports(cd);
                        if (active.Count > 0)
                            activeByTarget[cd.ConnectionName] = active;
                    }
                    we.Result = activeByTarget;
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null)
                    {
                        // Check failed, proceed anyway
                        ExecuteTransfer(e, null);
                        return;
                    }

                    var activeByTarget = (Dictionary<string, List<SolutionTransferService.ActiveImportInfo>>)we.Result;

                    if (activeByTarget.Count == 0)
                    {
                        // No active imports — proceed immediately
                        ExecuteTransfer(e, null);
                    }
                    else
                    {
                        // Send to React for user decision
                        _pendingTransfer = e;
                        var json = JsonConvert.SerializeObject(activeByTarget);
                        cfForm.SendActiveImports(json);
                    }
                }
            });
        }

        /// <summary>
        /// Called when user responds to active imports dialog.
        /// skipTargets = targets to exclude, waitTargets = targets to poll until clear.
        /// </summary>
        private void HandleActiveImportsResponse(List<string> skipTargets, List<string> waitTargets)
        {
            if (_pendingTransfer == null) return;

            if (waitTargets.Count == 0)
            {
                // No waiting needed — execute immediately (with skipped targets excluded)
                ExecuteTransfer(_pendingTransfer, skipTargets);
                _pendingTransfer = null;
            }
            else
            {
                // Poll wait targets until clear — preserve skip targets for after wait
                _waitTargets = new HashSet<string>(waitTargets);
                _pendingSkipTargets = skipTargets;
                cfForm.ShowProgress(true);
                foreach (var t in waitTargets)
                {
                    SendProgress("", t, "running", "Waiting for active imports to complete...", 0, "import");
                }

                _waitTimer.Tick -= WaitTimer_Tick;
                _waitTimer.Tick += WaitTimer_Tick;
                _waitTimer.Start();
            }
        }

        private void WaitTimer_Tick(object sender, EventArgs e)
        {
            if (_waitTargets == null || _waitTargets.Count == 0 || _pendingTransfer == null)
            {
                _waitTimer.Stop();
                return;
            }

            var targetsToCheck = new List<string>(_waitTargets);

            foreach (var targetName in targetsToCheck)
            {
                var cd = AdditionalConnectionDetails.FirstOrDefault(c => c.ConnectionName == targetName);
                if (cd == null) { _waitTargets.Remove(targetName); continue; }

                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        we.Result = SolutionTransferService.GetActiveImports(cd);
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null) return;

                        var active = (List<SolutionTransferService.ActiveImportInfo>)we.Result;
                        if (active.Count == 0)
                        {
                            _waitTargets.Remove(targetName);
                            SendProgress("", targetName, "success", "Ready", 100, "import");

                            if (_waitTargets.Count == 0)
                            {
                                _waitTimer.Stop();
                                // All wait targets are clear — proceed (preserve skip targets)
                                ExecuteTransfer(_pendingTransfer, _pendingSkipTargets);
                                _pendingTransfer = null;
                                _pendingSkipTargets = null;
                            }
                        }
                        else
                        {
                            var names = string.Join(", ", active.Select(a => $"{a.SolutionName} ({a.Progress:N0}%)"));
                            SendProgress("", targetName, "running", $"Waiting: {names}", 0, "import");
                        }
                    }
                });
            }
        }

        private void CfForm_RetryRequested(object sender, EventArgs e)
        {
            if (_lastTransferArgs == null) return;
            ExecuteTransfer(_lastTransferArgs, _lastSkipTargets);
        }

        private void ExecuteTransfer(StartTransferEventArgs e, List<string> skipTargets)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            // Store for retry
            _lastTransferArgs = e;
            _lastSkipTargets = skipTargets;
            cfForm.ShowRetryButton(false);

            // Connection reference check — log warnings but proceed (confirmation was in React)
            var solutionIds = e.Solutions.Select(s => System.Guid.Parse(s.SolutionId)).ToList();
            var connRefWarnings = ConnectionRefCheckService.Check(sourceService, solutionIds, AdditionalConnectionDetails.ToList());
            if (connRefWarnings.Count > 0)
            {
                var warningMsg = string.Join("\n", connRefWarnings.Select(w => w.Message));
                cfForm.ShowAlert("Connection Reference Warnings", warningMsg, "warning");
            }

            // Show progress panel in React
            cfForm.ShowProgress(true);

            // Version bump: only update solutions where React explicitly sent a newVersion
            // (React omits newVersion when the user ticked "Skip version update" or unticked the solution row)
            foreach (var sol in e.Solutions)
            {
                if (string.IsNullOrEmpty(sol.NewVersion)) continue;
                SolutionTransferService.UpdateSolutionVersion(sourceService, System.Guid.Parse(sol.SolutionId), sol.NewVersion);
                Trace.WriteLine($"[Transfer] Version updated: {sol.FriendlyName} → {sol.NewVersion}");
                sol.Version = sol.NewVersion;
            }

            foreach (var sol in e.Solutions)
            {
                // Get per-solution settings (profile override or defaults)
                var solSettings = e.Settings.ForSolution(sol.UniqueName);

                // Phase 1: Export (once from source)
                SendProgress(sol.FriendlyName, "", "running", "Exporting...", 0, "export");

                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        var exportResult = SolutionTransferService.ExportSolution(
                            sourceService, sol.UniqueName, solSettings);
                        if (!exportResult.Success)
                            throw new Exception($"Export failed: {exportResult.ErrorMessage}");

                        // Auto-save to disk if configured
                        if (settings != null && settings.AutoExportSolutionsToDisk
                            && !string.IsNullOrEmpty(settings.AutoExportSolutionsFolderPath))
                        {
                            SolutionTransferService.SaveSolutionToDisk(
                                exportResult.SolutionContent, sol.UniqueName, sol.Version,
                                solSettings.Managed, settings.AutoExportSolutionsFolderPath);
                        }

                        we.Result = exportResult.SolutionContent;
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            Trace.WriteLine($"[Transfer] EXPORT FAILED: {sol.FriendlyName}: {we.Error.Message}");
                            SendProgress(sol.FriendlyName, "", "error", we.Error.Message, 100, "export");
                            cfForm.ShowRetryButton(true);
                            return;
                        }

                        Trace.WriteLine($"[Transfer] EXPORT SUCCESS: {sol.FriendlyName}");
                        SendProgress(sol.FriendlyName, "", "success", "Exported", 100, "export");

                        var solutionContent = (byte[])we.Result;

                        // Filter targets (exclude skipped ones)
                        var targets = AdditionalConnectionDetails.AsEnumerable();
                        if (skipTargets != null && skipTargets.Count > 0)
                            targets = targets.Where(cd => !skipTargets.Contains(cd.ConnectionName));

                        // Phase 2: Import async (per target, in parallel) — polling handles progress + publish
                        foreach (var cd in targets)
                        {
                            SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Starting import...", 0, "import");

                            WorkAsync(new WorkAsyncInfo
                            {
                                Message = null,
                                Work = (bw2, we2) =>
                                {
                                    we2.Result = SolutionTransferService.ImportSolutionAsync(
                                        cd, solutionContent, solSettings);
                                },
                                PostWorkCallBack = we2 =>
                                {
                                    if (we2.Error != null)
                                    {
                                        Trace.WriteLine($"[Transfer] IMPORT LAUNCH FAILED: {sol.FriendlyName} → {cd.ConnectionName}: {we2.Error.Message}");
                                        SendProgress(sol.FriendlyName, cd.ConnectionName, "error", we2.Error.Message, 100, "import");
                                        cfForm.ShowRetryButton(true);
                                        return;
                                    }

                                    var importResult = (SolutionTransferService.ImportResult)we2.Result;
                                    if (!importResult.Success)
                                    {
                                        Trace.WriteLine($"[Transfer] IMPORT FAILED: {sol.FriendlyName} → {cd.ConnectionName}: {importResult.ErrorMessage}");
                                        SendProgress(sol.FriendlyName, cd.ConnectionName, "error", importResult.ErrorMessage, 100, "import");
                                        cfForm.ShowRetryButton(true);
                                        return;
                                    }

                                    Trace.WriteLine($"[Transfer] IMPORT ASYNC STARTED: {sol.FriendlyName} → {cd.ConnectionName} (AsyncOp: {importResult.AsyncOperationId})");
                                    SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Importing...", 0, "import");

                                    // Register for polling
                                    lock (_activeImports)
                                    {
                                        _activeImports.Add(new ActiveImport
                                        {
                                            SolutionName = sol.FriendlyName,
                                            TargetName = cd.ConnectionName,
                                            Target = cd,
                                            AsyncOperationId = importResult.AsyncOperationId,
                                            ImportJobId = importResult.ImportJobId,
                                            Settings = solSettings,
                                            StartedAt = DateTime.UtcNow
                                        });

                                        if (!_importTimer.Enabled)
                                            _importTimer.Start();
                                    }
                                }
                            });
                        }
                    }
                });
            }
        }

        private void ImportTimer_Tick(object sender, EventArgs e)
        {
            List<ActiveImport> snapshot;
            lock (_activeImports)
            {
                if (_activeImports.Count == 0)
                {
                    _importTimer.Stop();
                    return;
                }
                snapshot = new List<ActiveImport>(_activeImports);
            }

            foreach (var ai in snapshot)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        we.Result = SolutionTransferService.PollImportProgress(
                            ai.Target, ai.AsyncOperationId, ai.ImportJobId);
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null) return;

                        var (status, progress, errorMessage) = ((string, double, string))we.Result;

                        if (status == "running")
                        {
                            SendProgress(ai.SolutionName, ai.TargetName, "running",
                                $"Importing... {progress:N0}%", (int)progress, "import");
                        }
                        else if (status == "success")
                        {
                            Trace.WriteLine($"[Transfer] IMPORT SUCCESS: {ai.SolutionName} → {ai.TargetName}");
                            SendProgress(ai.SolutionName, ai.TargetName, "success", "Imported", 100, "import");

                            lock (_activeImports)
                            {
                                _activeImports.Remove(ai);
                                if (_activeImports.Count == 0)
                                    _importTimer.Stop();
                            }

                            // Phase 3: Publish
                            StartPublish(ai);
                        }
                        else // error
                        {
                            Trace.WriteLine($"[Transfer] IMPORT FAILED: {ai.SolutionName} → {ai.TargetName}: {errorMessage}");
                            SendProgress(ai.SolutionName, ai.TargetName, "error", errorMessage, 100, "import");
                            cfForm.ShowRetryButton(true);

                            lock (_activeImports)
                            {
                                _activeImports.Remove(ai);
                                if (_activeImports.Count == 0)
                                    _importTimer.Stop();
                            }

                            // Send transfer result (failed)
                            var totalMs = (long)(DateTime.UtcNow - ai.StartedAt).TotalMilliseconds;
                            var json = JsonConvert.SerializeObject(new
                            {
                                solution = ai.SolutionName,
                                target = ai.TargetName,
                                success = false,
                                error = errorMessage ?? "",
                                elapsedMs = totalMs
                            });
                            cfForm.SendTransferResult(json);
                        }
                    }
                });
            }
        }

        private void StartPublish(ActiveImport ai)
        {
            SendProgress(ai.SolutionName, ai.TargetName, "running", "Publishing...", 0, "publish");

            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw, we) =>
                {
                    SolutionTransferService.PublishCustomizations(ai.Target);
                    we.Result = true;
                },
                PostWorkCallBack = we =>
                {
                    if (we.Error != null)
                    {
                        Trace.WriteLine($"[Transfer] PUBLISH FAILED: {ai.SolutionName} → {ai.TargetName}: {we.Error.Message}");
                        SendProgress(ai.SolutionName, ai.TargetName, "error", we.Error.Message, 100, "publish");
                    }
                    else
                    {
                        Trace.WriteLine($"[Transfer] SUCCESS: {ai.SolutionName} → {ai.TargetName}");
                        SendProgress(ai.SolutionName, ai.TargetName, "success", "Complete", 100, "publish");
                    }

                    var totalMs = (long)(DateTime.UtcNow - ai.StartedAt).TotalMilliseconds;
                    var json = JsonConvert.SerializeObject(new
                    {
                        solution = ai.SolutionName,
                        target = ai.TargetName,
                        success = we.Error == null,
                        error = we.Error?.Message ?? "",
                        elapsedMs = totalMs
                    });
                    cfForm.SendTransferResult(json);

                    // Toast notification
                    if (settings?.UseWindowsToastNotification == true)
                    {
                        try
                        {
                            new ToastContentBuilder()
                                .AddText(we.Error == null ? "Transfer Complete" : "Transfer Failed")
                                .AddText($"{ai.SolutionName} → {ai.TargetName}: {(we.Error == null ? "Success" : we.Error.Message)}")
                                .Show();
                        }
                        catch { /* toast may not be available */ }
                    }

                    // Refresh target versions after transfer
                    FetchTargetSolutions(ai.Target);
                }
            });
        }

        private void SendProgress(string solution, string target, string status, string message, int percentage, string phase = "export")
        {
            var id = $"{solution}|{target}|{phase}";

            // Track start time for new items
            if (status == "running" && !_phaseStartTimes.ContainsKey(id))
            {
                _phaseStartTimes[id] = DateTime.UtcNow;
            }

            // Compute elapsed
            string startedAtIso = null;
            long? elapsedMs = null;
            if (_phaseStartTimes.TryGetValue(id, out var startedAt))
            {
                startedAtIso = startedAt.ToString("o");
                elapsedMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            }

            // Clean up completed items
            if (status == "success" || status == "error")
            {
                _phaseStartTimes.Remove(id);
            }

            var json = JsonConvert.SerializeObject(new
            {
                id,
                action = $"{solution}",
                direction = $"→ {target}",
                target,
                phase,
                status,
                percentage,
                elapsed = status == "error" ? "Failed" : message,
                errorMessage = status == "error" ? message : (string)null,
                startedAt = startedAtIso,
                elapsedMs,
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
                cfForm.ShowAlert("No Targets", "No target environments connected.", "warning");
                return;
            }

            var targetConnections = AdditionalConnectionDetails.AsEnumerable();
            if (!string.IsNullOrEmpty(e.TargetName))
            {
                targetConnections = targetConnections.Where(cd => cd.ConnectionName == e.TargetName);
            }

            foreach (var cd in targetConnections)
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
                            cfForm.ShowAlert("Error", we.Error.Message, "error");
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
                            // Get per-solution settings (profile override or defaults)
                            var solSettings = transferSettings.ForSolution(sol.UniqueName);

                            // Export from source
                            var exportResult = SolutionTransferService.ExportSolution(
                                sourceService, sol.UniqueName, solSettings);
                            if (!exportResult.Success)
                                throw new Exception($"Export failed: {exportResult.ErrorMessage}");

                            // Import to target
                            var importResult = SolutionTransferService.ImportSolution(
                                cd, exportResult.SolutionContent, solSettings);
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

                cfForm.ShowProgress(true);

                foreach (var sol in e.Solutions)
                {
                    SendProgress(sol.FriendlyName, "", "running", "Exporting...", 0, "export");

                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null, // React shows progress
                        Work = (bw, we) =>
                        {
                            var result = SolutionTransferService.ExportSolution(
                                sourceService, sol.UniqueName, new SolutionTransferService.TransferSettings
                                {
                                    Managed = settings?.Managed ?? true,
                                    ExportAutoNumberingSettings = settings?.ExportAutoNumberingSettings ?? false,
                                    ExportCalendarSettings = settings?.ExportCalendarSettings ?? false,
                                    ExportCustomizationSettings = settings?.ExportCustomizationSettings ?? false,
                                    ExportEmailTrackingSettings = settings?.ExportEmailTrackingSettings ?? false,
                                    ExportExternalApplications = settings?.ExportExternalApplications ?? false,
                                    ExportGeneralSettings = settings?.ExportGeneralSettings ?? false,
                                    ExportIsvConfig = settings?.ExportIsvConfig ?? false,
                                    ExportMarketingSettings = settings?.ExportMarketingSettings ?? false,
                                    ExportOutlookSynchronizationSettings = settings?.ExportOutlookSynchronizationSettings ?? false,
                                    ExportRelationshipRoles = settings?.ExportRelationshipRoles ?? false,
                                    ExportSales = settings?.ExportSales ?? false
                                });
                            if (!result.Success)
                                throw new Exception(result.ErrorMessage);

                            Invoke(new System.Action(() =>
                                SendProgress(sol.FriendlyName, "", "running", "Saving to disk...", 50, "export")));

                            var path = SolutionTransferService.SaveSolutionToDisk(
                                result.SolutionContent, sol.UniqueName, sol.Version,
                                settings?.Managed ?? true, fbd.SelectedPath);
                            we.Result = path;
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                            {
                                SendProgress(sol.FriendlyName, "", "error", we.Error.Message, 100, "export");
                                cfForm.ShowAlert("Export Error", we.Error.Message, "error");
                            }
                            else
                            {
                                SendProgress(sol.FriendlyName, "", "success", $"Saved to {we.Result}", 100, "export");
                            }
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
                cfForm.ShowAlert("No Targets", "No target environments connected.", "warning");
                return;
            }

            using (var ofd = new OpenFileDialog { Filter = "Solution files (*.zip)|*.zip", Multiselect = false })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                var content = System.IO.File.ReadAllBytes(ofd.FileName);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);

                // Build transfer settings from current plugin settings
                var transferSettings = new SolutionTransferService.TransferSettings
                {
                    Managed = settings?.Managed ?? true,
                    ImportMode = settings?.ImportMode.ToString() ?? "Update",
                    OverwriteUnmanaged = settings?.OverwriteUnmanagedCustomizations ?? true,
                    PublishWorkflows = settings?.PublishWorkflows ?? true,
                    ConvertToManaged = settings?.ConvertToManaged ?? false,
                    SkipProductUpdateDependencies = settings?.SkipProductUpdateDependencies ?? false
                };

                cfForm.ShowProgress(true);

                foreach (var cd in AdditionalConnectionDetails)
                {
                    SendProgress(fileName, cd.ConnectionName, "running", "Starting import...", 0, "import");

                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null,
                        Work = (bw, we) =>
                        {
                            we.Result = SolutionTransferService.ImportSolutionAsync(cd, content, transferSettings);
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                            {
                                Trace.WriteLine($"[ImportFromFile] FAILED: {fileName} → {cd.ConnectionName}: {we.Error.Message}");
                                SendProgress(fileName, cd.ConnectionName, "error", we.Error.Message, 100, "import");
                                return;
                            }

                            var importResult = (SolutionTransferService.ImportResult)we.Result;
                            if (!importResult.Success)
                            {
                                Trace.WriteLine($"[ImportFromFile] FAILED: {fileName} → {cd.ConnectionName}: {importResult.ErrorMessage}");
                                SendProgress(fileName, cd.ConnectionName, "error", importResult.ErrorMessage, 100, "import");
                                return;
                            }

                            Trace.WriteLine($"[ImportFromFile] ASYNC STARTED: {fileName} → {cd.ConnectionName} (AsyncOp: {importResult.AsyncOperationId})");
                            SendProgress(fileName, cd.ConnectionName, "running", "Importing...", 0, "import");

                            // Register for async polling — same mechanism as normal transfer
                            lock (_activeImports)
                            {
                                _activeImports.Add(new ActiveImport
                                {
                                    SolutionName = fileName,
                                    TargetName = cd.ConnectionName,
                                    Target = cd,
                                    AsyncOperationId = importResult.AsyncOperationId,
                                    ImportJobId = importResult.ImportJobId,
                                    Settings = transferSettings,
                                    StartedAt = DateTime.UtcNow
                                });

                                if (!_importTimer.Enabled)
                                    _importTimer.Start();
                            }
                        }
                    });
                }
            }
        }

        // ── Remove solutions from targets ──

        private void CfForm_RemoveFromTargetsRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any()) return;

            cfForm.ShowProgress(true);

            // Confirmation is handled in React before this event fires
            foreach (var sol in e.Solutions)
            {
                foreach (var cd in AdditionalConnectionDetails)
                {
                    SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Removing...", 0, "import");

                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null,
                        Work = (bw, we) =>
                        {
                            we.Result = SolutionRemovalService.RemoveFromTarget(sol.UniqueName, sol.FriendlyName, cd);
                        },
                        PostWorkCallBack = we =>
                        {
                            if (we.Error != null)
                            {
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "error", we.Error.Message, 100, "import");
                                cfForm.ShowAlert("Error", we.Error.Message, "error");
                            }
                            else
                            {
                                SendProgress(sol.FriendlyName, cd.ConnectionName, "success", "Removed", 100, "import");
                                FetchTargetSolutions(cd);
                            }
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
                cfForm.ShowAlert("Warning", "Switch requires exactly one target.", "warning");
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

            // Confirmation is handled in React before this event fires
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
                            cfForm.ShowAlert("Error", we.Error.Message, "error");
                    }
                });
            }
        }

        // ── Find missing dependencies (pre-import check) ──

        private void CfForm_FindMissingDepsRequested(object sender, SolutionActionEventArgs e)
        {
            if (e.Solutions == null || e.Solutions.Count == 0 || !AdditionalConnectionDetails.Any())
            {
                cfForm.ShowAlert("No Selection", "Select one or more solutions and connect at least one target environment.", "warning");
                return;
            }

            cfForm.ShowProgress(true);
            var allResults = new System.Collections.Concurrent.ConcurrentBag<SolutionTransferService.MissingComponentResult>();
            // Use int[] so lambdas can capture by reference via the array
            var pendingBox = new int[] { e.Solutions.Count };

            foreach (var sol in e.Solutions)
            {
                SendProgress(sol.FriendlyName, "", "running", "Exporting for dependency check...", 0, "export");

                WorkAsync(new WorkAsyncInfo
                {
                    Message = null,
                    Work = (bw, we) =>
                    {
                        // Export solution bytes (needed for dependency check API)
                        var exportResult = SolutionTransferService.ExportSolution(
                            sourceService, sol.UniqueName,
                            new SolutionTransferService.TransferSettings { Managed = settings?.Managed ?? true });

                        if (!exportResult.Success)
                            throw new Exception($"Export failed: {exportResult.ErrorMessage}");

                        we.Result = exportResult.SolutionContent;
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            SendProgress(sol.FriendlyName, "", "error", we.Error.Message, 100, "export");
                            if (System.Threading.Interlocked.Decrement(ref pendingBox[0]) <= 0)
                                FinalizeMissingDepsResults(allResults);
                            return;
                        }

                        SendProgress(sol.FriendlyName, "", "success", "Exported", 100, "export");
                        var solutionContent = (byte[])we.Result;

                        // Check missing components on each target
                        var targetPendingBox = new int[] { AdditionalConnectionDetails.Count };
                        foreach (var cd in AdditionalConnectionDetails)
                        {
                            SendProgress(sol.FriendlyName, cd.ConnectionName, "running", "Checking dependencies...", 0, "import");

                            WorkAsync(new WorkAsyncInfo
                            {
                                Message = null,
                                Work = (bw2, we2) =>
                                {
                                    we2.Result = SolutionTransferService.CheckMissingComponents(
                                        cd, sol.FriendlyName, solutionContent);
                                },
                                PostWorkCallBack = we2 =>
                                {
                                    if (we2.Error != null)
                                    {
                                        SendProgress(sol.FriendlyName, cd.ConnectionName, "error", we2.Error.Message, 100, "import");
                                    }
                                    else
                                    {
                                        var missing = (System.Collections.Generic.List<SolutionTransferService.MissingComponentResult>)we2.Result;
                                        foreach (var m in missing) allResults.Add(m);

                                        if (missing.Count == 0)
                                            SendProgress(sol.FriendlyName, cd.ConnectionName, "success", "No missing dependencies", 100, "import");
                                        else
                                            SendProgress(sol.FriendlyName, cd.ConnectionName, "error",
                                                $"{missing.Count} missing component{(missing.Count != 1 ? "s" : "")}", 100, "import");
                                    }

                                    if (System.Threading.Interlocked.Decrement(ref targetPendingBox[0]) <= 0)
                                    {
                                        if (System.Threading.Interlocked.Decrement(ref pendingBox[0]) <= 0)
                                            FinalizeMissingDepsResults(allResults);
                                    }
                                }
                            });
                        }
                    }
                });
            }
        }

        // Sends aggregated missing deps results to React
        private void FinalizeMissingDepsResults(System.Collections.Concurrent.ConcurrentBag<SolutionTransferService.MissingComponentResult> results)
        {
            var list = results.ToList();
            var json = JsonConvert.SerializeObject(list);
            Trace.WriteLine($"[FindMissingDeps] Found {list.Count} missing component(s) across all targets.");
            cfForm.SendMissingDeps(json);
        }
    }
}
