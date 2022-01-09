namespace Prospect.Unreal.Net;

public enum HandlerComponentState
{
    UnInitialized,		// HandlerComponent not yet initialized
    InitializedOnLocal, // Initialized on local instance
    InitializeOnRemote, // Initialized on remote instance, not on local instance
    Initialized         // Initialized on both local and remote instances
}