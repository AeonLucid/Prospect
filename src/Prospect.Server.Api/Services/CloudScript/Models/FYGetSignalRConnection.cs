using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYGetSignalRConnection
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; }
}