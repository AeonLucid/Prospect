using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Header;
using Prospect.Unreal.Net.Packets.Header.Sequence;
using Prospect.Unreal.Net.Player;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net;

public abstract class UNetConnection : UPlayer
{
    private static readonly ILogger Logger = Log.ForContext<UNetConnection>();
    
    public const int ReliableBuffer = 256;
    public const int MaxPacketId = 16384;
    public const int MaxChSequence = 1024;
    public const int MaxBunchHeaderBits = 256;
    public const int MaxPacketSize = 1024;

    public const int DefaultMaxChannelSize = 32767;

    public const int MaxJitterClockTimeValue = 1023;
    public const int NumBitsForJitterClockTimeInHeader = 10;

    public const EEngineNetworkVersionHistory DefaultEngineNetworkProtocolVersion = EEngineNetworkVersionHistory.HISTORY_ENGINENETVERSION_LATEST;
    public const uint DefaultGameNetworkProtocolVersion = 0;

    private HashSet<UChannel> _channelsToTick;
    private List<FBitReader>? _packetOrderCache;
    private int _packetOrderCacheStartIdx;
    private int _packetOrderCacheCount;
    private bool _bFlushingPacketOrderCache;
    
    private bool _bInternalAck;
    private bool _bReplay;

    private double _statUpdateTime;
    private double _lastReceiveTime;
    private double _lastReceiveRealTime;
    private double _lastGoodPacketRealtime;
    private double _lastTime;
    private double _lastSendTime;
    private double _lastTickTime;

    public UNetConnection()
    {
        _channelsToTick = new HashSet<UChannel>();
        _packetOrderCache = null;
        _packetOrderCacheStartIdx = 0;
        _packetOrderCacheCount = 0;
        _bFlushingPacketOrderCache = false;
        _bInternalAck = false;
        _bReplay = false;
        
        Driver = null;
        PackageMap = null;
        OpenChannels = new List<UChannel>();
        MaxPacket = 0;
        Url = new FUrl();
        RemoteAddr = new IPEndPoint(IPAddress.None, 0);
        State = EConnectionState.USOCK_Invalid;
        Handler = null;
        ClientLoginState = EClientLoginState.Invalid;
        ExpectedClientLoginMsgType = 0;
        PacketOverhead = 0;
        InPacketId = -1;
        OutPacketId = 0;
        OutAckPacketId = -1;
        MaxChannelSize = DefaultMaxChannelSize;
        Channels = new UChannel[DefaultMaxChannelSize];
        OutReliable = new int[DefaultMaxChannelSize];
        InReliable = new int[DefaultMaxChannelSize];
        PendingOutRec = new int[DefaultMaxChannelSize];

        EngineNetworkProtocolVersion = DefaultEngineNetworkProtocolVersion;
        GameNetworkProtocolVersion = DefaultGameNetworkProtocolVersion;
        
        PacketNotify = new FNetPacketNotify();
        PacketNotify.Init(
            new SequenceNumber((ushort)InPacketId), 
            new SequenceNumber((ushort)OutPacketId));
    }
    
    /// <summary>
    ///     Owning net driver
    /// </summary>
    public UNetDriver? Driver { get; private set; }
    
    /// <summary>
    ///     Package map between local and remote. (negotiates net serialization)
    /// </summary>
    public UPackageMapClient? PackageMap { get; private set; }
    
    public List<UChannel> OpenChannels { get; private set; }
    
    /// <summary>
    ///     Maximum packet size.
    /// </summary>
    public int MaxPacket { get; private set; }
    
    /// <summary>
    ///     URL of the other side.
    /// </summary>
    public FUrl Url { get; }
    
    /// <summary>
    ///     The remote address of this connection, typically generated from the URL.
    /// </summary>
    public IPEndPoint? RemoteAddr { get; protected set; }
    
    /// <summary>
    ///     State this connection is in.
    /// </summary>
    public EConnectionState State { get; private set; }
    
    /// <summary>
    ///     PacketHandler, for managing layered handler components, which modify packets as they are sent/received
    /// </summary>
    public PacketHandler? Handler { get; private set; }
    
    public EClientLoginState ClientLoginState { get; private set; }
    
