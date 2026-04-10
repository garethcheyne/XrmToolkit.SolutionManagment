using McTools.Xrm.Connection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace err403.SolutionManagment.AppCode
{
    public static class EnvironmentIdResolver
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<string, string> _envIdCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string GdsResource = "https://globaldisco.crm.dynamics.com";
        private const string XrmToolBoxClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
        private static readonly Uri RedirectUri = new Uri("app://58145B91-0C36-4500-8554-080854F2AC97");

        // Cached GDS token from interactive auth
        private static string _gdsToken;
        private static DateTimeOffset _gdsTokenExpiry = DateTimeOffset.MinValue;

        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MscrmTools", "XrmToolBox", "Settings", "err403.gds.token");

        public static bool HasGdsToken => !string.IsNullOrEmpty(_gdsToken) && DateTimeOffset.UtcNow < _gdsTokenExpiry;

        static EnvironmentIdResolver()
        {
            LoadPersistedToken();
        }

        public static string Resolve(ConnectionDetail detail)
        {
            Trace.WriteLine($"[EnvIdResolver] ConnectionName={detail?.ConnectionName}, detail.EnvironmentId={detail?.EnvironmentId ?? "(null)"}");

            if (!string.IsNullOrEmpty(detail?.EnvironmentId))
            {
                Trace.WriteLine($"[EnvIdResolver] Using detail.EnvironmentId: {detail.EnvironmentId}");
                return detail.EnvironmentId;
            }

            var svc = detail?.GetCrmServiceClient();
            if (svc == null) return null;

            // Try CrmServiceClient.EnvironmentId
            try
            {
                var envId = svc.EnvironmentId;
                Trace.WriteLine($"[EnvIdResolver] CrmServiceClient.EnvironmentId={envId ?? "(null)"}");
                if (!string.IsNullOrEmpty(envId))
                    return envId;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EnvIdResolver] CrmServiceClient ERROR: {ex.Message}");
            }

            // Try OrganizationDetail.EnvironmentId
            try
            {
                var envId = svc.OrganizationDetail?.EnvironmentId;
                Trace.WriteLine($"[EnvIdResolver] OrganizationDetail.EnvironmentId={envId ?? "(null)"}");
                if (!string.IsNullOrEmpty(envId))
                    return envId;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EnvIdResolver] OrgDetail ERROR: {ex.Message}");
            }

            // Fallback: Global Discovery Service
            try
            {
                var orgUrl = svc.CrmConnectOrgUriActual?.GetLeftPart(UriPartial.Authority)?.TrimEnd('/');
                if (!string.IsNullOrEmpty(orgUrl) && HasGdsToken)
                {
                    Trace.WriteLine($"[EnvIdResolver] GDS lookup with cached token, orgUrl={orgUrl}");
                    var envId = QueryGlobalDiscovery(_gdsToken, orgUrl);
                    Trace.WriteLine($"[EnvIdResolver] GDS result={envId ?? "(null)"}");
                    if (!string.IsNullOrEmpty(envId))
                        return envId;
                }
                else
                {
                    Trace.WriteLine($"[EnvIdResolver] GDS skipped: orgUrl={orgUrl ?? "(null)"}, hasToken={HasGdsToken}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EnvIdResolver] GDS ERROR: {ex.Message}");
            }

            Trace.WriteLine("[EnvIdResolver] Returning null — use Authenticate button for GDS");
            return null;
        }

        /// <summary>
        /// Opens an interactive OAuth browser window to acquire a token for the GDS.
        /// Call this from the UI thread.
        /// </summary>
        public static bool AuthenticateInteractively(ConnectionDetail detail, IWin32Window owner)
        {
            try
            {
                var svc = detail?.GetCrmServiceClient();
                var authority = svc?.Authority?.TrimEnd('/');

                if (string.IsNullOrEmpty(authority))
                {
                    // Fallback to common endpoint
                    var tenantId = detail?.TenantId ?? Guid.Empty;
                    authority = tenantId != Guid.Empty
                        ? $"https://login.microsoftonline.com/{tenantId}"
                        : "https://login.microsoftonline.com/common";
                }

                string clientId = detail?.AzureAdAppId != Guid.Empty
                    ? detail.AzureAdAppId.ToString()
                    : XrmToolBoxClientId;

                Trace.WriteLine($"[GDS Auth] Interactive auth: authority={authority}, clientId={clientId}");

                var authContext = new AuthenticationContext(authority);
                var platformParams = new PlatformParameters(PromptBehavior.SelectAccount, owner);

                var result = authContext.AcquireTokenAsync(GdsResource, clientId, RedirectUri, platformParams)
                    .GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(result?.AccessToken))
                {
                    _gdsToken = result.AccessToken;
                    _gdsTokenExpiry = result.ExpiresOn;
                    PersistToken();
                    Trace.WriteLine($"[GDS Auth] Interactive auth succeeded, expires={result.ExpiresOn:u}");
                    return true;
                }

                Trace.WriteLine("[GDS Auth] Interactive auth returned null token");
            }
            catch (AdalException ex) when (ex.ErrorCode == "authentication_canceled")
            {
                Trace.WriteLine("[GDS Auth] User cancelled authentication");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GDS Auth] Interactive auth error: {ex.Message}");
                MessageBox.Show(owner as Control,
                    $"Authentication failed:\n{ex.Message}",
                    "GDS Authentication",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            return false;
        }

        /// <summary>
        /// Try silent token acquisition (uses ADAL cache from previous interactive auth).
        /// </summary>
        public static bool TrySilentAuth(ConnectionDetail detail)
        {
            try
            {
                var svc = detail?.GetCrmServiceClient();
                var authority = svc?.Authority?.TrimEnd('/');
                if (string.IsNullOrEmpty(authority)) return false;

                string clientId = detail?.AzureAdAppId != Guid.Empty
                    ? detail.AzureAdAppId.ToString()
                    : XrmToolBoxClientId;

                var authContext = new AuthenticationContext(authority);
                var result = authContext.AcquireTokenSilentAsync(GdsResource, clientId)
                    .GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(result?.AccessToken))
                {
                    _gdsToken = result.AccessToken;
                    _gdsTokenExpiry = result.ExpiresOn;
                    PersistToken();
                    Trace.WriteLine($"[GDS Auth] Silent auth succeeded, expires={result.ExpiresOn:u}");
                    return true;
                }
            }
            catch (AdalSilentTokenAcquisitionException)
            {
                Trace.WriteLine("[GDS Auth] Silent auth failed — interactive auth required");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GDS Auth] Silent auth error: {ex.Message}");
            }
            return false;
        }

        private static string QueryGlobalDiscovery(string accessToken, string orgUrl)
        {
            if (_envIdCache.TryGetValue(orgUrl, out var cached))
            {
                Trace.WriteLine($"[GDS] Cache hit for {orgUrl} = {cached}");
                return cached;
            }

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            Trace.WriteLine("[GDS] Calling globaldisco.crm.dynamics.com...");
            var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Trace.WriteLine($"[GDS] HTTP {(int)response.StatusCode}, body length={body.Length}");

            if (!response.IsSuccessStatusCode)
            {
                Trace.WriteLine($"[GDS] Error body: {body.Substring(0, Math.Min(500, body.Length))}");
                return null;
            }

            var json = Newtonsoft.Json.Linq.JObject.Parse(body);
            var instances = json["value"] as Newtonsoft.Json.Linq.JArray;
            if (instances == null)
            {
                Trace.WriteLine("[GDS] No 'value' array in response");
                return null;
            }

            Trace.WriteLine($"[GDS] Found {instances.Count} instances");

            foreach (var inst in instances)
            {
                var apiUrl = inst["ApiUrl"]?.ToString()?.TrimEnd('/');
                var envId = inst["EnvironmentId"]?.ToString();
                var friendlyName = inst["FriendlyName"]?.ToString();

                Trace.WriteLine($"[GDS]   {friendlyName}: ApiUrl={apiUrl}, EnvironmentId={envId}");

                if (!string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(envId))
                {
                    _envIdCache[apiUrl] = envId;
                }
            }

            if (_envIdCache.TryGetValue(orgUrl, out var found))
            {
                Trace.WriteLine($"[GDS] Matched {orgUrl} => {found}");
                return found;
            }

            Trace.WriteLine($"[GDS] No match for {orgUrl}");
            return null;
        }

        private static void PersistToken()
        {
            try
            {
                var dir = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var payload = $"{_gdsTokenExpiry:O}|{_gdsToken}";
                var clearBytes = Encoding.UTF8.GetBytes(payload);
                var encrypted = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenFilePath, encrypted);
                Trace.WriteLine($"[GDS Auth] Token persisted to {TokenFilePath}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GDS Auth] Failed to persist token: {ex.Message}");
            }
        }

        private static void LoadPersistedToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath)) return;

                var encrypted = File.ReadAllBytes(TokenFilePath);
                var clearBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var payload = Encoding.UTF8.GetString(clearBytes);

                var separatorIndex = payload.IndexOf('|');
                if (separatorIndex < 0) return;

                var expiryStr = payload.Substring(0, separatorIndex);
                var token = payload.Substring(separatorIndex + 1);

                if (DateTimeOffset.TryParse(expiryStr, out var expiry) && DateTimeOffset.UtcNow < expiry)
                {
                    _gdsToken = token;
                    _gdsTokenExpiry = expiry;
                    Trace.WriteLine($"[GDS Auth] Loaded persisted token, expires={expiry:u}");
                }
                else
                {
                    Trace.WriteLine("[GDS Auth] Persisted token expired, ignoring");
                    File.Delete(TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[GDS Auth] Failed to load persisted token: {ex.Message}");
            }
        }
    }
}
