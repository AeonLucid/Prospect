namespace Prospect.Unreal.Core.Names;

public enum EFindName
{
    /** Find a name; return 0 if it doesn't exist. */
    FNAME_Find,

    /** Find a name or add it if it doesn't exist. */
    FNAME_Add,

    /** Finds a name and replaces it. Adds it if missing. This is only used by UHT and is generally not safe for threading. 
	 * All this really is used for is correcting the case of names. In MT conditions you might get a half-changed name.
	 */
    FNAME_Replace_Not_Safe_For_Threading,
}