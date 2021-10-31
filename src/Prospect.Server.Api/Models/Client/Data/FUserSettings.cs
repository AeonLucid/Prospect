using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data
{
    public class FUserSettings
    {
        /// <summary>
        ///     Boolean for whether this player is eligible for gathering device info.
        /// </summary>
        [JsonPropertyName("GatherDeviceInfo")]
        public bool GatherDeviceInfo { get; set; }
        
        /// <summary>
        ///     Boolean for whether this player should report OnFocus play-time tracking.
        /// </summary>
        [JsonPropertyName("GatherFocusInfo")]
        public bool GatherFocusInfo { get; set; }
        
        /// <summary>
        ///     Boolean for whether this player is eligible for ad tracking.
        /// </summary>
        [JsonPropertyName("NeedsAttribution")]
        public bool NeedsAttribution { get; set; }
    }
}