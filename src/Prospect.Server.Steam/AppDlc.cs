namespace Prospect.Server.Steam;

public class AppDlc
{
    public uint AppId { get; set; }
        
    public List<uint> Licenses { get; set; }
}