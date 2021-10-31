using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client
{
    public class FGetUserInventoryRequest
    {
        /// <summary>
        ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
        /// </summary>
        [JsonPropertyName("CustomTags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<string, string> CustomTags { get; set; }
    }
}