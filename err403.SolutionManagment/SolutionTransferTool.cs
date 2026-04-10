using err403.SolutionManagment.AppCode;
using err403.SolutionManagment.Forms;
using err403.SolutionManagment.Properties;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.WebServiceClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using static err403.SolutionManagment.BaseToProcess;
using Settings = err403.SolutionManagment.AppCode.Settings;

namespace err403.SolutionManagment
{
    public partial class SolutionTransferTool : MultipleConnectionsPluginControlBase, IGitHubPlugin, IHelpPlugin
    {
        #region Variables

        private readonly MainForm mForm;
        private readonly ProgressForm pForm;
        private readonly EnvironmentVariablesForm evForm;
        private readonly CloudFlowsForm cfForm;
        private readonly EnvVarEditPanel evEditPanel;
        private bool envVarsLoaded;
        private bool flowsLoaded;
        private bool cancelPending;
        private Dictionary<Guid, List<ConnectionReferenceInfo>> connectionReferencesBySolution = new Dictionary<Guid, List<ConnectionReferenceInfo>>();
        private string lastConnectionName;
        private Guid lastImportId;
        private IOrganizationService lastTargetService;
        private MissingComponentsControl mcControl;
        private Settings oneTimeSettings;
        private Dictionary<OrganizationRequest, ProgressItem> progressItems;
        private Settings settings;
        private SettingsForm sForm;
        private Dictionary<int, string> solutionComponentTypes = new Dictionary<int, string>();
        private ConnectionDetail sourceDetail;
        private IOrganizationService sourceService;
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private List<BaseToProcess> toProcessList = new List<BaseToProcess>();

        #endregion Variables

        #region Constructor

        public SolutionTransferTool()
        {
            InitializeComponent();

            dpMain.Theme = new VS2015LightTheme();

            mForm = new MainForm();
            mForm.gbTargetOrgs = gbTargetOrgs;
            mForm.lblSource = lblSource;
            mForm.scOrganizations = scOrganizations;
            mForm.TargetOrganizationRemoved += MForm_TargetOrganizationRemoved;
            mForm.TargetOrganizationRequested += MForm_TargetOrganizationRequested;
            mForm.Show(dpMain, DockState.Document);

            evForm = new EnvironmentVariablesForm();
            evForm.EditRequested += EvForm_EditRequested;
            evForm.TransferRequested += EvForm_TransferRequested;
            evForm.RefreshRequested += EvForm_RefreshRequested;
            evForm.Show(dpMain, DockState.Document);

            cfForm = new CloudFlowsForm();
            cfForm.RefreshRequested += CfForm_RefreshRequested;
            cfForm.ActivateRequested += CfForm_ActivateRequested;
            cfForm.DeactivateRequested += CfForm_DeactivateRequested;
            cfForm.Show(dpMain, DockState.Document);

            pForm = new ProgressForm();
            pForm.OnRetry += PForm_OnRetry;
            pForm.Show(dpMain, DockState.DockRight);

            evEditPanel = new EnvVarEditPanel();
            evEditPanel.SaveRequested += EvEditPanel_SaveRequested;
            evEditPanel.Show(dpMain, DockState.DockRight);

            sForm = new SettingsForm();
            sForm.Show(dpMain, DockState.DockRight);

            dpMain.ActiveDocumentChanged += DpMain_ActiveDocumentChanged;

            mForm.Activate();

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                var args = ToastArguments.Parse(toastArgs.Argument);
                string pid = args["pid"];

                try
                {
                    // Get the process by ID
                    Process process = Process.GetProcessById(int.Parse(pid));

                    // Check if the process has a main window
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        // Bring the process to the foreground
                        SetForegroundWindow(process.MainWindowHandle);
                    }
                }
                catch
                {
                }
            };
            PrepareNotificationImages();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void PForm_OnRetry(object sender, EventArgs e)
        {
            Retry();
        }

        private void PrepareNotificationImages()
        {
            var successPath = Path.Combine(Path.GetTempPath(), "xtb.stt.success.png");
            var errorPath = Path.Combine(Path.GetTempPath(), "xtb.stt.error.png");
            var images = new[] { successPath, errorPath };

            foreach (var image in images)
            {
                if (File.Exists(image)) continue;

                using (MemoryStream imageStream = new MemoryStream())
                {
                    // Simulate writing an image to the stream (e.g., from a resource or download)
                    Image exampleImage = image.EndsWith("success.png") ? Resources.Success64 : Resources.Error64;
                    exampleImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    imageStream.Seek(0, SeekOrigin.Begin);

                    // Save the stream to a temporary file
                    using (FileStream fileStream = new FileStream(image, FileMode.Create, FileAccess.Write))
                    {
                        imageStream.CopyTo(fileStream);
                    }
                }
            }
        }

        #endregion Constructor

        #region Forms events callback

        private void DpMain_ActiveDocumentChanged(object sender, EventArgs e)
        {
            bool isEnvTab = dpMain.ActiveDocument == evForm;
            bool isFlowTab = dpMain.ActiveDocument == cfForm;
            bool isSolutionTab = !isEnvTab && !isFlowTab;

            // Right panels
            if (isEnvTab)
            {
                sForm.Hide();
                pForm.Hide();
                evEditPanel.Show(dpMain, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);

                if (!envVarsLoaded && sourceService != null)
                {
                    envVarsLoaded = true;
                    RetrieveEnvironmentVariables();
                }
            }
            else if (isFlowTab)
            {
                sForm.Hide();
                pForm.Hide();
                evEditPanel.Hide();

                if (!flowsLoaded && sourceService != null)
                {
                    flowsLoaded = true;
                    RetrieveCloudFlows();
                }
            }
            else
            {
                evEditPanel.Hide();
                pForm.Show(dpMain, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
                sForm.Show(dpMain, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
            }

            // Solution-specific buttons
            tsbLoadSolutions.Visible = isSolutionTab;
            toolStripSeparator1.Visible = isSolutionTab;
            tssbTransfer.Visible = isSolutionTab;
            if (!isSolutionTab) tsbCancel.Visible = false;
            tsbImportFromFile.Visible = isSolutionTab;
            toolStripSeparator2.Visible = isSolutionTab;
            tsbDownload.Visible = isSolutionTab;
            tsbExportSolutions.Visible = isSolutionTab;
            toolStripSeparator3.Visible = isSolutionTab;
            tsbRemoveFromTargets.Visible = isSolutionTab;
            toolStripSeparator4.Visible = isSolutionTab;
            tsbSwitchOrgs.Visible = isSolutionTab;
            tsbFindMissingDependencies.Visible = isSolutionTab;

            // Environment Variables buttons
            tsbEnvSeparator.Visible = isEnvTab;
            tsbRefreshEnvVars.Visible = isEnvTab;
            tsbTransferEnvVars.Visible = isEnvTab;

            // Cloud Flows buttons
            tsbFlowSeparator.Visible = isFlowTab;
            tsbRefreshFlows.Visible = isFlowTab;
            tsbActivateFlows.Visible = isFlowTab;
            tsbDeactivateFlows.Visible = isFlowTab;
        }

        private void EvForm_EditRequested(object sender, Forms.EnvVarEditRequestedEventArgs e)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, "No target environments connected. Add targets from the Solutions tab first.",
                    "No Targets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var targets = new List<Forms.TargetVariableInfo>();

            foreach (var cd in AdditionalConnectionDetails)
            {
                var col = evForm.LvEnvVars.Columns.Cast<ColumnHeader>()
                    .FirstOrDefault(c => c.Text == cd.ConnectionName);

                string targetValue = null;
                bool exists = false;

                if (col != null)
                {
                    var subItem = e.Item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .ElementAtOrDefault(col.Index);
                    if (subItem != null && subItem.Text != "(not found)")
                    {
                        targetValue = subItem.Text == "(default)" ? "" : subItem.Text;
                        exists = true;
                    }
                }

                targets.Add(new Forms.TargetVariableInfo
                {
                    Detail = cd,
                    Value = targetValue,
                    Exists = exists
                });
            }

            evEditPanel.LoadVariable(e.DisplayName, e.SchemaName, e.TypeName, e.SourceValue, targets, e.Item);
        }

        private void EvEditPanel_SaveRequested(object sender, Forms.EnvVarEditSaveEventArgs e)
        {
            if (e.Item == null || !e.ChangedValues.Any()) return;

            var def = (Entity)e.Item.Tag;
            var schemaName = e.Item.SubItems[1].Text; // colSchemaName index
            var displayName = e.Item.Text;

            var editArgs = new Forms.EnvVarEditRequestedEventArgs
            {
                DisplayName = displayName,
                SchemaName = schemaName,
                Item = e.Item,
                Definition = def
            };

            SaveEnvironmentVariableChanges(editArgs, e.ChangedValues);
        }

        private void SaveEnvironmentVariableChanges(Forms.EnvVarEditRequestedEventArgs e,
            Dictionary<McTools.Xrm.Connection.ConnectionDetail, string> changedValues)
        {
            foreach (var kvp in changedValues)
            {
                var cd = kvp.Key;
                var newValue = kvp.Value;

                WorkAsync(new WorkAsyncInfo
                {
                    Message = $"Updating \"{e.DisplayName}\" on {cd.ConnectionName}...",
                    Work = (bw, we) =>
                    {
                        var svc = cd.GetCrmServiceClient();

                        // Find the definition on target by schema name
                        var defQuery = new QueryExpression("environmentvariabledefinition")
                        {
                            ColumnSet = new ColumnSet("environmentvariabledefinitionid"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("schemaname", ConditionOperator.Equal, e.SchemaName),
                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                }
                            }
                        };
                        var targetDef = svc.RetrieveMultiple(defQuery).Entities.FirstOrDefault();
                        if (targetDef == null) throw new Exception($"Definition '{e.SchemaName}' not found on {cd.ConnectionName}");

                        // Find existing value record
                        var valQuery = new QueryExpression("environmentvariablevalue")
                        {
                            ColumnSet = new ColumnSet("environmentvariablevalueid"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.Equal, targetDef.Id),
                                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                }
                            }
                        };
                        var existingVal = svc.RetrieveMultiple(valQuery).Entities.FirstOrDefault();

                        if (existingVal != null)
                        {
                            existingVal["value"] = newValue;
                            svc.Update(existingVal);
                        }
                        else
                        {
                            var newVal = new Entity("environmentvariablevalue")
                            {
                                ["value"] = newValue,
                                ["environmentvariabledefinitionid"] = new EntityReference("environmentvariabledefinition", targetDef.Id)
                            };
                            svc.Create(newVal);
                        }

                        we.Result = cd;
                    },
                    PostWorkCallBack = we =>
                    {
                        if (we.Error != null)
                        {
                            MessageBox.Show(this,
                                $"Error updating on {cd.ConnectionName}:\n{we.Error.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Refresh the target column in the list
                        var col = evForm.LvEnvVars.Columns.Cast<ColumnHeader>()
                            .FirstOrDefault(c => c.Text == cd.ConnectionName);
                        if (col != null)
                        {
                            var subItem = e.Item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                .ElementAtOrDefault(col.Index);
                            if (subItem != null)
                            {
                                subItem.Text = newValue;
                                var sourceValue = e.Item.SubItems[evForm.ColCurrentValue.Index].Text;
                                if (newValue == sourceValue)
                                {
                                    subItem.BackColor = Color.LightGreen;
                                    subItem.ForeColor = Color.DarkGreen;
                                }
                                else
                                {
                                    subItem.BackColor = SystemColors.Info;
                                    subItem.ForeColor = Color.DarkRed;
                                }
                            }
                        }
                    }
                });
            }
        }

