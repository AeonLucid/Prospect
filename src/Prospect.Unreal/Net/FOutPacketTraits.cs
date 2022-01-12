namespace Prospect.Unreal.Net;

public class FOutPacketTraits
{
    public bool AllowCompression { get; set; } = true;
    public uint NumAckBits { get; set; }
    public uint NumBunchBits { get; set; }
    public bool IsKeepAlive { get; set; }
    public bool IsCompressed { get; set; }
}