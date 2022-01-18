namespace Prospect.Unreal.Core.Objects;

/// <summary>
///     Custom class to help with <code>T::StaticClass()</code> code.
/// </summary>
public class GUClassArray
{
    public static UClass StaticClass<T>()
    {
        return StaticClass(typeof(T));
    }

    public static UClass StaticClass(Type type)
    {
        // TODO: Implement
        return new UClass(type);
    }
}