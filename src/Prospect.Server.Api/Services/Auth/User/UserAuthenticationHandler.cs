using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Services.Auth.Extensions;

namespace Prospect.Server.Api.Services.Auth.User;

public class UserAuthenticationHandler : AuthenticationHandler<UserAuthenticationOptions>
{
    private const string Header = "X-Authorization";
    private const string Type = AuthType.User;
        
    private readonly AuthTokenService _authTokenService;
        
    public UserAuthenticationHandler(
        IOptionsMonitor<UserAuthenticationOptions> options, 
        ILoggerFactory logger, 
        UrlEncoder encoder, 
        ISystemClock clock,
        AuthTokenService authTokenService) : base(options, logger, encoder, clock)
    {
        _authTokenService = authTokenService;
    }
        
    private string Failure { get; set; } = "Not Authenticated";

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

        Failure = "X-Authentication HTTP header contains invalid ticket";

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
        
        await Response.WriteAsync(JsonSerializer.Serialize(new ClientResponse
        {
            Code = 401,
            Status = "Unauthorized",
            Error = "NotAuthenticated",
            ErrorCode = 1074,
            ErrorMessage = Failure
        }));
    }
}