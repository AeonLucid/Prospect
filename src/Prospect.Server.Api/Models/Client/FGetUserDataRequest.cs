using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client;

public class FGetUserDataRequest
{
    /// <summary>
    ///     [optional] The version that currently exists according to the caller. The call will return the data for all of the keys if the
    ///     version in the system is greater than this.
    /// </summary>
    [JsonPropertyName("IfChangedFromDataVersion")]
    public List<uint>? IfChangedFromDataVersion { get; set; }
        
    /// <summary>
    ///     [optional] List of unique keys to load from.
    /// </summary>
    [JsonPropertyName("Keys")]
    public List<string>? Keys { get; set; }
        
    /// <summary>
    ///     [optional] Unique PlayFab identifier of the user to load data for. Optional, defaults to yourself if not set. When specified to a
    ///     PlayFab id of another player, then this will only return public keys for that account.
    /// </summary>
    [JsonPropertyName("PlayFabId")]
    public string? PlayFabId { get; set; }
}