using Prospect.Unreal.Core.Properties;

namespace Prospect.Unreal.Core.Objects;

public class UClass : UStruct
{
    public UClass(Type type)
    {
        Type = type;
    }
    
    public Type Type { get; }
    
    public UObject GetDefaultObject<T>() where T : UObject
    {
        // TODO: Implement
        var obj = (UObject) Activator.CreateInstance<T>();
        
        obj.SetFlags(EObjectFlags.RF_Public | EObjectFlags.RF_ClassDefaultObject | EObjectFlags.RF_ArchetypeObject);
        
        return obj;
    }

    public UObject GetDefaultObject(bool bCreateIfNeeded = true)
    {
        throw new NotImplementedException();
    }

    public UObject CreateDefaultObject()
    {
        throw new NotImplementedException();
    }
}