    /// <summary>
    ///     Used to determine what the next expected control channel msg type should be from a connecting client
    /// </summary>
    public byte ExpectedClientLoginMsgType { get; private set; }
    
    /// <summary>
    ///     Reference to the PacketHandler component, for managing stateless connection handshakes
    /// </summary>
    public StatelessConnectHandlerComponent? StatelessConnectComponent { get; private set; }
    
    /// <summary>
    ///     Bytes overhead per packet sent.
    /// </summary>
    public int PacketOverhead { get; private set; }
    
    /// <summary>
    ///     Full incoming packet index.
    /// </summary>
    public int InPacketId { get; private set; }
    
    /// <summary>
    ///     Most recently sent packet.
    /// </summary>
    public int OutPacketId { get; private set; }
    
    /// <summary>
    ///     Most recently acked outgoing packet.
    /// </summary>
    public int OutAckPacketId { get; private set; }
    
    public bool LastHasServerFrameTime { get; private set; }
    
    /// <summary>
    ///     Full PacketId  of last sent packet that we have received notification for (i.e. we know if it was delivered or not).
    ///     Related to OutAckPacketId which is tha last successfully delivered PacketId.
    /// </summary>
    public int LastNotifiedPacketId { get; private set; }

    public int MaxChannelSize { get; }
    public UChannel?[] Channels { get; }
    public int[] OutReliable { get; }
    public int[] InReliable { get; }
    public int[] PendingOutRec { get; }
    public int InitOutReliable { get; private set; }
    public int InitInReliable { get; private set; }
    
    public EEngineNetworkVersionHistory EngineNetworkProtocolVersion { get; private set; }
    public uint GameNetworkProtocolVersion { get; private set; }
    
    public FNetPacketNotify PacketNotify { get; }

    public virtual void InitBase(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        Driver = inDriver;
        
        // TODO: ConnectionId
        
        var driverElapsedTime = Driver.GetElapsedTime();

        _statUpdateTime = driverElapsedTime;
        _lastReceiveTime = driverElapsedTime;
        _lastReceiveRealTime = 0;
        _lastGoodPacketRealtime = 0;
        _lastTime = 0;
        _lastSendTime = driverElapsedTime;
        _lastTickTime = driverElapsedTime;
        
        State = inState;

        Url.Protocol = inURL.Protocol;
        Url.Host = inURL.Host;
        Url.Port = inURL.Port;
        Url.Map = inURL.Map;
        Url.RedirectUrl = inURL.RedirectUrl;
        Url.Options = inURL.Options;
        Url.Portal = inURL.Portal;

        MaxPacket = inMaxPacket;
        PacketOverhead = inPacketOverhead;

        InitHandler();

        PackageMap = new UPackageMapClient();
        PackageMap.Initialize(this, Driver.GuidCache);
    }

