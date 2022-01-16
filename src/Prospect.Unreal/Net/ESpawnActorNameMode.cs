namespace Prospect.Unreal.Net;

public enum ESpawnActorNameMode
{
    /// <summary>
    ///     Fatal if unavailable, application will assert
    /// </summary>
    Required_Fatal,

    /// <summary>
    ///     Report an error return null if unavailable
    /// </summary>
    Required_ErrorAndReturnNull,

    /// <summary>
    ///     Return null if unavailable
    /// </summary>
    Required_ReturnNull,

    /// <summary>
    ///     If the supplied Name is already in use the generate an unused one using the supplied version as a base
    /// </summary>
    Requested
}