namespace Prospect.Unreal.Net.Channels;

public static class EChannelType
{
    public const int CHTYPE_None = 0; // Invalid type.
    public const int CHTYPE_Control = 1; // Connection control.
    public const int CHTYPE_Actor = 2; // Actor-update channel.

    public const int CHTYPE_File = 3; // Binary file transfer.

    public const int CHTYPE_Voice = 4; // VoIP data channel
    public const int CHTYPE_MAX = 8; // Maximum.
}