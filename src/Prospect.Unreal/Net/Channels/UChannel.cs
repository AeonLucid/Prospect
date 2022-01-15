using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net.Channels;

public abstract class UChannel
{
    private static readonly ILogger Logger = Log.ForContext<UChannel>();
    
    private const int NetMaxConstructedPartialBunchSizeBytes = 1024 * 64;

    /// <summary>
    ///     Owner connection.
    /// </summary>
    public UNetConnection? Connection { get; set; }

    /// <summary>
    ///     If OpenedLocally is true, this means we have acknowledged the packet we sent the bOpen bunch on.
    ///     Otherwise, it means we have received the bOpen bunch from the server.
    /// </summary>
    public bool OpenAcked { get; set; }

    /// <summary>
    ///     State of the channel.
    /// </summary>
    public bool Closing { get; set; }

    /// <summary>
    ///     Channel is going dormant (it will close but the client will not destroy).
    /// </summary>
    public bool Dormant { get; set; }

    /// <summary>
    ///     Replication is being paused, but channel will not be closed.
    /// </summary>
    public bool bIsReplicationPaused { get; set; }

    /// <summary>
    ///     Opened temporarily.
    /// </summary>
    public bool OpenTemporary { get; set; }

    /// <summary>
    ///     Has encountered errors and is ignoring subsequent packets.
    /// </summary>
    public bool Broken { get; set; }

    /// <summary>
    ///     Actor associated with this channel was torn off.
    /// </summary>
    public bool bTornOff { get; set; }

    /// <summary>
    ///     Channel wants to go dormant (it will check during tick if it can go dormant).
    /// </summary>
    public bool bPendingDormancy { get; set; }

    /// <summary>
    ///     Channel wants to go dormant, and is otherwise ready to become dormant, but is waiting for a timeout before doing so.
    /// </summary>
    public bool bIsInDormancyHysteresis { get; set; }

    /// <summary>
    ///     Unreliable property replication is paused until all reliables are ack'd.
    /// </summary>
    public bool bPausedUntilReliableACK { get; set; }

    /// <summary>
    ///     Set when sending closing bunch to avoid recursion in send-failure-close case.
    /// </summary>
    public bool SentClosingBunch { get; set; }

    /// <summary>
    ///     Set when placed in the actor channel pool
    /// </summary>
    public bool bPooled { get; set; }

    /// <summary>
    ///     Whether channel was opened locally or by remote.
    /// </summary>
    public bool OpenedLocally { get; set; }

    /// <summary>
    ///     Whether channel was opened by replay checkpoint recording
    /// </summary>
    public bool bOpenedForCheckpoint { get; set; }

    /// <summary>
    ///     Index of this channel.
    /// </summary>
    public int ChIndex { get; set; }

    /// <summary>
    ///     If OpenedLocally is true, this is the packet we sent the bOpen bunch on.
    ///     Otherwise, it's the packet we received the bOpen bunch on.
    /// </summary>
    public FPacketIdRange OpenPacketId { get; set; }

    /// <summary>
    ///     Type of this channel.
    /// </summary>
    public EChannelType ChType { get; set; }

    /// <summary>
    ///     Name of the type of this channel.
    /// </summary>
    public FName ChName { get; set; }

    /// <summary>
    ///     Number of packets in InRec.
    /// </summary>
    public int NumInRec { get; set; }

    /// <summary>
    ///     Number of packets in OutRec.
    /// </summary>
    public int NumOutRec { get; set; }

    /// <summary>
    ///     Incoming data with queued dependencies.
    /// </summary>
    public FInBunch? InRec { get; set; }

    /// <summary>
    ///     Outgoing reliable unacked data.
    /// </summary>
    public FOutBunch? OutRec { get; set; }

    /// <summary>
    ///     Partial bunch we are receiving (incoming partial bunches are appended to this).
    /// </summary>
    public FInBunch? InPartialBunch { get; set; }

