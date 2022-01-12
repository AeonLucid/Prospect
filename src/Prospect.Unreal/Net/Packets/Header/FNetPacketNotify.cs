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
    private uint _writtenHistoryWordCount;
    private SequenceNumber _writtenInAckSeq;
    
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

    public SequenceNumber GetInSeq()
    {
        return _inSeq;
    }

    public SequenceNumber GetInAckSeq()
    {
        return _inAckSeq;
    }

    public SequenceNumber GetOutSeq()
    {
        return _outSeq;
    }

    public SequenceNumber GetOutAckSeq()
    {
        return _outAckSeq;
    }

    /// <summary>
    ///     These methods must always write and read the exact same number of bits, that is the reason for not using WriteInt/WrittedWrappedInt
    /// </summary>
    public bool WriteHeader(FBitWriter writer, bool bRefresh)
    {
        // we always write at least 1 word
        var currentHistoryWorkCount = Math.Clamp((GetCurrentSequenceHistoryLength() + SequenceHistory.BitsPerWord - 1) / SequenceHistory.BitsPerWord, 1, SequenceHistory.WordCount);
        
        // We can only do a refresh if we do not need more space for the history
        if (bRefresh && (currentHistoryWorkCount > _writtenHistoryWordCount))
        {
            return false;
        }
        
        // How many words of ack data should we write? If this is a refresh we must write the same size as the original header
        _writtenHistoryWordCount = bRefresh ? _writtenHistoryWordCount : currentHistoryWorkCount;
        
        // This is the last InAck we have acknowledged at this time
        _writtenInAckSeq = _inAckSeq;

        var seq = _outSeq;
        var ackedSeq = _inAckSeq;
        
        // Pack data into a uint
        var packedHeader = FPackedHeader.Pack(seq, ackedSeq, _writtenHistoryWordCount - 1);
        
        // Write packed header
        writer.WriteUInt32(packedHeader);
        
        // Write ack history
        _inSeqHistory.Write(writer, _writtenHistoryWordCount);

        return true;
    }

    private uint GetCurrentSequenceHistoryLength()
    {
        if (_inAckSeq.GreaterEq(_inAckSeqAck))
        {
            return (uint) SequenceNumber.Diff(_inAckSeq, _inAckSeqAck);
        }

        return SequenceHistory.Size;
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
            Logger.Verbose("Update - Seq {Seq}, InSeq {InSeq}", notificationData.Seq.Value, _inSeq.Value);

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

    /// <summary>
    ///     Mark Seq as received and update current InSeq, missing sequence numbers will be marked as lost
    /// </summary>
    public void AckSeq(SequenceNumber seq)
    {
        AckSeq(seq, true);
    }

    /// <summary>
    ///     Explicitly mark Seq as not received and update current InSeq, additional missing sequence numbers will be marked as lost
    /// </summary>
    public void NakSeq(SequenceNumber seq)
    {
        AckSeq(seq, false);
    }

    private void AckSeq(SequenceNumber ackedSeq, bool isAck)
    {
        while (ackedSeq.Greater(_inAckSeq))
        {
            _inAckSeq.IncrementAndGet();

            var bReportAcked = _inAckSeq.Equals(ackedSeq) ? isAck : false;
            
            Logger.Verbose("AckSeq - AckedSeq: {Seq}, IsAck {IsAck}", _inAckSeq.Value, bReportAcked);

            _inSeqHistory.AddDeliveryStatus(bReportAcked);
        }
    }
    
    public SequenceNumber CommitAndIncrementOutSeq()
    {
        // Add entry to the ack-record so that we can update the InAckSeqAck when we received the ack for this OutSeq.
        _ackRecord.Enqueue(new FSentAckData(_outSeq, _writtenInAckSeq));
        _writtenHistoryWordCount = 0;

        return _outSeq.IncrementAndGet();
    }
}