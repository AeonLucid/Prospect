using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Control;
using Prospect.Unreal.Net.Packets.Header;
using Prospect.Unreal.Net.Packets.Header.Sequence;
using Prospect.Unreal.Runtime;
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
    public const int MaxPacketReliableSequenceHeaderBits = 32 + FNetPacketNotify.MaxSequenceHistoryLength;
    public const int MaxPacketInfoHeaderBits = 1 /* bHasPacketInfo */ + NumBitsForJitterClockTimeInHeader  + 1 /* bHasServerFrameTime */ + 8 /* ServerFrameTime */;
    public const int MaxPacketHeaderBits = MaxPacketReliableSequenceHeaderBits + MaxPacketInfoHeaderBits;
    public const int MaxPacketTrailerBits = 1;

    public const int DefaultMaxChannelSize = 32767;

    public const int NumBitsForJitterClockTimeInHeader = 10;
    public const int MaxJitterClockTimeValue = (1 << NumBitsForJitterClockTimeInHeader) - 1;
    public const int MaxJitterPrecisionInMS = 1000;

    public const EEngineNetworkVersionHistory DefaultEngineNetworkProtocolVersion = EEngineNetworkVersionHistory.HISTORY_ENGINENETVERSION_LATEST;
    public const uint DefaultGameNetworkProtocolVersion = 0;

    /// <summary>
    /// 	The channels that need ticking. This will be a subset of OpenChannels, only including
	///     channels that need to process either dormancy or queued bunches. Should be a significant
	///     optimization over ticking and calling virtual functions on the potentially hundreds of
	///     OpenChannels every frame.
    /// </summary>
    private HashSet<UChannel> _channelsToTick;
    
    /// <summary>
    ///     Online platform ID of remote player on this connection. Only valid on client connections (server side).
    /// </summary>
    private FName _playerOnlinePlatformName;
    
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

    /// <summary>
    ///     Did we write the dummy PacketInfo in the current SendBuffer
    /// </summary>
    private bool _bSendBufferHasDummyPacketInfo;
    
    /// <summary>
    ///     Stores the bit number where we wrote the dummy packet info in the packet header
    /// </summary>
    private FBitWriterMark _headerMarkForPacketInfo;

    /// <summary>
    ///     Timestamp of the last packet sent
    /// </summary>
    private double _previousPacketSendTimeInS;

    private bool _bFlushedNetThisFrame;
    private bool _bAutoFlush;

    private readonly HandlePacketNotification _packetNotifyUpdateDelegate;

    public UNetConnection()
    {
        _channelsToTick = new HashSet<UChannel>();
        _playerOnlinePlatformName = EName.None;
        _packetOrderCache = null;
        _packetOrderCacheStartIdx = 0;
        _packetOrderCacheCount = 0;
        _bFlushingPacketOrderCache = false;
        _bInternalAck = false;
        _bReplay = false;
        _packetNotifyUpdateDelegate = PacketNotifyUpdate;

        Children = new List<UChildConnection>();
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
        Challenge = string.Empty;
        SendBuffer = new FBitWriter(0);
        OutLagTime = new double[256];
        OutLagPacketId = new int[256];
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
    ///     Child connections for secondary viewports
    /// </summary>
    public List<UChildConnection> Children { get; private set; }

    /// <summary>
    ///     Owning net driver
    /// </summary>
    public UNetDriver? Driver { get; private set; }
    
    /// <summary>
    ///     Package map between local and remote. (negotiates net serialization)
    /// </summary>
    public UPackageMap? PackageMap { get; private set; }
    
    public List<UChannel> OpenChannels { get; }
    
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
    ///     Number of bits used for the packet id in the current packet.
    /// </summary>
    public int NumPacketIdBits { get; private set; }
    
    /// <summary>
    ///     Number of bits used for bunches in the current packet.
    /// </summary>
    public int NumBunchBits { get; private set; }
    
    /// <summary>
    ///     Number of bits used for acks in the current packet.
    /// </summary>
    public int NumAckBits { get; private set; }
    
    /// <summary>
    ///     Number of bits used for padding in the current packet.
    /// </summary>
    public int NumPaddingBits { get; private set; }
    
    /// <summary>
    ///     The maximum number of bits all packet handlers will reserve.
    /// </summary>
    public int MaxPacketHandlerBits { get; private set; }
    
    /// <summary>
    ///     State this connection is in.
    /// </summary>
    public EConnectionState State { get; private set; }
    
    /// <summary>
    ///     This functionality is used during replay checkpoints for example, so we can re-use the existing connection and channels to record
    ///     a version of each actor and capture all properties that have changed since the actor has been alive...
    ///     This will also act as if it needs to re-open all the channels, etc.
    ///       NOTE - This doesn't force all exports to happen again though, it will only export new stuff, so keep that in mind.
    /// </summary>
    public EResendAllDataState ResendAllDataState { get; set; }
    
    /// <summary>
    ///     PacketHandler, for managing layered handler components, which modify packets as they are sent/received
    /// </summary>
    public PacketHandler? Handler { get; private set; }
    
    public EClientLoginState ClientLoginState { get; private set; }
    
    /// <summary>
    ///     Used to determine what the next expected control channel msg type should be from a connecting client
    /// </summary>
    public NMT ExpectedClientLoginMsgType { get; private set; }
    
    /// <summary>
    ///     Reference to the PacketHandler component, for managing stateless connection handshakes
    /// </summary>
    public StatelessConnectHandlerComponent? StatelessConnectComponent { get; private set; }
    
    /// <summary>
    ///     Net id of remote player on this connection. Only valid on client connections (server side).
    /// </summary>
    public FUniqueNetIdRepl? PlayerId { get; set; }
    
    /// <summary>
    ///     Bytes overhead per packet sent.
    /// </summary>
    public int PacketOverhead { get; private set; }
    
    /// <summary>
    ///     Server-generated challenge.
    /// </summary>
    public string Challenge { get; private set; }
    
    /// <summary>
    ///     Client-generated response.
    /// </summary>
    public string ClientResponse { get; set; }
    
    /// <summary>
    ///     URL requested by client
    /// </summary>
    public string RequestURL { get; set; }
    
    /// <summary>
    ///     The last time an ack was received
    /// </summary>
    public float LastRecvAckTime { get; set; }
    
    /// <summary>
    ///     The last time an ack was received
    /// </summary>
    public double LastRecvAckTimestamp { get; set; }
    
    /// <summary>
    ///     Queued up bits waiting to send
    /// </summary>
    public FBitWriter SendBuffer { get; private set; }
    
    public double[] OutLagTime { get; }
    
    public int[] OutLagPacketId { get; }
    
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
    
    /// <summary>
    ///     Keep old behavior where we send a packet with only acks even if we have no other outgoing data if we got incoming data
    /// </summary>
    public uint HasDirtyAcks { get; set; }
    
    /// <summary>
    ///     Most recently sent bunch start.
    /// </summary>
    public FBitWriterMark LastStart { get; set; }
    
    /// <summary>
    ///     Most recently sent bunch end.
    /// </summary>
    public FBitWriterMark LastEnd { get; set; }
    
    /// <summary>
    ///     Whether to allow merging.
    /// </summary>
    public bool AllowMerge { get; set; }
    
    /// <summary>
    ///     Whether contents are time-sensitive.
    /// </summary>
    public bool TimeSensitive { get; set; }
    
    /// <summary>
    ///     Most recent outgoing bunch.
    /// </summary>
    public FOutBunch? LastOutBunch { get; set; }
    
    public FOutBunch LastOut { get; set; }

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

        var packageMapClient = new UPackageMapClient();
        packageMapClient.Initialize(this, Driver.GuidCache);
        PackageMap = packageMapClient;
    }

    public virtual void InitRemoteConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, IPEndPoint inRemoteAddr, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotImplementedException();
    }
    
    public virtual void InitLocalConnection(UNetDriver inDriver, UdpClient inSocket, FUrl inURL, EConnectionState inState, int inMaxPacket = 0, int inPacketOverhead = 0)
    {
        throw new NotImplementedException();
    }
    
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
                Logger.Fatal("Packet failed PacketHandler processing");
                Close();
                return;
            }
            
            // See if we receive a packet that wasn't fully consumed by the handler before the handler is initialized.
            // TODO: Handler.IsFullyInitialized
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
                    
                    // TODO: FlushPacketOrderCache
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
                Logger.Fatal("Failed to read PacketHeader");
                Close();
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
                InPacketId += packetSequenceDelta;
            }
            else
            {
                // TODO: Increment things
                // TODO: PacketOrderCache
                Logger.Warning("Received out of order packet");
                return;
            }

            // Update incoming sequence data and deliver packet notifications
            // Packet is only accepted if both the incoming sequence number and incoming ack data are valid
            PacketNotify.Update(header, new PacketNotifyUpdateContext(_packetNotifyUpdateDelegate, channelsToClose));
            
            // Extra information associated with the header (read only after acks have been processed)
            if (packetSequenceDelta > 0 && !ReadPacketInfo(reader, bHasPacketInfoPayload))
            {
                Logger.Fatal("Failed to read PacketHeader");
                Close();
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
                            bunch.ChName = EName.Control;
                            break;
                        case EChannelType.CHTYPE_Voice:
                            bunch.ChName = EName.Voice;
                            break;
                        case EChannelType.CHTYPE_Actor:
                            bunch.ChName = EName.Actor;
                            break;
                    }
                }
                else
                {
                    if (bunch.bReliable || bunch.bOpen)
                    {
                        FName? chName = null;
                        
                        if (!UPackageMap.StaticSerializeName(reader, ref chName) || reader.IsError())
                        {
                            Close();
                            Logger.Fatal("Channel name serialization failed");
                            return;
                        }
                            
                        bunch.ChName = chName!.Value;

                        if (bunch.ChName == EName.Control)
                        {
                            bunch.ChType = EChannelType.CHTYPE_Control;
                        } 
                        else if (bunch.ChName == EName.Voice)
                        {
                            bunch.ChType = EChannelType.CHTYPE_Voice;
                        }
                        else if (bunch.ChName == EName.Actor)
                        {
                            bunch.ChType = EChannelType.CHTYPE_Actor;
                        }
                    }
                    else
                    {
                        bunch.ChType = EChannelType.CHTYPE_None;
                        bunch.ChName = EName.None;
                    }
                }
                    
                var channel = Channels[bunch.ChIndex];

                // If there's an existing channel and the bunch specified it's channel type, make sure they match.
                if (channel != null &&
                    (bunch.ChName != EName.None) &&
                    (bunch.ChName != channel.ChName))
                {
                    Logger.Error("Existing channel at index {ChIndex} with type \"{ChName}\" differs from the incoming bunch's expected channel type, \"{BunchChName}\"", bunch.ChIndex, channel.ChName, bunch.ChName);
                    Close();
                    return;
                }

                var bunchDataBits = reader.ReadInt((uint)(MaxPacket * 8));
                var headerPos = reader.GetPosBits();
                if (reader.IsError())
                {
                    Logger.Error("Bunch header overflow");
                    Close();
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
                    Logger.Verbose("  Reliable Bunch, Channel {Ch} Sequence {Seq}: Size {A:###.0}+{B:###.0}", bunch.ChIndex, bunch.ChSequence, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }
                else
                {
                    Logger.Verbose("  Unreliable Bunch, Channel {Ch}: Size {A:###.0}+{B:###.0}", bunch.ChIndex, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }

                if (bunch.bOpen)
                {
                    Logger.Verbose("  bOpen Bunch, Channel {Ch} Sequence {Seq}: Size {A:###.0}+{B:###.0}", bunch.ChIndex, bunch.ChSequence, (headerPos - incomingStartPos)/8.0f, (reader.GetPosBits()-headerPos)/8.0f);
                }

                if (Channels[bunch.ChIndex] == null && (bunch.ChIndex != 0 || bunch.ChName != EName.Control))
                {
                    if (Channels[0] == null)
                    {
                        Logger.Fatal("  Received non-control bunch before control channel was created. ChIndex: {Ch}, ChName: {Name}", bunch.ChIndex, bunch.ChName);
                        Close();
                        return;
                    } 
                    else if (PlayerController == null && Driver.ClientConnections.Contains(this))
                    {
                        Logger.Fatal("  Received non-control bunch before player controller was assigned. ChIndex: {Ch}, ChName: {Name}", bunch.ChIndex, bunch.ChName);
                        Close();
                        return;
                    }
                }

                // ignore control channel close if it hasn't been opened yet
                if (bunch.ChIndex == 0 && Channels[0] == null && bunch.bClose && bunch.ChName == EName.Control)
                {
                    Logger.Fatal("Received control channel close before open");
                    Close();
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
                        Close();
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
                    Close();
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
            TimeSensitive = true;
            ++HasDirtyAcks;
            
            if (HasDirtyAcks >= FNetPacketNotify.MaxSequenceHistoryLength)
            {
                Logger.Warning("ReceivedPacket - Too many received packets to ack ({Acks}) since last sent packet. InSeq: {Seq} {Conn} NextOutGoingSeq: {OutSeq}", HasDirtyAcks, PacketNotify.GetInSeq().Value, this, PacketNotify.GetOutSeq().Value);
                
                FlushNet();
                if (HasDirtyAcks != 0)
                {
                    FlushNet();
                }
            }
        }
    }

    private void PacketNotifyUpdate(PacketNotifyUpdateContext context, SequenceNumber ackedSequence, bool delivered)
    {
        ++LastNotifiedPacketId;
        // TODO: Increment things

        if (!new SequenceNumber((ushort)LastNotifiedPacketId).Equals(ackedSequence))
        {
            Close();
            Logger.Fatal("LastNotifiedPacketId != AckedSequence");
            return;
        }

        if (delivered)
        {
            ReceivedAck(LastNotifiedPacketId, context.ChannelToClose);
        }
        else
        {
            ReceivedNak(LastNotifiedPacketId);
        }
    }

    private void ReceivedAck(int ackPacketId, List<FChannelCloseInfo> outChannelsToClose)
    {
        Logger.Verbose("Received ack {AckPacketId}", ackPacketId);
        
        // Advance OutAckPacketId
        OutAckPacketId = ackPacketId;
        
        // Process the bunch.
        LastRecvAckTime = (float)Driver!.GetElapsedTime();
        LastRecvAckTimestamp = Driver.GetElapsedTime();

        if (PackageMap != null)
        {
            PackageMap.ReceivedAck(ackPacketId);
        }
        
        // TODO: Invoke AckChannelFunc on all channels written for this PacketId
    }

    private void ReceivedNak(int nakPacketId)
    {
        Logger.Verbose("Received nak {NakPacketId}", nakPacketId);
        
        // Update pending NetGUIDs
        PackageMap!.ReceivedNak(nakPacketId);
        
        // TODO: Invoke NakChannelFunc on all channels written for this PacketId
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
        Handler.Initialize(HandlerMode.Server /* Mode */, UNetConnection.MaxPacketSize * 8 ,false);
        
        StatelessConnectComponent = (StatelessConnectHandlerComponent) Handler.AddHandler<StatelessConnectHandlerComponent>();
        StatelessConnectComponent.SetDriver(Driver);

        Handler.InitializeComponents();

        MaxPacketHandlerBits = Handler.GetTotalReservedPacketBits();
    }

    public void Close()
    {
        // TODO: Implement
    }

    public long GetMaxSingleBunchSizeBits()
    {
        return (MaxPacket * 8) - MaxBunchHeaderBits - MaxPacketTrailerBits - MaxPacketHeaderBits - MaxPacketHandlerBits;
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

    public void SendChallengeControlMessage()
    {
        if (State != EConnectionState.USOCK_Invalid && State != EConnectionState.USOCK_Closed && Driver != null)
        {
            Challenge = "E660A966";
            SetExpectedClientLoginMsgType(NMT.Login);
            NMT_Challenge.Send(this, Challenge);
            FlushNet();
        }
    }

    public int SendRawBunch(FOutBunch bunch, bool inAllowMerge)
    {
        ValidateSendBuffer();

        if (Driver == null || bunch.ReceivedAck || bunch.IsError())
        {
            throw new UnrealNetException();
        }
        
        // TODO: Increment things
        TimeSensitive = true;

        using var sendBunchHeader = new FBitWriter(MaxBunchHeaderBits);

        var bIsOpenOrClose = bunch.bOpen || bunch.bClose;
        var bIsOpenOrReliable = bunch.bOpen || bunch.bReliable;
        
        sendBunchHeader.WriteBit(bIsOpenOrClose);

        if (bIsOpenOrClose)
        {
            sendBunchHeader.WriteBit(bunch.bOpen);
            sendBunchHeader.WriteBit(bunch.bClose);
            
            if (bunch.bClose)
            {
                sendBunchHeader.SerializeInt((uint)bunch.CloseReason, (uint)EChannelCloseReason.MAX);
            }
        }
        
        sendBunchHeader.WriteBit(bunch.bIsReplicationPaused);
        sendBunchHeader.WriteBit(bunch.bReliable);

        sendBunchHeader.SerializeIntPacked((uint)bunch.ChIndex);
        
        sendBunchHeader.WriteBit(bunch.bHasPackageMapExports);
        sendBunchHeader.WriteBit(bunch.bHasMustBeMappedGUIDs);
        sendBunchHeader.WriteBit(bunch.bPartial);

        if (bunch.bReliable && !IsInternalAck())
        {
            // 14 > 24
            sendBunchHeader.WriteIntWrapped((uint)bunch.ChSequence, MaxChSequence);
        }

        if (bunch.bPartial)
        {
            sendBunchHeader.WriteBit(bunch.bPartialInitial);
            sendBunchHeader.WriteBit(bunch.bPartialFinal);
        }

        if (bIsOpenOrReliable)
        {
            var name = (FName?) bunch.ChName;
            UPackageMap.StaticSerializeName(sendBunchHeader, ref name);
        }
        
        sendBunchHeader.WriteIntWrapped((uint)bunch.GetNumBits(), (uint)(MaxPacket * 8));

        if (sendBunchHeader.IsError())
        {
            Logger.Fatal("SendBunchHeader Error: Bunch = {Bunch}", bunch);
        }
        
        // Remember start position.
        AllowMerge = inAllowMerge;
        bunch.Time = Driver.GetElapsedTime();

        var bunchHeaderBits = sendBunchHeader.GetNumBits();
        var bunchBits = bunch.GetNumBits();
        
        // If the bunch does not fit in the current packet, 
        // flush packet now so that we can report collected stats in the correct scope
        PrepareWriteBitsToSendBuffer(bunchHeaderBits, bunchBits);
        
        // Write the bits to the buffer and remember the packet id used
        bunch.PacketId = WriteBitsToSendBufferInternal(sendBunchHeader.GetData(), (int)bunchHeaderBits, bunch.GetData(), (int)bunchBits, EWriteBitsDataType.Bunch);

        if (PackageMap != null && bunch.bHasPackageMapExports)
        {
            PackageMap.NotifyBunchCommit(bunch.PacketId, bunch);
        }

        if (bunch.bHasPackageMapExports)
        {
            // TOOD: Increment things
        }

        if (_bAutoFlush)
        {
            FlushNet();
        }

        return bunch.PacketId;
    }
    private void PrepareWriteBitsToSendBuffer(long sizeInBits, long extraSizeInBits)
    {
        ValidateSendBuffer();

        var totalSizeInBits = sizeInBits + extraSizeInBits;
        
        // Flush if we can't add to current buffer
        if (totalSizeInBits > GetFreeSendBufferBits())
        {
            FlushNet();
        }
        
        // If this is the start of the queue, make sure to add the packet id
        if (SendBuffer.GetNumBits() == 0 && !IsInternalAck())
        {
            // Write Packet Header, before sending the packet we will go back and rewrite the data
            WritePacketHeader(SendBuffer);
            
            // Pre-write the bits for the packet info
            WriteDummyPacketInfo(SendBuffer);
            
            // We do not allow the first bunch to merge with the ack data as this will "revert" the ack data.
            AllowMerge = false;
	
            // Update stats for PacketIdBits and ackdata (also including the data used for packet RTT and saturation calculations)
            var bitsWritten = (int)SendBuffer.GetNumBits();
            
            NumPacketIdBits += FNetPacketNotify.SequenceNumberBits;
            NumAckBits += bitsWritten - FNetPacketNotify.SequenceNumberBits;

            ValidateSendBuffer();
        }
    }

    private void WritePacketHeader(FBitWriter writer)
    {
        // If this is a header refresh, we only serialize the updated serial number information
        var bIsHeaderUpdate = writer.GetNumBits() > 0;
        
        // Header is always written first in the packet
        var restore = new FBitWriterMark(writer);
        
        writer.Num = 0;
        
        // Write notification header or refresh the header if used space is the same.
        var bWroteHeader = PacketNotify.WriteHeader(writer, bIsHeaderUpdate);
        
        // Jump back to where we came from.
        if (bIsHeaderUpdate)
        {
            restore.PopWithoutClear(writer);
            
            // if we wrote the header and successfully refreshed the header status we no longer has any dirty acks
            if (bWroteHeader)
            {
                HasDirtyAcks = 0;
            }
        }
    }

    private void WriteFinalPacketInfo(FBitWriter writer, double packetSentTimeInS)
    {
        if (!_bSendBufferHasDummyPacketInfo)
        {
            // PacketInfo payload is not included in this SendBuffer; nothing to rewrite
            return;
        }

        var currentMark = new FBitWriterMark(writer);
        
        // Go back to write over the dummy bits
        _headerMarkForPacketInfo.PopWithoutClear(writer);
        
        // Write Jitter clock time
        {
            var deltaSendTimeInMS = (packetSentTimeInS - _previousPacketSendTimeInS) * 1000.0;
            var clockTimeMilliseconds = 0;
            
            // If the delta is over our max precision, we send MAX value and jitter will be ignored by the receiver.
            if (deltaSendTimeInMS >= MaxJitterPrecisionInMS)
            {
                clockTimeMilliseconds = MaxJitterClockTimeValue;
            }
            else
            {
                // TODO: Proper
                // Get the fractional part (milliseconds) of the clock time
                clockTimeMilliseconds = 0;

                // Ensure we don't overflow
                clockTimeMilliseconds &= MaxJitterClockTimeValue;
            }
            
            writer.SerializeInt((uint)clockTimeMilliseconds, MaxJitterClockTimeValue + 1);

            _previousPacketSendTimeInS = packetSentTimeInS;
        }
        
        // Write server frame time
        {
            var bHasServerFrameTime = LastHasServerFrameTime;
            writer.WriteBit(bHasServerFrameTime);

            if (bHasServerFrameTime && Driver!.IsServer())
            {
                // Write data used to calculate link latency
                // TODO: Proper
                const int FrameTime = 123;
                var frameTimeByte = (byte) Math.Min(Math.Floor((double)(FrameTime * 1000)), 255);
                writer.WriteByte(frameTimeByte);
            }
        }
        
        _headerMarkForPacketInfo.Reset();
        
        // Revert to the correct bit writing place
        currentMark.PopWithoutClear(writer);
    }

    private void WriteDummyPacketInfo(FBitWriter writer)
    {
        var bHasPacketInfoPayload = _bFlushedNetThisFrame == false;
        
        writer.WriteBit(bHasPacketInfoPayload);

        if (bHasPacketInfoPayload)
        {
            // Pre-insert the bits since the final time values will be calculated and inserted right before LowLevelSend
            _headerMarkForPacketInfo.Init(writer);

            Span<byte> dummyJitterClockTime = stackalloc byte[4];
            writer.SerializeBits(dummyJitterClockTime, NumBitsForJitterClockTimeInHeader);

            var bHasServerFrameTime = LastHasServerFrameTime;
            writer.WriteBit(bHasServerFrameTime);

            if (bHasServerFrameTime && Driver!.IsServer()) // false
            {
                const byte dummyFrameTimeByte = 0;
                writer.WriteByte(dummyFrameTimeByte);
            }
        }

        _bSendBufferHasDummyPacketInfo = bHasPacketInfoPayload;
    }

    private int WriteBitsToSendBufferInternal(byte[]? bits, int sizeInBits, byte[]? extraBits, int extraSizeInBits, EWriteBitsDataType dataType)
    {
        // Remember start position in case we want to undo this write, no meaning to undo the header write as this is only used to pop bunches and the header should not count towards the bunch
        // Store this after the possible flush above so we have the correct start position in the case that we do flush
        LastStart = new FBitWriterMark(SendBuffer);
        
        // Add the bits to the queue
        if (sizeInBits != 0)
        {
            if (bits == null)
            {
                throw new UnrealNetException("bits should not be null if a size is set");
            }
            
            SendBuffer.SerializeBits(bits, sizeInBits);
            ValidateSendBuffer();
        }
        
        // Add any extra bits
        if (extraSizeInBits != 0)
        {
            if (extraBits == null)
            {
                throw new UnrealNetException("extraBits should not be null if a size is set");
            }
            
            SendBuffer.SerializeBits(extraBits, extraSizeInBits);
            ValidateSendBuffer();
        }

        // 242 after
        var rememberedPacketId = OutPacketId;

        if (dataType == EWriteBitsDataType.Bunch)
        {
            NumBunchBits += sizeInBits + extraSizeInBits;
        }
        
        // Flush now if we are full
        if (GetFreeSendBufferBits() == 0)
        {
            FlushNet();
        }

        return rememberedPacketId;
    }

    private int WriteBitsToSendBuffer(byte[]? bits, int sizeInBits, byte[]? extraBits = null, int extraSizeInBits = 0, EWriteBitsDataType dataType = EWriteBitsDataType.Unknown)
    {
        // Flush packet as needed and begin new packet
        PrepareWriteBitsToSendBuffer(sizeInBits, extraSizeInBits);
        
        // Write the data and flush if the packet is full, return value is the packetId into which the data was written
        return WriteBitsToSendBufferInternal(bits, sizeInBits, extraBits, extraSizeInBits, dataType);
    }

    /// <summary>
    ///     Returns number of bits left in current packet that can be used without causing a flush
    /// </summary>
    private long GetFreeSendBufferBits()
    {
        // If we haven't sent anything yet, make sure to account for the packet header + trailer size
        // Otherwise, we only need to account for trailer size
        var extraBits = (SendBuffer.GetNumBits() > 0) ? MaxPacketTrailerBits : MaxPacketHeaderBits + MaxPacketTrailerBits;
        var numberOfFreeBits = SendBuffer.GetMaxBits() - (SendBuffer.GetNumBits() + extraBits);

        return numberOfFreeBits;
    }

    private void ValidateSendBuffer()
    {
        if (SendBuffer.IsError())
        {
            Logger.Fatal("ValidateSendBuffer: Out.IsError() == true. NumBits: {A}, NumBytes: {B}, MaxBits: {C}", SendBuffer.GetNumBits(), SendBuffer.GetNumBytes(), SendBuffer.GetMaxBits());
        }
    }

    private protected void InitSendBuffer()
    {
        var finalBufferSize = (MaxPacket * 8) - MaxPacketHandlerBits;
        if (finalBufferSize == SendBuffer.GetMaxBits())
        {
            SendBuffer.Reset();
        }
        else
        {
            SendBuffer = new FBitWriter(finalBufferSize);
        }

        _headerMarkForPacketInfo.Reset();
        
        ResetPacketBitCounts();
        
        ValidateSendBuffer();
    }

    private void ResetPacketBitCounts()
    {
        NumPacketIdBits = 0;
        NumBunchBits = 0;
        NumAckBits = 0;
        NumPaddingBits = 0;
    }

    public void SetPlayerOnlinePlatformName(FName inPlayerOnlinePlatformName)
    {
        _playerOnlinePlatformName = inPlayerOnlinePlatformName;
    }

    public void FlushNet(bool bIgnoreSimulation = false)
    {
        if (Driver == null)
        {
            throw new UnrealNetException();
        }
        
        // Update info.
        ValidateSendBuffer();
        LastEnd = new FBitWriterMark();
        TimeSensitive = false;
        
        // If there is any pending data to send, send it.
        if (SendBuffer.GetNumBits() != 0 || HasDirtyAcks != 0 || (Driver.GetElapsedTime() - _lastSendTime > Driver.KeepAliveTime && !IsInternalAck() && State != EConnectionState.USOCK_Closed))
        {
            // Due to the PacketHandler handshake code, servers must never send the client data,
            // before first receiving a client control packet (which is taken as an indication of a complete handshake).
            if (!HasReceivedClientPacket())
            {
                Logger.Debug("Attempting to send data before handshake is complete. {Channel}", this);
                Close();
                InitSendBuffer();
                return;
            }

            var traits = new FOutPacketTraits();
            
            // If sending keepalive packet or just acks, still write the packet header
            if (SendBuffer.GetNumBits() == 0)
            {
                WriteBitsToSendBuffer(null, 0); // This will force the packet header to be written

                traits.IsKeepAlive = true;
            }

            if (Handler != null)
            {
                Handler.OutgoingHigh(SendBuffer);
            }

            var packetSentTimeInS = FPlatformTime.Seconds();
            
            // Write the UNetConnection-level termination bit
            SendBuffer.WriteBit(true);
            
            // Refresh outgoing header with latest data
            if (!IsInternalAck())
            {
                // if we update ack, we also update received ack associated with outgoing seq
                // so we know how many ack bits we need to write (which is updated in received packet)
                WritePacketHeader(SendBuffer);

                WriteFinalPacketInfo(SendBuffer, packetSentTimeInS);
            }
            
            ValidateSendBuffer();

            var numStrayBits = SendBuffer.GetNumBits();

            traits.NumAckBits = (uint)NumAckBits;
            traits.NumBunchBits = (uint)NumBunchBits;

            // Removed packet emulation

            if (Driver.IsNetResourceValid())
            {
                LowLevelSend(SendBuffer.GetData(), (int)SendBuffer.GetNumBits(), traits);
            }
            
            // Update stuff.
            var index = OutPacketId & (OutLagPacketId.Length - 1);
            
            // Remember the actual time this packet was sent out, so we can compute ping when the ack comes back
            OutLagPacketId[index] = OutPacketId;
            OutLagTime[index] = packetSentTimeInS;
            
            // Increase outgoing sequence number
            if (!IsInternalAck())
            {
                PacketNotify.CommitAndIncrementOutSeq();
            }
            
            // TODO: Make sure that we always push an ChannelRecordEntry for each transmitted packet even if it is empty

            ++OutPacketId;
            
            // TODO: Increment things

            _lastSendTime = Driver.GetElapsedTime();

            _bFlushedNetThisFrame = true;
            
            InitSendBuffer();
        }
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

    internal void SetClientLoginState(EClientLoginState newState)
    {
        if (ClientLoginState == newState)
        {
            Logger.Verbose("SetClientLoginState: State same: {Old}", newState);
            return;
        }
        
        Logger.Verbose("SetClientLoginState: State changing from {Old} to {New}", ClientLoginState, newState);

        ClientLoginState = newState;
    }

    private protected void SetExpectedClientLoginMsgType(NMT newType)
    {
        if (ExpectedClientLoginMsgType == newType)
        {
            Logger.Verbose("SetExpectedClientLoginMsgType: Type same: [{Old}]", newType);
            return;
        }
        
        Logger.Verbose("SetExpectedClientLoginMsgType: Type changing from [{Old}] to [{New}]", ExpectedClientLoginMsgType, newType);

        ExpectedClientLoginMsgType = newType;
    }

    public bool IsClientMsgTypeValid(NMT clientMsgType)
    {
        if (ClientLoginState == EClientLoginState.LoggingIn)
        {
            if (clientMsgType != ExpectedClientLoginMsgType)
            {
                Logger.Debug("IsClientMsgTypeValid FAILED: (ClientMsgType != ExpectedClientLoginMsgType) Remote Address={Address}", LowLevelGetRemoteAddress());
                return false;
            }
        }
        else
        {
            if (clientMsgType == NMT.Hello || clientMsgType == NMT.Login)
            {
                Logger.Debug("IsClientMsgTypeValid FAILED: Invalid msg after being logged in - Remote Address={Address}", LowLevelGetRemoteAddress());
                return false;
            }
        }

        return true;
    }

    private bool HasReceivedClientPacket()
    {
        return IsInternalAck() || !Driver!.IsServer() || InReliable[0] != InitInReliable;
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