    public virtual void Init(UNetConnection inConnection, int inChIndex, EChannelCreateFlags createFlags)
    {
        Connection = inConnection;
        ChIndex = inChIndex;
        OpenedLocally = (createFlags & EChannelCreateFlags.OpenedLocally) != 0;
        OpenPacketId = new FPacketIdRange();
        bPausedUntilReliableACK = false;
        SentClosingBunch = false;
    }

    public virtual void Tick()
    {
        // TODO: Dormancy
    }

    public virtual bool CanStopTicking()
    {
        return !bPendingDormancy;
    }

    public void ReceivedRawBunch(FInBunch bunch, out bool bOutSkipAck)
    {
        bOutSkipAck = false;

        // Immediately consume the NetGUID portion of this bunch, regardless if it is partial or reliable.
        // NOTE - For replays, we do this even earlier, to try and load this as soon as possible, in case there is an issue creating the channel
        // If a replay fails to create a channel, we want to salvage as much as possible
        if (bunch.bHasPackageMapExports && !Connection!.IsInternalAck())
        {
            throw new NotImplementedException();
        }

        if (Connection!.IsInternalAck() && Broken)
        {
            return;
        }

        if (bunch.bReliable && bunch.ChSequence != Connection.InReliable[ChIndex] + 1)
        {
            if (Connection.IsInternalAck())
            {
                throw new UnrealNetException("Shouldn't hit this path on 100% reliable connections");
            }

            if (bunch.ChSequence <= Connection.InReliable[ChIndex])
            {
                throw new UnrealNetException("Invalid bunch");
            }

            // TODO: (InRec) Queue
            throw new NotImplementedException();
        }
        else
        {
            var bDeleted = ReceivedNextBunch(bunch, out bOutSkipAck);

            if (bunch.IsError())
            {
                Logger.Error("Bunch.IsError() after ReceivedNextBunch 1");
                return;
            }

            if (bDeleted)
            {
                return;
            }
            
            // TODO: (InRec) Dispatch waiting bunches
            while (InRec != null)
            {
                throw new NotImplementedException();
            }
        }
    }

