using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data
{
    public class FGetPlayerCombinedInfoResultPayload
    {
        // TSharedPtr<FUserAccountInfo> AccountInfo;
        
        /// <summary>
        ///     [optional] Inventories for each character for the user.
        /// </summary>
        [JsonPropertyName("CharacterInventories")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<object> CharacterInventories { get; set; }
        
        // TArray<FCharacterResult> CharacterList;

        /// <summary>
        ///     [optional] The profile of the players. This profile is not guaranteed to be up-to-date. For a new player, this profile will not
        ///     exist.
        /// </summary>
        [JsonPropertyName("PlayerProfile")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public FPlayerProfileModel PlayerProfile { get; set; }
        
        // TArray<FStatisticValue> PlayerStatistics;
        
        // TMap<FString, FString> TitleData;
        
        // TMap<FString, FUserDataRecord> UserData;
        
        /// <summary>
        ///     The version of the UserData that was returned.
        /// </summary>
        [JsonPropertyName("UserDataVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint? UserDataVersion { get; set; }
        
        /// <summary>
        ///     [optional] Array of inventory items in the user's current inventory.
        /// </summary>
        [JsonPropertyName("UserInventory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<object> UserInventory { get; set; }

        // TMap<FString, FUserDataRecord> UserReadOnlyData;
        
        /// <summary>
        ///     The version of the Read-Only UserData that was returned.
        /// </summary>
        [JsonPropertyName("UserReadOnlyDataVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint? UserReadOnlyDataVersion { get; set; }
        
        // TMap<FString, int32> UserVirtualCurrency;
        
        // TMap<FString, FVirtualCurrencyRechargeTime> UserVirtualCurrencyRechargeTimes;
    }
}