using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data;

public class FYFactionsContractsData
{
    [JsonPropertyName("boards")]
    public List<FYFactionContractsData> Boards { get; set; }

    [JsonPropertyName("lastBoardRefreshTimeUtc")]
    public FYTimestamp LastBoardRefreshTimeUtc { get; set; }
}