    private bool ReceivedNextBunch(FInBunch bunch, out bool bOutSkipAck)
    {
        bOutSkipAck = false;
        
        // We received the next bunch. Basically at this point:
        //	-We know this is in order if reliable
        //	-We dont know if this is partial or not
        // If its not a partial bunch, of it completes a partial bunch, we can call ReceivedSequencedBunch to actually handle it

        // Note this bunch's retirement.
        if (bunch.bReliable)
        {
            // Reliables should be ordered properly at this point
            if (bunch.ChSequence != Connection.InReliable[bunch.ChIndex] + 1)
            {
                throw new UnrealNetException("Reliables should be ordered properly at this point");
            }

            Connection.InReliable[bunch.ChIndex] = bunch.ChSequence;
        }

        var handleBunch = bunch;
        
        if (bunch.bPartial)
        {
            handleBunch = null;
            
            if (bunch.bPartialInitial)
            {
                // Create new InPartialBunch if this is the initial bunch of a new sequence.
                if (InPartialBunch != null)
                {
                    if (!InPartialBunch.bPartialFinal)
                    {
                        if (InPartialBunch.bReliable)
                        {
                            if (bunch.bReliable)
                            {
                                Logger.Warning("Reliable partial trying to destroy reliable partial 1");
                                bunch.SetError();
                                return false;
                            }
                            
                            Logger.Information("Unreliable partial trying to destroy reliable partial 1");
                            bOutSkipAck = true;
                            return false;
                        }
                        
                        // We didn't complete the last partial bunch - this isn't fatal since they can be unreliable, but may want to log it.
                        Logger.Verbose("Incomplete partial bunch. Channel: {ChIndex} ChSequence: {ChSequence}", InPartialBunch.ChIndex, InPartialBunch.ChSequence);
                    }

                    InPartialBunch = null;
                }
                
                InPartialBunch = new FInBunch(bunch, false);

                if (!bunch.bHasPackageMapExports && bunch.GetBitsLeft() > 0)
                {
                    if (bunch.GetBitsLeft() % 8 != 0)
                    {
                        Logger.Warning("Corrupt partial bunch. Initial partial bunches are expected to be byte-aligned. BitsLeft = {BitCount}", bunch.GetBitsLeft());
                        bunch.SetError();
                        return false;
                    }

                    InPartialBunch.AppendDataFromChecked(bunch.GetBufferPosChecked(), bunch.GetBuffer(), bunch.GetBitsLeft());
                    
                    Log.Verbose("Received new partial bunch");
                }
                else
                {
                    Log.Verbose("Received New partial bunch. It only contained NetGUIDs");
                }
            }
            else
            {
                // Merge in next partial bunch to InPartialBunch if:
                //	-We have a valid InPartialBunch
                //	-The current InPartialBunch wasn't already complete
                //  -ChSequence is next in partial sequence
                //	-Reliability flag matches

                var bSequenceMatches = false;
                
                if (InPartialBunch != null)
                {
                    var bReliableSequencesMatches = bunch.ChSequence == InPartialBunch.ChSequence + 1;
                    var bUnreliableSequenceMatches = bReliableSequencesMatches || bunch.ChSequence == InPartialBunch.ChSequence;
                    
                    // Unreliable partial bunches use the packet sequence, and since we can merge multiple bunches into a single packet,
                    // it's perfectly legal for the ChSequence to match in this case.
                    // Reliable partial bunches must be in consecutive order though
                    bSequenceMatches = InPartialBunch.bReliable ? bReliableSequencesMatches : bUnreliableSequenceMatches;
                }

                if (InPartialBunch != null && !InPartialBunch.bPartialFinal && bSequenceMatches && InPartialBunch.bReliable == bunch.bReliable)
                {
                    // Merge.
                    Logger.Verbose("Merging Partial Bunch: {BytesLeft} Bytes", bunch.GetBytesLeft());

                    if (!bunch.bHasPackageMapExports && bunch.GetBitsLeft() > 0)
                    {
                        // TODO: Check if works.
                        InPartialBunch.AppendDataFromChecked(bunch.GetBufferPosChecked(), bunch.GetBuffer(), bunch.GetBitsLeft());
                    }
                    
                    // Only the final partial bunch should ever be non byte aligned. This is enforced during partial bunch creation
                    // This is to ensure fast copies/appending of partial bunches. The final partial bunch may be non byte aligned.
                    if (!bunch.bHasPackageMapExports && !bunch.bPartialFinal && bunch.GetBitsLeft() % 8 != 0)
                    {
                        Logger.Warning("Corrupt partial bunch. Non-final partial bunches are expected to be byte-aligned. bHasPackageMapExports = {HasPackageMapExports}, bPartialFinal = {PartialFinal}, BitsLeft = {BitsLeft}",
                            bunch.bHasPackageMapExports ? 1 : 0,
                            bunch.bPartialFinal ? 1 : 0,
                            bunch.GetBitsLeft());
                        bunch.SetError();
                        return false;
                    }
                    
                    // Advance the sequence of the current partial bunch so we know what to expect next
                    InPartialBunch.ChSequence = bunch.ChSequence;

                    if (bunch.bPartialFinal)
                    {
                        Logger.Verbose("Completed Partial Bunch ({BytesLeft} left)", bunch.GetBytesLeft());

                        if (bunch.bHasPackageMapExports)
                        {
                            // Shouldn't have these, they only go in initial partial export bunches
                            Logger.Warning("Corrupt partial bunch. Final partial bunch has package map exports");
                            bunch.SetError();
                            return false;
                        }

                        handleBunch = InPartialBunch;

                        InPartialBunch.bPartialFinal = true;
                        InPartialBunch.bClose = bunch.bClose;
                        InPartialBunch.bDormant = bunch.bDormant;
                        InPartialBunch.CloseReason = bunch.CloseReason;
                        InPartialBunch.bIsReplicationPaused = bunch.bIsReplicationPaused;
                        InPartialBunch.bHasMustBeMappedGUIDs = bunch.bHasMustBeMappedGUIDs;
                    }
                    else
                    {
                        Logger.Verbose("Received Partial Bunch");
                    }
                }
                else
                {
                    // Merge problem - delete InPartialBunch.
                    // This is mainly so that in the unlikely chance that ChSequence wraps around, we wont merge two completely separate partial bunches.
                    // We shouldn't hit this path on 100% reliable connections

                    if (Connection.IsInternalAck())
                    {
                        throw new UnrealNetException("We shouldn't hit this path on 100% reliable connections");
                    }

                    bOutSkipAck = true; // Don't ack the packet, since we didn't process the bunch

                    if (InPartialBunch != null && InPartialBunch.bReliable)
                    {
                        if (bunch.bReliable)
                        {
                            Logger.Warning("Reliable partial trying to destroy reliable partial 2");
                            bunch.SetError();
                            return false;
                        }
                        
                        Logger.Warning("Unreliable partial trying to destroy reliable partial 2");
                        return false;
                    }

                    if (InPartialBunch != null)
                    {
                        InPartialBunch = null;
                    }
                }
            }
            
            // Fairly large number, and probably a bad idea to even have a bunch this size, but want to be safe for now and not throw out legitimate data
            if (IsBunchTooLarge(Connection, InPartialBunch))
            {
                Logger.Error("Received a partial bunch exceeding max allowed size. BunchSize={Size}, MaximumSize={MaxSize}", InPartialBunch!.GetNumBytes(), NetMaxConstructedPartialBunchSizeBytes);
                bunch.SetError();
                return false;
            }
        }

        if (handleBunch != null)
        {
            var bBothSidesCanOpen = Connection.Driver != null &&
                                    Connection.Driver.ChannelDefinitionMap[ChName].ServerOpen &&
                                    Connection.Driver.ChannelDefinitionMap[ChName].ClientOpen;
                
            if (handleBunch.bOpen)
            {
                // Voice channels can open from both side simultaneously, so ignore this logic until we resolve this
                if (!bBothSidesCanOpen)
                {
                    // If we opened the channel, we shouldn't be receiving bOpen commands from the other side
                    if (OpenedLocally)
                    {
                        throw new UnrealNetException("Received channel open command for channel that was already opened locally.");
                    }

                    if (OpenPacketId.First != UnrealConstants.IndexNone || OpenPacketId.Last != UnrealConstants.IndexNone)
                    {
                        Logger.Error("This should be the first and only assignment of the packet range (we should only receive one bOpen bunch)");
                        bunch.SetError();
                        return false;
                    }
                }

                // Remember the range.
                // In the case of a non partial, HandleBunch == Bunch
                // In the case of a partial, HandleBunch should == InPartialBunch, and Bunch should be the last bunch.
                OpenPacketId = new FPacketIdRange(handleBunch.PacketId, bunch.PacketId);
                OpenAcked = true;

                Logger.Verbose("ReceivedNextBunch: Channel now fully open. ChIndex: {ChIndex}, OpenPacketId.First: {First}, OpenPacketId.Last: {Last}", ChIndex, OpenPacketId.First, OpenPacketId.Last);
            }

            // Voice channels can open from both side simultaneously, so ignore this logic until we resolve this
            if (!bBothSidesCanOpen)
            {
                // Don't process any packets until we've fully opened this channel 
                // (unless we opened it locally, in which case it's safe to process packets)
                if (!OpenedLocally && !OpenAcked)
                {
                    if (handleBunch.bReliable)
                    {
                        Logger.Error("ReceivedNextBunch: Reliable bunch before channel was fully open");
                        bunch.SetError();
                        return false;
                    }

                    if (Connection.IsInternalAck())
                    {
                        // Shouldn't be possible for 100% reliable connections
                        Broken = true;
                        return false;
                    }

                    // Don't ack this packet (since we won't process all of it)
                    bOutSkipAck = true;
                    
                    Logger.Verbose("ReceivedNextBunch: Skipping bunch since channel isn't fully open. ChIndex: {ChIndex}", ChIndex);
                    return false;
                }

                // At this point, we should have the open packet range
                // This is because if we opened the channel locally, we set it immediately when we sent the first bOpen bunch
                // If we opened it from a remote connection, then we shouldn't be processing any packets until it's fully opened (which is handled above)
                if (OpenPacketId.First == -1)
                {
                    throw new UnrealNetException("Should have open packet range.");
                }

                if (OpenPacketId.Last == -1)
                {
                    throw new UnrealNetException("Should have open packet range.");
                }
            }
            
            // Receive it in sequence.
            return ReceivedSequencedBunch(handleBunch);
        }

        return false;
    }

