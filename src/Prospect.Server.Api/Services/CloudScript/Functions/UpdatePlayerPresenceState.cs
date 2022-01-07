using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("UpdatePlayerPresenceState")]
public class UpdatePlayerPresenceState : ICloudScriptFunction<FYUpdatePlayerPresenceStateRequest, object>
{
    public Task<object> ExecuteAsync(FYUpdatePlayerPresenceStateRequest request)
    {
        return Task.FromResult<object>(new { });
    }
}