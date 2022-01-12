using System.Buffers;
using System.Collections;
using Serilog;

namespace Prospect.Unreal.Serialization;

public class FBitWriter : FArchive
{
    private static readonly ILogger Logger = Log.ForContext<FBitWriter>();
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create();

    private readonly bool _usesPool;
    
    public FBitWriter()
    {
        Num = 0;
        Max = 0;
        Data = Array.Empty<byte>();
        AllowResize = false;
        AllowOverflow = false;

        _usesPool = false;
        _arIsSaving = true;
        _arIsPersistent = true;
        _arIsNetArchive = true;
    }
    
    public FBitWriter(long inMaxBits, bool inAllowResize = false, bool usePool = true)
    {
        Num = 0;
        Max = inMaxBits;
        AllowResize = inAllowResize;

        var byteCount = (int)((inMaxBits + 7) >> 3);
        if (usePool)
        {
            Data = Pool.Rent(byteCount);
            _usesPool = true;
        }
        else
        {
            Data = new byte[byteCount];
            _usesPool = false;
        }

        _arIsSaving = true;
        _arIsPersistent = true;
        _arIsNetArchive = true;
    }

    public FBitWriter(FBitWriter writer) : base(writer)
    {
        Num = writer.Num;
        Max = writer.Max;
        AllowOverflow = writer.AllowOverflow;
        AllowResize = writer.AllowResize;

        if (writer.Data.Length > 0)
        {
            _usesPool = writer._usesPool;
            
            if (_usesPool)
            {
                Data = Pool.Rent(writer.Data.Length);
            }
            else
            {
                Data = new byte[writer.Data.Length];
            }
            
            Buffer.BlockCopy(writer.Data, 0, Data, 0, writer.Data.Length);
        }
        else
        {
            Data = Array.Empty<byte>();
        }
    }

    private byte[] Data { get; set; }
    internal long Num { get; set; }
    private long Max { get; set; }
    private bool AllowResize { get; set; }
    private bool AllowOverflow { get; }

    public byte[] GetData()
    {
        if (IsError())
        {
            Logger.Error("Retrieved data from a BitWriter that had an error");    
        }
        
        return Data;
    }

    public long GetNumBytes()
    {
        return (Num + 7) >> 3;
    }
    
    public long GetNumBits()
    {
        return Num;
    }

    public long GetMaxBits()
    {
        return Max;
    }

    public override unsafe void Serialize(void* src, long lengthBytes)
    {
        var lengthBits = lengthBytes * 8;
        if (AllowAppend(lengthBits))
        {
            fixed (byte* pBuffer = Data)
            {
                FBitUtil.AppBitsCpy(pBuffer, (int)Num, (byte*)src, 0, (int)lengthBits);
            }

            Num += lengthBits;
        }
        else
        {
            SetOverflowed(lengthBits);
        }
    }

    public override unsafe void SerializeBits(void* value, long lengthBits)
    {
        if (AllowAppend(lengthBits))
        {
            if (lengthBits == 1)
            {
                if ((((byte*)value)[0] & 0x01) != 0)
                {
                    Data[Num >> 3] |= FBitUtil.GShift[Num & 7];
                }

                Num++;
            }
            else
            {
                fixed (byte* pBuffer = Data)
                {
                    FBitUtil.AppBitsCpy(pBuffer, (int)Num, (byte*)value, 0, (int)lengthBits);
                    Num += lengthBits;
                }
            }
        }
        else
        {
            SetOverflowed(lengthBits);
        }
    }

    public void SerializeBits(BitArray bits, int lengthBits)
    {
        if (AllowAppend(lengthBits))
        {
            for (var i = 0; i < lengthBits; i++)
            {
                WriteBit((byte)(bits.Get(i) ? 1 : 0));
            }
        }
        else
        {
            SetOverflowed(lengthBits);
        }
    }

    public override unsafe void SerializeInt(uint* value, uint valueMax)
    {
        if (valueMax < 2)
        {
            throw new NotSupportedException();
        }

        var lengthBits = (int) Math.Ceiling(Math.Log2(valueMax));
        var writeValue = *value;
        if (writeValue >= valueMax)
        {
            Logger.Error("SerializeInt(): Value out of bounds (Value: {Value}, ValueMax: {ValueMax})", writeValue, valueMax);

            writeValue = valueMax - 1;
        }

        if (AllowAppend(lengthBits))
        {
            uint newValue = 0;
            var localNum = Num;

            for (uint mask = 1; (newValue + mask) < valueMax && (mask != 0); mask *= 2, localNum++)
            {
                if ((writeValue & mask) != 0)
                {
                    Data[localNum >> 3] += FBitUtil.GShift[localNum & 7];
                    newValue += mask;
                }
            }

            Num = localNum;
        }
        else
        {
            SetOverflowed(lengthBits);
        }
    }

