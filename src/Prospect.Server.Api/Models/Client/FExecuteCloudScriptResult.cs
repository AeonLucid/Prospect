using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FExecuteCloudScriptResult
{
    /// <summary>
    ///     Number of PlayFab API requests issued by the CloudScript function
    /// </summary>
    [JsonPropertyName("APIRequestsIssued")]
    public int APIRequestsIssued { get; set; }
        
    /// <summary>
    ///     [optional] Information about the error, if any, that occurred during execution
    /// </summary>
    [JsonPropertyName("Error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FScriptExecutionError Error { get; set; }
        
    [JsonPropertyName("ExecutionTimeSeconds")]
    public double? ExecutionTimeSeconds { get; set; }
        
    /// <summary>
    ///     [optional] The name of the function that executed
    /// </summary>
    [JsonPropertyName("FunctionName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string FunctionName { get; set; }
        
    /// <summary>
    ///     [optional] The object returned from the CloudScript function, if any
    /// </summary>
    [JsonPropertyName("FunctionResult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object FunctionResult { get; set; }
        
    /// <summary>
    ///     [optional] Flag indicating if the FunctionResult was too large and was subsequently dropped from this event. This only occurs if
    ///     the total event size is larger than 350KB.
    /// </summary>
    [JsonPropertyName("FunctionResultTooLarge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? FunctionResultTooLarge { get; set; }
        
    /// <summary>
    ///     Number of external HTTP requests issued by the CloudScript function
    /// </summary>
    [JsonPropertyName("HttpRequestsIssued")]
    public int HttpRequestsIssued { get; set; }
        
    /// <summary>
    ///     [optional] Entries logged during the function execution. These include both entries logged in the function code using log.info()
    ///     and log.error() and error entries for API and HTTP request failures.
    /// </summary>
    [JsonPropertyName("Logs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<FLogStatement> Logs { get; set; }
        
    /// <summary>
    ///     [optional] Flag indicating if the logs were too large and were subsequently dropped from this event. This only occurs if the total
    ///     event size is larger than 350KB after the FunctionResult was removed.
    /// </summary>
    [JsonPropertyName("LogsTooLarge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? LogsTooLarge { get; set; }
        
    [JsonPropertyName("MemoryConsumedBytes")]
    public uint MemoryConsumedBytes { get; set; }
        
    /// <summary>
    ///     Processor time consumed while executing the function. This does not include time spent waiting on API calls or HTTP
    ///     requests.
    /// </summary>
    [JsonPropertyName("ProcessorTimeSeconds")]
    public double ProcessorTimeSeconds { get; set; }
        
    /// <summary>
    ///     The revision of the CloudScript that executed
    /// </summary>
    [JsonPropertyName("Revision")]
    public int Revision { get; set; }
}