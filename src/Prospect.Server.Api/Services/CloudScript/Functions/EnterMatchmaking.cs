using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("EnterMatchmaking")]
public class EnterMatchmaking : ICloudScriptFunction<FYEnterMatchAzureFunction, FYEnterMatchAzureFunctionResult>
{
    public Task<FYEnterMatchAzureFunctionResult> ExecuteAsync(FYEnterMatchAzureFunction request)
    {
        return Task.FromResult(new FYEnterMatchAzureFunctionResult
        {
            Success = true,
            Address = "127.0.0.1",
            ErrorMessage = "",
            Port = 7777,
            ShardIndex = 0,
            SingleplayerStation = false,
            MaintenanceMode = false,
            SessionTimeJoinDelay = 0
        });
    }
}