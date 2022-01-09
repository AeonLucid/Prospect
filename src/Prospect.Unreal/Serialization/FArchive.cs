using Prospect.Unreal.Core;

namespace Prospect.Unreal.Serialization;

public abstract class FArchive : IDisposable
{
    /// <summary>
    ///	Whether this archive is for loading data. 
    /// </summary>
    public bool _arIsLoading;

    /// <summary>
    ///	Whether this archive is for saving data. 
    /// </summary>
    public bool _arIsSaving;

    /// <summary>
    ///	Whether archive is transacting. 
    /// </summary>
    public bool _arIsTransacting;

    /// <summary>
    ///	Whether this archive serializes to a text format. Text format archives should use high level constructs from FStructuredArchive for delimiting data rather than manually seeking through the file. 
    /// </summary>
    public bool _arIsTextFormat;

    /// <summary>
    ///	Whether this archive wants properties to be serialized in binary form instead of tagged. 
    /// </summary>
    public bool _arWantBinaryPropertySerialization;

    /// <summary>
    ///	Whether this archive wants to always save strings in unicode format 
    /// </summary>
    public bool _arForceUnicode;

    /// <summary>
    ///	Whether this archive saves to persistent storage. 
    /// </summary>
    public bool _arIsPersistent;

    /// <summary>
    ///	Whether this archive contains errors. 
    /// </summary>
    public bool _arIsError;

    /// <summary>
    ///	Whether this archive contains critical errors. 
    /// </summary>
    public bool _arIsCriticalError;

    /// <summary>
    ///	Quickly tell if an archive contains script code. 
    /// </summary>
    public bool _arContainsCode;

    /// <summary>
    ///	Used to determine whether FArchive contains a level or world. 
    /// </summary>
    public bool _arContainsMap;

    /// <summary>
    ///	Used to determine whether FArchive contains data required to be gathered for localization. 
    /// </summary>
    public bool _arRequiresLocalizationGather;

    /// <summary>
    ///	Whether we should forcefully swap bytes. 
    /// </summary>
    public bool _arForceByteSwapping;

    /// <summary>
    ///	If true, we will not serialize the ObjectArchetype reference in UObject. 
    /// </summary>
    public bool _arIgnoreArchetypeRef;

    /// <summary>
    ///	If true, we will not serialize the ObjectArchetype reference in UObject. 
    /// </summary>
    public bool _arNoDelta;

    /// <summary>
    ///	If true, we will not serialize the Outer reference in UObject. 
    /// </summary>
    public bool _arIgnoreOuterRef;

    /// <summary>
    ///	If true, we will not serialize ClassGeneratedBy reference in UClass. 
    /// </summary>
    public bool _arIgnoreClassGeneratedByRef;

    /// <summary>
    ///	If true, UObject::Serialize will skip serialization of the Class property. 
    /// </summary>
    public bool _arIgnoreClassRef;

    /// <summary>
    ///	Whether to allow lazy loading. 
    /// </summary>
    public bool _arAllowLazyLoading;

    /// <summary>
    ///	Whether this archive only cares about serializing object references. 
    /// </summary>
    public bool _arIsObjectReferenceCollector;

    /// <summary>
    ///	Whether a reference collector is modifying the references and wants both weak and strong ones 
    /// </summary>
    public bool _arIsModifyingWeakAndStrongReferences;

    /// <summary>
    ///	Whether this archive is counting memory and therefore wants e.g. TMaps to be serialized. 
    /// </summary>
    public bool _arIsCountingMemory;

    /// <summary>
    ///	Whether bulk data serialization should be skipped or not. 
    /// </summary>
    public bool _arShouldSkipBulkData;

    /// <summary>
    ///	Whether editor only properties are being filtered from the archive (or has been filtered). 
    /// </summary>
    public bool _arIsFilterEditorOnly;

    /// <summary>
    ///	Whether this archive is saving/loading game state 
    /// </summary>
    public bool _arIsSaveGame;

    /// <summary>
    ///	Whether or not this archive is sending/receiving network data 
    /// </summary>
    public bool _arIsNetArchive;

    /// <summary>
    ///	Set TRUE to use the custom property list attribute for serialization. 
    /// </summary>
    public bool _arUseCustomPropertyList;

