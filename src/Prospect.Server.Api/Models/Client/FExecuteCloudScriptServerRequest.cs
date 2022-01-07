using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FExecuteCloudScriptServerRequest
{
    /// <summary>
    ///     [optional] The optional custom tags associated with the request (e.g. build number, external trace identifiers, etc.).
    /// </summary>
    [JsonPropertyName("CustomTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? CustomTags { get; set; }

    /// <summary>
    ///     The name of the CloudScript function to execute
    /// </summary>
    [JsonPropertyName("FunctionName")]
    public string FunctionName { get; set; } = null!;

    /// <summary>
    ///     [optional] Object that is passed in to the function as the first argument
    /// </summary>
    [JsonPropertyName("FunctionParameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? FunctionParameter { get; set; }

    /// <summary>
    ///     [optional] Generate a 'player_executed_cloudscript' PlayStream event containing the results of the function execution and other
    ///     contextual information. This event will show up in the PlayStream debugger console for the player in Game Manager.
    /// </summary>
    [JsonPropertyName("GeneratePlayStreamEvent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? GeneratePlayStreamEvent { get; set; }

    /// <summary>
    ///     The unique user identifier for the player on whose behalf the script is being run
    /// </summary>
    [JsonPropertyName("PlayFabId")]
    public string PlayFabId { get; set; } = null!;
        
    /// <summary>
    ///     [optional] Option for which revision of the CloudScript to execute. 'Latest' executes the most recently created revision, 'Live'
    ///     executes the current live, published revision, and 'Specific' executes the specified revision. The default value is
    ///     'Specific', if the SpeificRevision parameter is specified, otherwise it is 'Live'.
    /// </summary>
    [JsonPropertyName("RevisionSelection")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CloudScriptRevisionOption? RevisionSelection { get; set; }
        
    /// <summary>
    ///     [optional] The specivic revision to execute, when RevisionSelection is set to 'Specific'
    /// </summary>
    [JsonPropertyName("SpecificRevision")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? SpecificRevision { get; set; }
}