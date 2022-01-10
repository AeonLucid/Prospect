namespace Prospect.Unreal.Net.Channels;

public enum EChannelCloseReason
{
    Destroyed,
    Dormancy,
    LevelUnloaded,
    Relevancy,
    TearOff,
    /* reserved */
    MAX	= 15		// this value is used for serialization, modifying it may require a network version change
}