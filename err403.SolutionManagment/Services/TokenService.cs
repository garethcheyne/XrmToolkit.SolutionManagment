using McTools.Xrm.Connection;
using err403.SolutionManagment.AppCode;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Extracts auth tokens and org URLs from CrmServiceClient connections.
    /// Pure execution — no UI.
    /// </summary>
    public static class TokenService
    {
        public class AuthContext
        {
            public string OrgUrl { get; set; }
            public string Token { get; set; }
            public string EnvironmentId { get; set; }
            public string ConnectionName { get; set; }
        }

        public static AuthContext GetAuthContext(ConnectionDetail detail)
        {
            var svc = detail.GetCrmServiceClient();
            return new AuthContext
            {
                OrgUrl = svc?.CrmConnectOrgUriActual?.GetLeftPart(System.UriPartial.Authority)?.TrimEnd('/')
                    ?? detail.WebApplicationUrl?.TrimEnd('/'),
                Token = svc?.CurrentAccessToken ?? "",
                EnvironmentId = EnvironmentIdResolver.Resolve(detail) ?? "",
                ConnectionName = detail.ConnectionName
            };
        }

        public static AuthContext RefreshToken(ConnectionDetail detail)
        {
            // GetCrmServiceClient() returns a fresh client which refreshes the token
            return GetAuthContext(detail);
        }
    }
}
