using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data
{
    public class FYActiveContractPlayerData
    {
        [JsonPropertyName("contractId")]
        public string ContractId { get; set; }

        [JsonPropertyName("progress")]
        public List<int> Progress { get; set; }
    }
}