using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models;

public class FYMaintenanceModeState
{
    [JsonPropertyName("bypassMaintenanceMode")]
    public bool BypassMaintenanceMode { get; set; }
}