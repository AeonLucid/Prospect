using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYGetCraftingInProgressDataRequest
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}