using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data;

public class FItemInstance
{
    /// <summary>
    ///     [optional] Game specific comment associated with this instance when it was added to the user inventory.
    /// </summary>
    [JsonPropertyName("Annotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Annotation { get; set; }
        
    /// <summary>
    ///     [optional] Array of unique items that were awarded when this catalog item was purchased.
    /// </summary>
    [JsonPropertyName("BundleContents")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? BundleContents { get; set; }
        
    /// <summary>
    ///     [optional] Unique identifier for the parent inventory item, as defined in the catalog, for object which were added from a bundle or
    ///     container.
    /// </summary>
    [JsonPropertyName("BundleParent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? BundleParent { get; set; }
        
    /// <summary>
    ///     [optional] Catalog version for the inventory item, when this instance was created.
    /// </summary>
    [JsonPropertyName("CatalogVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? CatalogVersion { get; set; }
        
    /// <summary>
    ///     [optional] A set of custom key-value pairs on the instance of the inventory item, which is not to be confused with the catalog
    ///     item's custom data.
    /// </summary>
    [JsonPropertyName("CustomData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? CustomData { get; set; }
        
    /// <summary>
    ///     [optional] CatalogItem.DisplayName at the time this item was purchased.
    /// </summary>
    [JsonPropertyName("DisplayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? DisplayName { get; set; }
        
    /// <summary>
    ///     [optional] Timestamp for when this instance will expire.
    /// </summary>
    [JsonPropertyName("Expiration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? Expiration { get; set; }
        
    /// <summary>
    ///     [optional] Class name for the inventory item, as defined in the catalog.
    /// </summary>
    [JsonPropertyName("ItemClass")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ItemClass { get; set; }
        
    /// <summary>
    ///     [optional] Unique identifier for the inventory item, as defined in the catalog.
    /// </summary>
    [JsonPropertyName("ItemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ItemId { get; set; }
        
    /// <summary>
    ///     [optional] Unique item identifier for this specific instance of the item.
    /// </summary>
    [JsonPropertyName("ItemInstanceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ItemInstanceId { get; set; }
        
    /// <summary>
    ///     [optional] Timestamp for when this instance was purchased.
    /// </summary>
    [JsonPropertyName("PurchaseDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? PurchaseDate { get; set; }
        
    /// <summary>
    ///     [optional] Total number of remaining uses, if this is a consumable item.
    /// </summary>
    [JsonPropertyName("RemainingUses")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? RemainingUses { get; set; }
        
    /// <summary>
    ///     [optional] Currency type for the cost of the catalog item. Not available when granting items.
    /// </summary>
    [JsonPropertyName("UnitCurrency")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? UnitCurrency { get; set; }
        
    /// <summary>
    ///     Cost of the catalog item in the given currency. Not available when granting items.
    /// </summary>
    [JsonPropertyName("UnitPrice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint? UnitPrice { get; set; }
        
    /// <summary>
    ///     [optional] The number of uses that were added or removed to this item in this call.
    /// </summary>
    [JsonPropertyName("UsesIncrementedBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? UsesIncrementedBy { get; set; }
}