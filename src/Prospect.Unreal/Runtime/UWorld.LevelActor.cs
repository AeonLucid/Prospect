using System.Text;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Math;
using Prospect.Unreal.Core.Objects;
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

    public AActor SpawnActor(UClass? clazz, FTransform userTransform, FActorSpawnParameters spawnParameters)
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
            // template = clazz.GetDefaultObject();
        }
        
        return null;
    }

    public T SpawnActor<T>(UClass? clazz, FActorSpawnParameters spawnParameters) where T : AActor
    {
        return (T)SpawnActor(clazz, null, null, spawnParameters);
    }
}