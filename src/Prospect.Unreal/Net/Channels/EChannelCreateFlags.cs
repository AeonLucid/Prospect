namespace Prospect.Unreal.Net.Channels;

[Flags]
public enum EChannelCreateFlags
{
    None			= (1 << 0),
    OpenedLocally	= (1 << 1)
}