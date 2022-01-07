using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript.Models.Data;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYEnterMatchAzureFunction
{
    [JsonPropertyName("optimalRegion")]
    public string? OptimalRegion { get; set; }
    
    [JsonPropertyName("isMatch")]
    public bool IsMatch { get; set; }
    
    [JsonPropertyName("bypassMaintenanceMode")]
    public bool BypassMaintenanceMode { get; set; }
    
    [JsonPropertyName("debugOption")]
    public string? DebugOption { get; set; }
    
    [JsonPropertyName("mapName")]
    public string? MapName { get; set; }
    
    [JsonPropertyName("squad_id")]
    public string? SquadId { get; set; }
    
    [JsonPropertyName("purchaseInventoryInsurances")]
    public List<FYPurchaseInventoryInsuranceRequest>? PurchaseInventoryInsurances { get; set; }
}