using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Multiplayer.Data;

namespace Prospect.Server.Api.Models.Multiplayer;

public class FListQosServersForTitleResponse
{
    /// <summary>
    ///     The page size on the response.
    /// </summary>
    [JsonPropertyName("PageSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int PageSize { get; set; }
    
    /// <summary>
    ///     [optional] The list of QoS servers.
    /// </summary>
    [JsonPropertyName("QosServers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<FQosServer>? QosServers { get; set; }

    /// <summary>
    ///     [optional] The skip token for the paged response.
    /// </summary>
    [JsonPropertyName("SkipToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SkipToken { get; set; }
}