using Prospect.Unreal.Net.Actors;

namespace Prospect.Unreal.Runtime;

public class UPlayer
{
    public APlayerController? PlayerController { get; set; }
    
    public int CurrentNetSpeed { get; set; }
}