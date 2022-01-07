using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FEntityKey
{
    /// <summary>
    ///     Unique ID of the entity.
    /// </summary>
    [JsonPropertyName("Id")]
    public string Id { get; set; } = null!;

    /// <summary>
    ///     [optional] Entity type. See https://docs.microsoft.com/gaming/playfab/features/data/entities/available-built-in-entity-types
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    // Not in PlayFab SDK
    [JsonPropertyName("TypeString")] 
    public string? TypeString { get; set; } = null!;
}