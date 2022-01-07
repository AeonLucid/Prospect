using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client;

public class FUpdateUserTitleDisplayNameRequest
{
    /// <summary>
    ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
    /// </summary>
    [JsonPropertyName("CustomTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string> CustomTags { get; set; }
        
    /// <summary>
    ///     New title display name for the user - must be between 3 and 25 characters.
    /// </summary>
    [JsonPropertyName("DisplayName")]
    public string DisplayName { get; set; }
}