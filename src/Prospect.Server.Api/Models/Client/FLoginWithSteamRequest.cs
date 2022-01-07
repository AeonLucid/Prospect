using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FLoginWithSteamRequest
{
    /// <summary>
    ///     [optional] Automatically create a PlayFab account if one is not currently linked to this ID.
    /// </summary>
    [JsonPropertyName("CreateAccount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? CreateAccount { get; set; }
        
    /// <summary>
    ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
    /// </summary>
    [JsonPropertyName("CustomTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? CustomTags { get; set; }
        
    /// <summary>
    ///     [optional] Base64 encoded body that is encrypted with the Title's public RSA key (Enterprise Only).
    /// </summary>
    [JsonPropertyName("EncryptedRequest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? EncryptedRequest { get; set; }
        
    /// <summary>
    ///     [optional] Flags for which pieces of info to return for the user.
    /// </summary>
    [JsonPropertyName("InfoRequestParameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FGetPlayerCombinedInfoRequestParams? InfoRequestParameters { get; set; }
        
    /// <summary>
    ///     [optional] Player secret that is used to verify API request signatures (Enterprise Only).
    /// </summary>
    [JsonPropertyName("PlayerSecret")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? PlayerSecret { get; set; }
        
    /// <summary>
    ///     [optional] Authentication token for the user, returned as a byte array from Steam, and converted to a string (for example, the byte
    ///     0x08 should become "08").
    /// </summary>
    [JsonPropertyName("SteamTicket")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SteamTicket { get; set; }
}