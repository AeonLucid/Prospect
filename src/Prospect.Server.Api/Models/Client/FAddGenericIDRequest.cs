using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FAddGenericIDRequest
{
    /// <summary>
    ///     Generic service identifier to add to the player account.
    /// </summary>
    [JsonPropertyName("GenericId")]
    public FGenericServiceId GenericId { get; set; } = null!;
}