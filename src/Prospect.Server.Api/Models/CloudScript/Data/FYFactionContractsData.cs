using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data
{
    public class FYFactionContractsData
    {
        [JsonPropertyName("factionId")]
        public string FactionId { get; set; }

        [JsonPropertyName("contracts")]
        public List<FYFactionContractData> Contracts { get; set; }
    }
}