using System;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Models.Client.Data
{
    public class FVirtualCurrencyRechargeTime
    {
        /// <summary>
        ///     Maximum value to which the regenerating currency will automatically increment. Note that it can exceed this value
        ///     through use of the AddUserVirtualCurrency API call. However, it will not regenerate automatically until it has fallen
        ///     below this value.
        /// </summary>
        [JsonPropertyName("RechargeMax")]
        public int RechargeMax { get; set; }
        
        /// <summary>
        ///     Server timestamp in UTC indicating the next time the virtual currency will be incremented.
        /// </summary>
        [JsonPropertyName("RechargeTime")]
        public DateTime RechargeTime { get; set; }
        
        /// <summary>
        ///     Time remaining (in seconds) before the next recharge increment of the virtual currency.
        /// </summary>
        [JsonPropertyName("SecondsToRecharge")]
        public int SecondsToRecharge { get; set; }
    }
}