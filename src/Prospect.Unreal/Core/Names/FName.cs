using System.Runtime.CompilerServices;

namespace Prospect.Unreal.Core.Names;

public readonly struct FName
{
    public FName() : this(EName.None, FNameHelper.NAME_NO_NUMBER_INTERNAL)
    {
    }
    
    /// <summary>
    ///     Create an FName with a hardcoded string index.
    /// </summary>
    /// <param name="hardcodedName">The hardcoded value the string portion of the name will have</param>
    public FName(EName hardcodedName) : this(hardcodedName, FNameHelper.NAME_NO_NUMBER_INTERNAL)
    {
        
    }
    
    /// <summary>
    ///     Create an FName with a hardcoded string index and (instance).
    /// </summary>
    /// <param name="hardcodedName">The hardcoded value the string portion of the name will have</param>
    /// <param name="number">The hardcoded value for the number portion of the name</param>
    public FName(EName hardcodedName, int number)
    {
        Index = FNamePool.Find(hardcodedName);
        Number = (uint)number;
    }

    /// <summary>
    ///     Create an FName. If FindType is FNAME_Find, and the string part of the name
    ///     doesn't already exist, then the name will be NAME_None.
    /// </summary>
    /// <param name="value">Value for the string portion of the name</param>
    /// <param name="findType">Action to take</param>
    public FName(string value, EFindName findType = EFindName.FNAME_Add)
    {
        var name = FNameHelper.MakeDetectNumber(value, findType);

        Index = name.Index;
        Number = name.Number;
    }

    /// <summary>
    ///     Create an FName. If FindType is FNAME_Find, and the string part of the name
    ///     doesn't already exist, then the name will be NAME_None.
    /// </summary>
    /// <param name="value">Value for the string portion of the name</param>
    /// <param name="number">Value for the number portion of the name</param>
    /// <param name="findType">Action to take</param>
    public FName(string value, int number, EFindName findType = EFindName.FNAME_Add)
    {
        var name = FNameHelper.MakeWithNumber(value, findType, number);

        Index = name.Index;
        Number = name.Number;
    }

    /// <summary>
    ///     Only use this if you know what you are doing (:
    /// </summary>
    public FName(FNameEntryId index, int number)
    {
        Index = index;
        Number = (uint)number;
    }

    /// <summary>
    ///     Index of the name
    /// </summary>
    public FNameEntryId Index { get; }
    
    /// <summary>
    ///     Number portion of the string/number pair
    ///     (stored internally as 1 more than actual, so zero'd memory will be the default, no-instance case)
    /// </summary>
    public uint Number { get; }
    
    public static implicit operator FName(EName name)
    {
        return new FName(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNumber()
    {
        return (int)Number;
    }

    public string GetPlainNameString()
    {
        return FNamePool.Resolve(Index);
    }
    
    public EName? ToEName()
    {
        return FNamePool.FindEName(Index);
    }

    public override string ToString()
    {
        var name = GetPlainNameString();
        
        if (Number == FNameHelper.NAME_NO_NUMBER_INTERNAL)
        {
            return name;
        }

        return $"{name}_{FNameHelper.NAME_INTERNAL_TO_EXTERNAL(GetNumber())}";
    }
    
    public static bool operator ==(FName left, EName right)
    {
        return (left.Index == right) & (left.GetNumber() == 0);
    }

    public static bool operator !=(FName left, EName right)
    {
        return (left.Index != right) | (left.GetNumber() != 0);
    }
    
    public static bool operator ==(FName left, FName right)
    {
        return (left.Index == right.Index) & (left.GetNumber() == right.GetNumber());
    }

    public static bool operator !=(FName left, FName right)
    {
        return !(left == right);
    }
    
    public bool Equals(FName other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is FName other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Index.GetHashCode() * 397) ^ (int)Number;
        }
    }
}