using System.Runtime.CompilerServices;

namespace Prospect.Unreal.Core.Objects;

public class UObjectBaseUtility : UObjectBase
{
    /*
     * Flags
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(EObjectFlags newFlags)
    {
        SetFlagsTo(GetFlags() | newFlags);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFlags(EObjectFlags newFlags)
    {
        SetFlagsTo(GetFlags() | ~newFlags);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyFlags(EObjectFlags flagsToCheck)
    {
        return (GetFlags() & flagsToCheck) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAllFlags(EObjectFlags flagsToCheck)
    {
        return (GetFlags() & flagsToCheck) == flagsToCheck;
    }
    
    /*
     * Marks
     */
    public bool IsUnreachable()
    {
        // TODO: GUObjectArray
        return false;
    }

    public bool IsPendingKill()
    {
        // TODO: GUObjectArray
        return false;
    }
    
    /*
     * Outer & Package
     */
    public UObject? GetTypedOuter(UClass target)
    {
        UObject? result = null;
        for (var nextOuter = GetOuter(); result == null && nextOuter != null; nextOuter = nextOuter.GetOuter())
        {
            if (nextOuter.IsA(target))
            {
                result = nextOuter;
            }
        }
        return result;
    }
    
    public T? GetTypedOuter<T>() where T : UObject
    {
        return (T?)GetTypedOuter(GUClassArray.StaticClass<T>());
    }
    
    /*
     * Class
     */
    public bool IsChildOfWorkaround(UClass objClass, UClass testClass)
    {
        return objClass.IsChildOf(testClass);
    }
    
    public bool IsA(UClass someBase)
    {
        var someBaseClass = someBase;
        var thisClass = GetClass();
        
        return IsChildOfWorkaround(thisClass, someBaseClass);
    }
}