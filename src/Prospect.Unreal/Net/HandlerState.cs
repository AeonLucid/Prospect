namespace Prospect.Unreal.Net;

public enum HandlerState
{
    Uninitialized,			// PacketHandler is uninitialized
    InitializingComponents,	// PacketHandler is initializing HandlerComponents
    Initialized				// PacketHandler and all HandlerComponents (if any) are initialized
}