    public override unsafe void SerializeIntPacked(uint* inValue)
    {
        uint value = *inValue;
        Span<uint> bytesAsWords = stackalloc uint[5];
        uint byteCount = 0;

        for (uint It = 0; (It == 0) | (value != 0); ++It, value = value >> 7)
        {
            if ((value & ~0x7F) != 0)
            {
                bytesAsWords[(int)byteCount++] = ((value & 0x7FU) << 1) | 1;
            }
            else
            {
                bytesAsWords[(int)byteCount++] = ((value & 0x7FU) << 1);
            }
        }

        var lengthBits = byteCount * 8;
        if (!AllowAppend(lengthBits))
        {
            SetOverflowed(lengthBits);
            return;
        }
        
        int BitCountUsedInByte = (int)(Num & 7);
        int BitCountLeftInByte = (int)(8 - (Num & 7));
        byte DestMaskByte0 = (byte)((1U << BitCountUsedInByte) - 1U);
        byte DestMaskByte1 = (byte)(0xFF ^ DestMaskByte0);
        bool bStraddlesTwoBytes = (BitCountUsedInByte != 0);

        fixed (byte* pData = Data)
        {
            var Dest = pData + (Num >> 3);
            
            Num += lengthBits;
            for (var ByteIt = 0; ByteIt != byteCount; ++ByteIt)
            {
                uint ByteAsWord = bytesAsWords[ByteIt];

                *Dest = (byte)((*Dest & DestMaskByte0) | (byte)(ByteAsWord << BitCountUsedInByte));
                ++Dest;
                if (bStraddlesTwoBytes)
                    *Dest = (byte)((*Dest & DestMaskByte1) | (byte)(ByteAsWord >> BitCountLeftInByte));
            }
        }
    }

    public void WriteIntWrapped(uint value, uint valueMax)
    {
        var lengthBits = (int) Math.Ceiling(Math.Log2(valueMax));

        if (AllowAppend(lengthBits))
        {
            uint newValue = 0;

            for (uint mask = 1; newValue + mask < valueMax && (mask != 0); mask *= 2, Num++)
            {
                if ((value & mask) != 0)
                {
                    Data[Num >> 3] += FBitUtil.GShift[Num & 7];
                    newValue += mask;
                }
            }
        }
        else
        {
            SetOverflowed(lengthBits);
        }
    }

    public void WriteBit(bool value)
    {
        if (value)
        {
            WriteBit(1);
        }
        else
        {
            WriteBit(0);
        }
    }

    public void WriteBit(byte value)
    {
        if (AllowAppend(1))
        {
            if (value != 0)
            {
                Data[Num >> 3] |= FBitUtil.GShift[Num & 7];
            }

            Num++;
        }
        else
        {
            SetOverflowed(1);
        }
    }

    protected void SetOverflowed(long lengthBits)
    {
        if (!AllowOverflow)
        {
            Logger.Error("FBitWriter overflowed (WriteLen: {Len}, Remaining: {Remaining}, Max: {Max})", lengthBits, (Max - Num), Max);
        }
        
        SetError();
    }

    public bool AllowAppend(long lengthBits)
    {
        if (Num + lengthBits > Max)
        {
            if (AllowResize)
            {
                // Resize our buffer. The common case for resizing bitwriters is hitting the max and continuing to add a lot of small segments of data
                // Though we could just allow the TArray buffer to handle the slack and resizing, we would still constantly hit the FBitWriter's max
                // and cause this block to be executed, as well as constantly zeroing out memory inside AddZeroes (though the memory would be allocated
                // in chunks).
                Max = Math.Max(Max << 1, Num + lengthBits);
                var byteMax = (Max + 7) >> 3;
                
                if (!_usesPool)
                {
                    var dataTemp = Data;
                    Array.Resize(ref dataTemp, (int) byteMax);
                    Data = dataTemp;
                }
                else
                {
                    throw new NotImplementedException();
                }
                
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void SetAllowResize(bool newResize)
    {
        AllowResize = true;
    }

    public override void Reset()
    {
        base.Reset();
        Num = 0;
        Array.Clear(Data);
        _arIsSaving = true;
        _arIsPersistent = true;
        _arIsNetArchive = true;
    }
    
    public override void Dispose()
    {
        if (_usesPool)
        {
            Pool.Return(Data, true);
        }
    }
}