using System.Runtime.CompilerServices;
using Prospect.Unreal.Core.Names;

namespace Prospect.Unreal.Core.Objects;

public class UObjectBase
{
    /// <summary>
    ///     Flags used to track and report various object states.
    /// </summary>
    private EObjectFlags _objectFlags;

    /// <summary>
    ///     Index into GObjectArray...very private.
    /// </summary>
    private int _internalIndex;

    /// <summary>
    ///     Class the object belongs to.
    /// </summary>
    private UClass _classPrivate;

    /// <summary>
    ///     Name of this object.
    /// </summary>
    private FName _namePrivate;

    /// <summary>
    ///     Object this object resides in.
    /// </summary>
    private UObject? _outerPrivate;

    public UObjectBase()
    {
    }

    /// <summary>
    ///     Returns the unique ID of the object...these are reused so it is only unique while the object is alive.
    ///     Useful as a tag.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetUniqueID()
    {
        return (uint)_internalIndex;
    }

    /// <summary>
    ///     Returns the UClass that defines the fields of this object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UClass GetClass()
    {
        return _classPrivate;
    }

    /// <summary>
    ///     Returns the UObject this object resides in.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject? GetOuter()
    {
        return _outerPrivate;
    }

    /// <summary>
    ///     Returns the logical name of this object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FName GetFName()
    {
        return _namePrivate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetFlagsTo(EObjectFlags newFlags)
    {
        _objectFlags = newFlags;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EObjectFlags GetFlags()
    {
        return _objectFlags;
    }
}