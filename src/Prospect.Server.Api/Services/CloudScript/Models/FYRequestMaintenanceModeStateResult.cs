using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYRequestMaintenanceModeStateResult
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}