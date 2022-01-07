namespace Prospect.Server.Api.Services.CloudScript;

public interface ICloudScriptFunction<TReq, TRes>
{
    Task<TRes> ExecuteAsync(TReq request);
}