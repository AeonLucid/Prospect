using Prospect.Unreal.Core;
using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Runtime;

public class UGameInstance
{
    public AGameModeBase? CreateGameModeForURL(FUrl inUrl, UWorld inWorld)
    {
        // TODO: World.SpawnActor
        return new AGameModeBase();
    }
}