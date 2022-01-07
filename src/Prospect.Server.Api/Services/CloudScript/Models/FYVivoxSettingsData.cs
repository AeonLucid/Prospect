using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYVivoxSettingsData
{
    [JsonPropertyName("vivoxServer")]
    public string? VivoxServer { get; set; }
    
    [JsonPropertyName("vivoxDomain")]
    public string? VivoxDomain { get; set; }
    
    [JsonPropertyName("vivoxIssuer")]
    public string? VivoxIssuer { get; set; }
}