    private bool ReceivedSequencedBunch(FInBunch bunch)
    {
        // Handle a regular bunch.
        if (!Closing)
        {
            ReceivedBunch(bunch);
        }
        
        // We have fully received the bunch, so process it.
        if (bunch.bClose)
        {
            Dormant = bunch.bDormant || (bunch.CloseReason == EChannelCloseReason.Dormancy);

            if (InRec != null)
            {
                Logger.Warning("Close Anomaly {Seq} / {InRecSeq}", bunch.ChSequence, InRec.ChSequence);
            }

            if (ChIndex == 0)
            {
                Logger.Debug("UChannel::ReceivedSequencedBunch: Bunch.bClose == true. ChIndex == 0. Calling ConditionalCleanUp");   
            }
            
            ConditionalCleanUp(false, bunch.CloseReason);
            return true;
        }

        return false;
    }

    protected abstract void ReceivedBunch(FInBunch bunch);

    public virtual FPacketIdRange SendBunch(FOutBunch bunch, bool merge)
    {
        if (Connection == null || Connection.Driver == null)
        {
            throw new UnrealNetException();
        }
        
        if (ChIndex == -1)
        {
            return new FPacketIdRange(UnrealConstants.IndexNone);
        }

        if (IsBunchTooLarge(Connection!, bunch))
        {
            Logger.Error("Attempted to send bunch exceeding max allowed size. BunchSize={Size}, MaximumSize={MaxSize}", bunch.GetNumBytes(), NetMaxConstructedPartialBunchSizeBytes);
            bunch.SetError();
            return new FPacketIdRange(UnrealConstants.IndexNone);
        }

        if (Closing || Connection.Channels[ChIndex] != this || bunch.IsError() || bunch.bHasPackageMapExports)
        {
            throw new UnrealNetException();
        }

        // Set bunch flags.
        var bDormancyClose = bunch.bClose && (bunch.CloseReason == EChannelCloseReason.Dormancy);

        if (OpenedLocally && ((OpenPacketId.First == UnrealConstants.IndexNone) || ((Connection.ResendAllDataState == EResendAllDataState.None) && !bDormancyClose)))
        {
            var bOpenBunch = true;

            if (Connection.ResendAllDataState == EResendAllDataState.SinceCheckpoint)
            {
                bOpenBunch = !bOpenedForCheckpoint;
                bOpenedForCheckpoint = true;
            }

            if (bOpenBunch)
            {
                bunch.bOpen = true;
                OpenTemporary = !bunch.bReliable;
            }
        }

        if (OpenTemporary && bunch.bReliable)
        {
            throw new UnrealNetException("Channel was opened temporarily, we are never allowed to send reliable packets on it");
        }

        // This is the max number of bits we can have in a single bunch
        var MAX_SINGLE_BUNCH_SIZE_BITS = Connection.GetMaxSingleBunchSizeBits();

        // Max bytes we'll put in a partial bunch
        var MAX_SINGLE_BUNCH_SIZE_BYTES = MAX_SINGLE_BUNCH_SIZE_BITS / 8;

        // Max bits will put in a partial bunch (byte aligned, we dont want to deal with partial bytes in the partial bunches)
        var MAX_PARTIAL_BUNCH_SIZE_BITS = MAX_SINGLE_BUNCH_SIZE_BYTES * 8;

        var outgoingBunches = new List<FOutBunch>();

        // Add any export bunches
        // Replay connections will manage export bunches separately.
        if (!Connection.IsInternalAck())
        {
            // TODO: AppendExportBunches
        }

        if (outgoingBunches.Count != 0)
        {
            // Don't merge if we are exporting guid's
            // We can't be for sure if the last bunch has exported guids as well, so this just simplifies things
            merge = false;
        }

        if (Connection.Driver.IsServer())
        {
            // Append any "must be mapped" guids to front of bunch from the packagemap
            // TODO: AppendMustBeMappedGuids

            if (bunch.bHasMustBeMappedGUIDs)
            {
                merge = false;
            }
        }

        //-----------------------------------------------------
        // Contemplate merging.
        //-----------------------------------------------------
        
        // TODO: Merge
        // var preExistingBits = 0;
        FOutBunch? outBunch = null;
        
        // if (merge
        //     && Connection.LastOut.ChIndex == bunch.ChIndex
        //     && Connection.LastOut.bReliable == bunch.bReliable
        //     && Connection.AllowMerge
        //     && Connection.LastEnd.GetNumBits() != 0
        //     && Connection.LastEnd.GetNumBits() == Connection.SendBuffer.GetNumBits()
        //     && Connection.LastOut.GetNumBits() + bunch.GetNumBits() <= MAX_SINGLE_BUNCH_SIZE_BITS)
        // {
        //     
        // }

        //-----------------------------------------------------
        // Possibly split large bunch into list of smaller partial bunches
        //-----------------------------------------------------
        if (bunch.GetNumBits() > MAX_SINGLE_BUNCH_SIZE_BITS)
        {
            var data = bunch.GetData().AsSpan();
            var bitsLeft = bunch.GetNumBits();
            
            merge = false;

            while (bitsLeft > 0)
            {
                var partialBunch = new FOutBunch(this, false);
                var bitsThisBunch = (long) Math.Min(bitsLeft, MAX_PARTIAL_BUNCH_SIZE_BITS);
                
                partialBunch.SerializeBits(data, bitsThisBunch);
                
                outgoingBunches.Add(partialBunch);

                bitsLeft -= bitsThisBunch;
                data = data.Slice((int)(bitsThisBunch >> 3));
                
                Logger.Debug("Making partial bunch from content bunch. bitsThisBunch: {Bits} bitsLeft: {Left}", bitsThisBunch, bitsLeft);
            }
        }
        else
        {
            outgoingBunches.Add(bunch);
        }

        //-----------------------------------------------------
        // Send all the bunches we need to
        //	Note: this is done all at once. We could queue this up somewhere else before sending to Out.
        //-----------------------------------------------------
        var packetIdRange = new FPacketIdRange();
        var bOverflowsReliable = (NumOutRec + outgoingBunches.Count >= UNetConnection.ReliableBuffer + (bunch.bClose ? 1 : 0));

        if (bunch.bReliable && bOverflowsReliable)
        {
            // TODO: Send NMT_Failure
            // TODO: FlushNet(true);
            Connection.Close();

            throw new NotImplementedException();
            return packetIdRange;
        }

        if (outgoingBunches.Count > 1)
        {
            Logger.Debug("Sending {Count} bunches. Channel: {ChIndex} {Channel}", outgoingBunches.Count, bunch.ChIndex, this);
        }

        for (var partialNum = 0; partialNum < outgoingBunches.Count; partialNum++)
        {
            var nextBunch = outgoingBunches[partialNum];

            nextBunch.bReliable = bunch.bReliable;
            nextBunch.bOpen = bunch.bOpen;
            nextBunch.bClose = bunch.bClose;
            nextBunch.bDormant = bunch.bDormant;
            nextBunch.CloseReason = bunch.CloseReason;
            nextBunch.bIsReplicationPaused = bunch.bIsReplicationPaused;
            nextBunch.ChIndex = bunch.ChIndex;
            nextBunch.ChType = bunch.ChType;
            nextBunch.ChName = bunch.ChName;

            if (!nextBunch.bHasPackageMapExports)
            {
                nextBunch.bHasMustBeMappedGUIDs |= bunch.bHasMustBeMappedGUIDs;
            }

            if (outgoingBunches.Count > 1)
            {
                nextBunch.bPartial = true;
                nextBunch.bPartialInitial = partialNum == 0;
                nextBunch.bPartialFinal = partialNum == outgoingBunches.Count - 1;
                nextBunch.bOpen &= partialNum == 0;                                             // Only the first bunch should have the bOpen bit set
                nextBunch.bClose = (bunch.bClose && (outgoingBunches.Count - 1 == partialNum)); // Only last bunch should have bClose bit set
            }

            var thisOutBunch = PrepBunch(nextBunch, ref outBunch, merge);

            // Update Packet Range
            var packetId = SendRawBunch(thisOutBunch, merge);
            if (partialNum == 0)
            {
                packetIdRange = new FPacketIdRange(packetId);
            }
            else
            {
                packetIdRange = new FPacketIdRange(packetIdRange.First, packetId);
            }

            // Update channel sequence count.
            Connection.LastOut = thisOutBunch;
            Connection.LastEnd = new FBitWriterMark(Connection.SendBuffer);
        }
        
        // Update open range if necessary
        if (bunch.bOpen && (Connection.ResendAllDataState == EResendAllDataState.None))
        {
            OpenPacketId = packetIdRange;
        }

        // Destroy outgoing bunches now that they are sent, except the one that was passed into ::SendBunch
        //	This is because the one passed in ::SendBunch is the responsibility of the caller, the other bunches in OutgoingBunches
        //	were either allocated in this function for partial bunches, or taken from the package map, which expects us to destroy them.
        foreach (var deleteBunch in outgoingBunches)
        {
            if (deleteBunch != bunch)
            {
                deleteBunch.Dispose();
            }
        }

        return packetIdRange;
    }

