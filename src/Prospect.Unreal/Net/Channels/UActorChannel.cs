using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;

namespace Prospect.Unreal.Net.Channels;

public class UActorChannel : UChannel
{
    public UActorChannel()
    {
        ChType = EChannelType.CHTYPE_Actor;
        ChName = UnrealNames.FNames[UnrealNameKey.Actor];
        // bClearRecentActorRefs = true;
        // bHoldQueuedExportBunchesAndGUIDs = false;
        // QueuedCloseReason = EChannelCloseReason::Destroyed;
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        throw new NotImplementedException();
    }
}