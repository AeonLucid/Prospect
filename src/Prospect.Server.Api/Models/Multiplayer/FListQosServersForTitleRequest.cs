using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Multiplayer;

public class FListQosServersForTitleRequest
{
    /// <summary>
    ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
    /// </summary>
    [JsonPropertyName("CustomTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? CustomTags { get; set; }
        
    /// <summary>
    ///     [optional] Indicates that the response should contain Qos servers for all regions, including those where there are no builds
    ///     deployed for the title.
    /// </summary>
    [JsonPropertyName("IncludeAllRegions")]
    public bool? IncludeAllRegions { get; set; }
}