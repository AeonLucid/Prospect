﻿using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Services.CloudScript.Models.Data;

public class FYFactionsContractsData
{
    [JsonPropertyName("boards")]
    public List<FYFactionContractsData> Boards { get; set; } = null!;

    [JsonPropertyName("lastBoardRefreshTimeUtc")]
    public FYTimestamp LastBoardRefreshTimeUtc { get; set; } = null!;
}