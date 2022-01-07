using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYGetPlayersSets
{
    [JsonPropertyName("userIds")]
    public List<string>? UserIds { get; set; }
}