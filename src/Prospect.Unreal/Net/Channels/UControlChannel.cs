using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;

namespace Prospect.Unreal.Net.Channels;

public class UControlChannel : UChannel
{
    public UControlChannel()
    {
        ChType = EChannelType.CHTYPE_Control;
        ChName = UnrealNames.FNames[UnrealNameKey.Control];
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        throw new NotImplementedException();
    }
}