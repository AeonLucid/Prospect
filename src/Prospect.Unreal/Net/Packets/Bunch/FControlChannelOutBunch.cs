using Prospect.Unreal.Net.Channels;

namespace Prospect.Unreal.Net.Packets.Bunch;

public class FControlChannelOutBunch : FOutBunch
{
    public FControlChannelOutBunch(UChannel inChannel, bool bClose) : base(inChannel, bClose)
    {
        bReliable = true;
    }
}