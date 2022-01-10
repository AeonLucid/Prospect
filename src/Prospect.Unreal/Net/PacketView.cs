using System.Net;
using Prospect.Unreal.Core;

namespace Prospect.Unreal.Net;

public class FInPacketTraits
{
    public bool ConnectionlessPacket { get; set; }
    public bool FromRecentlyDisconnected { get; set; }
}

public readonly struct FPacketDataView
{
    private readonly byte[] _data;
    private readonly int _count;
    private readonly int _countBits;

    public FPacketDataView(byte[] data, int length, ECountUnits unit)
    {
        _data = data;

        if (unit == ECountUnits.Bits)
        {
            _count = FMath.DivideAndRoundUp(length, 8);
            _countBits = length;
        }
        else /* if (unit == ECountUnits.Bytes) */
        {
            _count = length;
            _countBits = length * 8;
        }
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
        return _count;
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