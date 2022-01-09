using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;

namespace Prospect.Server.Game;

public class ProspectWorld : UWorld
{
    public ProspectWorld(FUrl url) : base(url)
    {
    }
}