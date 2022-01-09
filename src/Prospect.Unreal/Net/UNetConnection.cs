using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;

namespace Prospect.Unreal.Net;

public abstract class UNetConnection
{
    public const int ReliableBuffer = 256;
    public const int MaxPacketId = 16384;

    public abstract void InitBase(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0);
    public abstract void InitRemoteConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, IPEndPoint inRemoteAddr, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0);
    public abstract void InitLocalConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0);
    public abstract void LowLevelSend(byte[] data, int countBits, FOutPacketTraits traits);
    public abstract string LowLevelGetRemoteAddress(bool bAppendPort = false);
    public abstract string LowLevelDescribe();
    public abstract void Tick(float deltaSeconds);
    public abstract void CleanUp();
    public abstract void ReceivedRawPacket(byte[] data, int count);
    public abstract float GetTimeoutValue();

    public void InitSequence(int clientSequence, int serverSequence)
    {
        
    }
}