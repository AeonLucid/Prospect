using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Core;

public class ULevel
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

    public void InitializeNetworkActors()
    {
        throw new NotImplementedException();
    }
}