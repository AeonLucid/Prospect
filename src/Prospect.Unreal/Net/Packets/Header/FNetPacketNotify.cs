using Prospect.Unreal.Net.Packets.Header.Sequence;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net.Packets.Header;

public delegate void HandlePacketNotification(SequenceNumber ackedSequence, bool delivered);

public class FNetPacketNotify
{
    private static readonly ILogger Logger = Log.ForContext<FNetPacketNotify>();
    
    public const int SequenceNumberBits = 14;
    public const int MaxSequenceHistoryLength = 256;

    private readonly Queue<FSentAckData> _ackRecord = new Queue<FSentAckData>();

    private readonly SequenceHistory _inSeqHistory = new SequenceHistory();
    private SequenceNumber _inSeq;
    private SequenceNumber _inAckSeq;
    private SequenceNumber _inAckSeqAck;

    private SequenceNumber _outSeq;
    private SequenceNumber _outAckSeq;

    public void Init(SequenceNumber initialInSeq, SequenceNumber initialOutSeq)
    {
        _inSeqHistory.Reset();
        _inSeq = initialInSeq;
        _inAckSeq = initialInSeq;
        _inAckSeqAck = initialInSeq;
        _outSeq = initialOutSeq;
        _outAckSeq = new SequenceNumber((ushort)(initialOutSeq.Value - 1));
    }

    public bool ReadHeader(ref FNotificationHeader data, FBitReader reader)
    {
        var packedHeader = reader.ReadUInt32();

        data.Seq = FPackedHeader.GetSeq(packedHeader);
        data.AckedSeq = FPackedHeader.GetAckedSeq(packedHeader);
        data.HistoryWordCount = FPackedHeader.GetHistoryWordCount(packedHeader) + 1;
        data.History = new SequenceHistory();
        data.History.Read(reader, data.HistoryWordCount);
        
        return !reader.IsError();
    }

    public int GetSequenceDelta(FNotificationHeader notificationData)
    {
        if (!notificationData.Seq.Greater(_inSeq))
        {
            return 0;
        }

        if (!notificationData.AckedSeq.GreaterEq(_outAckSeq))
        {
            return 0;
        }

        if (!_outSeq.Greater(notificationData.AckedSeq))
        {
            return 0;
        }
        
        return SequenceNumber.Diff(notificationData.Seq, _inSeq);
    }

    public int Update(FNotificationHeader notificationData, HandlePacketNotification func)
    {
        var inSeqDelta = GetSequenceDelta(notificationData);
        if (inSeqDelta > 0)
        {
            Logger.Verbose("FNetPacketNotify::Update - Seq {Seq}, InSeq {InSeq}", notificationData.Seq.Value, _inSeq.Value);

            ProcessReceivedAcks(notificationData, func);

            _inSeq = notificationData.Seq;
            
            return inSeqDelta;
        }

        return 0;
    }

    private void ProcessReceivedAcks(FNotificationHeader notificationData, HandlePacketNotification func)
    {
        if (notificationData.AckedSeq.Greater(_outAckSeq))
        {
            Logger.Verbose("ProcessReceivedAcks - AckedSeq {AckedSeq}, OutAckSeq {OutAckSeq}", notificationData.AckedSeq.Value, _outAckSeq.Value);

            var ackCount = SequenceNumber.Diff(notificationData.AckedSeq, _outAckSeq);

            // Update InAckSeqAck used to track the needed number of bits to transmit our ack history
            _inAckSeqAck = UpdateInAckSeqAck(ackCount, notificationData.AckedSeq);

            // ExpectedAck = OutAckSeq + 1
            var currentAck = new SequenceNumber(_outAckSeq.Value).IncrementAndGet();

            if (ackCount > SequenceHistory.Size)
            {
                Logger.Warning("ProcessReceivedAcks - Missed Acks");
            }
            
            // Everything not found in the history buffer is treated as lost
            while (ackCount > SequenceHistory.Size)
            {
                --ackCount;
                func(currentAck, false);
                currentAck = currentAck.IncrementAndGet();
            }
            
            // For sequence numbers contained in the history we lookup the delivery status from the history
            while (ackCount > 0)
            {
                --ackCount;
                func(currentAck, notificationData.History.IsDelivered(ackCount));
                currentAck = currentAck.IncrementAndGet();
            }

            _outAckSeq = notificationData.AckedSeq;
        }
    }

    private SequenceNumber UpdateInAckSeqAck(int ackCount, SequenceNumber ackedSeq)
    {
        if (ackCount <= _ackRecord.Count)
        {
            if (ackCount > 1)
            {
                for (var i = 0; i < ackCount - 1; i++)
                {
                    _ackRecord.Dequeue();
                }
            }

            var ackData = _ackRecord.Dequeue();
            if (ackData.OutSeq.Equals(ackedSeq))
            {
                return ackData.InAckSeq;
            }
        }

        return new SequenceNumber((ushort)(ackedSeq.Value - MaxSequenceHistoryLength));
    }
}