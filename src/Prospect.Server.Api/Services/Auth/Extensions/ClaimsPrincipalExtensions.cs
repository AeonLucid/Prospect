using System.Security.Claims;
using Prospect.Server.Api.Exceptions;

namespace Prospect.Server.Api.Services.Auth.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string FindAuthId(this ClaimsPrincipal principal)
        {
            return Find(principal, AuthClaimTypes.Id);
        }
        
        public static string FindAuthType(this ClaimsPrincipal principal)
        {
            return Find(principal, AuthClaimTypes.Type);
        }

        private static string Find(ClaimsPrincipal principal, string claimType)
        {
            var claim = principal.FindFirst(claimType);
            if (claim == null)
            {
                throw new ProspectException($"Failed to find claim {claimType}");
            }

            return claim.Value;
        }
    }
}