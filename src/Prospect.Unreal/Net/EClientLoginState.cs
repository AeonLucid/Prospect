namespace Prospect.Unreal.Net;

public enum EClientLoginState
{
    Invalid		 = 0, // This must be a client (which doesn't use this state) or uninitialized.
    LoggingIn	 = 1, // The client is currently logging in.
    Welcomed	 = 2, // Told client to load map and will respond with SendJoin
    ReceivedJoin = 3, // NMT_Join received and a player controller has been created
    CleanedUp	 = 4  // Cleanup has been called at least once, the connection is considered abandoned/terminated/gone
}