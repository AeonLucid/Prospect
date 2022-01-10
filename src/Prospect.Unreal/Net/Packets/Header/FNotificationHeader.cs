using Prospect.Unreal.Net.Packets.Header.Sequence;

namespace Prospect.Unreal.Net.Packets.Header;

public ref struct FNotificationHeader
{
    public SequenceHistory History;
    public uint HistoryWordCount;
    public SequenceNumber Seq;
    public SequenceNumber AckedSeq;
}