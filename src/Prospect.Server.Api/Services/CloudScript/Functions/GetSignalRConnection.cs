using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetSignalRConnection")]
public class GetSignalRConnection : ICloudScriptFunction<FYGetSignalRConnection, FYGetSignalRConnectionResult>
{
    public Task<FYGetSignalRConnectionResult> ExecuteAsync(FYGetSignalRConnection request)
    {
        return Task.FromResult(new FYGetSignalRConnectionResult
        {
            Url = "https://localhost:5443/signalr/?hub=pubsub",
            AccessToken = "TEST"
        });
    }
}