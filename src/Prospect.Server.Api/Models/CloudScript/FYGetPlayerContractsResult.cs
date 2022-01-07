using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.CloudScript.Data;

namespace Prospect.Server.Api.Models.CloudScript;

public class FYGetPlayerContractsResult
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
        
    [JsonPropertyName("activeContracts")]
    public List<FYActiveContractPlayerData> ActiveContracts { get; set; } = null!;

    [JsonPropertyName("factionsContracts")]
    public FYFactionsContractsData FactionsContracts { get; set; } = null!;

    [JsonPropertyName("refreshHours24UtcFromBackend")]
    public int RefreshHours24UtcFromBackend { get; set; }
}