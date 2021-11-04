using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data
{
    public class FYItemCurrentlyBeingCrafted
    {
        [JsonPropertyName("")]
        public string ItemId { get; set; }
        
        [JsonPropertyName("")]
        public int ItemRarity { get; set; }
        
        [JsonPropertyName("")]
        public int PurchaseAmount { get; set; }
        
        [JsonPropertyName("")]
        public FYTimestamp UtcTimestampWhenCraftingStarted { get; set; }
    }
}