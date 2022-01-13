using Prospect.Unreal.Runtime;

namespace Prospect.Unreal.Net.Actors;

public class APlayerController : AController
{
    /// <summary>
    ///     Index identifying players using the same base connection (splitscreen clients)
    ///     Used by netcode to match replicated PlayerControllers to the correct splitscreen viewport and child connection
    ///     replicated via special internal code, not through normal variable replication
    /// </summary>
    public byte NetPlayerIndex { get; set; }

    public void SetPlayer(UPlayer inPlayer)
    {
        throw new NotImplementedException();
    }
}