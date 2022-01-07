using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models.Data;

public class FYTimestamp
{
    [JsonPropertyName("seconds")]
    public int Seconds { get; set; }
}