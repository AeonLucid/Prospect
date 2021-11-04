using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.CloudScript.Data
{
    public class FYTimestamp
    {
        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }
    }
}