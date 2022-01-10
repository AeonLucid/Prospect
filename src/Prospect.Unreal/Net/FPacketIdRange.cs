namespace Prospect.Unreal.Net;

public class FPacketIdRange
{
    public int First = -1;
    public int Last = -1;

    public bool InRange(int packetId)
    {
        return First <= packetId && packetId <= Last;
    }
}