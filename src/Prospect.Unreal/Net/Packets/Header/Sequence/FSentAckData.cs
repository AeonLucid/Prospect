namespace Prospect.Unreal.Net.Packets.Header.Sequence;

public readonly struct FSentAckData
{
    public FSentAckData(SequenceNumber outSeq, SequenceNumber inAckSeq)
    {
        OutSeq = outSeq;
        InAckSeq = inAckSeq;
    }

    public SequenceNumber OutSeq { get; }
    public SequenceNumber InAckSeq { get; }
}