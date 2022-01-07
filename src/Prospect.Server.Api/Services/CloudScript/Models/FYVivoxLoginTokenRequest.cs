using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYVivoxLoginTokenRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}