    /// <summary>
    /// Whether we are currently serializing defaults. > 0 means yes, &lt;= 0 means no.
    /// </summary>
    public int _arSerializingDefaults;

    /// <summary>
    /// Modifier flags that be used when serializing UProperties
    /// </summary>
    public uint _arPortFlags;

    /// <summary>
    /// Max size of data that this archive is allowed to serialize.
    /// </summary>
    public long _arMaxSerializeSize;

    /// <summary>
    /// Holds the archive version.
    /// </summary>
    protected int _arUE4Ver;

    /// <summary>
    /// Holds the archive version for licensees.
    /// </summary>
    protected int _arLicenseeUE4Ver;

    /// <summary>
    /// Holds the engine version.
    /// </summary>
    protected FEngineVersion _arEngineVer;

    /// <summary>
    /// Holds the engine network protocol version.
    /// </summary>
    protected EEngineNetworkVersionHistory _arEngineNetVer;

    /// <summary>
    /// Holds the game network protocol version.
    /// </summary>
    protected uint _arGameNetVer;

    public virtual bool ReadBit()
    {
        throw new NotImplementedException();
    }
    
    public virtual unsafe byte ReadByte()
    {
        byte value;
        Serialize(&value, 1);
        return value;
    }

    public virtual unsafe byte[] ReadBytes(long amount)
    {
        byte[] value = new byte[amount];

        fixed (byte* pValue = value)
        {
            Serialize(pValue, amount);
        }

        return value;
    }

    public virtual unsafe ushort ReadUInt16()
    {
        ushort value;
        ByteOrderSerialize(&value, sizeof(ushort));
        return value;
    }

    public virtual unsafe short ReadInt16()
    {
        short value;
        ByteOrderSerialize(&value, sizeof(short));
        return value;
    }

    public virtual unsafe uint ReadUInt32()
    {
        uint value;
        ByteOrderSerialize(&value, sizeof(uint));
        return value;
    }

    public virtual unsafe int ReadInt32()
    {
        int value;
        ByteOrderSerialize(&value, sizeof(int));
        return value;
    }

    public unsafe void WriteInt32(int value)
    {
        ByteOrderSerialize((uint*)&value, sizeof(uint));
    }

    public virtual unsafe uint ReadInt(uint max)
    {
        throw new NotImplementedException();
    }

    public virtual unsafe uint ReadUInt32Packed()
    {
        uint value;
        SerializeIntPacked(&value);
        return value;
    }

    public virtual unsafe ulong ReadUInt64()
    {
        ulong value;
        ByteOrderSerialize(&value, sizeof(ulong));
        return value;
    }

    public virtual unsafe long ReadInt64()
    {
        long value;
        ByteOrderSerialize(&value, sizeof(long));
        return value;
    }

    public virtual unsafe float ReadFloat()
    {
        float value;
        ByteOrderSerialize(&value, sizeof(float));
        return value;
    }

    public virtual unsafe double ReadDouble()
    {
        double value;
        ByteOrderSerialize(&value, sizeof(double));
        return value;
    }

    public unsafe void WriteDouble(double value)
    {
        ByteOrderSerialize((ulong*)&value, sizeof(ulong));
    }

    public string ReadString()
    {
        return FString.Deserialize(this);
    }
    
    public void WriteString(string value)
    {
        FString.Serialize(this, value);
    }

    public unsafe void Serialize(Span<byte> value, long num)
    {
        fixed (byte* pBuffer = value)
        {
            Serialize(pBuffer, num);
        }
    }

    public unsafe void Serialize(byte[] value, long num)
    {
        fixed (byte* pBuffer = value)
        {
            Serialize(pBuffer, num);
        }
    }

    public virtual unsafe void Serialize(void* value, long num)
    {
        throw new NotImplementedException();
    }

