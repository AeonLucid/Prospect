using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FUserDataRecord
{
    /// <summary>
    ///     Timestamp for when this data was last updated.
    /// </summary>
    [JsonPropertyName("LastUpdated")]
    public DateTime LastUpdated { get; set; }
        
    /// <summary>
    ///     [optional] Indicates whether this data can be read by all users (public) or only the user (private). This is used for GetUserData
    ///     requests being made by one player about another player.
    /// </summary>
    [JsonPropertyName("Permission")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserDataPermission? Permission { get; set; }
        
    /// <summary>
    ///     [optional] Data stored for the specified user data key.
    /// </summary>
    [JsonPropertyName("Value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Value { get; set; }
}