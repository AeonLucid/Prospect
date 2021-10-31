using System.Collections.Generic;
using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client
{
    public class FGetUserDataResult
    {
        /// <summary>
        ///     [optional] User specific data for this title.
        /// </summary>
        [JsonPropertyName("Data")]
        public Dictionary<string, FUserDataRecord> Data { get; set; }
        
        /// <summary>
        ///     [optional] Indicates the current version of the data that has been set. This is incremented with every set call for that type of
        ///     data (read-only, internal, etc). This version can be provided in Get calls to find updated data.
        /// </summary>
        [JsonPropertyName("DataVersion")]
        public uint DataVersion { get; set; }
    }
}