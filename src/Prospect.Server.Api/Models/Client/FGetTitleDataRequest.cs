using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client
{
    public class FGetTitleDataRequest
    {
        /// <summary>
        ///     [optional] Specific keys to search for in the title data (leave null to get all keys)
        /// </summary>
        [JsonPropertyName("Keys")]
        public List<string> Keys { get; set; }
        
        /// <summary>
        ///     [optional] Name of the override. This value is ignored when used by the game client; otherwise, the overrides are applied
        ///     automatically to the title data.
        /// </summary>
        [JsonPropertyName("OverrideLabel")]
        public string OverrideLabel { get; set; }
    }
}