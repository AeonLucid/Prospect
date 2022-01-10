namespace Prospect.Unreal.Net;

public enum EAcceptConnection
{
    /** Reject the connection */
    Reject,
    /** Accept the connection */
    Accept,
    /** Ignore the connection, sending no reply, while server traveling */
    Ignore
}