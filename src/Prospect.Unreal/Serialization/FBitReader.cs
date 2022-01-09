using System.Collections;
using Prospect.Unreal.Exceptions;

namespace Prospect.Unreal.Serialization;

public class FBitReader : FArchive
{
    public FBitReader(byte[] src) : this(src, src?.Length << 3 ?? 0)
    {
    }
    
    public FBitReader(byte[]? src, int num)
    {
        _arIsPersistent = true;
        _arIsLoading = true;
        _arIsNetArchive = true;

        Num = num;
        
        if (src != null)
        {
            Buffer = src;
            BufferBits = new BitArray(src);

            ApplyMask();
        }
        else
        {
            Buffer = Array.Empty<byte>();
            BufferBits = new BitArray(Buffer);
        }
    }

    private byte[] Buffer { get; set; }

    private BitArray BufferBits { get; set; }

    public long Pos { get; set; }

    private long Num { get; set; }

    public unsafe void SetData(byte[] src, long countBits)
    {
        Pos = 0;
        Num = countBits;
        Buffer = src;

        if ((Num & 7) != 0)
        {
            Buffer[Num >> 3] &= FBitUtil.GMask[Num & 7];
        }
        
        BufferBits = new BitArray(Buffer);
    }

    public unsafe void SetData(FBitReader src, long countBits)
    {
        Pos = 0;
        Num = countBits;

        if (src.GetBitsLeft() < countBits)
        {
            throw new UnrealSerializationException($"SetData overflow. ({src.GetBitsLeft()} < {countBits})");
        }

        SetEngineNetVer(src.EngineNetVer());
        SetGameNetVer(src.GameNetVer());

        Buffer = new byte[(countBits + 7) >> 3];

        fixed (byte* pBuffer = Buffer)
        {
            src.SerializeBits(pBuffer, countBits);
        }

        BufferBits = new BitArray(Buffer);
    }

    public override unsafe void SerializeBits(void* dest, long lengthBits)
    {
        if (lengthBits == 1)
        {
            ((byte*) dest)[0] = (byte) (BufferBits[(int) Pos++] ? 0x01 : 0x00);
        }
        else if (lengthBits != 0)
        {
            ((byte*) dest)[0] = 0;

            fixed (byte* pBuffer = Buffer)
            {
                FBitUtil.AppBitsCpy((byte*) dest, 0, pBuffer, (int) Pos, (int) lengthBits);
            }

            Pos += lengthBits;
        }
    }

    public override unsafe void SerializeInt(uint* outValue, uint valueMax)
    {
        uint value = 0;
        var localPos = Pos;
        var localNum = Num;

        for (uint mask = 1; (value + mask) < valueMax && (mask != 0); mask *= 2, localPos++)
        {
            if (localPos >= localNum)
            {
                throw new UnrealSerializationException("BitReader::SerializeInt Overflow.");
            }

            if (BufferBits[(int) localPos])
            {
                value |= mask;
            }
        }

        Pos = localPos;
        *outValue = value;
    }
    
    public override unsafe uint ReadInt(uint max)
    {
        uint value = 0;
        SerializeInt(&value, max);
        return value;
    }

    public override bool ReadBit()
    {
        return BufferBits[(int) Pos++];
    }

    public override unsafe void Serialize(void* dest, long lengthBytes)
    {
        SerializeBits(dest, lengthBytes * 8);
    }

    public unsafe byte* GetData()
    {
        fixed (byte* pBuffer = Buffer)
        {
            return pBuffer;
        }
    }

    public byte[] GetBuffer()
    {
        return Buffer;
    }

    public int GetBufferPosChecked()
    {
        if (Pos % 8 != 0)
        {
            throw new UnrealSerializationException("FBitReader Pos % 8 != 0");
        }

        return (int) (Pos >> 3);
    }
    
    public int GetBytesLeft()
    {
        return (int) ((Num - Pos + 7) >> 3);
    }

    public int GetBitsLeft()
    {
        return Math.Max((int) (Num - Pos), 0);
    }

    public bool AtEnd()
    {
        return _arIsError || Pos >= Num;
    }

    public int GetNumBytes()
    {
        return (int) ((Num + 7) >> 3);
    }

    public int GetNumBits()
    {
        return (int) Num;
    }

    public int GetPosBits()
    {
        return (int) Pos;
    }

    public void AppendDataFromChecked(int inBufferPos, byte[] inBuffer, int inBitsCount)
    {
        if (Num % 8 != 0)
        {
            throw new UnrealSerializationException("FBitReader Pos % 8 != 0");
        }
        
        // Copy to buffer.
        var merged = new byte[Buffer.Length + inBuffer.Length - inBufferPos];
        
        System.Buffer.BlockCopy(Buffer, 0, merged, 0, Buffer.Length);
        System.Buffer.BlockCopy(inBuffer, inBufferPos, merged, Buffer.Length, inBuffer.Length - inBufferPos);

        Buffer = merged;
        BufferBits = new BitArray(Buffer);

        Num += inBitsCount;

        ApplyMask();
    }

    private void ApplyMask()
    {
        if ((Num & 7) != 0)
        {
            var mask = FBitUtil.GMask[Num & 7];
            var num = (int) Num;
            
            // Apply to bytes.
            Buffer[num >> 3] &= mask;
            
            // Apply to bits.
            for (int i = 0; i <= 7; i++)
            {
                BufferBits[num - i] &= ((mask >> 7 - i) & 0x1) == 1;
            }
        }
    }
    
    public override void Dispose()
    {
        
    }
}