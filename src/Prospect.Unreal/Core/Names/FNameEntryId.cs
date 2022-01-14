namespace Prospect.Unreal.Core.Names;

public readonly struct FNameEntryId
{
    public FNameEntryId()
    {
        Value = 0;
    }
    
    public FNameEntryId(uint value)
    {
        Value = value;
    }
    
    public readonly uint Value;
    
    public static bool operator ==(FNameEntryId left, EName right)
    {
        return left == FNamePool.Find(right);
    }

    public static bool operator !=(FNameEntryId left, EName right)
    {
        return !(left == right);
    }
    
    public static bool operator ==(FNameEntryId left, FNameEntryId right)
    {
        return left.Value == right.Value;
    }
    
    public static bool operator !=(FNameEntryId left, FNameEntryId right)
    {
        return left.Value != right.Value;
    }
    
    public bool Equals(FNameEntryId other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is FNameEntryId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Value;
    }
}