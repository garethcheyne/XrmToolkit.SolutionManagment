using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Handles solution export, import, publish, and async polling.
    /// Pure execution — no UI. Sends progress/results via callbacks.
    /// React handles all confirmation dialogs and settings input.
    /// </summary>
    public class SolutionTransferService
    {
        // ── DTOs for React communication ──

        public class TransferSettings
        {
            [JsonProperty("managed")] public bool Managed { get; set; } = true;
            [JsonProperty("importMode")] public string ImportMode { get; set; } = "Update"; // Update, StageForUpgrade, Upgrade
            [JsonProperty("overwriteUnmanaged")] public bool OverwriteUnmanaged { get; set; } = true;
            [JsonProperty("publishWorkflows")] public bool PublishWorkflows { get; set; } = true;
            [JsonProperty("checkDependencies")] public bool CheckDependencies { get; set; }
            [JsonProperty("convertToManaged")] public bool ConvertToManaged { get; set; }
            [JsonProperty("skipProductUpdateDeps")] public bool SkipProductUpdateDependencies { get; set; }

            // Export sub-settings
            [JsonProperty("autoNumbering")] public bool ExportAutoNumberingSettings { get; set; }
            [JsonProperty("calendarSettings")] public bool ExportCalendarSettings { get; set; }
            [JsonProperty("customizationSettings")] public bool ExportCustomizationSettings { get; set; }
            [JsonProperty("emailTracking")] public bool ExportEmailTrackingSettings { get; set; }
            [JsonProperty("externalApps")] public bool ExportExternalApplications { get; set; }
            [JsonProperty("generalSettings")] public bool ExportGeneralSettings { get; set; }
            [JsonProperty("isvConfig")] public bool ExportIsvConfig { get; set; }
            [JsonProperty("marketingSettings")] public bool ExportMarketingSettings { get; set; }
            [JsonProperty("outlookSync")] public bool ExportOutlookSynchronizationSettings { get; set; }
            [JsonProperty("relationshipRoles")] public bool ExportRelationshipRoles { get; set; }
            [JsonProperty("sales")] public bool ExportSales { get; set; }

            // Per-solution overrides (key = solution unique name)
            [JsonProperty("perSolution")] public Dictionary<string, TransferSettings> PerSolution { get; set; }

            /// <summary>
            /// Get effective settings for a specific solution (profile overrides defaults).
            /// </summary>
            public TransferSettings ForSolution(string solutionUniqueName)
            {
                if (PerSolution != null && !string.IsNullOrEmpty(solutionUniqueName)
                    && PerSolution.TryGetValue(solutionUniqueName, out var profile))
                {
                    return profile;
                }
                return this;
            }
        }

        public class TransferProgress
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("solution")] public string Solution { get; set; }
            [JsonProperty("target")] public string Target { get; set; }
            [JsonProperty("phase")] public string Phase { get; set; } // export, import, publish
            [JsonProperty("status")] public string Status { get; set; } // running, success, error
            [JsonProperty("percentage")] public double Percentage { get; set; }
            [JsonProperty("elapsed")] public string Elapsed { get; set; }
            [JsonProperty("errorMessage")] public string ErrorMessage { get; set; }
        }

        public class ExportResult
        {
            public bool Success { get; set; }
            public byte[] SolutionContent { get; set; }
            public string SolutionName { get; set; }
            public string Version { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class ImportResult
        {
            public bool Success { get; set; }
            public Guid ImportJobId { get; set; }
            public Guid AsyncOperationId { get; set; }
            public string ErrorMessage { get; set; }
            public string ImportLogXml { get; set; }
        }

        // ── Export ──

        public static ExportResult ExportSolution(
            IOrganizationService sourceService,
            string solutionUniqueName,
            TransferSettings settings)
        {
            try
            {
                var request = new ExportSolutionRequest
                {
                    SolutionName = solutionUniqueName,
                    Managed = settings.Managed,
                    ExportAutoNumberingSettings = settings.ExportAutoNumberingSettings,
                    ExportCalendarSettings = settings.ExportCalendarSettings,
                    ExportCustomizationSettings = settings.ExportCustomizationSettings,
                    ExportEmailTrackingSettings = settings.ExportEmailTrackingSettings,
                    ExportExternalApplications = settings.ExportExternalApplications,
                    ExportGeneralSettings = settings.ExportGeneralSettings,
                    ExportIsvConfig = settings.ExportIsvConfig,
                    ExportMarketingSettings = settings.ExportMarketingSettings,
                    ExportOutlookSynchronizationSettings = settings.ExportOutlookSynchronizationSettings,
                    ExportRelationshipRoles = settings.ExportRelationshipRoles,
                    ExportSales = settings.ExportSales
                };

                var response = (ExportSolutionResponse)sourceService.Execute(request);

                return new ExportResult
                {
                    Success = true,
                    SolutionContent = response.ExportSolutionFile,
                    SolutionName = solutionUniqueName
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    SolutionName = solutionUniqueName,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ── Import ──

        public static ImportResult ImportSolution(
            ConnectionDetail targetDetail,
            byte[] solutionContent,
            TransferSettings settings)
        {
            var importJobId = Guid.NewGuid();

            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                OrganizationRequest request;

                if (settings.ImportMode == "StageForUpgrade" || settings.ImportMode == "Upgrade")
                {
                    request = new StageAndUpgradeRequest
                    {
                        CustomizationFile = solutionContent,
                        ImportJobId = importJobId,
                        OverwriteUnmanagedCustomizations = settings.OverwriteUnmanaged,
                        PublishWorkflows = settings.PublishWorkflows,
                        ConvertToManaged = settings.ConvertToManaged,
                        SkipProductUpdateDependencies = settings.SkipProductUpdateDependencies
                    };
                }
                else
                {
                    request = new ImportSolutionRequest
                    {
                        CustomizationFile = solutionContent,
                        ImportJobId = importJobId,
                        OverwriteUnmanagedCustomizations = settings.OverwriteUnmanaged,
                        PublishWorkflows = settings.PublishWorkflows,
                        ConvertToManaged = settings.ConvertToManaged,
                        SkipProductUpdateDependencies = settings.SkipProductUpdateDependencies
                    };
                }

                svc.Execute(request);

                return new ImportResult
                {
                    Success = true,
                    ImportJobId = importJobId
                };
            }
            catch (Exception ex)
            {
                return new ImportResult
                {
                    Success = false,
                    ImportJobId = importJobId,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ── Async Import (with progress polling support) ──

        public static ImportResult ImportSolutionAsync(
            ConnectionDetail targetDetail,
            byte[] solutionContent,
            TransferSettings settings)
        {
            var importJobId = Guid.NewGuid();

            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                OrganizationRequest request;

                if (settings.ImportMode == "StageForUpgrade" || settings.ImportMode == "Upgrade")
                {
                    request = new StageAndUpgradeRequest
                    {
                        CustomizationFile = solutionContent,
                        ImportJobId = importJobId,
                        OverwriteUnmanagedCustomizations = settings.OverwriteUnmanaged,
                        PublishWorkflows = settings.PublishWorkflows,
                        ConvertToManaged = settings.ConvertToManaged,
                        SkipProductUpdateDependencies = settings.SkipProductUpdateDependencies
                    };
                }
                else
                {
                    request = new ImportSolutionRequest
                    {
                        CustomizationFile = solutionContent,
                        ImportJobId = importJobId,
                        OverwriteUnmanagedCustomizations = settings.OverwriteUnmanaged,
                        PublishWorkflows = settings.PublishWorkflows,
                        ConvertToManaged = settings.ConvertToManaged,
                        SkipProductUpdateDependencies = settings.SkipProductUpdateDependencies
                    };
                }

                var asyncResponse = (ExecuteAsyncResponse)svc.Execute(new ExecuteAsyncRequest
                {
                    Request = request
                });

                return new ImportResult
                {
                    Success = true,
                    ImportJobId = importJobId,
                    AsyncOperationId = asyncResponse.AsyncJobId
                };
            }
            catch (Exception ex)
            {
                return new ImportResult
                {
                    Success = false,
                    ImportJobId = importJobId,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Polls the asyncoperation and importjob for progress.
        /// Returns: status ("running","success","error"), progress (0-100), errorMessage
        /// </summary>
        public static (string status, double progress, string errorMessage) PollImportProgress(
            ConnectionDetail targetDetail,
            Guid asyncOperationId,
            Guid importJobId)
        {
            try
            {
                var svc = targetDetail.GetCrmServiceClient();

                // Check async operation state
                var asyncOp = svc.Retrieve("asyncoperation", asyncOperationId,
                    new ColumnSet("statecode", "statuscode", "message"));

                var stateCode = asyncOp.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;

                if (stateCode == 3) // Completed
                {
                    var statusCode = asyncOp.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0;
                    if (statusCode == 30) // Succeeded
                        return ("success", 100, null);
                    else
                    {
                        var rawMessage = asyncOp.GetAttributeValue<string>("message") ?? "Import failed";
                        return ("error", 100, ParseImportErrorMessage(rawMessage));
                    }
                }

                // Still running — poll importjob for progress percentage
                try
                {
                    var importJob = svc.Retrieve("importjob", importJobId,
                        new ColumnSet("progress"));
                    var progress = importJob.GetAttributeValue<double>("progress");
                    return ("running", progress, null);
                }
                catch
                {
                    // importjob may not exist yet
                    return ("running", 0, null);
                }
            }
            catch (Exception ex)
            {
                return ("error", 0, ex.Message);
            }
        }

        /// <summary>
        /// Parse the raw asyncoperation.message into a human-readable error.
        /// Extracts the core message and formats MissingDependencies XML into a readable list.
        /// </summary>
        internal static string ParseImportErrorMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return "Import failed";

            try
            {
                // Extract the "Message: ..." line from the raw exception dump
                // Format: "Exception type: ...\nMessage: ...\nDetail:\n..."
                var message = rawMessage;
                var messageMatch = Regex.Match(rawMessage, @"Message:\s*(.+?)(?:\r?\nDetail:|$)", RegexOptions.Singleline);
                if (messageMatch.Success)
                    message = messageMatch.Groups[1].Value.Trim();

                // Check for MissingDependencies XML in the message
                var depMatch = Regex.Match(message, @"<MissingDependencies[^>]*>(.*?)</MissingDependencies>", RegexOptions.Singleline);
                if (depMatch.Success)
                {
                    // Extract a clean intro (everything before the XML)
                    var intro = message.Substring(0, depMatch.Index).Trim();
                    // Remove trailing " : " or similar
                    intro = Regex.Replace(intro, @"\s*:\s*$", "");
                    // Remove "FAILURE: " prefix duplication
                    intro = Regex.Replace(intro, @"^Solution manifest import:\s*FAILURE:\s*", "", RegexOptions.IgnoreCase);
                    intro = intro.Trim();

                    // Parse the XML to extract dependency details
                    var deps = new List<string>();
                    try
                    {
                        var xml = "<root>" + depMatch.Value + "</root>";
                        var doc = new XmlDocument();
                        doc.LoadXml(xml);
                        var nodes = doc.SelectNodes("//MissingDependency");
                        if (nodes != null)
                        {
                            foreach (XmlNode node in nodes)
                            {
                                var required = node.SelectSingleNode("Required");
                                var dependent = node.SelectSingleNode("Dependent");
                                var reqName = required?.Attributes?["displayName"]?.Value ?? "Unknown";
                                var reqType = required?.Attributes?["type"]?.Value ?? "";
                                var reqId = required?.Attributes?["id"]?.Value ?? "";
                                var reqSolution = required?.Attributes?["solution"]?.Value ?? "";
                                var depName = dependent?.Attributes?["displayName"]?.Value ?? "Unknown";
                                var depType = dependent?.Attributes?["type"]?.Value ?? "";
                                var depId = dependent?.Attributes?["id"]?.Value ?? "";
                                var canResolve = node.Attributes?["canResolveMissingDependency"]?.Value ?? "";

                                var reqTypeName = GetComponentTypeName(reqType);
                                var depTypeName = GetComponentTypeName(depType);

                                var entry = $"  • {depName}  ({depTypeName})\n" +
                                            $"    requires: {reqName}  ({reqTypeName})";
                                if (!string.IsNullOrEmpty(reqSolution))
                                    entry += $"\n    solution: {reqSolution}";
                                if (!string.IsNullOrEmpty(reqId))
                                    entry += $"   id: {reqId}";
                                if (canResolve == "True")
                                    entry += "\n    [can be resolved automatically]";
                                deps.Add(entry);
                            }
                        }
                    }
                    catch
                    {
                        // XML parse failed — just use the intro
                    }

                    if (deps.Count > 0)
                        return intro + "\n\nMissing Dependencies:\n" + string.Join("\n\n", deps);
                    return intro;
                }

                // No MissingDependencies — clean up the message
                // Strip "Solution manifest import: FAILURE: " prefix
                message = Regex.Replace(message, @"^Solution manifest import:\s*FAILURE:\s*", "", RegexOptions.IgnoreCase);
                // Remove trailing ", ProductUpdatesOnly : ..." noise
                message = Regex.Replace(message, @",\s*ProductUpdatesOnly\s*:\s*\w+\s*$", "", RegexOptions.IgnoreCase);
                return message.Trim();
            }
            catch
            {
                // Fallback: return first 500 chars of raw message
                return rawMessage.Length > 500 ? rawMessage.Substring(0, 500) + "..." : rawMessage;
            }
        }

        /// <summary>
        /// Maps Dynamics 365 component type codes to human-readable names.
        /// See: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
        /// </summary>
        internal static string GetComponentTypeName(string typeCode)
        {
            switch (typeCode)
            {
                case "1": return "Entity";
                case "2": return "Attribute";
                case "3": return "Relationship";
                case "4": return "Attribute Picklist Value";
                case "5": return "Attribute Lookup Value";
                case "6": return "View Attribute";
                case "7": return "Localized Label";
                case "8": return "Relationship Extra Condition";
                case "9": return "Option Set";
                case "10": return "Entity Relationship";
                case "11": return "Entity Relationship Role";
                case "12": return "Entity Relationship Relationships";
                case "13": return "Managed Property";
                case "14": return "Entity Key";
                case "20": return "Security Role";
                case "21": return "Role Privilege";
                case "22": return "Display String";
                case "23": return "Display String Map";
                case "24": return "Form";
                case "25": return "Organization";
                case "26": return "Saved Query";
                case "29": return "Workflow";
                case "31": return "Report";
                case "36": return "Connection Role";
                case "37": return "Custom Control";
                case "38": return "Custom Control Default Config";
                case "44": return "Entity Map";
                case "45": return "Attribute Map";
                case "46": return "Ribbon Command";
                case "47": return "Ribbon Context Group";
                case "48": return "Ribbon Customization";
                case "49": return "Ribbon Rule";
                case "50": return "Ribbon Tab To Command Map";
                case "52": return "Ribbon Diff";
                case "53": return "Saved Query Visualization";
                case "55": return "System Form";
                case "59": return "Chart";
                case "60": return "User Chart";
                case "61": return "Web Resource";
                case "62": return "Site Map";
                case "63": return "Connection Role";
                case "65": return "Hierarchy Rule";
                case "66": return "Custom Control Resource";
                case "70": return "Field Security Profile";
                case "71": return "Field Permission";
                case "80": return "Model-driven App";
                case "90": return "Plugin Type";
                case "91": return "Plugin Assembly";
                case "92": return "SDK Message Processing Step";
                case "93": return "SDK Message Processing Step Image";
                case "95": return "Service Endpoint";
                case "150": return "Routing Rule";
                case "151": return "Routing Rule Item";
                case "152": return "SLA";
                case "153": return "SLA Item";
                case "154": return "Convert Rule";
                case "155": return "Convert Rule Item";
                case "161": return "Mobile Offline Profile";
                case "162": return "Mobile Offline Profile Item";
                case "165": return "Similarity Rule";
                case "166": return "Data Source Mapping";
                case "170": return "SdkMessage";
                case "171": return "SdkMessageFilter";
                case "172": return "SdkMessagePair";
                case "173": return "SdkMessageRequest";
                case "174": return "SdkMessageRequestField";
                case "175": return "SdkMessageResponse";
                case "176": return "SdkMessageResponseField";
                case "208": return "Import Map";
                case "210": return "Canvas App";
                case "300": return "Canvas App";
                case "371": return "Connector";
                case "372": return "Environment Variable Definition";
                case "373": return "Environment Variable Value";
                case "380": return "AI Project Type";
                case "381": return "AI Project";
                case "382": return "AI Configuration";
                case "400": return "AI Plugin";
                case "401": return "AI Plugin External Schema";
                case "402": return "AI Plugin External Schema Property";
                case "500": return "Power Automate Flow";
                default: return $"Component (type {typeCode})";
            }
        }

        // ── Pre-import dependency check ──

        public class MissingComponentResult
        {
            [JsonProperty("solution")] public string Solution { get; set; }
            [JsonProperty("target")] public string Target { get; set; }
            [JsonProperty("requiredName")] public string RequiredName { get; set; }
            [JsonProperty("requiredType")] public string RequiredType { get; set; }
            [JsonProperty("requiredId")] public string RequiredId { get; set; }
            [JsonProperty("requiredSolution")] public string RequiredSolution { get; set; }
            [JsonProperty("dependentName")] public string DependentName { get; set; }
            [JsonProperty("dependentType")] public string DependentType { get; set; }
        }

        /// <summary>
        /// Checks a solution zip against a target environment for missing dependency components.
        /// Uses the Dataverse "RetrieveMissingComponents" message — does NOT import anything.
        /// See: https://learn.microsoft.com/en-us/power-platform/alm/solution-api#detect-solution-dependencies
        /// </summary>
        public static List<MissingComponentResult> CheckMissingComponents(
            ConnectionDetail targetDetail,
            string solutionFriendlyName,
            byte[] solutionContent)
        {
            var results = new List<MissingComponentResult>();
            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                var request = new OrganizationRequest("RetrieveMissingComponents")
                {
                    ["CustomizationFile"] = solutionContent
                };
                var response = svc.Execute(request);

                if (response.Results.ContainsKey("MissingComponents"))
                {
                    var entities = response.Results["MissingComponents"] as EntityCollection;
                    if (entities != null)
                    {
                        foreach (var entity in entities.Entities)
                        {
                            var requiredType = entity.GetAttributeValue<OptionSetValue>("requiredcomponenttype")?.Value.ToString() ?? "";
                            var dependentType = entity.GetAttributeValue<OptionSetValue>("dependentcomponenttype")?.Value.ToString() ?? "";

                            results.Add(new MissingComponentResult
                            {
                                Solution = solutionFriendlyName,
                                Target = targetDetail.ConnectionName,
                                RequiredName = entity.GetAttributeValue<string>("requiredcomponentschema") ?? entity.GetAttributeValue<string>("requiredcomponentparentschema") ?? "",
                                RequiredType = GetComponentTypeName(requiredType),
                                RequiredId = entity.GetAttributeValue<Guid?>("requiredcomponentobjectid")?.ToString() ?? "",
                                RequiredSolution = entity.GetAttributeValue<string>("requiredcomponentparentschema") ?? "",
                                DependentName = entity.GetAttributeValue<string>("dependentcomponentschema") ?? entity.GetAttributeValue<string>("dependentcomponentparentschema") ?? "",
                                DependentType = GetComponentTypeName(dependentType),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[CheckMissingComponents] {solutionFriendlyName} → {targetDetail.ConnectionName}: {ex.Message}");
            }
            return results;
        }

        // ── Active import detection ──

        public class ActiveImportInfo
        {
            [JsonProperty("solutionName")] public string SolutionName { get; set; }
            [JsonProperty("startedOn")] public DateTime StartedOn { get; set; }
            [JsonProperty("progress")] public double Progress { get; set; }
            [JsonProperty("createdBy")] public string CreatedBy { get; set; }
        }

        /// <summary>
        /// Check for active (incomplete) imports on a target environment.
        /// </summary>
        public static List<ActiveImportInfo> GetActiveImports(ConnectionDetail targetDetail)
        {
            var result = new List<ActiveImportInfo>();
            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                var query = new QueryExpression("importjob")
                {
                    NoLock = true,
                    ColumnSet = new ColumnSet("solutionname", "startedon", "progress", "createdby"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("completedon", ConditionOperator.Null),
                            // Only imports started in the last 24 hours to avoid stale records
                            new ConditionExpression("startedon", ConditionOperator.LastXHours, 24)
                        }
                    }
                };

                var entities = svc.RetrieveMultiple(query).Entities;
                foreach (var e in entities)
                {
                    result.Add(new ActiveImportInfo
                    {
                        SolutionName = e.GetAttributeValue<string>("solutionname") ?? "Unknown",
                        StartedOn = e.GetAttributeValue<DateTime>("startedon"),
                        Progress = e.GetAttributeValue<double>("progress"),
                        CreatedBy = e.GetAttributeValue<EntityReference>("createdby")?.Name ?? "Unknown"
                    });
                }
            }
            catch { /* If check fails, proceed anyway */ }
            return result;
        }

        // ── Publish ──

        public static bool PublishCustomizations(ConnectionDetail targetDetail)
        {
            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                svc.Execute(new PublishAllXmlRequest());
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── Version bump ──

        public static string BumpVersion(string currentVersion, string policy, string dateMask = "yyyy.MM.dd.x")
        {
            var parts = currentVersion.Split('.');
            while (parts.Length < 4) parts = parts.Concat(new[] { "0" }).ToArray();

            int major = int.TryParse(parts[0], out var m) ? m : 0;
            int minor = int.TryParse(parts[1], out var mi) ? mi : 0;
            int build = int.TryParse(parts[2], out var b) ? b : 0;
            int revision = int.TryParse(parts[3], out var r) ? r : 0;

            switch (policy)
            {
                case "Major": return $"{major + 1}.0.0.0";
                case "Minor": return $"{major}.{minor + 1}.0.0";
                case "Build": return $"{major}.{minor}.{build + 1}.0";
                case "Revision": return $"{major}.{minor}.{build}.{revision + 1}";
                case "Date":
                    var now = DateTime.Now;
                    var dateBase = dateMask
                        .Replace("yyyy", now.Year.ToString())
                        .Replace("MM", now.Month.ToString("D2"))
                        .Replace("dd", now.Day.ToString("D2"))
                        .Replace("HHmm", now.ToString("HHmm"));
                    // Handle 'x' as incremental — start at 1, increment if same date prefix
                    var prefix = dateBase.Substring(0, dateBase.IndexOf('x') >= 0 ? dateBase.IndexOf('x') : dateBase.Length);
                    if (currentVersion.StartsWith(prefix))
                    {
                        var lastPart = currentVersion.Substring(prefix.Length);
                        var lastNum = int.TryParse(lastPart, out var ln) ? ln : 0;
                        return prefix + (lastNum + 1);
                    }
                    return dateBase.Replace("x", "1");
                default: return currentVersion;
            }
        }

        // ── Export to disk ──

        public static string SaveSolutionToDisk(
            byte[] content,
            string solutionName,
            string version,
            bool managed,
            string folderPath)
        {
            var fileName = $"{solutionName}_{version.Replace(".", "_")}{(managed ? "_managed" : "")}.zip";
            var filePath = Path.Combine(folderPath, fileName);
            File.WriteAllBytes(filePath, content);
            return filePath;
        }

        // ── Import log retrieval ──

        public static string GetImportLog(
            IOrganizationService targetService,
            Guid importJobId)
        {
            try
            {
                var importJob = targetService.Retrieve("importjob", importJobId,
                    new ColumnSet("data", "progress"));
                return importJob.GetAttributeValue<string>("data") ?? "";
            }
            catch
            {
                return "";
            }
        }

        // ── Update solution version ──

        public static bool UpdateSolutionVersion(
            IOrganizationService sourceService,
            Guid solutionId,
            string newVersion)
        {
            try
            {
                var update = new Entity("solution", solutionId)
                {
                    ["version"] = newVersion
                };
                sourceService.Update(update);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
