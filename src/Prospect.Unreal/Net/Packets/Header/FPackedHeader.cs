using Prospect.Unreal.Net.Packets.Header.Sequence;

namespace Prospect.Unreal.Net.Packets.Header;

public class FPackedHeader
{
    public const int HistoryWordCountBits = 4;
    public const int SeqMask = (1 << FNetPacketNotify.SequenceNumberBits) - 1;
    public const int HistoryWordCountMask = (1 << HistoryWordCountBits) - 1;
    public const int AckSeqShift = HistoryWordCountBits;
    public const int SeqShift = AckSeqShift + FNetPacketNotify.SequenceNumberBits;

    public static uint Pack(SequenceNumber seq, SequenceNumber ackedSeq, uint historyWordCount)
    {
        uint packed = 0;

        packed |= (uint)(seq.Value << SeqShift);
        packed |= (uint)(ackedSeq.Value << AckSeqShift);
        packed |= (uint)(historyWordCount & HistoryWordCountMask);

        return packed;
    }
    
    public static SequenceNumber GetSeq(uint packed)
    {
        return new SequenceNumber((ushort)(packed >> SeqShift & SeqMask));
    }

    public static SequenceNumber GetAckedSeq(uint packed)
    {
        return new SequenceNumber((ushort)(packed >> AckSeqShift & SeqMask));
    }

    public static uint GetHistoryWordCount(uint packed)
    {
        return packed & HistoryWordCountMask;
    }
}