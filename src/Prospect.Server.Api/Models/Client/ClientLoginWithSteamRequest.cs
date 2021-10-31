using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client
{
    public class ClientLoginWithSteamRequest
    {
        [JsonPropertyName("CreateAccount")]
        public bool CreateAccount { get; set; }
        
        [JsonPropertyName("CustomTags")]
        public JsonDocument CustomTags { get; set; }
        
        [JsonPropertyName("EncryptedRequest")]
        public string EncryptedRequest { get; set; }
        
        [JsonPropertyName("InfoRequestParameters")]
        public InfoParameters InfoRequestParameters { get; set; }
        
        [JsonPropertyName("PlayerSecret")]
        public string PlayerSecret { get; set; }
        
        [JsonPropertyName("SteamTicket")]
        public string SteamTicket { get; set; }
    }

    public class InfoParameters
    {
        [JsonPropertyName("GetCharacterInventories")]
        public bool GetCharacterInventories { get; set; }

        [JsonPropertyName("GetCharacterList")]
        public bool GetCharacterList { get; set; }

        [JsonPropertyName("GetPlayerProfile")]
        public bool GetPlayerProfile { get; set; }

        [JsonPropertyName("GetPlayerStatistics")]
        public bool GetPlayerStatistics { get; set; }

        [JsonPropertyName("GetTitleData")]
        public bool GetTitleData { get; set; }

        [JsonPropertyName("GetUserAccountInfo")]
        public bool GetUserAccountInfo { get; set; }

        [JsonPropertyName("GetUserData")]
        public bool GetUserData { get; set; }

        [JsonPropertyName("GetUserInventory")]
        public bool GetUserInventory { get; set; }

        [JsonPropertyName("GetUserReadOnlyData")]
        public bool GetUserReadOnlyData { get; set; }

        [JsonPropertyName("GetUserVirtualCurrency")]
        public bool GetUserVirtualCurrency { get; set; }
    }
}