namespace Prospect.Unreal.Net;

public enum EConnectionState
{
    USOCK_Invalid   = 0, // Connection is invalid, possibly uninitialized.
    USOCK_Closed    = 1, // Connection permanently closed.
    USOCK_Pending	= 2, // Connection is awaiting connection.
    USOCK_Open      = 3, // Connection is open.
}