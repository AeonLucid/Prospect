using Microsoft.AspNetCore.Authentication;

namespace Prospect.Server.Api.Services.Auth.Entity;

public class EntityAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "EntityAuth";
    public string Scheme => DefaultScheme;
    public string AuthenticationType = DefaultScheme;
}