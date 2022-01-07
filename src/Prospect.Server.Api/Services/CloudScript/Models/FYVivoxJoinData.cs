using System.Text.Json.Serialization;
using Prospect.Server.Api.Services.CloudScript.Models.Data;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYVivoxJoinData
{
    [JsonPropertyName("request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FYVivoxJoinTokenRequest? Request { get; set; }
    
    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }
}