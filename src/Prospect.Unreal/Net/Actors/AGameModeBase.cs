using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Runtime;

namespace Prospect.Unreal.Net.Actors;

public class AGameModeBase : AInfo
{
    public AGameModeBase()
    {
        OptionsString = string.Empty;
    }
    
    /// <summary>
    ///     Save options string and parse it when needed
    /// </summary>
    public string OptionsString { get; set; }
    
    public AGameSession? GameSession { get; set; }

    public virtual void InitGame(string mapName, string options, out string errorMessage)
    {
        // Default error.
        errorMessage = string.Empty;
        
        // Find world.
        var world = GetWorld();
        
        // Save Options for future use
        OptionsString = options;

        var spawnInfo = new FActorSpawnParameters
        {
            Instigator = GetInstigator(),
            ObjectFlags = EObjectFlags.RF_Transient
        };

        // GameSession = world.SpawnActor();
    }

    public virtual void InitGameState()
    {
        throw new NotImplementedException();
    }

    public void PreLogin(string options, string address, FUniqueNetIdRepl uniqueId, out string? errorMessage)
    {
        // Login unique id must match server expected unique id type OR No unique id could mean game doesn't use them
        errorMessage = null;
    }

    public APlayerController? Login(UPlayer newPlayer, ENetRole inRemoteRole, string portal, string options, FUniqueNetIdRepl uniqueId, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (GameSession == null)
        {
            errorMessage = "Failed to spawn player controller, GameSession is null";
            return null;
        }

        errorMessage = GameSession.ApproveLogin(options);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            return null;
        }
        
        throw new NotImplementedException();
    }

    public void PostLogin(APlayerController newPlayer)
    {
        throw new NotImplementedException();
    }
}