using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Net.Actors;
using Prospect.Unreal.Runtime;

namespace Prospect.Unreal.Core;

public class ULevel : UObject
{
    public ULevel()
    {
        Actors = new List<AActor>();
    }
    
    /// <summary>
    ///     URL associated with this level.
    /// </summary>
    public FUrl URL { get; private set; }
    
    /// <summary>
    ///     Array of all actors in this level, used by FActorIteratorBase and derived classes
    /// </summary>
    public List<AActor> Actors { get; private set; }

    /// <summary>
    ///     The World that has this level in its Levels array. 
    ///     This is not the same as GetOuter(), because GetOuter() for a streaming level is a vestigial world that is not used. 
    ///     It should not be accessed during BeginDestroy(), just like any other UObject references, since GC may occur in any order.
    /// </summary>
    public UWorld OwningWorld { get; private set; }
    
    public void InitializeNetworkActors()
    {
        throw new NotImplementedException();
    }
}