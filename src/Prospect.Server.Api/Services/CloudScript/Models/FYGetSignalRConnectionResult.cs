using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYGetSignalRConnectionResult
{
    [JsonPropertyName("url")] 
    public string Url { get; set; } = null!;
    
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = null!;
}