    public abstract void InitRemoteConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, IPEndPoint inRemoteAddr, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0);
    public abstract void InitLocalConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0);
    public abstract void LowLevelSend(byte[] data, int countBits, FOutPacketTraits traits);
    public abstract string LowLevelGetRemoteAddress(bool bAppendPort = false);
    public abstract string LowLevelDescribe();
    public abstract void Tick(float deltaSeconds);
    public abstract void CleanUp();

    public virtual void ReceivedRawPacket(FReceivedPacketView packetView)
    {
        if (Handler != null)
        {
            var result = Handler.Incoming(packetView);
            if (result)
            {
                if (packetView.DataView.NumBytes() == 0)
                {
                    return;
                }
            }
            else
            {
                // TODO: Close connection, malformed packet.
                Logger.Fatal("Connection should be closed here");
                return;
            }
        }

        // Handle an incoming raw packet from the driver.
        if (packetView.DataView.NumBytes() > 0)
        {
            var data = packetView.DataView.GetData();
            var count = packetView.DataView.NumBytes();
            
            var lastByte = data[count - 1];
            if (lastByte != 0)
            {
                var bitSize = (count * 8) - 1;

                // Bit streaming, starts at the Least Significant Bit, and ends at the MSB.
                while ((lastByte & 0x80) == 0)
                {
                    lastByte *= 2;
                    bitSize--;
                }
                
                var reader = new FBitReader(data, bitSize);
                
                reader.SetEngineNetVer(EngineNetworkProtocolVersion);
                reader.SetGameNetVer(GameNetworkProtocolVersion);

                if (Handler != null)
                {
                    Handler.IncomingHigh(reader);
                }

                if (reader.GetBitsLeft() > 0)
                {
                    ReceivedPacket(reader);
                    
                    // TODO: Flush out of order cache
                }
            }
        }
    }

    private void ReceivedPacket(FBitReader reader, bool bIsReinjectedPacket = false)
    {
        if (reader.IsError())
        {
            Logger.Error("Packet too small");
            return;
        }

        var resetReaderMark = new FBitReaderMark(reader);
        var channelsToClose = new List<FChannelCloseInfo>();
        
        if (_bInternalAck)
        {
            ++InPacketId;
        }
        else
        {
            // Read packet header.
            var header = new FNotificationHeader();

            if (!PacketNotify.ReadHeader(ref header, reader))
            {
                // TODO: Close connection, malformed packet.
                Logger.Fatal("Failed to read PacketHeader");
                return;
            }

            var bHasPacketInfoPayload = true;
            
            if (reader.EngineNetVer() > EEngineNetworkVersionHistory.HISTORY_JITTER_IN_HEADER)
            {
                bHasPacketInfoPayload = reader.ReadBit();
                
                if (bHasPacketInfoPayload)
                {
                    var bitsReadPreJitterClock = reader.GetPosBits();
                
                    var packetJitterClockTimeMs = reader.ReadInt(MaxJitterClockTimeValue + 1);

                    if (reader.GetPosBits() - bitsReadPreJitterClock != NumBitsForJitterClockTimeInHeader)
                    {
                        throw new UnrealNetException("JitterClockTime did not read the expected amount of bits");
                    }
                
                    if (!bIsReinjectedPacket)
                    {
                        ProcessJitter(packetJitterClockTimeMs);
                    }
                }
            }

            var packetSequenceDelta = PacketNotify.GetSequenceDelta(header);
            if (packetSequenceDelta > 0)
            {
                var bPacketOrderCacheActive = !_bFlushingPacketOrderCache && _packetOrderCache != null;
                var bCheckForMissingSequence = bPacketOrderCacheActive && _packetOrderCacheCount == 0;
                var bFillingPacketOrderCache = bPacketOrderCacheActive && _packetOrderCacheCount > 0;

                const int maxMissingPackets = 0; // CVarNetPacketOrderMaxMissingPackets
                var missingPacketCount = packetSequenceDelta - 1;
                
                // Cache the packet if we are already caching, and begin caching if we just encountered a missing sequence, within range
                if (bFillingPacketOrderCache || (bCheckForMissingSequence && missingPacketCount > 0 && missingPacketCount <= maxMissingPackets))
                {
                    throw new NotImplementedException();
                }

                if (missingPacketCount > 10)
                {
                    Logger.Verbose("High single frame packet loss, PacketsLost: {Amount}", missingPacketCount);
                }
                
                // TODO: Increment things
            }
            else
            {
                // TODO: Increment things
                // TODO: PacketOrderCache
                return;
            }

            // Update incoming sequence data and deliver packet notifications
            // Packet is only accepted if both the incoming sequence number and incoming ack data are valid
            PacketNotify.Update(header, (ackedSequence, delivered) =>
            {
                // TODO: Increment things

                if (!new SequenceNumber((ushort)LastNotifiedPacketId).Equals(ackedSequence))
                {
                    // TODO: Close connection.
                    Logger.Fatal("LastNotifiedPacketId != AckedSequence");
                    return;
                }

                if (delivered)
                {
                    // ReceivedAck(LastNotifiedPacketId, ChannelsToClose)
                    Logger.Verbose("TODO: ReceivedAck");
                }
                else
                {
                    // ReceivedNak(LastNotifiedPacketId)
                    Logger.Verbose("TODO: ReceivedNak");
                }
            });
            
            // Extra information associated with the header (read only after acks have been processed)
            if (packetSequenceDelta > 0 && !ReadPacketInfo(reader, bHasPacketInfoPayload))
            {
                // TODO: Close connection.
                Logger.Fatal("Failed to read PacketHeader");
                return;
            }
        }

        var bIgnoreRPCS = Driver!.ShouldIgnoreRPCs();
        var bSkipAck = false;
        
        // Track channels that were rejected while processing this packet - used to avoid sending multiple close-channel bunches,
        // which would cause a disconnect serverside
        var rejectedChans = new List<int>();
        
            
        // Disassemble and dispatch all bunches in the packet.
        while (!reader.AtEnd() && State != EConnectionState.USOCK_Closed)
        {
            if (_bInternalAck && EngineNetworkProtocolVersion < EEngineNetworkVersionHistory.HISTORY_ACKS_INCLUDED_IN_HEADER)
            {
                _ = reader.ReadBit();
            }
                
            // Parse the bunch.
            var startPos = reader.GetPosBits();
            
            // Process Received data
            {
                // Parse the incoming data.
                var bunch = new FInBunch(this);

                var incomingStartPos = reader.GetPosBits();

                var bControl = reader.ReadBit();

                bunch.PacketId = InPacketId;
                bunch.bOpen = bControl && reader.ReadBit();
                bunch.bClose = bControl && reader.ReadBit();

                if (bunch.EngineNetVer() < EEngineNetworkVersionHistory.HISTORY_CHANNEL_CLOSE_REASON)
                {
                    bunch.bDormant = bunch.bClose && reader.ReadBit();
                    bunch.CloseReason = bunch.bDormant
                        ? EChannelCloseReason.Dormancy
                        : EChannelCloseReason.Destroyed;
                }
                else
                {
                    bunch.CloseReason = bunch.bClose
                        ? (EChannelCloseReason)reader.ReadInt((uint)EChannelCloseReason.MAX)
                        : EChannelCloseReason.Destroyed;
                    bunch.bDormant = bunch.CloseReason == EChannelCloseReason.Dormancy;
                }

                bunch.bIsReplicationPaused = reader.ReadBit();
                bunch.bReliable = reader.ReadBit();

                if (bunch.EngineNetVer() < EEngineNetworkVersionHistory.HISTORY_MAX_ACTOR_CHANNELS_CUSTOMIZATION)
                {
                    const int oldMaxActorChannels = 10240;
                    bunch.ChIndex = (int)reader.ReadInt(oldMaxActorChannels);
                }
                else
                {
                    bunch.ChIndex = (int)reader.ReadUInt32Packed();

                    if (bunch.ChIndex >= MaxChannelSize)
                    {
                        throw new Exception("Bunch channel index exceeds channel limit");
                    }
                }
                
                // if flag is set, remap channel index values, we're fast forwarding a replay checkpoint
                // and there should be no bunches for existing channels
                if (_bInternalAck /* && bAllowExistingChannelIndex */ && (bunch.EngineNetVer() >= EEngineNetworkVersionHistory.HISTORY_REPLAY_DORMANCY))
                {
                    throw new NotSupportedException("Replay code");
                }

                bunch.bHasPackageMapExports = reader.ReadBit();
                bunch.bHasMustBeMappedGUIDs = reader.ReadBit();
                bunch.bPartial = reader.ReadBit();

                if (bunch.bReliable)
                {
                    if (_bInternalAck)
                    {
                        // We can derive the sequence for 100% reliable connections.
                        bunch.ChSequence = InReliable[bunch.ChIndex] + 1;
                    }
                    else
                    {
                        bunch.ChSequence = MakeRelative((int)reader.ReadInt(MaxChSequence), InReliable[bunch.ChIndex], MaxChSequence);
                    }
                }
                else if (bunch.bPartial)
                {
                    // If this is an unreliable partial bunch, we simply use packet sequence since we already have it
                    bunch.ChSequence = InPacketId;
                }
                else
                {
                    bunch.ChSequence = 0;
                }

                bunch.bPartialInitial = bunch.bPartial && reader.ReadBit();
                bunch.bPartialFinal = bunch.bPartial && reader.ReadBit();
                
                if (bunch.EngineNetVer() < EEngineNetworkVersionHistory.HISTORY_CHANNEL_NAMES)
                {
                    bunch.ChType = ((bunch.bReliable || bunch.bOpen) ? (EChannelType) reader.ReadInt((int) EChannelType.CHTYPE_MAX) : EChannelType.CHTYPE_None);

                    switch (bunch.ChType)
                    {
                        case EChannelType.CHTYPE_Control:
                            bunch.ChName = UnrealNames.FNames[UnrealNameKey.Control];
                            break;
                        case EChannelType.CHTYPE_Voice:
                            bunch.ChName = UnrealNames.FNames[UnrealNameKey.Voice];
                            break;
                        case EChannelType.CHTYPE_Actor:
                            bunch.ChName = UnrealNames.FNames[UnrealNameKey.Actor];
                            break;
                    }
                }
                else
                {
                    if (bunch.bReliable || bunch.bOpen)
                    {
                        if (!UPackageMap.StaticSerializeName(reader, out var chName) || reader.IsError())
                        {
                            // TODO: Close connection
                            Logger.Fatal("Channel name serialization failed");
                            return;
                        }
                            
                        bunch.ChName = chName;
                        
                        switch ((UnrealNameKey) bunch.ChName.Number)
                        {
                            case UnrealNameKey.Control:
                                bunch.ChType = EChannelType.CHTYPE_Control;
                                break;
                                
                            case UnrealNameKey.Voice:
                                bunch.ChType = EChannelType.CHTYPE_Voice;
                                break;
                                
                            case UnrealNameKey.Actor:
                                bunch.ChType = EChannelType.CHTYPE_Actor;
                                break;
                        }
                    }
                    else
                    {
                        bunch.ChType = EChannelType.CHTYPE_None;
                        bunch.ChName = UnrealNames.FNames[UnrealNameKey.None];
                    }
                }
                    
                var channel = Channels[bunch.ChIndex];

                // If there's an existing channel and the bunch specified it's channel type, make sure they match.
                if (channel != null &&
                    (bunch.ChName.Number != (int)UnrealNameKey.None) &&
                    (bunch.ChName.Number != channel.ChName.Number))
                {
                    Logger.Error("Existing channel at index {ChIndex} with type \"{ChName}\" differs from the incoming bunch's expected channel type, \"{BunchChName}\"", 
                        bunch.ChIndex, channel.ChName.Str, bunch.ChName.Str);
                    // TODO: Close();
                    return;
                }

                var bunchDataBits = reader.ReadInt((uint)(MaxPacket * 8));
                var headerPos = reader.GetPosBits();
                if (reader.IsError())
                {
                    Logger.Error("Bunch header overflow");
                    // TODO: Close();
                    return;
                }
                
                bunch.SetData(reader, bunchDataBits);

                if (reader.IsError())
                {
                    Logger.Fatal("Bunch data overflowed ({IncomingStartPos} {HeaderPos}+{BunchDataBits}/{NumBits})", incomingStartPos, headerPos, bunchDataBits, reader.GetNumBits());
                    // TOOD: Close();
                    return;
                }

                if (bunch.bHasPackageMapExports)
                {
                    throw new NotImplementedException();
                }

                if (bunch.bReliable)
                {
                    Logger.Verbose("  Reliable Bunch, Channel {Ch} Sequence {Seq}: Size {A.0}+{B.0}", bunch.ChIndex, bunch.ChSequence, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }
                else
                {
                    Logger.Verbose("  Unreliable Bunch, Channel {Ch}: Size {A.0}+{B.0}", bunch.ChIndex, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }

                if (bunch.bOpen)
                {
                    Logger.Verbose("  bOpen Bunch, Channel {Ch} Sequence {Seq}: Size {A.0}+{B.0}", bunch.ChIndex, bunch.ChSequence, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }

                if (Channels[bunch.ChIndex] == null && (bunch.ChIndex != 0 || bunch.ChName != UnrealNames.FNames[UnrealNameKey.Control]))
                {
                    if (Channels[0] == null)
                    {
                        Logger.Fatal("  Received non-control bunch before control channel was created. ChIndex: {Ch}, ChName: {Name}", bunch.ChIndex, bunch.ChName);
                        // TODO: Close();
                        return;
                    } 
                    else if (PlayerController == null && Driver.ClientConnections.Contains(this))
                    {
                        Logger.Fatal("  Received non-control bunch before player controller was assigned. ChIndex: {Ch}, ChName: {Name}", bunch.ChIndex, bunch.ChName);
                        // TODO: Close();
                        return;
                    }
                }

                // ignore control channel close if it hasn't been opened yet
                if (bunch.ChIndex == 0 && Channels[0] == null && bunch.bClose && bunch.ChName == UnrealNames.FNames[UnrealNameKey.Control])
                {
                    Logger.Fatal("Received control channel close before open");
                    // Close();
                    return;
                }

                // We're on a 100% reliable connection and we are rolling back some data.
                // In that case, we can generally ignore these bunches.
                if (_bInternalAck /* && _bAllowExistingChannelIndex */)
                {
                    throw new NotImplementedException();
                }
                
                // Ignore if reliable packet has already been processed.
                if (bunch.bReliable && bunch.ChSequence <= InReliable[bunch.ChIndex])
                {
                    Logger.Warning("Received outdated bunch (Channel {Ch} Current Sequence {Seq})", bunch.ChIndex, InReliable[bunch.ChIndex]);
                    continue;
                }

                // If opening the channel with an unreliable packet, check that it is "bNetTemporary", otherwise discard it
                if (channel == null && !bunch.bReliable)
                {
                    // Unreliable bunches that open channels should be bOpen && (bClose || bPartial)
                    // NetTemporary usually means one bunch that is unreliable (bOpen and bClose):	1(bOpen, bClose)
                    // But if that bunch export NetGUIDs, it will get split into 2 bunches:			1(bOpen, bPartial) - 2(bClose).
                    // (the initial actor bunch itself could also be split into multiple bunches. So bPartial is the right check here)

                    var validUnreliableOpen = bunch.bOpen && (bunch.bClose || bunch.bPartial);
                    if (!validUnreliableOpen)
                    {
                        if (_bInternalAck)
                        {
                            // Should be impossible with 100% reliable connections
                            Logger.Error("Received unreliable bunch before open with reliable connection (Channel {Ch} Current Sequence {Seq})", bunch.ChIndex, InReliable[bunch.ChIndex]);
                        }
                        else
                        {
                            // Simply a log (not a warning, since this can happen under normal conditions, like from a re-join, etc)
                            Logger.Information("Received unreliable bunch before open (Channel {Ch} Current Sequence {Seq})", bunch.ChIndex, InReliable[bunch.ChIndex]);
                        }

                        // Since we won't be processing this packet, don't ack it
                        // We don't want the sender to think this bunch was processed when it really wasn't
                        bSkipAck = true;
                        continue;
                    }
                }

                // Create channel if necessary.
                if (channel == null)
                {
                    if (rejectedChans.Contains(bunch.ChIndex))
                    {
                        Logger.Warning("Ignoring Bunch for ChIndex {Ch}, as the channel was already rejected while processing this packet", bunch.ChIndex);
                        continue;
                    }
                    
                    // Validate channel type.
                    if (!Driver.IsKnownChannelName(bunch.ChName))
                    {
                        // Unknown type.
                        Logger.Fatal("Connection unknown channel type ({Name})", bunch.ChName);
                        // TODO: Close()
                        return;
                    }

                    // Ignore incoming data on channel types that the client are not allowed to create. This can occur if we have in-flight data when server is closing a channel
                    if (Driver.IsServer() && (Driver.ChannelDefinitionMap[bunch.ChName].ClientOpen == false))
                    {
                        Logger.Warning("Ignoring Bunch Create received from client since only server is allowed to create this type of channel: Bunch  {Ch}: ChName {Name}, ChSequence: {Seq}", bunch.ChIndex, bunch.ChName, bunch.ChSequence);
                        rejectedChans.Add(bunch.ChIndex);
                        continue;
                    }

                    // peek for guid
                    if (_bInternalAck /* && bIgnoreActorBunches */)
                    {
                        throw new NotImplementedException();
                    }
                    
                    // Reliable (either open or later), so create new channel.
                    Logger.Information("  Bunch Create {ChIndex}: ChName {ChName}, ChSequence: {ChSequence}, bReliable: {Reliable}, bPartial: {Partial}, bPartialInitial: {PartInit}, bPartialFinal: {PartFin}",
                        bunch.ChIndex,
                        bunch.ChName,
                        bunch.ChSequence,
                        bunch.bReliable,
                        bunch.bPartial,
                        bunch.bPartialInitial,
                        bunch.bPartialFinal);

                    channel = CreateChannelByName(bunch.ChName, EChannelCreateFlags.None, bunch.ChIndex);

                    // Notify the server of the new channel.
                    if (!Driver.Notify.NotifyAcceptingChannel(channel))
                    {
                        // Channel refused, so close it, flush it, and delete it.
                        Logger.Verbose("NotifyAcceptingChannel Failed! Channel: {Channel}", channel);
                        rejectedChans.Add(bunch.ChIndex);
                        
                        // TODO: FOutBunch
                        continue;
                    }
                }

                bunch.bIgnoreRPCs = bIgnoreRPCS;
                
                // Dispatch the raw, unsequenced bunch to the channel.
                channel.ReceivedRawBunch(bunch, out var bLocalSkipAck);

                if (bLocalSkipAck)
                {
                    bSkipAck = true;
                }
                
                // Disconnect if we received a corrupted packet from the client (eg server crash attempt).
                if (Driver.IsServer() && (bunch.IsCriticalError() || bunch.IsError()))
                {
                    Logger.Error("Received corrupted packet data from client {RemoteAddress}.  Disconnecting", LowLevelGetRemoteAddress());
                    // TODO: Close()
                    return;
                }
            }
        }

        // Close/clean-up channels pending close due to received acks.
        foreach (var info in channelsToClose)
        {
            var channel = Channels[info.Id];
            if (channel != null)
            {
                channel.ConditionalCleanUp(false, info.CloseReason);
            }
        }

        // TODO: ValidateSendBuffer();

        if (!bSkipAck)
        {
            _lastGoodPacketRealtime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        if (!_bInternalAck)
        {
            if (bSkipAck)
            {
                PacketNotify.NakSeq(new SequenceNumber((ushort)InPacketId));
            }
            else
            {
                PacketNotify.AckSeq(new SequenceNumber((ushort)InPacketId));
                
                // TODO: Increment things
                // ++OutTotalAcks;
                // ++Driver->OutTotalAcks;
            }
            
            // We do want to let the other side know about the ack, so even if there are no other outgoing data when we tick the connection we will send an ackpacket.
            // TimeSensitive = 1;
            // ++HasDirtyAcks;
            
            // TODO: FlushNet if HasDirtyAcks
        }
    }

    private bool ReadPacketInfo(FBitReader reader, bool bHasPacketInfoPayload)
    {
        if (!bHasPacketInfoPayload)
        {
            var bCanContinueReading = reader.IsError() == false;
            return bCanContinueReading;
        }

        var bHasServerFrameTime = reader.ReadBit();
        var serverFrameTime = 0.0d;

        if (!Driver!.IsServer())
        {
            throw new NotImplementedException("Client code");
        }
        else
        {
            LastHasServerFrameTime = bHasServerFrameTime;
        }

        if (reader.EngineNetVer() < EEngineNetworkVersionHistory.HISTORY_JITTER_IN_HEADER)
        {
            // RemoteInKBytesPerSecondByte
            reader.ReadByte();
        }

        if (reader.IsError())
        {
            return false;
        }
        
        // TODO: Update ping, lag measuring

        return true;
    }

    private void ProcessJitter(uint packetJitterClockTimeMs)
    {
        if (packetJitterClockTimeMs >= MaxJitterClockTimeValue)
        {
            return;
        }
        
        Logger.Verbose("Jitter calculations missing");
    }

    public abstract float GetTimeoutValue();

    public void InitSequence(int incomingSequence, int outgoingSequence)
    {
        if (InPacketId == -1)
        {
            // Initialize the base UNetConnection packet sequence (not very useful/effective at preventing attacks)
            InPacketId = incomingSequence - 1;
            OutPacketId = outgoingSequence;
            OutAckPacketId = outgoingSequence - 1;
            LastNotifiedPacketId = OutAckPacketId;

            // Initialize the reliable packet sequence (more useful/effective at preventing attacks)
            InitInReliable = incomingSequence & (MaxChSequence - 1);
            InitOutReliable = outgoingSequence & (MaxChSequence - 1);
            
            for (var i = 0; i < InReliable.Length; i++)
            {
                InReliable[i] = InitInReliable;
            }

            for (var i = 0; i < OutReliable.Length; i++)
            {
                OutReliable[i] = InitOutReliable;
            }

            PacketNotify.Init(
                new SequenceNumber((ushort)InPacketId),
                new SequenceNumber((ushort)OutPacketId));
            
            Logger.Verbose("InitSequence: IncomingSequence: {SeqA}, OutgoingSequence: {SeqB}", incomingSequence, outgoingSequence);
            Logger.Verbose("InitSequence: InitInReliable: {In}, InitOutReliable: {Out}", InitInReliable, InitOutReliable);
        }
    }

    private void InitHandler()
    {
        if (Handler != null)
        {
            return;
        }

        if (Driver == null)
        {
            throw new UnrealNetException("Driver was null");
        }
        
        Handler = new PacketHandler();
        Handler.Initialize(false);
        
        StatelessConnectComponent = (StatelessConnectHandlerComponent) Handler.AddHandler<StatelessConnectHandlerComponent>();
        StatelessConnectComponent.SetDriver(Driver);

        Handler.InitializeComponents();
    }

    public UChannel CreateChannelByName(FName chName, EChannelCreateFlags createFlags, int chIndex)
    {
        if (!Driver!.IsKnownChannelName(chName))
        {
            throw new UnrealNetException("Unknown channel name was specified.");
        }

        if (chIndex == UnrealConstants.IndexNone)
        {
            chIndex = GetFreeChannelIndex(chName);

            if (chIndex == UnrealConstants.IndexNone)
            {
                Logger.Warning("No free channel could be found in the channel list (current limit is {Max} channels)", MaxChannelSize);
                throw new UnrealNetException("Exhausted channels");
            }
        }
        
        // Make sure channel is valid.
        if (chIndex >= Channels.Length)
        {
            throw new UnrealNetException("Channel index is too high.");
        }

        if (Channels[chIndex] != null)
        {
            throw new UnrealNetException("Trying to replace an existing channel.");
        }
        
        // Create channel.
        var channel = Driver.GetOrCreateChannelByName(chName);
        if (channel == null)
        {
            throw new UnrealNetException("Failed to create channel.");
        }

        channel.Init(this, chIndex, createFlags);
        
        Channels[chIndex] = channel;
        OpenChannels.Add(channel);

        if (Driver.ChannelDefinitionMap[chName].TickOnCreate)
        {
            StartTickingChannel(channel);
        }
        
        Logger.Verbose("Created channel {Ch} of type {Name}", chIndex, chName);

        return channel;
    }

    private int GetFreeChannelIndex(FName chName)
    {
        int chIndex;
        var firstChannel = 1;

        var staticChannelIndex = Driver!.ChannelDefinitionMap[chName].StaticChannelIndex;
        if (staticChannelIndex != -1)
        {
            firstChannel = staticChannelIndex;
        }
                
        // Search the channel array for an available location
        for (chIndex = firstChannel; chIndex < Channels.Length; chIndex++)
        {
            if (Channels[chIndex] == null)
            {
                break;
            }
        }

        if (chIndex == Channels.Length)
        {
            chIndex = UnrealConstants.IndexNone;
        }

        return chIndex;
    }

    private void StartTickingChannel(UChannel channel)
    {
        _channelsToTick.Add(channel);
    }

    private void StopTickingChannel(UChannel channel)
    {
        _channelsToTick.Remove(channel);
    }

    private protected void SetClientLoginState(EClientLoginState newState)
    {
        if (ClientLoginState == newState)
        {
            Logger.Verbose("SetClientLoginState: State same: {Old}", newState);
            return;
        }
        
        Logger.Verbose("SetClientLoginState: State changing from {Old} to {New}", ClientLoginState, newState);

        ClientLoginState = newState;
    }

    private protected void SetExpectedClientLoginMsgType(byte newType)
    {
        if (ExpectedClientLoginMsgType == newType)
        {
            Logger.Verbose("SetExpectedClientLoginMsgType: Type same: [{Old}]", newType);
            return;
        }
        
        Logger.Verbose("SetExpectedClientLoginMsgType: Type changing from [{Old}] to [{New}]", ExpectedClientLoginMsgType, newType);

        ExpectedClientLoginMsgType = newType;
    }

    public bool IsInternalAck()
    {
        return _bInternalAck;
    }

    private static int BestSignedDifference(int value, int reference, int max)
    {
        return ((value - reference + max / 2) & (max - 1)) - max / 2;
    }

    private static int MakeRelative(int value, int reference, int max)
    {
        return reference + BestSignedDifference(value, reference, max);
    }
}