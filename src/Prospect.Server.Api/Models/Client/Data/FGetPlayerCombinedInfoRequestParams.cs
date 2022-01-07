using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FGetPlayerCombinedInfoRequestParams
{
    /// <summary>
    ///     Whether to get character inventories. Defaults to false.
    /// </summary>
    [JsonPropertyName("GetCharacterInventories")]
    public bool GetCharacterInventories { get; set; }

    /// <summary>
    ///     Whether to get the list of characters. Defaults to false.
    /// </summary>
    [JsonPropertyName("GetCharacterList")]
    public bool GetCharacterList { get; set; }

    /// <summary>
    ///     Whether to get player profile. Defaults to false. Has no effect for a new player.
    /// </summary>
    [JsonPropertyName("GetPlayerProfile")]
    public bool GetPlayerProfile { get; set; }

    /// <summary>
    ///     Whether to get player statistics. Defaults to false.
    /// </summary>
    [JsonPropertyName("GetPlayerStatistics")]
    public bool GetPlayerStatistics { get; set; }

    /// <summary>
    ///     Whether to get title data. Defaults to false.
    /// </summary>
    [JsonPropertyName("GetTitleData")]
    public bool GetTitleData { get; set; }

    /// <summary>
    ///     Whether to get the player's account Info. Defaults to false
    /// </summary>
    [JsonPropertyName("GetUserAccountInfo")]
    public bool GetUserAccountInfo { get; set; }

    /// <summary>
    ///     Whether to get the player's custom data. Defaults to false
    /// </summary>
    [JsonPropertyName("GetUserData")]
    public bool GetUserData { get; set; }

    /// <summary>
    ///     Whether to get the player's inventory. Defaults to false
    /// </summary>
    [JsonPropertyName("GetUserInventory")]
    public bool GetUserInventory { get; set; }

    /// <summary>
    ///     Whether to get the player's read only data. Defaults to false
    /// </summary>
    [JsonPropertyName("GetUserReadOnlyData")]
    public bool GetUserReadOnlyData { get; set; }

    /// <summary>
    ///     Whether to get the player's virtual currency balances. Defaults to false
    /// </summary>
    [JsonPropertyName("GetUserVirtualCurrency")]
    public bool GetUserVirtualCurrency { get; set; }
}