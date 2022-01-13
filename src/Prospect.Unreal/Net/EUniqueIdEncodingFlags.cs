namespace Prospect.Unreal.Net;

[Flags]
public enum EUniqueIdEncodingFlags
{
    /** Default, nothing encoded, use normal FString serialization */
    NotEncoded = 0,
    /** Data is optimized based on some assumptions (even number of [0-9][a-f][A-F] that can be packed into nibbles) */
    IsEncoded = (1 << 0),
    /** This unique id is empty or invalid, nothing further to serialize */
    IsEmpty = (1 << 1),
    /** Reserved for future use */
    Unused1 = (1 << 2),
    /** Remaining bits are used for encoding the type without requiring another byte */
    Reserved1 = (1 << 3),
    Reserved2 = (1 << 4),
    Reserved3 = (1 << 5),
    Reserved4 = (1 << 6),
    Reserved5 = (1 << 7),
    /** Helper masks */
    FlagsMask = (Reserved1 - 1),
    TypeMask = (255 ^ FlagsMask)
}