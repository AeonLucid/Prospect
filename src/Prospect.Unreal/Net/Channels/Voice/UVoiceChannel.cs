using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;

namespace Prospect.Unreal.Net.Channels.Voice;

public class UVoiceChannel : UChannel
{
    public UVoiceChannel()
    {
        ChType = EChannelType.CHTYPE_Voice;
        ChName = EName.Voice;
    }

    public override void Tick()
    {
        // TODO: Tick
    }

    public override bool CanStopTicking()
    {
        return false;
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        throw new NotImplementedException();
    }
}