        private void EvForm_TransferRequested(object sender, Forms.EnvVarTransferRequestedEventArgs e)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, "No target environments connected. Add targets from the Solutions tab first.",
                    "No Targets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var targetList = AdditionalConnectionDetails.ToList();

            using (var summary = new Forms.TransferEnvVarSummaryForm(e.Items, targetList))
            {
                if (summary.ShowDialog(this) != DialogResult.OK || summary.ConfirmedItems == null) return;

                foreach (var item in summary.ConfirmedItems)
                {
                    foreach (var cd in targetList)
                    {
                        var sourceValue = item.SourceValue;
                        var schemaName = item.SchemaName;
                        var displayName = item.DisplayName;
                        var listViewItem = item.Item;

                        WorkAsync(new WorkAsyncInfo
                        {
                            Message = $"Transferring \"{displayName}\" to {cd.ConnectionName}...",
                            Work = (bw, we) =>
                            {
                                var svc = cd.GetCrmServiceClient();

                                var defQuery = new QueryExpression("environmentvariabledefinition")
                                {
                                    ColumnSet = new ColumnSet("environmentvariabledefinitionid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("schemaname", ConditionOperator.Equal, schemaName),
                                            new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                        }
                                    }
                                };
                                var targetDef = svc.RetrieveMultiple(defQuery).Entities.FirstOrDefault();
                                if (targetDef == null) throw new Exception($"Definition '{schemaName}' not found on {cd.ConnectionName}");

                                var valQuery = new QueryExpression("environmentvariablevalue")
                                {
                                    ColumnSet = new ColumnSet("environmentvariablevalueid"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.Equal, targetDef.Id),
                                            new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                                        }
                                    }
                                };
                                var existingVal = svc.RetrieveMultiple(valQuery).Entities.FirstOrDefault();

                                if (existingVal != null)
                                {
                                    existingVal["value"] = sourceValue;
                                    svc.Update(existingVal);
                                }
                                else
                                {
                                    var newVal = new Entity("environmentvariablevalue")
                                    {
                                        ["value"] = sourceValue,
                                        ["environmentvariabledefinitionid"] = new EntityReference("environmentvariabledefinition", targetDef.Id)
                                    };
                                    svc.Create(newVal);
                                }

                                we.Result = new Tuple<ConnectionDetail, ListViewItem, string>(cd, listViewItem, sourceValue);
                            },
                            PostWorkCallBack = we =>
                            {
                                if (we.Error != null)
                                {
                                    MessageBox.Show(this,
                                        $"Error transferring \"{displayName}\" to {cd.ConnectionName}:\n{we.Error.Message}",
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                var result = (Tuple<ConnectionDetail, ListViewItem, string>)we.Result;
                                var col = evForm.LvEnvVars.Columns.Cast<ColumnHeader>()
                                    .FirstOrDefault(c => c.Text == result.Item1.ConnectionName);
                                if (col != null)
                                {
                                    var subItem = result.Item2.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                        .ElementAtOrDefault(col.Index);
                                    if (subItem != null)
                                    {
                                        subItem.Text = result.Item3;
                                        subItem.BackColor = Color.LightGreen;
                                        subItem.ForeColor = Color.DarkGreen;
                                    }
                                }
                            }
                        });
                    }
                }
            }
        }

        private void EvForm_RefreshRequested(object sender, EventArgs e)
        {
            if (sourceService == null) return;
            envVarsLoaded = true;
            RetrieveEnvironmentVariables();
        }

        private void MForm_TargetOrganizationRemoved(object sender, TargetOrganizationsEventArgs e)
        {
            var toRemove = AdditionalConnectionDetails.FirstOrDefault(c => !e.TargetOrganizations.Contains(c));

            if (toRemove != null)
            {
                // Remove the matching column from the env vars list
                var col = evForm.LvEnvVars.Columns.Cast<ColumnHeader>()
                    .FirstOrDefault(c => c.Text == toRemove.ConnectionName);
                if (col != null)
                {
                    var idx = col.Index;
                    evForm.LvEnvVars.Columns.Remove(col);
                    foreach (ListViewItem item in evForm.LvEnvVars.Items)
                    {
                        if (item.SubItems.Count > idx)
                            item.SubItems.RemoveAt(idx);
                    }
                }

                // Remove the matching column from the cloud flows list
                cfForm.RemoveTargetColumn(toRemove);
            }

            RemoveAdditionalOrganization(toRemove);
        }

        private void MForm_TargetOrganizationRequested(object sender, EventArgs e)
        {
            AddAdditionalOrganization();
        }

        private void btnAddTarget_Click(object sender, EventArgs e)
        {
            AddAdditionalOrganization();
        }

        #endregion Forms events callback

        #region XrmToolbox

        public string HelpUrl => "https://github.com/garethcheyne/SolutionTransferTool/wiki";
        public string RepositoryName => "SolutionTransferTool";

        public string UserName => "garethcheyne";

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (ConnectionDetail == null || settings == null) return;

            settings.Save(ConnectionDetail?.ConnectionName);

            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            if (actionName == "AdditionalOrganization")
            {
                if (!AdditionalConnectionDetails.Any(c => c.ConnectionId == detail.ConnectionId))
                {
                    AdditionalConnectionDetails.Add(detail);

                    mForm.DisplayTargetOrganizations(AdditionalConnectionDetails.ToList());
                    mForm.DisplayTargetOrganizationsSolutions(AdditionalConnectionDetails.ToList(), this);
                    evForm.DisplayTargetEnvironmentValues(new List<ConnectionDetail> { detail }, this);
                    if (flowsLoaded)
                        cfForm.DisplayTargetFlowStatus(new List<ConnectionDetail> { detail }, this);
                }

                if (newService is OrganizationServiceProxy proxy)
                {
                    proxy.Timeout = detail.Timeout;
                }
                else if (newService is OrganizationWebProxyClient client)
                {
                    client.InnerChannel.OperationTimeout = detail.Timeout;
                }
            }
            else
            {
                settings?.Save(ConnectionDetail?.ConnectionName);

                ConnectionDetail = detail;
                sourceDetail = detail;
                sourceService = newService;
                RetrieveSolutions();

                if (!SettingsManager.Instance.TryLoad(GetType(), out settings, ConnectionDetail.ConnectionName))
                {
                    settings = new Settings();
                }

                sForm.Settings = settings;
                mForm.SetSourceOrganization(detail);

                base.UpdateConnection(newService, detail, actionName, parameter);
            }
        }

        protected override void ConnectionDetailsUpdated(NotifyCollectionChangedEventArgs e)
        {
            mForm.DisplayTargetOrganizations(AdditionalConnectionDetails.ToList());
        }

        #endregion XrmToolbox

        #region UI Events

        private void Pi_LogFileRequested(object sender, DownloadLogEventArgs e)
        {
            DownloadLogFile(e.ImportJobId, e.Service);
        }

        private void tsbFindMissingDependencies_Click(object sender, EventArgs e)
        {
            var child = new MissingComponentsForm();
            child.ShowMissingComponents(ParentForm, lastTargetService, lastConnectionName, sourceService, lastImportId);
        }

        private void TsbLoadSolutionsClick(object sender, EventArgs e)
        {
            ExecuteMethod(RetrieveSolutions);
        }

        private void tsbSwitchOrgs_Click(object sender, EventArgs e)
        {
            if (AdditionalConnectionDetails.Count > 1)
            {
                MessageBox.Show(this,
                    @"Switch can only be performed when no more than one target organization is defined",
                    @"Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var tempDetail = sourceDetail;
            sourceDetail = AdditionalConnectionDetails.FirstOrDefault();
            ConnectionDetail = AdditionalConnectionDetails.FirstOrDefault();
            AdditionalConnectionDetails.Clear();

            if (tempDetail != null)
            {
                AdditionalConnectionDetails.Add(tempDetail);
            }

            mForm.SwitchSourceAndTarget(tempDetail, sourceDetail);

            if (sourceDetail != null)
            {
                sourceService = sourceDetail.GetCrmServiceClient();
                base.UpdateConnection(sourceService, sourceDetail, "", null);
                RetrieveSolutions();
            }
        }

        private void tsbAbout_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void tsbRefreshEnvVars_Click(object sender, EventArgs e)
        {
            evForm.InvokeRefresh();
        }

        private void tsbTransferEnvVars_Click(object sender, EventArgs e)
        {
            evForm.InvokeTransfer();
        }

        private void TsbTransfertSolutionClick(object sender, EventArgs e)
        {
            oneTimeSettings = null;

            if (mForm.SelectedSolutions.Count == 0 || !AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, @"You have to select a source solution and a target organization to continue.", @"Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DoTransfer();
        }

        private void tssbTransfer_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (mForm.SelectedSolutions.Count == 0 || !AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, @"You have to select a source solution and a target organization to continue.", @"Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new SettingsForm(true))
            {
                dialog.Settings = (Settings)settings.Clone();
                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    oneTimeSettings = (Settings)dialog.Settings;
                    DoTransfer();
                }
            }
        }

        private void tsbImportFromFile_Click(object sender, EventArgs e)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, @"You have to select at least one target organization to continue.", @"Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = @"Solution files (*.zip)|*.zip";
                ofd.Title = @"Select a solution file to import";
                ofd.Multiselect = false;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                var solutionContent = File.ReadAllBytes(ofd.FileName);
                var solutionName = Path.GetFileNameWithoutExtension(ofd.FileName);

                var fakeSolution = new Entity("solution")
                {
                    Id = Guid.NewGuid()
                };
                fakeSolution["friendlyname"] = solutionName;
                fakeSolution["uniquename"] = solutionName;
                fakeSolution["version"] = "";

                progressItems = new Dictionary<OrganizationRequest, ProgressItem>();
                toProcessList = new List<BaseToProcess>();

                var fakeExport = new ExportToProcess
                {
                    Solution = fakeSolution,
                    SolutionContent = solutionContent,
                    IsProcessed = true,
                    IsProcessing = false,
                    Succeeded = true,
                    CompletedOn = DateTime.Now,
                    StartedOn = DateTime.Now,
                    Detail = sourceDetail,
                    Request = new ExportSolutionRequest { SolutionName = solutionName }
                };
                toProcessList.Add(fakeExport);

                foreach (var detail in AdditionalConnectionDetails)
                {
                    toProcessList.Add(new ImportToProcess
                    {
                        Solution = fakeSolution,
                        Previous = toProcessList.OfType<ImportToProcess>().LastOrDefault(x => x.Detail == detail),
                        Export = fakeExport,
                        Request = PrepareImportRequest(detail, fakeSolution),
                        Detail = detail
                    });
                }

                pForm.Items = progressItems.Values.ToList();
                pForm.Start();
                pForm.Show(dpMain, DockState.DockRight);

                ToggleWaitMode(true);

                timer.Tick -= Timer_Elapsed;
                timer.Tick += Timer_Elapsed;
                timer.Interval = (int)(oneTimeSettings ?? settings).RefreshIntervalProp.TotalMilliseconds;
                timer.Start();
            }
        }

        private void tsbRemoveFromTargets_Click(object sender, EventArgs e)
        {
            if (mForm.SelectedSolutions.Count == 0 || !AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, @"You have to select a source solution and a target organization to continue.", @"Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedSolutions = mForm.SelectedSolutions;
            var targetDetails = AdditionalConnectionDetails.ToList();

            var solutionNames = string.Join(", ", selectedSolutions.Select(s => s.GetAttributeValue<string>("friendlyname")));
            var targetNames = string.Join(", ", targetDetails.Select(d => d.ConnectionName));

            var result = MessageBox.Show(this,
                $@"Are you sure you want to remove the following solution(s) from the target environment(s)?

Solutions: {solutionNames}
Targets: {targetNames}",
                @"Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            var removeProgressItems = new List<ProgressItem>();
            var itemLookup = new Dictionary<string, ProgressItem>();

            foreach (var solution in selectedSolutions)
            {
                var friendlyName = solution.GetAttributeValue<string>("friendlyname");
                var uniqueName = solution.GetAttributeValue<string>("uniquename");
                var version = solution.GetAttributeValue<string>("version");

                foreach (var detail in targetDetails)
                {
                    var pi = new ProgressItem
                    {
                        Type = Enumerations.RequestType.Remove,
                        Detail = detail,
                        Solution = friendlyName,
                        SolutionVersion = version
                    };
                    removeProgressItems.Add(pi);
                    itemLookup[$"{uniqueName}|{detail.ConnectionName}"] = pi;
                }
            }

            pForm.Items = removeProgressItems;
            pForm.Start();
            pForm.Show(dpMain, WeifenLuo.WinFormsUI.Docking.DockState.DockRight);

            ToggleWaitMode(true);

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Removing solutions from target(s)...",
                Work = (bw, evt) =>
                {
                    var removedCount = 0;

                    foreach (var solution in selectedSolutions)
                    {
                        var uniqueName = solution.GetAttributeValue<string>("uniquename");
                        var friendlyName = solution.GetAttributeValue<string>("friendlyname");

                        foreach (var detail in targetDetails)
                        {
                            var key = $"{uniqueName}|{detail.ConnectionName}";
                            var pi = itemLookup[key];
                            var startedOn = DateTime.Now;
                            pi.Start();

                            try
                            {
                                var svc = detail.GetCrmServiceClient();
                                var query = new QueryExpression("solution")
                                {
                                    ColumnSet = new ColumnSet("solutionid", "ismanaged", "friendlyname"),
                                    Criteria = new FilterExpression
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName)
                                        }
                                    }
                                };
                                var targetSolution = svc.RetrieveMultiple(query).Entities.FirstOrDefault();
                                if (targetSolution == null)
                                {
                                    pi.Skip($"Solution not found on target");
                                    continue;
                                }

                                var isManaged = targetSolution.GetAttributeValue<bool>("ismanaged");
                                if (isManaged)
                                {
                                    var proceed = false;
                                    Invoke(new Action(() =>
                                    {
                                        proceed = MessageBox.Show(this,
                                            $@"Solution '{targetSolution.GetAttributeValue<string>("friendlyname")}' is MANAGED in {detail.ConnectionName}.

Removing a managed solution will permanently delete ALL its components (entities, fields, workflows, plugins, etc.) from the target environment.

Are you sure you want to proceed?",
                                            @"Managed Solution Warning",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Warning) == DialogResult.Yes;
                                    }));

                                    if (!proceed)
                                    {
                                        pi.Skip("Skipped by user (managed solution)");
                                        continue;
                                    }
                                }

                                svc.Delete("solution", targetSolution.Id);
                                pi.Success(startedOn, DateTime.Now);
                                removedCount++;
                            }
                            catch (Exception ex)
                            {
                                pi.Error(DateTime.Now, ex.Message);
                            }
                        }
                    }

                    evt.Result = removedCount;
                },
                PostWorkCallBack = evt =>
                {
                    ToggleWaitMode(false);

                    if (evt.Error != null)
                    {
                        MessageBox.Show(this, $@"An error occurred: {evt.Error.Message}", @"Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var removedCount = (int)evt.Result;

                    mForm.DisplayTargetOrganizationsSolutions(targetDetails, this);

                    if (removedCount > 0)
                    {
                        var removeFromSource = MessageBox.Show(this,
                            @"Do you also want to remove the selected solution(s) from the source environment?",
                            @"Remove from Source",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (removeFromSource == DialogResult.Yes)
                        {
                            RemoveSolutionsFromSource(selectedSolutions);
                        }
                    }
                }
            });
        }

        private void RemoveSolutionsFromSource(List<Entity> solutions)
        {
            ToggleWaitMode(true);

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Removing solution(s) from source...",
                Work = (bw, evt) =>
                {
                    var errors = new List<string>();
                    foreach (var solution in solutions)
                    {
                        var uniqueName = solution.GetAttributeValue<string>("uniquename");
                        bw.ReportProgress(0, $"Removing {uniqueName} from source...");
                        try
                        {
                            sourceService.Delete("solution", solution.Id);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error removing '{uniqueName}' from source: {ex.Message}");
                        }
                    }
                    evt.Result = errors;
                },
                PostWorkCallBack = evt =>
                {
                    ToggleWaitMode(false);

                    if (evt.Error != null)
                    {
                        MessageBox.Show(this, $@"An error occurred: {evt.Error.Message}", @"Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var errors = (List<string>)evt.Result;
                    if (errors.Count > 0)
                    {
                        MessageBox.Show(this,
                            $@"Completed with issues:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                            @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(this, @"Solution(s) removed from source successfully.", @"Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    RetrieveSolutions();
                },
                ProgressChanged = evt =>
                {
                    SetWorkingMessage(evt.UserState.ToString());
                }
            });
        }

        #endregion UI Events

        #region Methods

        private void DoTransfer()
        {
            var solutionsToTransfer = mForm.SelectedSolutions;
            if (!(oneTimeSettings ?? settings).ShowPreImportSummary)
            {
                solutionsToTransfer = PreparareSolutionsToTransfer();
                if (solutionsToTransfer.Count == 0)
                {
                    return;
                }
            }

            if (ConnectionDetail.OrganizationMajorVersion > 9 || (ConnectionDetail.OrganizationMajorVersion == 9 && ConnectionDetail.OrganizationMinorVersion >= 1))
            {
                StringBuilder missingReferences = new StringBuilder();

                foreach (var targetDetail in AdditionalConnectionDetails)
                {
                    var missingConnectionReferences = SolutionHelper.CheckForNewConnectionReferences(solutionsToTransfer.Select(e => e.Id).ToList(), targetDetail.GetCrmServiceClient(), connectionReferencesBySolution);

                    if (missingConnectionReferences.Count > 0)
                    {
                        missingReferences.AppendLine($"- to {targetDetail.ConnectionName}");
                        foreach (var mcr in missingConnectionReferences)
                        {
                            missingReferences.AppendLine($"\t- {mcr}");
                        }
                    }
                }

                if (missingReferences.Length > 0)
                {
                    var result = MessageBox.Show(this, $@"It seems you are shipping new connection reference(s):
{missingReferences}
It is recommended to use Power Apps maker portal to import the solution that contains new connection references so that you can map new connection references in the target environment(s).

Alternatively, do not forget to update connection references in target environment(s) and start any flow that is using one of these connection references.

Are you sure you want to continue and import solution(s) using this tool?", @"New Connection reference(s) detected!",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.No) return;
                }
            }

            // Warn if exporting as managed but target already has unmanaged version
            if ((oneTimeSettings ?? settings).Managed)
            {
                var conflicts = new StringBuilder();

                foreach (var targetDetail in AdditionalConnectionDetails)
                {
                    var svc = targetDetail.GetCrmServiceClient();
                    var uniqueNames = solutionsToTransfer
                        .Select(s => s.GetAttributeValue<string>("uniquename"))
                        .ToArray();

                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("uniquename", "friendlyname", "ismanaged", "version"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("uniquename", ConditionOperator.In, uniqueNames),
                                new ConditionExpression("ismanaged", ConditionOperator.Equal, false)
                            }
                        }
                    };

                    var unmanagedOnTarget = svc.RetrieveMultiple(query).Entities;

                    foreach (var u in unmanagedOnTarget)
                    {
                        conflicts.AppendLine($"  \u2022 \"{u.GetAttributeValue<string>("friendlyname")}\" " +
                            $"(v{u.GetAttributeValue<string>("version")}) is UNMANAGED on {targetDetail.ConnectionName}");
                    }
                }

                if (conflicts.Length > 0)
                {
                    var result = MessageBox.Show(this,
                        $@"You are about to import a MANAGED solution over existing UNMANAGED solution(s):

{conflicts}
Importing a managed solution over an unmanaged one can cause unexpected behaviour — unmanaged customisations may be overwritten or the import may fail.

Consider removing the unmanaged solution from the target first, or importing as unmanaged instead.

Do you want to continue?",
                        @"Managed over Unmanaged Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No) return;
                }
            }

            foreach (var solution in solutionsToTransfer)
            {
                string newVersion = solution.GetAttributeValue<string>("version");

                if ((oneTimeSettings ?? settings).UpdateSourceSolutionVersionNew == UpdateVersionEnum.Yes
                    || (oneTimeSettings ?? settings).UpdateSourceSolutionVersionNew == UpdateVersionEnum.Prompt
                    )
                {
                    string computedNewVersion = "Manual";

                    if ((oneTimeSettings ?? settings).VersionSchema != VersionType.Manual)
                    {
                        computedNewVersion = GetUpdatedSolutionVersion(solution);
                    }

                    solution["newversion"] = computedNewVersion;
                }
            }

            bool hasUsedPreImportSummary = false;

            if ((oneTimeSettings ?? settings).ShowPreImportSummary)
            {
                var tmpSettings = (Settings)settings.Clone();

                using (var dialog = new PreImportSummaryForm(tmpSettings, solutionsToTransfer))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    oneTimeSettings = tmpSettings;
                    settings.ShowPreImportSummary = tmpSettings.ShowPreImportSummary;
                    settings.Save(ConnectionDetail?.ConnectionName);

                    hasUsedPreImportSummary = true;
                }
            }

            progressItems = new Dictionary<OrganizationRequest, ProgressItem>();
            toProcessList = new List<BaseToProcess>();

            foreach (var solution in solutionsToTransfer.OrderBy(s => s.GetAttributeValue<int>("sortorder")))
            {
                string newVersion = solution.GetAttributeValue<string>("version");

                if ((oneTimeSettings ?? settings).UpdateSourceSolutionVersionNew == UpdateVersionEnum.Yes
                    || (oneTimeSettings ?? settings).UpdateSourceSolutionVersionNew == UpdateVersionEnum.Prompt)
                {
                    if ((oneTimeSettings ?? settings).VersionSchema == VersionType.Manual)
                    {
                        var dialog = new UpdateVersionForm(solution.GetAttributeValue<string>("version"), solution.GetAttributeValue<string>("friendlyname"));
                        if (dialog.ShowDialog(this) == DialogResult.OK)
                        {
                            solution["newversion"] = dialog.NewVersion;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (!hasUsedPreImportSummary)
                        {
                            var computedNewVersion = GetUpdatedSolutionVersion(solution);

                            if ((oneTimeSettings ?? settings).UpdateSourceSolutionVersionNew == UpdateVersionEnum.Prompt)
                            {
                                if (DialogResult.Yes == MessageBox.Show(this,
                                $@"Do you want to update version for solution {solution.GetAttributeValue<string>("friendlyname")} ?

Current version: {solution.GetAttributeValue<string>("version")}
New version: {computedNewVersion}",
                                @"Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                                {
                                    solution["newversion"] = computedNewVersion;
                                }
                            }
                            else
                            {
                                solution["newversion"] = computedNewVersion;
                            }
                        }
                    }

                    if (solution.GetAttributeValue<string>("version") != solution.GetAttributeValue<string>("newversion")
                        && (!hasUsedPreImportSummary
                        || hasUsedPreImportSummary && solution.GetAttributeValue<bool>("updateversion")))
                    {
                        solution["version"] = solution.GetAttributeValue<string>("newversion");
                        solution.Attributes.Remove("newversion");
                        solution.Attributes.Remove("updateversion");
                        solution.Attributes.Remove("sortorder");
                        Service.Update(solution);
                        mForm.UpdateSolutionVersion(solution);
                    }

                    solution.Attributes.Remove("newversion");
                    solution.Attributes.Remove("updateversion");
                    solution.Attributes.Remove("sortorder");
                }

                var exportItem = new ExportToProcess
                {
                    Solution = solution,
                    Previous = toProcessList.OfType<ExportToProcess>().LastOrDefault(),
                    Request = PrepareExportRequest(solution),
                    Detail = sourceDetail
                };
                toProcessList.Add(exportItem);

                foreach (var detail in AdditionalConnectionDetails)
                {
                    toProcessList.Add(new ImportToProcess
                    {
                        Solution = solution,
                        Previous = toProcessList.OfType<ImportToProcess>().LastOrDefault(x => x.Detail == detail),
                        Export = exportItem,
                        Request = PrepareImportRequest(detail, solution),
                        Detail = detail
                    });
                }
            }

            if ((oneTimeSettings ?? settings).Publish)
            {
                foreach (var detail in AdditionalConnectionDetails)
                {
                    toProcessList.Add(new PublishToProcess
                    {
                        Request = PreparePublishRequest(detail),
                        Detail = detail
                    });
                }
            }

            // Add items to progress form
            pForm.Items = progressItems.Values.ToList();
            pForm.Start();

            pForm.Show(dpMain, DockState.DockRight);

            StartExport(toProcessList.OfType<ExportToProcess>().First());

            timer.Tick -= Timer_Elapsed;
            timer.Tick += Timer_Elapsed;
            timer.Interval = (int)(oneTimeSettings ?? settings).RefreshIntervalProp.TotalMilliseconds;
            timer.Start();
        }

        private void DownloadLogFile(Guid importJobId, IOrganizationService service)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.LastFolderUsed))
                    dialog.SelectedPath = Properties.Settings.Default.LastFolderUsed;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default.LastFolderUsed = dialog.SelectedPath;
                    Properties.Settings.Default.Save();

                ToggleWaitMode(true);

                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Downloading log file",
                    Work = (sender, e) =>
                    {
                        var importLogRequest = new RetrieveFormattedImportJobResultsRequest
                        {
                            ImportJobId = importJobId
                        };
                        var importLogResponse = (RetrieveFormattedImportJobResultsResponse)service.Execute(importLogRequest);

                        var filePath = $@"{dialog.SelectedPath}\{DateTime.Now:yyyy_MM_dd__HH_mm}.xml";
                        File.WriteAllText(filePath, importLogResponse.FormattedResults);

                        e.Result = filePath;
                    },
                    PostWorkCallBack = e =>
                    {
                        if (e.Error != null)
                        {
                            var message = string.Format("An error was encountered while downloading the log file.{0}Error:{0}{1}",
                                Environment.NewLine, e.Error.Message);
                            MessageBox.Show(message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            if (
                                MessageBox.Show(
                                    $@"Download completed!

Would you like to open the file now ({e.Result})?

(Microsoft Excel is required)",
                                    @"File Download", MessageBoxButtons.YesNo, MessageBoxIcon.Information) ==
                                DialogResult.Yes)
                            {
                                Process.Start("Excel.exe", $"\"{e.Result}\"");
                            }
                        }

                        ToggleWaitMode(false);
                    }
                });
                }
            }
        }

        private string GetUpdatedSolutionVersion(Entity etpSolution)
        {
            string version = etpSolution.GetAttributeValue<string>("version");

            if ((oneTimeSettings?.VersionSchema ?? settings.VersionSchema) == VersionType.Date)
            {
                int i = 0;
                var newVersion = DateTime.Now.ToString(oneTimeSettings?.VersionDateMask ?? settings.VersionDateMask).Replace("x", i.ToString());
                while (new Version(newVersion) <= new Version(version) && i < 1000)
                {
                    i++;
                    newVersion = DateTime.Now.ToString(oneTimeSettings?.VersionDateMask ?? settings.VersionDateMask).Replace("x", i.ToString());
                }

                return newVersion;
            }

            var versionParts = version.Split('.');
            switch (oneTimeSettings?.VersionSchema ?? settings.VersionSchema)
            {
                case VersionType.Major:
                    versionParts[0] = (int.Parse(versionParts[0]) + 1).ToString();
                    break;

                case VersionType.Minor:
                    if (versionParts.Length < 2) break;
                    versionParts[1] = (int.Parse(versionParts[1]) + 1).ToString();
                    break;

                case VersionType.Build:
                    if (versionParts.Length < 3) break;
                    versionParts[2] = (int.Parse(versionParts[2]) + 1).ToString();
                    break;

                case VersionType.Revision:
                    if (versionParts.Length < 4) break;
                    versionParts[3] = (int.Parse(versionParts[3]) + 1).ToString();
                    break;
            }

            return string.Join(".", versionParts);
        }

        private List<Entity> PreparareSolutionsToTransfer()
        {
            var solutionsToTransfer = new List<Entity>();
            if (mForm.SelectedSolutions.Count > 1)
            {
                // Open dialog to order solutions import
                foreach (var solution in mForm.SelectedSolutions)
                {
                    solutionsToTransfer.Add(solution);
                }

                var dialog = new SolutionOrderDialog(solutionsToTransfer);
                if (dialog.ShowDialog(ParentForm) == DialogResult.OK)
                {
                    solutionsToTransfer = dialog.Solutions;
                }
                else
                {
                    return new List<Entity>();
                }
            }
            else
            {
                solutionsToTransfer.Add(mForm.SelectedSolutions.First());
            }

            return solutionsToTransfer;
        }

        private ExportSolutionRequest PrepareExportRequest(Entity solution, ExportSolutionRequest request = null)
        {
            var isNull = request == null;
            if (isNull)
            {
                request = new ExportSolutionRequest();
            }

            request.Managed = (oneTimeSettings ?? settings).Managed;
            request.SolutionName = solution.GetAttributeValue<string>("uniquename");
            request.ExportAutoNumberingSettings = (oneTimeSettings ?? settings).ExportAutoNumberingSettings;
            request.ExportCalendarSettings = (oneTimeSettings ?? settings).ExportCalendarSettings;
            request.ExportCustomizationSettings = (oneTimeSettings ?? settings).ExportCustomizationSettings;
            request.ExportEmailTrackingSettings = (oneTimeSettings ?? settings).ExportEmailTrackingSettings;
            request.ExportGeneralSettings = (oneTimeSettings ?? settings).ExportGeneralSettings;
            request.ExportIsvConfig = (oneTimeSettings ?? settings).ExportIsvConfig;
            request.ExportMarketingSettings = (oneTimeSettings ?? settings).ExportMarketingSettings;
            request.ExportOutlookSynchronizationSettings = (oneTimeSettings ?? settings).ExportOutlookSynchronizationSettings;
            request.ExportRelationshipRoles = (oneTimeSettings ?? settings).ExportRelationshipRoles;
            request.ExportSales = (oneTimeSettings ?? settings).ExportSales;

            if (ConnectionDetail.OrganizationMajorVersion >= 8)
            {
                request.ExportExternalApplications = (oneTimeSettings ?? settings).ExportExternalApplications;
            }

            if (isNull)
            {
                progressItems.Add(request, new ProgressItem
                {
                    Type = Enumerations.RequestType.Export,
                    Detail = sourceDetail,
                    Solution = solution.GetAttributeValue<string>("friendlyname"),
                    SolutionVersion = solution.GetAttributeValue<string>("version"),
                    Request = request
                });
            }

            return request;
        }

        private OrganizationRequest PrepareImportRequest(ConnectionDetail detail, Entity solution, OrganizationRequest request = null)
        {
            var isPatch = solution.GetAttributeValue<string>("uniquename").ToLower().Contains("_patch_");
            var isNull = request == null;
            if (isNull)
            {
                request = (oneTimeSettings ?? settings).ImportMode == ImportModeEnum.Upgrade && !isPatch ? new StageAndUpgradeRequest() : (OrganizationRequest)new ImportSolutionRequest();
            }

            if (request is ImportSolutionRequest isr)
            {
                isr.ConvertToManaged = (oneTimeSettings ?? settings).ConvertToManaged;
                isr.OverwriteUnmanagedCustomizations = (oneTimeSettings ?? settings).OverwriteUnmanagedCustomizations;
                isr.PublishWorkflows = (oneTimeSettings ?? settings).PublishWorkflows;
                isr.ImportJobId = Guid.NewGuid();

                if (ConnectionDetail.OrganizationMajorVersion >= 8)
                {
                    isr.HoldingSolution = (oneTimeSettings ?? settings).ImportMode == ImportModeEnum.StageForUpgrade && !isPatch;
                    isr.SkipProductUpdateDependencies = (oneTimeSettings ?? settings).SkipProductUpdateDependencies;
                    isr.SolutionParameters = new SolutionParameters
                    {
                        DeployMissingPackagesBeforeSolutionImport = (oneTimeSettings ?? settings).DeployMissingPackagesBeforeSolutionImport
                    };
                }
            }
            else if (request is StageAndUpgradeRequest saur)
            {
                saur.ConvertToManaged = (oneTimeSettings ?? settings).ConvertToManaged;
                saur.OverwriteUnmanagedCustomizations = (oneTimeSettings ?? settings).OverwriteUnmanagedCustomizations;
                saur.PublishWorkflows = (oneTimeSettings ?? settings).PublishWorkflows;
                saur.ImportJobId = Guid.NewGuid();

                if (ConnectionDetail.OrganizationMajorVersion >= 8)
                {
                    saur.SkipProductUpdateDependencies = (oneTimeSettings ?? settings).SkipProductUpdateDependencies;
                    saur.SolutionParameters = new SolutionParameters
                    {
                        DeployMissingPackagesBeforeSolutionImport = (oneTimeSettings ?? settings).DeployMissingPackagesBeforeSolutionImport
                    };
                }
            }

            if (isNull)
            {
                var pi = new ProgressItem
                {
                    Type = Enumerations.RequestType.Import,
                    Detail = detail,
                    Solution = solution.GetAttributeValue<string>("friendlyname"),
                    SolutionVersion = solution.GetAttributeValue<string>("version"),
                    Request = request
                };
                pi.LogFileRequested += Pi_LogFileRequested;
                progressItems.Add(request, pi);
            }

            return request;
        }

        private PublishAllXmlRequest PreparePublishRequest(ConnectionDetail detail)
        {
            var request = new PublishAllXmlRequest();
            progressItems.Add(request, new ProgressItem
            {
                Type = Enumerations.RequestType.Publish,
                Detail = detail,
                Request = request
            });

            return request;
        }

        /// <summary>
        /// Retrieves unmanaged solutions from the source organization
        /// </summary>
        private void RetrieveSolutions()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (bw, e) =>
                {
                    var solutionComponentsQuery = new QueryExpression
                    {
                        EntityName = "solutioncomponentdefinition",
                        ColumnSet = new ColumnSet("name", "solutioncomponenttype"),
                    };

                    if (ConnectionDetail.OrganizationMajorVersion > 8)
                    {
                        solutionComponentTypes = new Dictionary<int, string>();

                        foreach (var def in sourceService.RetrieveMultiple(solutionComponentsQuery).Entities)
                        {
                            var compDef = def.GetAttributeValue<int>("solutioncomponenttype");

                            if (!solutionComponentTypes.ContainsKey(compDef))
                            {
                                solutionComponentTypes.Add(compDef, def.GetAttributeValue<string>("name"));
                            }
                            else
                            {
                                solutionComponentTypes[compDef] = def.GetAttributeValue<string>("name");
                            }
                        }

                        var opt = (RetrieveOptionSetResponse)sourceService.Execute(new RetrieveOptionSetRequest
                        {
                            Name = "componenttype"
                        });

                        foreach (var op in ((OptionSetMetadata)opt.OptionSetMetadata).Options)
                        {
                            var label = op.Label.UserLocalizedLabel?.Label ?? op.Label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == 1033)?.Label ?? op.Label.LocalizedLabels[0].Label;
                            if (!solutionComponentTypes.ContainsKey(op.Value.Value))
                            {
                                solutionComponentTypes.Add(op.Value.Value, label);
                            }
                            else
                            {
                                solutionComponentTypes[op.Value.Value] = label;
                            }
                        }
                    }

                    var sourceSolutionsQuery = new QueryExpression
                    {
                        EntityName = "solution",
                        ColumnSet = new ColumnSet("publisherid", "installedon", "version", "uniquename", "friendlyname", "description"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                    {
                        new ConditionExpression("ismanaged", ConditionOperator.Equal, false),
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                        new ConditionExpression("uniquename", ConditionOperator.NotEqual, "Default")
                    }
                        }
                    };

                    if (solutionComponentTypes.Count > 0)
                    {
                        sourceSolutionsQuery.LinkEntities.Add(new LinkEntity
                        {
                            LinkFromEntityName = "solution",
                            LinkFromAttributeName = "solutionid",
                            LinkToAttributeName = "solutionid",
                            LinkToEntityName = "solutioncomponent",
                            JoinOperator = JoinOperator.LeftOuter,
                            EntityAlias = "component",
                            LinkEntities =
                    {
                        new LinkEntity
                        {
                            LinkFromEntityName = "solutioncomponent",
                            LinkFromAttributeName=  "objectid",
                            LinkToAttributeName = "connectionreferenceid",
                            LinkToEntityName = "connectionreference",
                            Columns = new ColumnSet("connectionreferencedisplayname","connectionreferencelogicalname"),
                            EntityAlias = "connectionreference",
                            JoinOperator = JoinOperator.LeftOuter,
                        }
                    }
                        });
                    }

                    var solutions = sourceService.RetrieveMultiple(sourceSolutionsQuery);
                    var uniqueSolutions = new List<Entity>();

                    foreach (var solution in solutions.Entities)
                    {
                        if (solution.Contains("connectionreference.connectionreferencelogicalname"))
                        {
                            var logicalName = solution.GetAttributeValue<AliasedValue>("connectionreference.connectionreferencelogicalname").Value.ToString();
                            var displayName = solution.GetAttributeValue<AliasedValue>("connectionreference.connectionreferencedisplayname").Value.ToString();
                            if (!connectionReferencesBySolution.ContainsKey(solution.Id))
                            {
                                connectionReferencesBySolution.Add(solution.Id, new List<ConnectionReferenceInfo>());
                            }
                            connectionReferencesBySolution[solution.Id].Add(new ConnectionReferenceInfo { DisplayName = displayName, LogicalName = logicalName });
                        }

                        if (uniqueSolutions.All(s => s.Id != solution.Id))
                        {
                            solution.Attributes.Remove("connectionreference.connectionreferencelogicalname");
                            solution.Attributes.Remove("connectionreference.connectionreferencedisplayname");
                            uniqueSolutions.Add(solution);
                        }
                    }

                    e.Result = uniqueSolutions;
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(this, $"An error occured while retrieving solutions:\n{e.Error.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var uniqueSolutions = (List<Entity>)e.Result;

                    mForm.DisplaySolutions(uniqueSolutions);
                    mForm.DisplayTargetOrganizationsSolutions(AdditionalConnectionDetails.ToList(), this);

                    envVarsLoaded = false;
                }
            });
        }

        private void RetrieveEnvironmentVariables()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading environment variables...",
                Work = (bw, e) =>
                {
                    var defQuery = new QueryExpression("environmentvariabledefinition")
                    {
                        ColumnSet = new ColumnSet("displayname", "schemaname", "type", "defaultvalue"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                            }
                        },
                        Orders = { new OrderExpression("displayname", OrderType.Ascending) }
                    };

                    var definitions = sourceService.RetrieveMultiple(defQuery).Entities.ToList();

                    var valQuery = new QueryExpression("environmentvariablevalue")
                    {
                        ColumnSet = new ColumnSet("value", "environmentvariabledefinitionid", "schemaname"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                            }
                        }
                    };

                    var values = sourceService.RetrieveMultiple(valQuery).Entities.ToList();

                    e.Result = new Tuple<List<Entity>, List<Entity>>(definitions, values);
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(this, $"An error occured while retrieving environment variables:\n{e.Error.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var result = (Tuple<List<Entity>, List<Entity>>)e.Result;
                    evForm.DisplayEnvironmentVariables(result.Item1, result.Item2);

                    if (AdditionalConnectionDetails.Any())
                    {
                        evForm.DisplayTargetEnvironmentValues(AdditionalConnectionDetails.ToList(), this);
                    }
                }
            });
        }

        #region Cloud Flows

        private void tsbRefreshFlows_Click(object sender, EventArgs e)
        {
            flowsLoaded = false;
            RetrieveCloudFlows();
        }

        private void tsbActivateFlows_Click(object sender, EventArgs e)
        {
            cfForm.InvokeActivateSelected();
        }

        private void tsbDeactivateFlows_Click(object sender, EventArgs e)
        {
            cfForm.InvokeDeactivateSelected();
        }

        private void CfForm_RefreshRequested(object sender, EventArgs e)
        {
            flowsLoaded = false;
            RetrieveCloudFlows();
        }

        private void CfForm_ActivateRequested(object sender, FlowActivateRequestedEventArgs e)
        {
            ToggleFlowsOnTargets(e, true);
        }

        private void CfForm_DeactivateRequested(object sender, FlowActivateRequestedEventArgs e)
        {
            ToggleFlowsOnTargets(e, false);
        }

        private void RetrieveCloudFlows()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading cloud flows...",
                Work = (bw, e) =>
                {
                    // Query cloud flows (category = 5, type = 1 = definition)
                    var flowQuery = new QueryExpression("workflow")
                    {
                        ColumnSet = new ColumnSet("name", "statecode", "statuscode",
                            "category", "ownerid", "modifiedon", "type"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("category", ConditionOperator.Equal, 5),
                                new ConditionExpression("type", ConditionOperator.Equal, 1)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };

                    // Join to solutioncomponent -> solution for solution name
                    var scLink = flowQuery.AddLink("solutioncomponent", "workflowid", "objectid", JoinOperator.LeftOuter);
                    scLink.Columns = new ColumnSet();
                    scLink.EntityAlias = "sc";
                    var solLink = scLink.AddLink("solution", "solutionid", "solutionid", JoinOperator.LeftOuter);
                    solLink.Columns = new ColumnSet("friendlyname");
                    solLink.EntityAlias = "solution";

                    var workflows = sourceService.RetrieveMultiple(flowQuery).Entities.ToList();

                    // Deduplicate: a flow can appear in multiple solutions
                    var deduped = workflows
                        .GroupBy(w => w.Id)
                        .Select(g =>
                        {
                            // Prefer the non-default solution entry
                            var preferred = g.FirstOrDefault(w =>
                            {
                                var solName = w.GetAttributeValue<AliasedValue>("solution.friendlyname")?.Value as string;
                                return !string.IsNullOrEmpty(solName) && solName != "Default Solution";
                            }) ?? g.First();
                            return preferred;
                        })
                        .ToList();

                    e.Result = deduped;
                },
                PostWorkCallBack = e =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show(this, $"Error loading cloud flows:\n{e.Error.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    cfForm.DisplayCloudFlows((List<Entity>)e.Result);

                    if (AdditionalConnectionDetails.Any())
                    {
                        cfForm.DisplayTargetFlowStatus(AdditionalConnectionDetails.ToList(), this);
                    }

                    flowsLoaded = true;
                }
            });
        }

        private void ToggleFlowsOnTargets(FlowActivateRequestedEventArgs e, bool activate)
        {
            if (!AdditionalConnectionDetails.Any())
            {
                MessageBox.Show(this, "No target environments connected.",
                    "No Targets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var action = activate ? "Activate" : "Deactivate";
            var flowNames = string.Join("\n", e.Flows.Select(f => $"  • {f.FlowName}"));
            var targetNames = string.Join(", ", AdditionalConnectionDetails.Select(c => c.ConnectionName));

            var confirm = MessageBox.Show(this,
                $"Are you sure you want to {action.ToLower()} these {e.Flows.Count} flow(s) on {targetNames}?\n\n{flowNames}",
                $"Confirm {action}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var results = new List<FlowToggleResult>();
            int totalOps = AdditionalConnectionDetails.Count * e.Flows.Count;
            int completedOps = 0;

            foreach (var cd in AdditionalConnectionDetails)
            {
                foreach (var flow in e.Flows)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = $"{(activate ? "Activating" : "Deactivating")} \"{flow.FlowName}\" on {cd.ConnectionName}...",
                        Work = (bw, we) =>
                        {
                            var svc = cd.GetCrmServiceClient();

                            var query = new QueryExpression("workflow")
                            {
                                ColumnSet = new ColumnSet("workflowid", "statecode"),
                                Criteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("name", ConditionOperator.Equal, flow.FlowName),
                                        new ConditionExpression("category", ConditionOperator.Equal, 5),
                                        new ConditionExpression("type", ConditionOperator.Equal, 1)
                                    }
                                }
                            };

                            var targetFlow = svc.RetrieveMultiple(query).Entities.FirstOrDefault();
                            if (targetFlow == null)
                                throw new Exception($"Flow not found on {cd.ConnectionName}");

                            var targetFlowId = targetFlow.Id;

                            var setState = new Microsoft.Crm.Sdk.Messages.SetStateRequest
                            {
                                EntityMoniker = targetFlow.ToEntityReference(),
                                State = new OptionSetValue(activate ? 1 : 0),
                                Status = new OptionSetValue(activate ? 2 : 1)
                            };

                            svc.Execute(setState);
                            we.Result = new Tuple<ConnectionDetail, FlowActionItem, Guid>(cd, flow, targetFlowId);
                        },
                        PostWorkCallBack = we =>
                        {
                            completedOps++;

                            var result = new FlowToggleResult
                            {
                                FlowName = flow.FlowName,
                                TargetName = cd.ConnectionName,
                                TargetOrgUrl = cd.WebApplicationUrl
                            };

                            if (we.Error != null)
                            {
                                var errorMsg = we.Error.Message;
                                result.Success = false;
                                result.ErrorMessage = errorMsg;
                                result.IsConnectionRefError = errorMsg.Contains("ConnectionAuthorizationFailed")
                                    || errorMsg.Contains("cannot be used to activate");

                                // Try to get the target flow ID for the "Open" button even on failure
                                // (the flow exists, just can't be activated)
                                try
                                {
                                    var svc = cd.GetCrmServiceClient();
                                    var q = new QueryExpression("workflow")
                                    {
                                        ColumnSet = new ColumnSet("workflowid"),
                                        Criteria = new FilterExpression
                                        {
                                            Conditions =
                                            {
                                                new ConditionExpression("name", ConditionOperator.Equal, flow.FlowName),
                                                new ConditionExpression("category", ConditionOperator.Equal, 5),
                                                new ConditionExpression("type", ConditionOperator.Equal, 1)
                                            }
                                        }
                                    };
                                    var tf = svc.RetrieveMultiple(q).Entities.FirstOrDefault();
                                    if (tf != null) result.TargetFlowId = tf.Id;
                                }
                                catch { /* best effort */ }

                                // Mark the target cell as failed
                                var col = cfForm.LvFlows.Columns.Cast<ColumnHeader>()
                                    .FirstOrDefault(c => c.Text == cd.ConnectionName);
                                if (col != null && flow.Item.SubItems.Count > col.Index)
                                {
                                    var subItem = flow.Item.SubItems[col.Index];
                                    subItem.BackColor = Color.MistyRose;
                                    subItem.ForeColor = Color.DarkRed;
                                    if (result.IsConnectionRefError)
                                        subItem.Text += " ⚠";
                                }
                            }
                            else
                            {
                                var tuple = (Tuple<ConnectionDetail, FlowActionItem, Guid>)we.Result;
                                result.Success = true;
                                result.TargetFlowId = tuple.Item3;

                                var col = cfForm.LvFlows.Columns.Cast<ColumnHeader>()
                                    .FirstOrDefault(c => c.Text == cd.ConnectionName);
                                if (col != null && flow.Item.SubItems.Count > col.Index)
                                {
                                    var subItem = flow.Item.SubItems[col.Index];
                                    subItem.Text = activate ? "On" : "Off";

                                    var sourceState = ((Entity)flow.Item.Tag).GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;
                                    var targetState = activate ? 1 : 0;

                                    if (targetState == sourceState)
                                    {
                                        subItem.BackColor = Color.LightGreen;
                                        subItem.ForeColor = Color.DarkGreen;
                                    }
                                    else
                                    {
                                        subItem.BackColor = SystemColors.Info;
                                        subItem.ForeColor = Color.DarkRed;
                                    }
                                }
                            }

                            results.Add(result);

                            // Show results dialog when all operations complete
                            if (completedOps >= totalOps)
                            {
                                using (var resultsForm = new FlowResultsForm(action, results))
                                {
                                    resultsForm.ShowDialog(this);
                                }
                            }
                        }
                    });
                }
            }
        }

        #endregion Cloud Flows

        private void RunImport(ImportToProcess itp)
        {
            progressItems[itp.Request].Start();

            if (itp.Request is ImportSolutionRequest isr)
            {
                isr.CustomizationFile = itp.Export.SolutionContent;
            }
            else if (itp.Request is StageAndUpgradeRequest saur)
            {
                saur.CustomizationFile = itp.Export.SolutionContent;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = null,
                Work = (bw2, evt2) =>
                {
                    itp.AsyncOperationId = ((ExecuteAsyncResponse)itp.Detail.GetCrmServiceClient().Execute(new ExecuteAsyncRequest
                    {
                        Request = itp.Request
                    })).AsyncJobId;
                },
                PostWorkCallBack = evt2 =>
                {
                    if (itp.Request is ImportSolutionRequest isr2)
                    {
                        lastImportId = isr2.ImportJobId;
                    }
                    else if (itp.Request is StageAndUpgradeRequest saur2)
                    {
                        lastImportId = saur2.ImportJobId;
                    }
                }
            });
        }

        private void StartExport(ExportToProcess etp)
        {
            if ((oneTimeSettings ?? settings).AutoExportSolutionsToDisk)
            {
                if (!Directory.Exists((oneTimeSettings ?? settings).AutoExportSolutionsFolderPath))
                {
                    MessageBox.Show(this,
                        $@"Folder {(oneTimeSettings ?? settings).AutoExportSolutionsFolderPath} does not exist! Please update settings",
                        @"Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    sForm.Show(dpMain, sForm.DockState);
                    return;
                }
            }

            ToggleWaitMode(true);

            etp.StartedOn = DateTime.Now;
            progressItems[etp.Request].Solution = etp.Solution.GetAttributeValue<string>("friendlyname");
            progressItems[etp.Request].SolutionVersion = etp.Solution.GetAttributeValue<string>("version");
            progressItems[etp.Request].Start();

            if ((oneTimeSettings ?? settings).ExportAsynchronously)
            {
                var request2 = new OrganizationRequest("ExportSolutionAsync");
                request2.Parameters = etp.Request.Parameters;

                var response2 = Service.Execute(request2);
                etp.AsyncOperationId = (Guid)response2.Results["AsyncOperationId"];
                etp.ExportJobId = (Guid)response2.Results["ExportJobId"];

                etp.IsProcessed = false;
                etp.IsProcessing = true;
                etp.Succeeded = false;

                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "",
                Work = (bw, evt) =>
                {
                    etp.IsProcessing = true;
                    etp.SolutionContent = ((ExportSolutionResponse)etp.Detail.GetCrmServiceClient().Execute(etp.Request))
                        .ExportSolutionFile;
                },
                PostWorkCallBack = evt =>
                {
                    etp.IsProcessed = true;
                    etp.IsProcessing = false;
                    etp.Succeeded = true;
                    etp.CompletedOn = DateTime.Now;
                    if (evt.Error != null)
                    {
                        etp.Succeeded = false;

                        progressItems[etp.Request].Error(DateTime.Now, evt.Error.Message);
                        pForm.ShowRetryButton(progressItems[etp.Request]);

                        ToggleWaitMode(false);
                    }
                    else
                    {
                        progressItems[etp.Request].Success(etp);
                        progressItems[etp.Request].SolutionFile = etp.SolutionContent;
                    }

                    if (toProcessList.All(p => p.IsProcessed))
                    {
                        ToggleWaitMode(false);
                    }

                    if ((oneTimeSettings ?? settings).AutoExportSolutionsToDisk)
                    {
                        var fileName = progressItems[etp.Request].SolutionFileName;
                        var filePath = Path.Combine((oneTimeSettings ?? settings).AutoExportSolutionsFolderPath, fileName);
                        try
                        {
                            File.WriteAllBytes(filePath, etp.SolutionContent);
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show(this, $@"Error when saving solution {fileName} to disk.

{error.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            });
        }

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            if (cancelPending)
            {
                timer.Stop();
                ToggleWaitMode(false);
                cancelPending = false;
                tsbCancel.Text = "Cancel";

                foreach (var etp in toProcessList.OfType<ExportToProcess>().Where(tp => !tp.IsProcessed))
                {
                    etp.IsProcessed = false;
                    etp.IsProcessing = false;
                    progressItems[etp.Request].Error(DateTime.Now.ToLocalTime(), "Export canceled by user");
                }

                foreach (var itp in toProcessList.OfType<ImportToProcess>().Where(tp => !tp.IsProcessed))
                {
                    itp.IsProcessed = false;
                    itp.IsProcessing = false;
                    progressItems[itp.Request].Error(DateTime.Now.ToLocalTime(), "Import canceled by user");
                }

                foreach (var ptp in toProcessList.OfType<PublishToProcess>().Where(tp => !tp.IsProcessed))
                {
                    ptp.IsProcessed = false;
                    ptp.IsProcessing = false;
                    progressItems[ptp.Request].Error(DateTime.Now.ToLocalTime(), "Publish canceled by user");
                }

                return;
            }

            foreach (var etp in toProcessList.OfType<ExportToProcess>())
            {
                if (!etp.IsProcessed && !etp.IsProcessing)
                {
                    StartExport(etp);
                }
                else if (etp.IsProcessing && (oneTimeSettings ?? settings).ExportAsynchronously)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null,
                        Work = (bw, evt) =>
                        {
                            evt.Result = etp.Detail.GetCrmServiceClient().RetrieveMultiple(new QueryExpression("asyncoperation")
                            {
                                NoLock = true,
                                ColumnSet = new ColumnSet(true),
                                Criteria =
                                {
                                    Conditions=
                                    {
                                        new ConditionExpression("asyncoperationid", ConditionOperator.Equal, etp.AsyncOperationId)
                                    }
                                }
                            }).Entities.FirstOrDefault();
                        },
                        PostWorkCallBack = evt =>
                        {
                            var task = (Entity)evt.Result;
                            if (task != null)
                            {
                                if (task.GetAttributeValue<OptionSetValue>("statecode")?.Value == 3)
                                {
                                    etp.IsProcessed = true;
                                    etp.IsProcessing = false;
                                    if (task.GetAttributeValue<OptionSetValue>("statuscode")?.Value == 30)
                                    {
                                        var req = new OrganizationRequest("DownloadSolutionExportData");
                                        req.Parameters.Add("ExportJobId", etp.ExportJobId);
                                        var response = Service.Execute(req);

                                        etp.SolutionContent = (byte[])response["ExportSolutionFile"];
                                        etp.CompletedOn = DateTime.Now;

                                        progressItems[etp.Request].Success(etp);

                                        progressItems[etp.Request].SolutionFile = etp.SolutionContent;

                                        etp.Succeeded = true;

                                        if (toProcessList.All(tp => tp.IsProcessed))
                                        {
                                            timer.Stop();
                                            ToggleWaitMode(false);
                                        }

                                        if ((oneTimeSettings ?? settings).AutoExportSolutionsToDisk || etp.IsSolutionDownload)
                                        {
                                            var fileName = progressItems[etp.Request].SolutionFileName;
                                            var filePath = Path.Combine((oneTimeSettings ?? settings).AutoExportSolutionsFolderPath, fileName);
                                            try
                                            {
                                                File.WriteAllBytes(filePath, etp.SolutionContent);

                                                if (etp.IsSolutionDownload)
                                                {
                                                    Invoke(new Action(() =>
                                                    {
                                                        MessageBox.Show(this, $@"Solution exported to {filePath}", @"Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                    }));
                                                }
                                            }
                                            catch (Exception error)
                                            {
                                                Invoke(new Action(() =>
                                                {
                                                    MessageBox.Show(this, $@"Error when saving solution {fileName} to disk.

{error.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                }));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        progressItems[etp.Request].Error(task.GetAttributeValue<DateTime>("completedon").ToLocalTime());
                                        ToggleWaitMode(false);
                                        timer.Stop();
                                        pForm.ShowRetryButton(progressItems[etp.Request]);
                                    }

                                    if (toProcessList.All(tp => tp.IsProcessed))
                                    {
                                        timer.Stop();
                                        ToggleWaitMode(false);
                                    }
                                }
                                else
                                {
                                    progressItems[etp.Request]
                                        .ReportProgress(task.GetAttributeValue<double>("progress"), etp);
                                }
                            }
                        }
                    });
                }
            }

            foreach (var itp in toProcessList.OfType<ImportToProcess>().Where(i => i.Export.IsProcessed && i.Export.Succeeded))
            {
                if (itp.Previous != null && !itp.Previous.IsProcessed || itp.IsProcessed)
                {
                    continue;
                }

                if (!itp.IsProcessing && !itp.IsProcessed)
                {
                    itp.StartedOn = DateTime.Now;
                    progressItems[itp.Request].Solution = itp.Solution.GetAttributeValue<string>("friendlyname");
                    itp.IsProcessing = true;

                    if ((oneTimeSettings ?? settings).CheckForMissingDependencies && ConnectionDetail.OrganizationMajorVersion > 8)
                    {
                        progressItems[itp.Request].CheckDependencies();

                        WorkAsync(new WorkAsyncInfo
                        {
                            Message = null,
                            Work = (bw, evt) =>
                            {
                                evt.Result = (RetrieveMissingComponentsResponse)itp.Detail.GetCrmServiceClient().Execute(new RetrieveMissingComponentsRequest
                                {
                                    CustomizationFile = itp.Export.SolutionContent
                                });
                            },
                            PostWorkCallBack = evt =>
                            {
                                if (evt.Error != null)
                                {
                                    Invoke(new Action(() =>
                                    {
                                        if (MessageBox.Show(this, $"An error when checking for missing components:\n\n{evt.Error.Message}\n\nDo you want to continue to import?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                                        {
                                            progressItems[itp.Request].Error(DateTime.Now, "An error when checking for missing components");
                                            itp.IsProcessing = false;
                                            ToggleWaitMode(false);
                                            timer.Stop();
                                            pForm.ShowRetryButton(progressItems[itp.Request]);

                                            return;
                                        }
                                    }));
                                }

                                var response = (RetrieveMissingComponentsResponse)evt.Result;
                                if (response.MissingComponents != null && response.MissingComponents.Any())
                                {
                                    if (mcControl == null)
                                    {
                                        mcControl = new MissingComponentsControl();
                                        mcControl.Name = "MissingComponentsControl1";
                                        mcControl.OnClose += (s, evt2) => { Controls.Remove(mcControl); };
                                    }

                                    Invoke(new Action(() =>
                                    {
                                        mcControl.ComponentsTypes = solutionComponentTypes;
                                        mcControl.Components = response.MissingComponents;
                                        mcControl.ShowData();
                                        Controls.Add(mcControl);
                                        mcControl.DisplayCentered();
                                        mcControl.BringToFront();
                                    }));

                                    progressItems[itp.Request].Error(DateTime.Now, "Your solution has missing components in the target environment");
                                    itp.IsProcessing = false;
                                    ToggleWaitMode(false);
                                    timer.Stop();
                                    pForm.ShowRetryButton(progressItems[itp.Request]);

                                    if ((oneTimeSettings ?? settings).UseWindowsToastNotification)
                                    {
                                        try
                                        {
                                            new ToastContentBuilder()
                                               .AddArgument("action", "viewDetails")
                                               .AddHeader("XTB.TTO.STT", "Solution Transfer Tool", "")
                                               .AddText($"{itp.Solution.GetAttributeValue<string>("friendlyname")} {itp.Solution.GetAttributeValue<string>("version")}")
                                               .AddText("The solution has missing dependencies.")
                                               .AddArgument("pid", Process.GetCurrentProcess().Id)
                                               .Show();
                                        }
                                        catch
                                        {
                                            // Ignore to not fail if XrmToolBox does not implement Toast properly
                                        }
                                    }

                                    return;
                                }

                                RunImport(itp);
                            }
                        });
                    }
                    else
                    {
                        RunImport(itp);
                    }
                }
                else if (itp.IsProcessing)
                {
                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = null,
                        Work = (bw, evt) =>
                        {
                            evt.Result = itp.Detail.GetCrmServiceClient().RetrieveMultiple(new QueryExpression("asyncoperation")
                            {
                                NoLock = true,
                                ColumnSet = new ColumnSet(true),
                                Criteria =
                                {
                                    Conditions=
                                    {
                                        new ConditionExpression("asyncoperationid", ConditionOperator.Equal, itp.AsyncOperationId)
                                    }
                                }
                            }).Entities.FirstOrDefault();
                        },
                        PostWorkCallBack = evt =>
                        {
                            var task = (Entity)evt.Result;
                            if (task != null)
                            {
                                if (task.GetAttributeValue<OptionSetValue>("statecode")?.Value == 3)
                                {
                                    itp.IsProcessed = true;
                                    itp.IsProcessing = false;
                                    itp.CompletedOn = DateTime.Now;
                                    if (task.GetAttributeValue<OptionSetValue>("statuscode")?.Value == 30)
                                    {
                                        progressItems[itp.Request].Success(itp);
                                        itp.Succeeded = true;

                                        Invoke(new Action(() =>
                                        {
                                            mForm.SetTargetSolutionVersion(itp.Solution, itp.Detail);
                                        }));

                                        if ((oneTimeSettings ?? settings).UseWindowsToastNotification)
                                        {
                                            try
                                            {
                                                new ToastContentBuilder()
                                                   .AddArgument("action", "viewDetails")
                                                   .AddHeader("XTB.TTO.STT", "Solution Transfer Tool", "")
                                                   .AddText($"{itp.Solution.GetAttributeValue<string>("friendlyname")} {itp.Solution.GetAttributeValue<string>("version")}")
                                                   .AddText("Imported successfully")
                                                   .AddText($"To {itp.Detail.ConnectionName}")
                                                   .AddArgument("pid", Process.GetCurrentProcess().Id)
                                                   .AddAppLogoOverride(new Uri(Path.Combine(Path.GetTempPath(), "xtb.stt.success.png")))
                                                   .Show(toast =>
                                                   {
                                                       toast.ExpirationTime = DateTime.Now.AddMinutes(5);
                                                   });
                                            }
                                            catch
                                            {
                                                // Ignore to not fail if XrmToolBox does not implement Toast properly
                                            }
                                        }
                                    }
                                    else
                                    {
                                        progressItems[itp.Request].Error(task.GetAttributeValue<DateTime>("completedon").ToLocalTime());
                                        ToggleWaitMode(false);
                                        timer.Stop();
                                        pForm.ShowRetryButton(progressItems[itp.Request]);

                                        if ((oneTimeSettings ?? settings).UseWindowsToastNotification)
                                        {
                                            try
                                            {
                                                new ToastContentBuilder()
                                               .AddArgument("action", "viewDetails")
                                               .AddHeader("XTB.TTO.STT", "Solution Transfer Tool", "")
                                               .AddText($"{itp.Solution.GetAttributeValue<string>("friendlyname")} {itp.Solution.GetAttributeValue<string>("version")}")
                                               .AddText($"To {itp.Detail.ConnectionName}")
                                               .AddText("Failed to import")
                                               .AddArgument("pid", Process.GetCurrentProcess().Id)
                                               .AddAppLogoOverride(new Uri(Path.Combine(Path.GetTempPath(), "xtb.stt.error.png")))
                                               .Show(toast =>
                                               {
                                                   toast.ExpirationTime = DateTime.Now.AddMinutes(5);
                                               });
                                            }
                                            catch
                                            {
                                                // Ignore to not fail if XrmToolBox does not implement Toast properly
                                            }
                                        }
                                    }

                                    if (toProcessList.All(tp => tp.IsProcessed))
                                    {
                                        timer.Stop();
                                        ToggleWaitMode(false);
                                    }
                                }
                                else
                                {
                                    Guid importJobId = Guid.Empty;
                                    if (itp.Request is ImportSolutionRequest isr)
                                    {
                                        importJobId = isr.ImportJobId;
                                    }
                                    else if (itp.Request is StageAndUpgradeRequest saur)
                                    {
                                        importJobId = saur.ImportJobId;
                                    }

                                    WorkAsync(new WorkAsyncInfo
                                    {
                                        Message = null,
                                        Work = (bw2, evt2) =>
                                        {
                                            evt2.Result = itp.Detail.GetCrmServiceClient().RetrieveMultiple(new QueryExpression("importjob")
                                            {
                                                NoLock = true,
                                                ColumnSet = new ColumnSet(true),
                                                Criteria =
                                                {
                                                    Conditions=
                                                    {
                                                        new ConditionExpression("importjobid", ConditionOperator.Equal, importJobId)
                                                    }
                                                }
                                            }).Entities.FirstOrDefault();
                                        },
                                        PostWorkCallBack = evt2 =>
                                        {
                                            var job = (Entity)evt2.Result;
                                            if (job != null)
                                            {
                                                progressItems[itp.Request]
                                                    .ReportProgress(job.GetAttributeValue<double>("progress"), itp, job.GetAttributeValue<string>("operationcontext") == "Upgrade");
                                            }
                                        }
                                    }
                                    );
                                }
                            }
                        }
                    });
                }
            }

            foreach (var ptp in toProcessList.OfType<PublishToProcess>())
            {
                if (toProcessList.OfType<ImportToProcess>()
                        .Where(i => i.Detail == ptp.Detail)
                        .All(i => i.Succeeded)
                    && !ptp.IsProcessed && !ptp.IsProcessing)
                {
                    ptp.StartedOn = DateTime.Now;
                    progressItems[ptp.Request].Start();

                    WorkAsync(new WorkAsyncInfo
                    {
                        Message = "",
                        Work = (bw, evt) =>
                        {
                            ptp.IsProcessing = true;
                            ptp.Detail.GetCrmServiceClient().Execute(ptp.Request);
                            ptp.IsProcessed = true;
                            ptp.IsProcessing = false;
                        },
                        PostWorkCallBack = evt =>
                        {
                            if (evt.Error != null)
                            {
                                if (evt.Error is CommunicationException ce && ce.HResult == -2146233087)
                                {
                                    progressItems[ptp.Request].PublishTimeout(DateTime.Now);
                                }
                                else
                                {
                                    progressItems[ptp.Request].Error(DateTime.Now);
                                }
                            }
                            else
                            {
                                ptp.CompletedOn = DateTime.Now;
                                progressItems[ptp.Request].Success(ptp);
                            }

                            timer.Stop();
                            ToggleWaitMode(false);
                        }
                    });
                }

                if (toProcessList.OfType<ImportToProcess>()
                       .Where(i => i.Detail == ptp.Detail)
                       .Any(i => i.IsProcessed && !i.Succeeded)
                   && !ptp.IsProcessed)
                {
                    progressItems[ptp.Request].Error(DateTime.Now);
                    timer.Stop();
                    ToggleWaitMode(false);
                }
            }

            if (toProcessList.All(p => p.IsProcessed))
            {
                timer.Stop();
            }
        }

        #endregion Methods

        private void Retry()
        {
            if (DialogResult.Yes != MessageBox.Show(this, @"Are you sure you want to retry last failed action?",
                    @"Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            var firstNotSucceededProcess = toProcessList.FirstOrDefault(x => x.IsProcessed && !x.Succeeded);
            if (firstNotSucceededProcess == null)
            {
                return;
            }

            if (firstNotSucceededProcess is ExportToProcess etp)
            {
                etp.IsProcessed = false;
                StartExport(etp);
            }
            else if (firstNotSucceededProcess is ImportToProcess itp)
            {
                itp.IsProcessed = false;
                progressItems[itp.Request].Start();
            }

            foreach (var ep in toProcessList.OfType<ExportToProcess>().Where(x => x.IsProcessed == false))
            {
                PrepareExportRequest(ep.Solution, (ExportSolutionRequest)ep.Request);
            }

            foreach (var ip in toProcessList.OfType<ImportToProcess>().Where(x => x.IsProcessed == false))
            {
                PrepareImportRequest(ip.Detail, ip.Solution, ip.Request);
            }

            ToggleWaitMode(true);

            timer.Tick -= Timer_Elapsed;
            timer.Tick += Timer_Elapsed;
            timer.Interval = (int)(oneTimeSettings ?? settings).RefreshIntervalProp.TotalMilliseconds;
            timer.Start();
        }

        private void SolutionTransferTool_Resize(object sender, EventArgs e)
        {
            // Update splitter distance based on source label width
            if (lblSource != null && scOrganizations != null)
            {
                var size = System.Windows.Forms.TextRenderer.MeasureText(lblSource.Text, lblSource.Font);
                var dist = size.Width + 20;
                if (dist > 0 && dist < scOrganizations.Width - scOrganizations.SplitterWidth)
                    scOrganizations.SplitterDistance = dist;
            }

            var control = Controls.Find("MissingComponentsControl1", true);
            if (control.Length == 1)
            {
                if (((MissingComponentsControl)control[0]).IsMaximized)
                {
                    control[0].Width = Width;
                    control[0].Height = Height;
                    control[0].Location = new System.Drawing.Point(0, 0);
                }
                else
                {
                    control[0].Width = Convert.ToInt32(Width * 0.7);
                    control[0].Height = Convert.ToInt32(Height * 0.7);
                    control[0].Location = new System.Drawing.Point(Width / 2 - control[0].Width / 2, Height / 2 - control[0].Height / 2);
                }
            }
        }

        private void ToggleWaitMode(bool on)
        {
            Invoke(new Action(() =>
            {
                if (on)
                {
                    tssbTransfer.Enabled = false;
                    tsbLoadSolutions.Enabled = false;
                    tsbFindMissingDependencies.Enabled = false;
                    tsbSwitchOrgs.Enabled = false;
                    tsbExportSolutions.Enabled = false;
                    tsbDownload.Enabled = false;
                    tsbImportFromFile.Enabled = false;
                    tsbRemoveFromTargets.Enabled = false;
                    tsbCancel.Visible = true;
                }
                else
                {
                    tsbDownload.Enabled = true;
                    tssbTransfer.Enabled = true;
                    tsbLoadSolutions.Enabled = true;
                    tsbFindMissingDependencies.Enabled = lastImportId != Guid.Empty;
                    tsbSwitchOrgs.Enabled = true;
                    tsbExportSolutions.Enabled = toProcessList.OfType<ExportToProcess>().Any(etp =>
                        etp.SolutionContent != null);
                    tsbImportFromFile.Enabled = true;
                    tsbRemoveFromTargets.Enabled = true;
                    tsbCancel.Visible = false;
                }
            }));
        }

        private void tsbCancel_Click(object sender, EventArgs e)
        {
            cancelPending = true;
            tsbCancel.Text = "Cancelling...";
        }

        private void tsbDownload_Click(object sender, EventArgs e)
        {
            if (mForm.SelectedSolutions.Count == 0)
            {
                MessageBox.Show(this, @"No solution selected!", @"Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var path = "";

            if ((oneTimeSettings ?? settings).AutoExportSolutionsToDisk && string.IsNullOrEmpty((oneTimeSettings ?? settings).AutoExportSolutionsFolderPath))
            {
                var dialog = new CustomFolderBrowserDialog();
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                path = dialog.FolderPath;
            }

            var solutions = mForm.SelectedSolutions;

            if ((oneTimeSettings ?? settings).ExportAsynchronously)
            {
                progressItems = new Dictionary<OrganizationRequest, ProgressItem>();
                toProcessList = new List<BaseToProcess>();

                foreach (var solution in solutions)
                {
                    var exportItem = new ExportToProcess
                    {
                        Solution = solution,
                        Previous = toProcessList.OfType<ExportToProcess>().LastOrDefault(),
                        Request = PrepareExportRequest(solution),
                        Detail = sourceDetail,
                        IsSolutionDownload = true
                    };
                    toProcessList.Add(exportItem);
                }

                pForm.Items = progressItems.Values.ToList();
                pForm.Start();

                pForm.Show(dpMain, DockState.DockRight);

                StartExport(toProcessList.OfType<ExportToProcess>().First());

                timer.Tick -= Timer_Elapsed;
                timer.Tick += Timer_Elapsed;
                timer.Interval = (int)settings.RefreshIntervalProp.TotalMilliseconds;
                timer.Start();

                return;
            }

            if (string.IsNullOrEmpty((oneTimeSettings ?? settings).AutoExportSolutionsFolderPath))
            {
                var dialog = new CustomFolderBrowserDialog();
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                path = dialog.FolderPath;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "",
                Work = (bw, evt) =>
                {
                    foreach (var solution in solutions)
                    {
                        bw.ReportProgress(0, $"Exporting solution {solution.GetAttributeValue<string>("friendlyname")}...");

                        var request = new ExportSolutionRequest();
                        PrepareExportRequest(solution, request);

                        string filename = Path.Combine(path,
                            $"{solution.GetAttributeValue<string>("uniquename")}_{solution.GetAttributeValue<string>("version").Replace(".", "_")}{(request.Managed ? "_managed" : "")}.zip");
                        var contentFile = ((ExportSolutionResponse)sourceService.Execute(request)).ExportSolutionFile;
                        File.WriteAllBytes(filename, contentFile);
                    }
                },
                PostWorkCallBack = evt =>
                {
                    if (evt.Error != null)
                    {
                        MessageBox.Show(this, $@"An error occured when exporting solution(s): {evt.Error.Message}", @"Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show(this, $@"Solution(s) exported to {path}", @"Success", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                },
                ProgressChanged = evt =>
                {
                    SetWorkingMessage(evt.UserState.ToString());
                }
            });
        }

        private void tsbExportSolutions_Click(object sender, EventArgs e)
        {
            var cfd = new CustomFolderBrowserDialog();
            if (cfd.ShowDialog(Parent) == DialogResult.OK)
            {
                foreach (var etp in toProcessList.OfType<ExportToProcess>())
                {
                    if (etp.SolutionContent != null)
                    {
                        string filename = Path.Combine(cfd.FolderPath,
                            $"{etp.Solution.GetAttributeValue<string>("uniquename")}_{etp.Solution.GetAttributeValue<string>("version").Replace(".", "_")}{(((ExportSolutionRequest)etp.Request).Managed ? "_managed" : "")}.zip");
                        File.WriteAllBytes(filename, etp.SolutionContent);
                    }
                }

                MessageBox.Show(this, $@"Solution(s) saved to {cfd.FolderPath}", @"Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}