    private int SendRawBunch(FOutBunch outBunch, bool merge)
    {
        // Send the raw bunch.
        outBunch.ReceivedAck = false;
        
        var packetId = Connection!.SendRawBunch(outBunch, merge);
        
        if (OpenPacketId.First == UnrealConstants.IndexNone && OpenedLocally)
        {
            OpenPacketId = new FPacketIdRange(packetId);
        }

        if (outBunch.bClose)
        {
            SetClosingFlag();
        }
        
        return packetId;
    }

    private FOutBunch PrepBunch(FOutBunch bunch, ref FOutBunch? outBunch, bool merge)
    {
        if (Connection!.ResendAllDataState != EResendAllDataState.None)
        {
            return bunch;
        }
        
        // Find outgoing bunch index.
        if (bunch.bReliable)
        {
            // Find spot, which was guaranteed available by FOutBunch constructor.
            if (outBunch == null)
            {
                if (!(NumOutRec < UNetConnection.ReliableBuffer - 1 + (bunch.bClose ? 1 : 0)))
                {
                    Logger.Warning("PrepBunch: Reliable buffer overflow! {Channel}", this);
                }
                
                bunch.Next = null;
                bunch.ChSequence = ++Connection.OutReliable[ChIndex];
                NumOutRec++;
                // TODO: Verify below works as expected
                outBunch = new FOutBunch(bunch);
                var outLink = OutRec;
                
                while (outLink != null)
                {
                    outLink = outLink.Next;
                }

                if (outLink == null)
                {
                    OutRec = outBunch;
                }
                else
                {
                    outLink.Next = outBunch;
                }
            }
            else
            {
                bunch.Next = outBunch.Next;
                outBunch = bunch;
            }

            Connection.LastOutBunch = outBunch;
        }
        else
        {
            outBunch = bunch;
            Connection.LastOutBunch = null;
        }

        return outBunch;
    }

    private void SetClosingFlag()
    {
        Closing = true;
    }

    public void ConditionalCleanUp(bool bForDestroy, EChannelCloseReason closeReason)
    {
        throw new NotImplementedException();
    }

    private static bool IsBunchTooLarge(UNetConnection connection, FInBunch? bunch)
    {
        return !connection.IsInternalAck() && bunch != null && bunch.GetNumBytes() > NetMaxConstructedPartialBunchSizeBytes;
    }

    private static bool IsBunchTooLarge(UNetConnection connection, FOutBunch? bunch)
    {
        return !connection.IsInternalAck() && bunch != null && bunch.GetNumBytes() > NetMaxConstructedPartialBunchSizeBytes;
    }
}