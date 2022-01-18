using System.Text;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Math;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Runtime;

public partial class UWorld
{
    private APlayerController? SpawnPlayActor(UPlayer newPlayer, ENetRole remoteRole, FUrl inURL, FUniqueNetIdRepl uniqueId, out string error, byte inNetPlayerIndex = 0)
    {
        error = string.Empty;
        
        // Make the option string.
        var options = inURL.OptionsToString();

        var gameMode = GetAuthGameMode();
        if (gameMode != null)
        {
            var newPlayerController = gameMode.Login(newPlayer, remoteRole, inURL.Portal, options, uniqueId, out error);
            if (newPlayerController == null)
            {
                Logger.Warning("Login failed: {Error}", error);
                return null;
            }
            
            // Logger.Debug("{A} got player {B} [{C}]", newPlayerController);
            
            // Possess the newly-spawned player.
            newPlayerController.NetPlayerIndex = inNetPlayerIndex;
            newPlayerController.SetRole(ENetRole.ROLE_Authority);
            newPlayerController.SetReplicates(remoteRole != ENetRole.ROLE_None);
            if (remoteRole == ENetRole.ROLE_AutonomousProxy)
            {
                newPlayerController.SetAutonomousProxy(true);
            }
            newPlayerController.SetPlayer(newPlayer);
            gameMode.PostLogin(newPlayerController);
            return newPlayerController;
        }
        
        Logger.Warning("Login failed: No game mode set");
        return null;
    }

    public AActor SpawnActor(UClass? clazz, FVector? location, FRotator? rotation, FActorSpawnParameters spawnParameters)
    {
        var transform = new FTransform();
        
        if (location != null)
        {
            transform.Location = location;
        }

        if (rotation != null)
        {
            // TODO: FQuat
            // transform.Rotation = 
        }

        return SpawnActor(clazz, transform, spawnParameters);
    }

    public AActor SpawnActor(UClass? clazz, FTransform? userTransformPtr, FActorSpawnParameters spawnParameters)
    {
        if (clazz == null)
        {
            Logger.Warning("SpawnActor failed because no class was specified");
            return null;
        }
        
        // TODO: Bunch of if checks
        var levelToSpawnIn = spawnParameters.OverrideLevel;
        if (levelToSpawnIn == null)
        {
            // Spawn in the same level as the owner if we have one.
            levelToSpawnIn = (spawnParameters.Owner != null) ? spawnParameters.Owner.GetLevel() : _currentLevel;
        }

        var newActorName = spawnParameters.Name;
        var template = spawnParameters.Template;

        if (template == null)
        {
            template = (AActor)clazz.GetDefaultObject<AActor>();
        }

        if (newActorName == EName.None)
        {
            // If we are using a template object and haven't specified a name, create a name relative to the template, otherwise let the default object naming behavior in Stat
            if (!template.HasAnyFlags(EObjectFlags.RF_ClassDefaultObject))
            {
                throw new NotImplementedException();
            }
        } 
        /* else if (StaticFindObjectFast(nullptr, LevelToSpawnIn, NewActorName)) */

        // See if we can spawn on ded.server/client only etc (check NeedsLoadForClient & NeedsLoadForServer)
        // TODO: CanCreateInCurrentContext

        var userTransform = userTransformPtr ?? new FTransform(); // TODO: FTransform::Identity
        var collisionHandlingOverride = spawnParameters.SpawnCollisionHandlingOverride;
        
        // "no fail" take preedence over collision handling settings that include fails
        if (spawnParameters.bNoFail)
        {
            // maybe upgrade to disallow fail
            if (collisionHandlingOverride == ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButDontSpawnIfColliding)
            {
                collisionHandlingOverride = ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn;
            } 
            else if (collisionHandlingOverride == ESpawnActorCollisionHandlingMethod.DontSpawnIfColliding)
            {
                collisionHandlingOverride = ESpawnActorCollisionHandlingMethod.AlwaysSpawn;
            }
        }

        // use override if set, else fall back to actor's preference
        var collisionHandlingMethod = (collisionHandlingOverride == ESpawnActorCollisionHandlingMethod.Undefined)
            ? template.SpawnCollisionHandlingMethod
            : collisionHandlingOverride;
        
        // see if we can avoid spawning altogether by checking native components
        // note: we can't handle all cases here, since we don't know the full component hierarchy until after the actor is spawned
        if (collisionHandlingMethod == ESpawnActorCollisionHandlingMethod.DontSpawnIfColliding)
        {
            throw new NotImplementedException();
        }

        var actorFlags = spawnParameters.ObjectFlags;
        UPackage? externalPackage = null;
        
        // actually make the actor object
        var actor = UObjectGlobals.NewObject<AActor>(levelToSpawnIn, clazz, newActorName, actorFlags, template, false, null, externalPackage);

        if (actor == null)
        {
            throw new UnrealException("Failed to create actor");
        }

        if (actor.GetLevel() != levelToSpawnIn)
        {
            throw new UnrealException("Actor spawned with the incorrect level");
        }
        
        // tell the actor what method to use, in case it was overridden
        actor.SpawnCollisionHandlingMethod = collisionHandlingMethod;
        
        actor.PostSpawnInitialize(userTransform, spawnParameters.Owner, spawnParameters.Instigator, spawnParameters.bRemoteOwned, spawnParameters.bNoFail, spawnParameters.bDeferConstruction);
        
        // if we are spawning an external actor, clear the dirty flag after post spawn initialize which might have dirtied the level package through running construction scripts
        if (externalPackage != null)
        {
            throw new NotImplementedException();
        }

        if (actor.IsPendingKill() && !spawnParameters.bNoFail)
        {
            // TODO: GetPathName
            Logger.Debug("SpawnActor failed because the spawned actor %s IsPendingKill");
            return null;
        }

        actor.CheckDefaultSubobjects();
        
        // Add this newly spawned actor to the network actor list. Do this after PostSpawnInitialize so that actor has "finished" spawning.
        AddNetworkActor(actor);
        
        return actor;
    }

    public T SpawnActor<T>(UClass? clazz, FActorSpawnParameters spawnParameters) where T : AActor
    {
        return (T)SpawnActor(clazz, null, null, spawnParameters);
    }
}