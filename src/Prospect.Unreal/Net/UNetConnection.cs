using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Header;
using Prospect.Unreal.Net.Packets.Header.Sequence;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net;

public abstract class UNetConnection
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

    private List<FBitReader>? _packetOrderCache;
    private int _packetOrderCacheStartIdx;
    private int _packetOrderCacheCount;
    private bool _bFlushingPacketOrderCache;
    
    private bool _bInternalAck;
    private bool _bReplay;

    public UNetConnection()
    {
        _packetOrderCache = null;
        _packetOrderCacheStartIdx = 0;
        _packetOrderCacheCount = 0;
        _bFlushingPacketOrderCache = false;
        _bInternalAck = false;
        _bReplay = false;
        
        Driver = null;
        PackageMap = null;
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
        var bSickAcp = false;
        
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
                    bunch.ChType = (bunch.bReliable || bunch.bOpen) ? (int) reader.ReadInt(EChannelType.CHTYPE_MAX) : EChannelType.CHTYPE_None;

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
            }
        }

        throw new NotImplementedException();
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

    public void CreateChannelByName(string chName, EChannelCreateFlags createFlags, int channelIndex)
    {
        // TODO: Implement
        Logger.Verbose("Creating channel {Name} with index {Index}", chName, channelIndex);
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

    private static int BestSignedDifference(int value, int reference, int max)
    {
        return ((value - reference + max / 2) & (max - 1)) - max / 2;
    }

    private static int MakeRelative(int value, int reference, int max)
    {
        return reference + BestSignedDifference(value, reference, max);
    }
}