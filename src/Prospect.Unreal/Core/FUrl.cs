using System.Net;
using System.Text;

namespace Prospect.Unreal.Core;

public class FUrl
{
    private const string DefaultProtocol = "unreal";
    private static readonly IPAddress DefaultHost = IPAddress.Any;
    private const int DefaultPort = 7777;
    
    public string Protocol { get; set; } = DefaultProtocol;
    public IPAddress Host { get; set; } = DefaultHost;
    public int Port { get; set; } = DefaultPort;
    public string Map { get; set; } = "GearStart"; // TODO: UGameMapsSettings::GetGameDefaultMap()
    public string RedirectUrl { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new List<string>();
    public string Portal { get; set; } = string.Empty;
    public bool Valid { get; set; } = true;

    public string ToString(bool fullyQualified)
    {
        var result = new StringBuilder();
        
        // Emit protocol.
        if ((Protocol != DefaultProtocol) || fullyQualified)
        {
            result.Append(Protocol);
            result.Append(':');

            if (!Equals(Host, DefaultHost))
            {
                result.Append("//");
            }
        }
        
        // Emit host and port
        if (!Equals(Host, DefaultHost) || (Port != DefaultPort))
        {
            result.Append(Port);

            if (!Map.StartsWith("/") && !Map.StartsWith("\\"))
            {
                result.Append('/');
            }
        }

        // Emit map.
        if (!string.IsNullOrEmpty(Map))
        {
            result.Append(Map);
        }

        // Emit options.
        foreach (var option in Options)
        {
            result.Append('?');
            result.Append(option);
        }
        
        // Emit portal.
        if (!string.IsNullOrEmpty(Portal))
        {
            result.Append('#');
            result.Append(Portal);
        }

        return result.ToString();
    }
    
    public override string ToString()
    {
        return ToString(false);
    }
}