using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Multiplayer.Data;

public class FQosServer
{
    /// <summary>
    ///     [optional] The region the QoS server is located in.
    /// </summary>
    [JsonPropertyName("Error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Region { get; set; }
    
    /// <summary>
    ///     [optional] The QoS server URL.
    /// </summary>
    [JsonPropertyName("ServerUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ServerUrl { get; set; }
}