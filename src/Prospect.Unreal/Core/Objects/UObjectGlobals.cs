using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;

namespace Prospect.Unreal.Core.Objects;

public class UObjectGlobals
{
    public static T? NewObject<T>(UObject outer,
        UClass clazz,
        FName? name = null,
        EObjectFlags flags = EObjectFlags.RF_NoFlags,
        UObject? template = null,
        bool bCopyTransientsFromClassDefaults = false,
        FObjectInstancingGraph? inInstanceGraph = null,
        UPackage? externalPackage = null)
    {
        if (name == null)
        {
            name = EName.None;
        }
        
        // NewObject
        // StaticConstructObject_Internal
        // StaticAllocateObject

        var obj = Activator.CreateInstance(clazz.Type);
        if (obj == null)
        {
            throw new UnrealException("Failed to create object.");
        }

        return (T?) obj;
    }
}