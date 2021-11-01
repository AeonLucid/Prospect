using System;
using Microsoft.AspNetCore.Authentication;
using Prospect.Server.Api.Services.Auth.Entity;
using Prospect.Server.Api.Services.Auth.User;

namespace Prospect.Server.Api.Services.Auth
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddUserAuthentication(this AuthenticationBuilder authenticationBuilder, Action<UserAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<UserAuthenticationOptions, UserAuthenticationHandler>(UserAuthenticationOptions.DefaultScheme, options);
        }
        
        public static AuthenticationBuilder AddEntityAuthentication(this AuthenticationBuilder authenticationBuilder, Action<EntityAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<EntityAuthenticationOptions, EntityAuthenticationHandler>(EntityAuthenticationOptions.DefaultScheme, options);
        }
    }
}