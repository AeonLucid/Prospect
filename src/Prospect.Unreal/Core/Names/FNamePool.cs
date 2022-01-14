namespace Prospect.Unreal.Core.Names;

public static class FNamePool
{
    private static readonly object NamesLock = new object();
    
    /// <summary>
    ///     Map hardcoded names in <see cref="UnrealNames"/>.
    /// </summary>
    private static readonly Dictionary<EName, FNameEntryId> HardcodedNames = new Dictionary<EName, FNameEntryId>();
    private static readonly Dictionary<FNameEntryId, EName> HardcodedNamesReverse = new Dictionary<FNameEntryId, EName>();

    /// <summary>
    ///     Map existing strings to a <see cref="FName.Index"/>.
    /// </summary>
    private static readonly Dictionary<string, FNameEntryId> Names = new Dictionary<string, FNameEntryId>();
    private static readonly Dictionary<FNameEntryId, string> NamesReverse = new Dictionary<FNameEntryId, string>();

    private static uint _counter;
    
    static FNamePool()
    {
        // Initialize hardcoded names.
        foreach (var (key, value) in UnrealNames.Names)
        {
            var index = Store(value);
            HardcodedNames[key] = index;
            HardcodedNamesReverse[index] = key;
        }
    }

    public static EName? FindEName(FNameEntryId index)
    {
        if (HardcodedNamesReverse.TryGetValue(index, out var result))
        {
            return result;
        }

        return null;
    }

    public static FNameEntryId Find(string name)
    {
        if (Names.TryGetValue(name, out var result))
        {
            return result;
        }

        return new FNameEntryId((uint)EName.None);
    }
    
    public static FNameEntryId Find(EName name)
    {
        return HardcodedNames[name];
    }

    public static string Resolve(FNameEntryId index)
    {
        return NamesReverse[index];
    }

    public static FNameEntryId Store(ReadOnlySpan<char> valueSpan)
    {
        var value = valueSpan.ToString();
        
        if (Names.TryGetValue(value, out var result))
        {
            return result;
        }

        lock (NamesLock)
        {
            // Check again incase a previous lock added it.
            if (Names.TryGetValue(value, out result))
            {
                return result;
            }
            
            // Create new FName.
            result = new FNameEntryId(_counter++);

            // Store FName.
            Names[value] = result;
            NamesReverse[result] = value;
        }

        return result;
    }
}