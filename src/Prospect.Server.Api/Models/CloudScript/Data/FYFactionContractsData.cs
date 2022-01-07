using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data;

public class FYFactionContractsData
{
    [JsonPropertyName("factionId")]
    public string FactionId { get; set; } = null!;

    [JsonPropertyName("contracts")]
    public List<FYFactionContractData> Contracts { get; set; } = null!;
}