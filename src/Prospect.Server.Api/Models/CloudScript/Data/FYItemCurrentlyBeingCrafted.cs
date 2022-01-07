using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data;

public class FYItemCurrentlyBeingCrafted
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = null!;
        
    [JsonPropertyName("itemRarity")]
    public int ItemRarity { get; set; }
        
    [JsonPropertyName("purchaseAmount")]
    public int PurchaseAmount { get; set; }
        
    [JsonPropertyName("utcTimestampWhenCraftingStarted")]
    public FYTimestamp UtcTimestampWhenCraftingStarted { get; set; } = null!;
}