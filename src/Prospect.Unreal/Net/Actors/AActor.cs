using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;

namespace Prospect.Unreal.Net.Actors;

public class AActor
{
    private bool bActorInitialized;
    
    /// <summary>
    ///     Sets the value of Role without causing other side effects to this instance.
    /// </summary>
    public void SetRole(ENetRole inRole)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Set whether this actor replicates to network clients. When this actor is spawned on the server it will be sent to clients as well.
    ///     Properties flagged for replication will update on clients if they change on the server.
    ///     Internally changes the RemoteRole property and handles the cases where the actor needs to be added to the network actor list.
    /// </summary>
    public void SetReplicates(bool bInReplicates)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Sets whether or not this Actor is an autonomous proxy, which is an actor on a network client that is controlled by a user on that client.
    /// </summary>
    public void SetAutonomousProxy(bool bInAutonomousProxy, bool bAllowForcePropertyCompare = true)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public UWorld GetWorld()
    {
        // if ()
    }

    public bool IsActorInitialized()
    {
        return bActorInitialized;
    }
}