using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;

namespace Prospect.Unreal.Net;

public class UIpConnection : UNetConnection
{
    public override void InitBase(UNetDriver inDriver, UdpClient inSocket, FURL inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotImplementedException();
    }

    public override void InitRemoteConnection(UNetDriver inDriver, UdpClient inSocket, FURL inURL, IPEndPoint inRemoteAddr, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotImplementedException();
    }

    public override void InitLocalConnection(UNetDriver inDriver, UdpClient inSocket, FURL inURL, EConnectionState inState,
        int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotImplementedException();
    }

    public override void LowLevelSend(byte[] data, int countBits, FOutPacketTraits traits)
    {
        throw new NotImplementedException();
    }

    public override string LowLevelGetRemoteAddress(bool bAppendPort = false)
    {
        throw new NotImplementedException();
    }

    public override string LowLevelDescribe()
    {
        throw new NotImplementedException();
    }

    public override void Tick(float deltaSeconds)
    {
        throw new NotImplementedException();
    }

    public override void CleanUp()
    {
        throw new NotImplementedException();
    }

    public override void ReceivedRawPacket(byte[] data, int count)
    {
        throw new NotImplementedException();
    }

    public override float GetTimeoutValue()
    {
        throw new NotImplementedException();
    }
}