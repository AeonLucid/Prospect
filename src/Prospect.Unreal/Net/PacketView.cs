using System.Net;

namespace Prospect.Unreal.Net;

public class FInPacketTraits
{
    public bool ConnectionlessPacket { get; set; }
    public bool FromRecentlyDisconnected { get; set; }
}

public class FPacketDataView
{
    private readonly byte[] _data;
    private readonly int _countBits;

    public FPacketDataView(byte[] data, int length)
    {
        _data = data;
        _countBits = length * 8;
    }

    public byte[] GetData()
    {
        return _data;
    }
    
    public int NumBits()
    {
        return _countBits;
    }

    public int NumBytes()
    {
        return _data.Length;
    }
}

public class FReceivedPacketView
{
    public FReceivedPacketView(FPacketDataView dataView, IPEndPoint address, FInPacketTraits traits)
    {
        DataView = dataView;
        Address = address;
        Traits = traits;
    }

    public FPacketDataView DataView { get; set; }
    public IPEndPoint Address { get; set; }
    public FInPacketTraits Traits { get; set; }
}