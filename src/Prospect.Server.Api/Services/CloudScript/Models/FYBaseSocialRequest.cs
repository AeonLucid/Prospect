using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYBaseSocialRequest
{
    [JsonPropertyName("targetPlayFabId")]
    public string? TargetPlayFabId { get; set; }
}