using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FUpdateUserDataRequest
{
    /// <summary>
    ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
    /// </summary>
    [JsonPropertyName("CustomTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string> CustomTags { get; set; }
        
    /// <summary>
    ///     [optional] Key-value pairs to be written to the custom data. Note that keys are trimmed of whitespace, are limited in size, and may
    ///     not begin with a '!' character or be null.
    /// </summary>
    public Dictionary<string, string> Data { get; set; }
        
    /// <summary>
    ///     [optional] Optional list of Data-keys to remove from UserData. Some SDKs cannot insert null-values into Data due to language
    ///     constraints. Use this to delete the keys directly.
    /// </summary>
    public List<string> KeysToRemove { get; set; }
        
    /// <summary>
    ///     [optional] Permission to be applied to all user data keys written in this request. Defaults to "private" if not set.
    /// </summary>
    public UserDataPermission? Permission { get; set; }
        
    /// <summary>
    ///     Unique PlayFab assigned ID of the user on whom the operation will be performed.
    /// </summary>
    public string PlayFabId { get; set; }
}