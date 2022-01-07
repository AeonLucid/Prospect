using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetPlayersInventoriesLimits")]
public class GetPlayersInventoriesLimits : ICloudScriptFunction<FYGetPlayersInventoriesLimitsRequest, object?>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPlayersInventoriesLimits(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<object?> ExecuteAsync(FYGetPlayersInventoriesLimitsRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return Task.FromResult<object?>(null);
        }
        
        return Task.FromResult<object?>(new
        {
            success = true,
            entries = new []
            {
                new {
                    userId = context.User.FindAuthUserId(),
                    inventoryStashLimit = 75,
                    inventoryBagLimit = 300,
                    inventorySafeLimit = 5
                }
            }
        });
    }
}