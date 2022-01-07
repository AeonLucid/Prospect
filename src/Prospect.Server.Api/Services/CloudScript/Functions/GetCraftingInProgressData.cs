using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;
using Prospect.Server.Api.Services.CloudScript.Models.Data;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetCraftingInProgressData")]
public class GetCraftingInProgressData : ICloudScriptFunction<FYGetCraftingInProgressDataRequest, FYGetCraftingInProgressDataResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetCraftingInProgressData(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<FYGetCraftingInProgressDataResult> ExecuteAsync(FYGetCraftingInProgressDataRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            throw new CloudScriptException("CloudScript was not called within a http request");
        }

        return Task.FromResult(new FYGetCraftingInProgressDataResult
        {
            UserId = context.User.FindAuthUserId(),
            Error = string.Empty,
            ItemCurrentlyBeingCrafted = new FYItemCurrentlyBeingCrafted
            {
                ItemId = null,
                ItemRarity = -1,
                PurchaseAmount = -1,
                UtcTimestampWhenCraftingStarted = new FYTimestamp
                {
                    Seconds = 0
                }
            }
        });
    }
}