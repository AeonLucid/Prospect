using System.Text;
using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Runtime;

public partial class UWorld
{
    private APlayerController? SpawnPlayActor(UPlayer newPlayer, ENetRole remoteRole, FUrl inURL, FUniqueNetIdRepl uniqueId, out string error, byte inNetPlayerIndex = 0)
    {
        error = string.Empty;
        
        // Make the option string.
        var optionsBuilder = new StringBuilder();
        
        foreach (var op in inURL.Options)
        {
            optionsBuilder.Append('?');
            optionsBuilder.Append(op);
        }

        var options = optionsBuilder.ToString();

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
}