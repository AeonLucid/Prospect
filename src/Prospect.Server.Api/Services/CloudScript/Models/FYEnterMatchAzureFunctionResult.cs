using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYEnterMatchAzureFunctionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("port")]
    public int Port { get; set; }
    
    [JsonPropertyName("sharedIndex")]
    public int ShardIndex { get; set; }
    
    [JsonPropertyName("singleplayerStation")]
    public bool SingleplayerStation { get; set; }
    
    [JsonPropertyName("maintenanceMode")]
    public bool MaintenanceMode { get; set; }
    
    [JsonPropertyName("sessionTimeJoinDelay")]
    public float SessionTimeJoinDelay { get; set; }
}