    public unsafe void SerializeBits(byte[] value, long lengthBits)
    {
        fixed (byte* pBuffer = value)
        {
            SerializeBits(pBuffer, lengthBits);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="lengthBits"></param>
    public virtual unsafe void SerializeBits(void* value, long lengthBits)
    {
        Serialize(value, (lengthBits + 7) / 8);

        if (IsLoading() && (lengthBits % 8) != 0)
        {
            ((byte*) value)[lengthBits / 8] &= (byte) ((1 << (int) (lengthBits & 7)) - 1);
        }
    }

    public virtual unsafe void SerializeInt(uint* value, uint max)
    {
        throw new NotImplementedException();
        ByteOrderSerialize(&value, sizeof(uint));
    }

    /// <summary>
    /// Packs int value into bytes of 7 bits with 8th bit for 'more'.
    /// </summary>
    /// <param name="value"></param>
    public virtual unsafe void SerializeIntPacked(uint* value)
    {
        if (IsLoading())
        {
            byte count = 0;
            byte more = 1;

            while (more != 0)
            {
                byte nextByte;
                Serialize(&nextByte, 1);                     // Read next byte

                more = (byte)(nextByte & 1);                 // Check 1 bit to see if theres more after this
                nextByte = (byte)(nextByte >> 1);            // Shift to get actual 7 bit value
                *value += (uint)nextByte << (7 * count++);   // Add to total value
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }
    
    public virtual string GetArchiveName()
    {
        return "FArchive";
    }

    public virtual long Tell()
    {
        return -1;
    }

    public virtual long TotalSize()
    {
        return -1;
    }

    public virtual bool AtEnd()
    {
        var pos = Tell();

        return pos != -1 && pos >= TotalSize();
    }

    public virtual void Seek(long inPos)
    {
        throw new NotImplementedException();
    }

    public virtual void Flush()
    {
        throw new NotImplementedException();
    }

    public virtual bool Close()
    {
        return !_arIsError;
    }

    public virtual bool GetError()
    {
        return _arIsError;
    }

    public void SetError()
    {
        _arIsError = true;
    }

    public bool IsByteSwapping()
    {
        return BitConverter.IsLittleEndian 
            ? _arForceByteSwapping 
            : _arIsPersistent;
    }

    /// <summary>
    /// Used to do byte swapping on small items. This does not happen usually, so we don't want it inline
    /// </summary>
    /// <param name="v"></param>
    /// <param name="length"></param>
    public unsafe void ByteSwap(void* v, int length)
    {
        byte* ptr = (byte*) v;
        int top = length - 1;
        int bottom = 0;

        while (bottom < top)
        {
            var aPos = top--;
            var bPos = bottom++;

            (ptr[aPos], ptr[bPos]) = (ptr[bPos], ptr[aPos]);
        }
    }

    public unsafe void ByteOrderSerialize(void* v, int length)
    {
        if (!IsByteSwapping())
        {
            Serialize(v, length);
            return;
        }

        SerializeByteOrderSwapped(v, length);
    }

    private unsafe void SerializeByteOrderSwapped(void* v, int length)
    {
        if (IsLoading())
        {
            Serialize(v, length);
            ByteSwap(v, length);
        }
        else
        {
            ByteSwap(v, length);
            Serialize(v, length);
            ByteSwap(v, length);
        }
    }

    public void ThisContainsCode()
    {
        _arContainsCode = true;
    }

    public void ThisContainsMap()
    {
        _arContainsMap = true;
    }

    public void ThisRequiresLocalizationGather()
    {
        _arRequiresLocalizationGather = true;
    }

    public void StartSerializingDefaults()
    {
        _arSerializingDefaults++;
    }

    public void StopSerializingDefaults()
    {
        _arSerializingDefaults--;
    }

    public int UE4Ver()
    {
        return _arUE4Ver;
    }

    public int LicenseeUE4Ver()
    {
        return _arLicenseeUE4Ver;
    }

    public FEngineVersion EngineVer()
    {
        return _arEngineVer;
    }

    public EEngineNetworkVersionHistory EngineNetVer()
    {
        return _arEngineNetVer;
    }

    public uint GameNetVer()
    {
        return _arGameNetVer;
    }

    public bool IsLoading()
    {
        return _arIsLoading;
    }

    public bool IsSaving()
    {
        return _arIsSaving;
    }

    public bool IsTransacting()
    {
        // Misses FPlatformProperties::HasEditorOnlyData.

        return _arIsTransacting;
    }

    public bool IsTextFormat()
    {
        return _arIsTextFormat;
    }

    public bool WantBinaryPropertySerialization()
    {
        return _arWantBinaryPropertySerialization;
    }

    public bool IsForcingUnicode()
    {
        return _arForceUnicode;
    }

    public bool IsPersistent()
    {
        return _arIsPersistent;
    }

    public bool IsError()
    {
        return _arIsError;
    }

    public bool IsCriticalError()
    {
        return _arIsCriticalError;
    }

    public bool ContainsCode()
    {
        return _arContainsCode;
    }

    public bool ContainsMap()
    {
        return _arContainsMap;
    }

    public bool RequiresLocalizationGather()
    {
        return _arRequiresLocalizationGather;
    }

    public bool ForceByteSwapping()
    {
        return _arForceByteSwapping;
    }

    public bool IsSerializingDefaults()
    {
        return _arSerializingDefaults > 0;
    }

    public bool IsIgnoringArchetypeRef()
    {
        return _arIgnoreArchetypeRef;
    }

    public bool DoDelta()
    {
        return !_arNoDelta;
    }

    public bool IsIgnoringOuterRef()
    {
        return _arIgnoreOuterRef;
    }

    public bool IsIgnoringClassGeneratedByRef()
    {
        return _arIgnoreClassGeneratedByRef;
    }

    public bool IsIgnoringClassRef()
    {
        return _arIgnoreClassRef;
    }

    public bool IsAllowingLazyLoading()
    {
        return _arAllowLazyLoading;
    }

    public bool IsObjectReferenceCollector()
    {
        return _arIsObjectReferenceCollector;
    }

    public bool IsModifyingWeakAndStrongReferences()
    {
        return _arIsModifyingWeakAndStrongReferences;
    }

    public bool IsCountingMemory()
    {
        return _arIsCountingMemory;
    }

    public uint GetPortFlags()
    {
        return _arPortFlags;
    }

    public bool HasAnyPortFlags(uint flags)
    {
        return (_arPortFlags & flags) != 0;
    }

    public bool HasAllPortFlags(uint flags)
    {
        return (_arPortFlags & flags) == flags;
    }

    public bool ShouldSkipBulkData()
    {
        return _arShouldSkipBulkData;
    }

    public long GetMaxSerializeSize()
    {
        return _arMaxSerializeSize;
    }

    /// <summary>
    /// Sets the archive version number. Used by the code that makes sure that FLinkerLoad's 
    /// internal archive versions match the file reader it creates.
    /// </summary>
    /// <param name="inVer">new version number</param>
    public void SetUE4Ver(int inVer)
    {
        _arUE4Ver = inVer;
    }

    /// <summary>
    /// Sets the archive licensee version number. Used by the code that makes sure that FLinkerLoad's 
    /// internal archive versions match the file reader it creates.
    /// </summary>
    /// <param name="inVer">new version number</param>
    public void SetLicenseeUE4Ver(int inVer)
    {
        _arLicenseeUE4Ver = inVer;
    }

    /// <summary>
    /// Sets the archive engine version. Used by the code that makes sure that FLinkerLoad's
    /// internal archive versions match the file reader it creates.
    /// </summary>
    /// <param name="inVer">new version number</param>
    public void SetEngineVer(FEngineVersion inVer)
    {
        _arEngineVer = inVer;
    }

    /// <summary>
    /// Sets the archive engine network version.
    /// </summary>
    /// <param name="inEngineNetVer"></param>
    public void SetEngineNetVer(EEngineNetworkVersionHistory inEngineNetVer)
    {
        _arEngineNetVer = inEngineNetVer;
    }

    /// <summary>
    /// Sets the archive game network version.
    /// </summary>
    /// <param name="inGameNetVer"></param>
    public void SetGameNetVer(uint inGameNetVer)
    {
        _arGameNetVer = inGameNetVer;
    }

    /// <summary>
    /// Toggle saving as Unicode. This is needed when we need to make sure ANSI strings are saved as Unicode
    /// </summary>
    /// <param name="enabled">set to true to force saving as Unicode</param>
    public void SetForceUnicode(bool enabled)
    {
        _arForceUnicode = enabled;
    }

    public virtual bool IsFilterEditorOnly()
    {
        return _arIsFilterEditorOnly;
    }

    public virtual void SetFilterEditorOnly(bool inFilterEditorOnly)
    {
        _arIsFilterEditorOnly = inFilterEditorOnly;
    }

    public abstract void Dispose();
}