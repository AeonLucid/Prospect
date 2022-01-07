using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models.Data;

public class FYVivoxJoinTokenRequest
{
    [JsonPropertyName("userName")]
    public string? Username { get; set; }
    
    [JsonPropertyName("channel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Channel { get; set; }
    
    // TODO: channelType
    // TODO: hasText
    // TODO: hasAudio
    // TODO: channelId
}