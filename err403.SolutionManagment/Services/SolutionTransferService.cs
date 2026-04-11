using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            public string ErrorMessage { get; set; }
            public string ImportLogXml { get; set; }
        }

        // ── Export ──

        public static ExportResult ExportSolution(
            IOrganizationService sourceService,
            string solutionUniqueName,
            bool managed)
        {
            try
            {
                var request = new ExportSolutionRequest
                {
                    SolutionName = solutionUniqueName,
                    Managed = managed
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
                        SkipProductUpdateDependencies = true
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
                        SkipProductUpdateDependencies = true
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
