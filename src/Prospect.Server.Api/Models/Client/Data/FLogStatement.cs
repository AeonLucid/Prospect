using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FLogStatement
{
    /// <summary>
    ///     [optional] Optional object accompanying the message as contextual information
    /// </summary>
    [JsonPropertyName("Data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object? Data { get; set; }
        
    /// <summary>
    ///     [optional] 'Debug', 'Info', or 'Error'
    /// </summary>
    [JsonPropertyName("Level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Level { get; set; }
        
    /// <summary>
    ///     [optional] undefined
    /// </summary>
    [JsonPropertyName("Message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Message { get; set; }
}