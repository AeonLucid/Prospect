using Microsoft.AspNetCore.Authentication;

namespace Prospect.Server.Api.Services.Auth.User
{
    public class UserAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "UserAuth";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }
}