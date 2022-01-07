using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data;

public class FYActiveContractPlayerData
{
    [JsonPropertyName("contractId")] 
    public string ContractId { get; set; } = null!;

    [JsonPropertyName("progress")]
    public List<int> Progress { get; set; } = null!;
}