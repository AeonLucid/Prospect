using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Net;

public ref struct FActorSpawnParameters
{
    /// <summary>
    ///     A name to assign as the Name of the Actor being spawned.
    ///     If no value is specified, the name of the spawned Actor will be automatically generated using the form [Class]_[Number].
    /// </summary>
    public FName Name { get; set; } = EName.None;
    
    /// <summary>
    ///     An Actor to use as a template when spawning the new Actor.
    ///     The spawned Actor will be initialized using the property values of the template Actor.
    ///     If left NULL the class default object (CDO) will be used to initialize the spawned Actor.
    /// </summary>
    public AActor? Template { get; set; }
    
    /// <summary>
    ///     The Actor that spawned this Actor. (Can be left as NULL).
    /// </summary>
    public AActor? Owner { get; set; }
    
    /// <summary>
    ///     The APawn that is responsible for damage done by the spawned Actor. (Can be left as NULL).
    /// </summary>
    public AActor? Instigator { get; set; }
    
    /// <summary>
    ///     The ULevel to spawn the Actor in, i.e. the Outer of the Actor.
    ///     If left as NULL the Outer of the Owner is used. If the Owner is NULL the persistent level is used.
    /// </summary>
    public ULevel? OverrideLevel { get; set; }
    
    /// <summary>
    ///     The UPackage to set the Actor in.
    ///     If left as NULL the Package will not be set and the actor will be saved in the same package as the persistent level.
    /// </summary>
    public UPackage? OverridePackage { get; set; }
    
    // UChildActorComponent
    
    /// <summary>
    ///     The Guid to set to this actor. Should only be set when reinstancing blueprint actors.
    /// </summary>
    public FGuid? OverrideActorGuid { get; set; }
    
    /// <summary>
    ///     Method for resolving collisions at the spawn point. Undefined means no override, use the actor's setting.
    /// </summary>
    public ESpawnActorCollisionHandlingMethod SpawnCollisionHandlingOverride { get; set; }
    
    /// <summary>
    ///     Is the actor remotely owned.
    ///     This should only be set true by the package map when it is creating an actor on a client that was replicated from the server.
    /// </summary>
    public bool bRemoteOwned { get; set; }
    
    /// <summary>
    ///     Determines whether spawning will not fail if certain conditions are not met.
    ///     If true, spawning will not fail because the class being spawned is `bStatic=true` or because the class of the template Actor is not the same as the class of the Actor being spawned.
    /// </summary>
    public bool bNoFail { get; set; }
    
    /// <summary>
    ///     Determines whether the construction script will be run.
    ///     If true, the construction script will not be run on the spawned Actor.
    ///     Only applicable if the Actor is being spawned from a Blueprint.
    /// </summary>
    public bool bDeferConstruction { get; set; }
    
    /// <summary>
    ///     Determines whether or not the actor may be spawned when running a construction script.
    ///     If true spawning will fail if a construction script is being run.
    /// </summary>
    public bool bAllowDuringConstructionScript { get; set; }
    
    /// <summary>
    ///     Determines whether the begin play cycle will run on the spawned actor when in the editor.
    /// </summary>
    public bool bTemporaryEditorActor { get; set; }
    
    /// <summary>
    ///     Determines whether or not the actor should be hidden from the Scene Outliner
    /// </summary>
    public bool bHideFromSceneOutliner { get; set; }
    
    /// <summary>
    ///     Determines whether to create a new package for the actor or not.
    /// </summary>
    public bool bCreateActorPackage { get; set; }
    
    /// <summary>
    ///     In which way should SpawnActor should treat the supplied Name if not none.
    /// </summary>
    public ESpawnActorNameMode NameMode { get; set; }
    
    /// <summary>
    ///     Flags used to describe the spawned actor/object instance.
    /// </summary>
    public EObjectFlags ObjectFlags { get; set; }
}