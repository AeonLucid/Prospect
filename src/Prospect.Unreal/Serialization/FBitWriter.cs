using System.Buffers;
using System.Collections;
using Prospect.Unreal.Core;
using Serilog;

namespace Prospect.Unreal.Serialization;

public class FBitWriter : FArchive
{
    private static readonly ILogger Logger = Log.ForContext<FBitWriter>();
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create();
    
    public FBitWriter(int inMaxBits, bool inAllowResize = false)
    {
        Num = 0;
        Max = inMaxBits;
        AllowResize = inAllowResize;
        Buffer = Pool.Rent((inMaxBits + 7) >> 3);

        _arIsSaving = true;
        _arIsPersistent = true;
        _arIsNetArchive = true;
    }
    
    private byte[] Buffer { get; set; }
    private long Num { get; set; }
    private long Max { get; set; }
    private bool AllowResize { get; }
    private bool AllowOverflow { get; }
    
    public override void Dispose()
    {
        Pool.Return(Buffer, true);
    }

    public override unsafe void Serialize(void* src, long lengthBytes)
    {
        var lengthBits = lengthBytes * 8;
        if (AllowAppend(lengthBits))
        {
            fixed (byte* pBuffer = Buffer)
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

    public void WriteBit(byte value)
    {
        if (AllowAppend(1))
        {
            if (value != 0)
            {
                Buffer[Num >> 3] |= FBitUtil.GShift[Num & 7];
            }

            Num++;
        }
        else
        {
            SetOverflowed(1);
        }
    }

    private void SetOverflowed(long lengthBits)
    {
        if (!AllowOverflow)
        {
            Logger.Error("FBitWriter overflowed (WriteLen: {Len}, Remaining: {Remaining}, Max: {Max})", lengthBits, (Max - Num), Max);
        }
        
        SetError();
    }

    private bool AllowAppend(long lengthBits)
    {
        if (Num + lengthBits > Max)
        {
            if (AllowResize)
            {
                throw new NotImplementedException();
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public byte[] GetData()
    {
        if (IsError())
        {
            Logger.Error("Retrieved data from a BitWriter that had an error");    
        }
        
        return Buffer;
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
}