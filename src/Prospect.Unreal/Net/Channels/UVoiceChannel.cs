using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;

namespace Prospect.Unreal.Net.Channels;

public class UVoiceChannel : UChannel
{
    public UVoiceChannel()
    {
        ChType = EChannelType.CHTYPE_Voice;
        ChName = UnrealNames.FNames[UnrealNameKey.Control];
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        throw new NotImplementedException();
    }
}