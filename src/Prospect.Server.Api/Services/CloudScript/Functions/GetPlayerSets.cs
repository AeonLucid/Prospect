using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetPlayerSets")]
public class GetPlayerSets : ICloudScriptFunction<FYGetPlayersSets, object?>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPlayerSets(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<object?> ExecuteAsync(FYGetPlayersSets request)
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
                new
                {
                    setData = new
                    {
                        id = "",
                        userId = context.User.FindAuthUserId(),
                        kit = "",
                        shield = "",
                        helmet = "",
                        weaponOne = "",
                        weaponTwo = "",
                        bag = "",
                        bagItemsAsJsonStr = "",
                        safeItemsAsJsonStr = ""
                    },
                    items = Array.Empty<object>()
                }
            }
        });
    }
}