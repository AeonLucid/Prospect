using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client
{
    public class FAddGenericIDRequest
    {
        [JsonPropertyName("GenericId")]
        public FGenericServiceId GenericId { get; set; }
    }
}