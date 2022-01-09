using System.Net;

namespace Prospect.Unreal.Core;

public class FUrl
{
    public string Protocol { get; init; } = "unreal";
    public IPAddress Host { get; init; } = IPAddress.Any;
    public int Port { get; init; } = 7777;
    public string Map { get; init; } = "GearStart";
    public string RedirectUrl { get; init; } = string.Empty;
    public List<string> Options { get; init; } = new List<string>();
    public string Portal { get; init; } = string.Empty;
}