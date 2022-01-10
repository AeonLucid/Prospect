using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;

namespace Prospect.Unreal.Net;

public class UIpConnection : UNetConnection
{
    public const int IpHeaderSize = 20;
    public const int UdpHeaderSize = IpHeaderSize + 8;

    /// <summary>
    ///     Cached time of the first send socket error that will be used to compute disconnect delay.
    /// </summary>
    private double _socketErrorSendDelayStartTime;
    
    /// <summary>
    ///     Cached time of the first recv socket error that will be used to compute disconnect delay.
    /// </summary>
    private double _socketErrorRecvDelayStartTime;
    
    public UdpClient? Socket { get; private set; }
    
    public override void InitBase(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        base.InitBase(inDriver, inSocket, inURL, inState, 
            (inMaxPacket == 0 || inMaxPacket > MaxPacketSize) ? MaxPacketSize : inMaxPacket, 
            inPacketOverhead == 0 ? UdpHeaderSize : inPacketOverhead);
        
        Socket = inSocket;
    }

    public override void InitRemoteConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, IPEndPoint inRemoteAddr, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        InitBase(inDriver, inSocket, inURL, inState, 
            (inMaxPacket == 0 || inMaxPacket > MaxPacketSize) ? MaxPacketSize : inMaxPacket, 
            inPacketOverhead == 0 ? UdpHeaderSize : inPacketOverhead);

        RemoteAddr = inRemoteAddr;
        Url.Host = RemoteAddr.Address;

        SetClientLoginState(EClientLoginState.LoggingIn);
        SetExpectedClientLoginMsgType(0); // TODO: NMT_HELLO
    }

    public override void InitLocalConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotSupportedException();
    }

    public override void LowLevelSend(byte[] data, int countBits, FOutPacketTraits traits)
    {
        throw new NotImplementedException();
    }

    public override string LowLevelGetRemoteAddress(bool bAppendPort = false)
    {
        if (RemoteAddr != null)
        {
            // TODO: Remove port
            return RemoteAddr.ToString();
        }

        return string.Empty;
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

    public override void ReceivedRawPacket(FReceivedPacketView packetView)
    {
        _socketErrorRecvDelayStartTime = 0;
        
        base.ReceivedRawPacket(packetView);
    }

    public override float GetTimeoutValue()
    {
        throw new NotImplementedException();
    }
}