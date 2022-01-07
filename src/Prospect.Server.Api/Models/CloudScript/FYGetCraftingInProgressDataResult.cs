using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.CloudScript.Data;

namespace Prospect.Server.Api.Models.CloudScript;

public class FYGetCraftingInProgressDataResult
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;
        
    [JsonPropertyName("error")]
    public string Error { get; set; } = null!;
        
    [JsonPropertyName("itemCurrentlyBeingCrafted")]
    public FYItemCurrentlyBeingCrafted ItemCurrentlyBeingCrafted { get; set; } = null!;
}