using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("RequestMaintenanceModeState")]
public class RequestMaintenanceModeState : ICloudScriptFunction<FYMaintenanceModeState, FYRequestMaintenanceModeStateResult>
{
    public Task<FYRequestMaintenanceModeStateResult> ExecuteAsync(FYMaintenanceModeState request)
    {
        return Task.FromResult(new FYRequestMaintenanceModeStateResult
        {
            Enabled = false
        });
    }
}