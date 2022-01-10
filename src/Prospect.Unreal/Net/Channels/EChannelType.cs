namespace Prospect.Unreal.Net.Channels;

public enum EChannelType
{
    CHTYPE_None = 0, // Invalid type.
    CHTYPE_Control = 1, // Connection control.
    CHTYPE_Actor = 2, // Actor-update channel.
    CHTYPE_File = 3, // Binary file transfer.
    CHTYPE_Voice = 4, // VoIP data channel
    CHTYPE_MAX = 8, // Maximum.
}