using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data
{
    public class FPlayerProfileModel
    {
        /// <summary>
        ///     [optional] Player display name
        /// </summary>
        [JsonPropertyName("DisplayName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string DisplayName { get; set; }
        
        /// <summary>
        ///     [optional] PlayFab player account unique identifier
        /// </summary>
        [JsonPropertyName("PlayerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string PlayerId { get; set; }
        
        /// <summary>
        ///     [optional] Publisher this player belongs to
        /// </summary>
        [JsonPropertyName("PublisherId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string PublisherId { get; set; }

        /// <summary>
        ///     [optional] Title ID this player profile applies to
        /// </summary>
        [JsonPropertyName("TitleId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TitleId { get; set; }
    }
}