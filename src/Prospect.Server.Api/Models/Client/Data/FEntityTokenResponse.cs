using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FEntityTokenResponse
{
    /// <summary>
    ///     [optional] The entity id and type.
    /// </summary>
    [JsonPropertyName("Entity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FEntityKey? Entity { get; set; }
        
    /// <summary>
    ///     [optional] The token used to set X-EntityToken for all entity based API calls.
    /// </summary>
    [JsonPropertyName("EntityToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? EntityToken { get; set; }

    /// <summary>
    ///     [optional] The time the token will expire, if it is an expiring token, in UTC.
    /// </summary>
    [JsonPropertyName("TokenExpiration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? TokenExpiration { get; set; }
}