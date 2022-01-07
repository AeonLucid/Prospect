using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FScriptExecutionError
{
    /// <summary>
    ///     [optional] Error code, such as CloudScriptNotFound, JavascriptException, CloudScriptFunctionArgumentSizeExceeded,
    ///     CloudScriptAPIRequestCountExceeded, CloudScriptAPIRequestError, or CloudScriptHTTPRequestError
    /// </summary>
    [JsonPropertyName("Error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Error { get; set; }
        
    /// <summary>
    ///     [optional] Details about the error
    /// </summary>
    [JsonPropertyName("Message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Message { get; set; }
        
    /// <summary>
    ///     [optional] Point during the execution of the script at which the error occurred, if any
    /// </summary>
    [JsonPropertyName("StackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? StackTrace { get; set; }
}