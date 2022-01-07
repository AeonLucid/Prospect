using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FServerLoginResult
{        
    /// <summary>
    ///     [optional] If LoginTitlePlayerAccountEntity flag is set on the login request the title_player_account will also be logged in and
    ///     returned.
    /// </summary>
    [JsonPropertyName("EntityToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FEntityTokenResponse EntityToken { get; set; }
        
    /// <summary>
    ///     [optional] Results for requested info.
    /// </summary>
    [JsonPropertyName("InfoResultPayload")]
    public FGetPlayerCombinedInfoResultPayload InfoResultPayload { get; set; }
        
    /// <summary>
    ///     [optional] The time of this user's previous login. If there was no previous login, then it's DateTime.MinValue
    /// </summary>
    [JsonPropertyName("LastLoginTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? LastLoginTime { get; set; }
        
    /// <summary>
    ///     True if the account was newly created on this login.
    /// </summary>
    [JsonPropertyName("NewlyCreated")]
    public bool NewlyCreated { get; set; }
        
    /// <summary>
    ///     [optional] Player's unique PlayFabId.
    /// </summary>
    [JsonPropertyName("PlayFabId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string PlayFabId { get; set; }
        
    /// <summary>
    ///     [optional] Unique token authorizing the user and game at the server level, for the current session.
    /// </summary>
    [JsonPropertyName("SessionTicket")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string SessionTicket { get; set; }
        
    /// <summary>
    ///     [optional] Settings specific to this user.
    /// </summary>
    [JsonPropertyName("SettingsForUser")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FUserSettings SettingsForUser { get; set; }

    /// <summary>
    ///     [optional] The experimentation treatments for this user at the time of login.
    /// </summary>
    [JsonPropertyName("TreatmentAssignment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FTreatmentAssignment TreatmentAssignment { get; set; }
}