using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;

namespace Prospect.Unreal.Net.Channels.Actor;

public class UActorChannel : UChannel
{
    public UActorChannel()
    {
        ChType = EChannelType.CHTYPE_Actor;
        ChName = EName.Actor;
        // bClearRecentActorRefs = true;
        // bHoldQueuedExportBunchesAndGUIDs = false;
        // QueuedCloseReason = EChannelCloseReason::Destroyed;
    }

    public override void Tick()
    {
        base.Tick();
        // TODO: ProcessQueuedBunches
    }

    public override bool CanStopTicking()
    {
        return base.CanStopTicking() /* PendingGuidResolves / QueuedBunches */;
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        throw new NotImplementedException();
    }
}