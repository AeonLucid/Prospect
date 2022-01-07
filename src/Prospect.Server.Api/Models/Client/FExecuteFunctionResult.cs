using System.Text.Json.Serialization;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Models.Client;

public class FExecuteFunctionResult
{
    /// <summary>
    ///     [optional] Error from the CloudScript Azure Function.
    /// </summary>
    [JsonPropertyName("Error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FFunctionExecutionError? Error { get; set; }

    /// <summary>
    ///     The amount of time the function took to execute
    /// </summary>
    [JsonPropertyName("ExecutionTimeMilliseconds")]
    public int ExecutionTimeMilliseconds { get; set; }

    /// <summary>
    ///     [optional] The name of the function that executed
    /// </summary>
    [JsonPropertyName("FunctionName")]
    public string? FunctionName { get; set; }
        
    /// <summary>
    ///     [optional] The object returned from the function, if any
    /// </summary>
    [JsonPropertyName("FunctionResult")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object? FunctionResult { get; set; }
        
    /// <summary>
    ///     [optional] Flag indicating if the FunctionResult was too large and was subsequently dropped from this event.
    /// </summary>
    [JsonPropertyName("FunctionResultTooLarge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? FunctionResultTooLarge { get; set; }
}