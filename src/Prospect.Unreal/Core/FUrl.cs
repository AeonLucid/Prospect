using System.Net;

namespace Prospect.Unreal.Core;

public class FUrl
{
    public string Protocol { get; set; } = "unreal";
    public IPAddress Host { get; set; } = IPAddress.Any;
    public int Port { get; set; } = 7777;
    public string Map { get; set; } = "GearStart";
    public string RedirectUrl { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new List<string>();
    public string Portal { get; set; } = string.Empty;
}