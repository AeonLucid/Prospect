using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Services.Auth.Extensions;

namespace Prospect.Server.Api.Services.Auth.Entity
{
    public class EntityAuthenticationHandler : AuthenticationHandler<EntityAuthenticationOptions>
    {
        private const string Header = "X-EntityToken";
        private const string Type = AuthType.Entity;
        
        private readonly AuthTokenService _authTokenService;
        
        public EntityAuthenticationHandler(
            IOptionsMonitor<EntityAuthenticationOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder, 
            ISystemClock clock,
            AuthTokenService authTokenService) : base(options, logger, encoder, clock)
        {
            _authTokenService = authTokenService;
        }
        
        private ClientResponse Res { get; set; } = new ClientResponse
        {
            Code = 401,
            Status = "Unauthorized",
            Error = "NotAuthenticated",
            ErrorCode = 1074,
            ErrorMessage = "This API method does not allow anonymous callers."
        };

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Header, out var headerValues) || headerValues.Count == 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("No header"));
            }
            
            var headerValue = headerValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                return Task.FromResult(AuthenticateResult.Fail("Empty header value"));
            }

            Res.Error = "EntityTokenInvalid";
            Res.ErrorCode = 1335;
            Res.ErrorMessage = "EntityTokenInvalid";
            
            var token = _authTokenService.Validate(headerValue);
            if (token != null)
            {
                var identity = new ClaimsIdentity(token.Claims, Options.AuthenticationType);
                var identities = new List<ClaimsIdentity> { identity };
                var principal = new ClaimsPrincipal(identities);
                if (principal.FindAuthType() != Type)
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid auth type"));
                }
                
                var ticket = new AuthenticationTicket(principal, Options.Scheme);
                
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid JWT"));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
        
            await Response.WriteAsync(JsonSerializer.Serialize(Res));
        }
    }
}