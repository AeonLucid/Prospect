using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data;

public class FYFactionContractData
{
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; }

    [JsonPropertyName("contractIsLockedDueToLowFactionReputation")]
    public bool ContractIsLockedDueToLowFactionReputation { get; set; }
}