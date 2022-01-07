using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("RequestFactionProgression")]
public class RequestFactionProgression : ICloudScriptFunction<FYQueryFactionProgressionRequest, object?>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestFactionProgression(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<object?> ExecuteAsync(FYQueryFactionProgressionRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return Task.FromResult<object?>(null);
        }
        
        return Task.FromResult<object?>(new
        {
            factions = new []
            {
                new
                {
                    factionId = "ICA",
                    currentProgression = 0
                },
                new
                {
                    factionId = "Korolev",
                    currentProgression = 0
                },
                new
                {
                    factionId = "Osiris",
                    currentProgression = 0
                },
            },
            userId = context.User.FindAuthUserId(),
            error = ""
        });
    }
}