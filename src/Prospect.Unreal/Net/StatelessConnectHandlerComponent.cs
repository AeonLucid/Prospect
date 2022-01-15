using System.Buffers.Binary;
using System.Collections;
using System.Net;
using System.Security.Cryptography;
using Prospect.Unreal.Core;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net;

public class StatelessConnectHandlerComponent : HandlerComponent
{
    private static readonly ILogger Logger = Log.ForContext<StatelessConnectHandlerComponent>();

    private const int SecretByteSize = 64;
    private const int SecretCount = 2;
    private const int CookieByteSize = 20;

    private const int HandshakePacketSizeBits = 227;
    private const int RestartHandshakePacketSizeBits = 2;
    private const int RestartResponseSizeBits = 387;

    private const float SecretUpdateTime = 15.0f;
    private const float SecretUpdateTimeVariance = 5.0f;

    private const float MaxCookieLifetime = ((SecretUpdateTime + SecretUpdateTimeVariance) * SecretCount);
    private const float MinCookieLifetime = SecretUpdateTime;
    
    private UNetDriver? _driver;

    /// <summary>
    ///     The serverside-only 'secret' value, used to help with generating cookies.
    /// </summary>
    private byte[][] _handshakeSecret;

    /// <summary>
    ///     Which of the two secret values above is active (values are changed frequently, to limit replay attacks)
    /// </summary>
    private byte _activeSecret;

    /// <summary>
    ///     The time of the last secret value update
    /// </summary>
    private double _lastSecretUpdateTimestamp;
    
    /// <summary>
    ///     The last address to successfully complete the handshake challenge
    /// </summary>
    private IPEndPoint? _lastChallengeSuccessAddress;

    /// <summary>
    ///     The initial server sequence value, from the last successful handshake
    /// </summary>
    private int _lastServerSequence;

    /// <summary>
    ///     The initial client sequence value, from the last successful handshake
    /// </summary>
    private int _lastClientSequence;

    /// <summary>
    ///     The last time a handshake packet was sent - used for detecting failed sends.
    /// </summary>
    private double _lastClientSendTimestamp;

    /// <summary>
    ///     The local (client) time at which the challenge was last updated
    /// </summary>
    private double _lastChallengeTimestamp;

    /// <summary>
    ///     The local (client) time at which the last restart handshake request was receive
    /// </summary>
    private double _lastRestartPacketTimestamp;

    /// <summary>
    ///     The SecretId value of the last challenge response sent
    /// </summary>
    private byte _lastSecretId;

    /// <summary>
    ///     The Timestamp value of the last challenge response sent
    /// </summary>
    private double _lastTimestamp;

    /// <summary>
    ///     The Cookie value of the last challenge response sent. Will differ from AuthorisedCookie, if a handshake retry is triggered.
    /// </summary>
    private byte[] _lastCookie = new byte[CookieByteSize];

    /// <summary>
    ///     Client: Whether or not we are in the middle of a restarted handshake.
    ///     Server: Whether or not the last handshake was a restarted handshake.
    /// </summary>
    private bool _bRestartedHandshake;

    /// <summary>
    ///     The cookie which completed the connection handshake.
    /// </summary>
    private byte[] _authorisedCookie;

    /// <summary>
    ///     The magic header which is prepended to all packets
    /// </summary>
    private BitArray _magicHeader;
    
    public StatelessConnectHandlerComponent(PacketHandler handler) : base(handler, nameof(StatelessConnectHandlerComponent))
    {
        SetActive(true);
        
        RequiresHandshake = true;
        
        _handshakeSecret = new byte[2][];
        _activeSecret = byte.MaxValue;
        _lastChallengeSuccessAddress = null;
        _lastServerSequence = 0;
        _lastClientSequence = 0;
        _bRestartedHandshake = false;
        _authorisedCookie = new byte[CookieByteSize];
        _magicHeader = new BitArray(0);
    }

    public void GetChallengeSequences(out int serverSequence, out int clientSequence)
    {
        serverSequence = _lastServerSequence;
        clientSequence = _lastClientSequence;
    }

    public void ResetChallengeData()
    {
        _lastChallengeSuccessAddress = null;
        _bRestartedHandshake = false;
        _lastServerSequence = 0;
        _lastClientSequence = 0;
        
        for (var i = 0; i < _authorisedCookie.Length; i++)
        {
            _authorisedCookie[i] = 0;
        }
    }

    public void SetDriver(UNetDriver driver)
    {
        _driver = driver;

        if (Handler.Mode == HandlerMode.Server)
        {
            var statelessComponent = _driver.StatelessConnectComponent;
            if (statelessComponent != null)
            {
                if (statelessComponent == this)
                {
                    UpdateSecret();
                }
                else
                {
                    InitFromConnectionless(statelessComponent);
                }
            }
        }
    }

    public override void Initialize()
    {
        if (Handler.Mode == HandlerMode.Server)
        {
            Initialized();
        }
    }

    private void InitFromConnectionless(StatelessConnectHandlerComponent connectionlessHandler)
    {
        Logger.Debug("InitFromConnectionless");
        
        // Store the cookie/address used for the handshake, to enable server ack-retries
        _lastChallengeSuccessAddress = connectionlessHandler._lastChallengeSuccessAddress;
        
        Buffer.BlockCopy(connectionlessHandler._authorisedCookie, 0, _authorisedCookie, 0, _authorisedCookie.Length);
    }

    public override void CountBytes(FArchive ar)
    {
        throw new NotImplementedException();
    }

    public override void Incoming(FBitReader packet)
    {
        if (_magicHeader.Length > 0)
        {
            // Skip magic header.
            packet.Pos += _magicHeader.Length;
        }
        
        var bHandshakePacket = packet.ReadBit() && !packet.IsError();
        if (bHandshakePacket)
        {
            var bRestartHandshake = false;
            var secretId = (byte) 0;
            var timestamp = 1.0d;
            Span<byte> cookie = stackalloc byte[CookieByteSize];
            Span<byte> origCookie = stackalloc byte[CookieByteSize];

            bHandshakePacket = ParseHandshakePacket(packet, ref bRestartHandshake, ref secretId, ref timestamp, cookie, origCookie);

            if (bHandshakePacket)
            {
                if (Handler.Mode == HandlerMode.Client)
                {
                    if (State == HandlerComponentState.UnInitialized || State == HandlerComponentState.InitializedOnLocal)
                    {
                        if (bRestartHandshake)
                        {
                            //UE_LOG(LogHandshake, Log, TEXT("Ignoring restart handshake request, while already restarted."));
                        }
                        // Receiving challenge, verify the timestamp is > 0.0f
                        else if (timestamp > 0.0)
                        {
                            _lastChallengeTimestamp = _driver?.GetElapsedTime() ?? 0.0;

                            SendChallengeResponse(secretId, timestamp, cookie);

                            // Utilize this state as an intermediary, indicating that the challenge response has been sent
                            SetState(HandlerComponentState.InitializedOnLocal);
                        }
                        // Receiving challenge ack, verify the timestamp is < 0.0f
                        else if (timestamp < 0.0)
                        {
                            if (!_bRestartedHandshake)
                            { 
                                var ServerConn =  _driver?.ServerConnection;

                                // Extract the initial packet sequence from the random Cookie data
                                if (ServerConn != null)
                                {
                                    _lastServerSequence = BinaryPrimitives.ReadInt16LittleEndian(cookie) & (UNetConnection.MaxPacketId - 1);
                                    _lastClientSequence = BinaryPrimitives.ReadInt16LittleEndian(cookie.Slice(2)) & (UNetConnection.MaxPacketId - 1);

                                    ServerConn.InitSequence(_lastServerSequence, _lastClientSequence);
                                }
                                // Save the final authorized cookie
                                cookie.CopyTo(_authorisedCookie);
                            }

                            // Now finish initializing the handler - flushing the queued packet buffer in the process.
                            SetState(HandlerComponentState.Initialized);
                            Initialized();

                            _bRestartedHandshake = false;
                        }
                    }
                    else if (bRestartHandshake)
                    {
                        var bValidAuthCookie = !_authorisedCookie.All(b => b == 0);

                        // The server has requested us to restart the handshake process - this is because
                        // it has received traffic from us on a different address than before.
                        if (bValidAuthCookie)
                        {
                            bool bPassedDelayCheck = false;
                            bool bPassedDualIPCheck = false;
                            double CurrentTime = FPlatformTime.Seconds();

                            if (!_bRestartedHandshake)
                            {
                                var ServerConn = _driver?.ServerConnection;
                                // todo
                                //double LastNetConnPacketTime = ServerConn?.lastre(ServerConn != nullptr ? ServerConn->LastReceiveRealtime : 0.0);

                                // The server may send multiple restart handshake packets, so have a 10 second delay between accepting them
                                //bPassedDelayCheck = (CurrentTime - LastClientSendTimestamp) > 10.0;

                                // Some clients end up sending packets duplicated over multiple IP's, triggering the restart handshake.
                                // Detect this by checking if any restart handshake requests have been received in roughly the last second
                                // (Dual IP situations will make the server send them constantly) - and override the checks as a failsafe,
                                // if no NetConnection packets have been received in the last second.
                                //double LastRestartPacketTimeDiff = CurrentTime - _lastRestartPacketTimestamp;
                                //double LastNetConnPacketTimeDiff = CurrentTime - LastNetConnPacketTime;

                                //bPassedDualIPCheck = _lastRestartPacketTimestamp == 0.0 || LastRestartPacketTimeDiff > 1.1 || LastNetConnPacketTimeDiff > 1.0;
                            }

                            _lastRestartPacketTimestamp = CurrentTime;
                            double LastLogStartTime = 0.0;
                            int LogCounter = 0;
                            Func<bool> WithinHandshakeLogLimit = new Func<bool>(() => {
                                const double LogCountPeriod = 30.0;
                                const byte MaxLogCount = 3;
                                double CurTimeApprox = _driver.GetElapsedTime();
                                bool bWithinLimit = false;
                                if ((CurTimeApprox - LastLogStartTime) > LogCountPeriod)
                                {
                                    LastLogStartTime = CurTimeApprox;
                                    LogCounter = 1;
                                    bWithinLimit = true;
                                }
                                else if (LogCounter < MaxLogCount)
                                {
                                    LogCounter++;
                                    bWithinLimit = true;
                                }

                                return bWithinLimit;
                            });

                            if (!_bRestartedHandshake && bPassedDelayCheck && bPassedDualIPCheck)
                            {
                                Logger.Verbose("Beginning restart handshake process.");

                                _bRestartedHandshake = true;

                                SetState(HandlerComponentState.UnInitialized);
                                NotifyHandshakeBegin();
                            }
                            else if (WithinHandshakeLogLimit())
                            {
                                if (_bRestartedHandshake)
                                {
                                    Logger.Verbose("Ignoring restart handshake request, while already restarted (this is normal).");
                                }
                                else if (!bPassedDelayCheck)
                                {
                                    Logger.Verbose("Ignoring restart handshake request, due to < 10 seconds since last handshake.");
                                }
                                else // if (!bPassedDualIPCheck)
                                {
                                    Logger.Verbose("Ignoring restart handshake request, due to recent NetConnection packets.");
                                }
                            }
                        }
                        else
                        {
                            Logger.Verbose("Server sent restart handshake request, when we don't have an authorised cookie.");
                        }
                    }
                    else
                    {
                        // Ignore, could be a dupe/out-of-order challenge packet
                    }
                }
                else if (Handler.Mode == HandlerMode.Server)
                {
                    if (_lastChallengeSuccessAddress != null)
                    {
                        // The server should not be receiving handshake packets at this stage - resend the ack in case it was lost.
                        // In this codepath, this component is linked to a UNetConnection, and the Last* values below, cache the handshake info.
                        SendChallengeAck(_lastChallengeSuccessAddress, _authorisedCookie);
                    }
                }
            }
            else
            {
                packet.SetError();
                Logger.Error("Incoming: Error reading handshake packet");
            }
        }
        else if (packet.IsError())
        {
            Logger.Error("Incoming: Error reading handshake bit from packet");
        } 
        else if (_lastChallengeSuccessAddress != null && Handler.Mode == HandlerMode.Server)
        {
            _lastChallengeSuccessAddress = null;
        }
    }

    public override void Outgoing(ref FBitWriter packet, FOutPacketTraits traits)
    {
        const bool bHandshakePacket = false;
        
        var newPacket = new FBitWriter(GetAdjustedSizeBits((int)packet.GetNumBits()) + 1, true, false);

        if (_magicHeader.Length > 0)
        {
            newPacket.SerializeBits(_magicHeader, _magicHeader.Length);
        }
        
        newPacket.WriteBit(bHandshakePacket);
        newPacket.SerializeBits(packet.GetData(), packet.GetNumBits());

        packet = newPacket;
    }

    public override void IncomingConnectionless(FIncomingPacketRef packetRef)
    {
        var packet = packetRef.Packet;
        var address = packetRef.Address;

        if (_magicHeader.Length > 0)
        {
            // Skip magic header.
            packet.Pos += _magicHeader.Length;
        }
        
        var bHandshakePacket = packet.ReadBit() && !packet.IsError();

        _lastChallengeSuccessAddress = null;
        
        if (bHandshakePacket)
        {
            var bRestartHandshake = false;
            var secretId = (byte) 0;
            var timestamp = 1.0d;
            Span<byte> cookie = stackalloc byte[CookieByteSize];
            Span<byte> origCookie = stackalloc byte[CookieByteSize];

            bHandshakePacket = ParseHandshakePacket(packet, ref bRestartHandshake, ref secretId, ref timestamp, cookie, origCookie);

            if (bHandshakePacket)
            {
                if (Handler.Mode == HandlerMode.Server)
                {
                    var bInitialConnect = timestamp == 0.0;
                    if (bInitialConnect)
                    {
                        SendConnectChallenge(address);
                    } 
                    else if (_driver != null)
                    {
                        var bChallengeSuccess = false;
                        var cookieDelta = _driver.GetElapsedTime() - timestamp;
                        var secretDelta = timestamp - _lastSecretUpdateTimestamp;
                        var bValidCookieLifetime = cookieDelta >= 0.0 && (MaxCookieLifetime - cookieDelta) > 0.0;
                        var bValidSecretIdTimestamp = (secretId == _activeSecret) ? (secretDelta >= 0.0) : (secretDelta <= 0.0);

                        if (bValidCookieLifetime && bValidSecretIdTimestamp)
                        {
                            // Regenerate the cookie from the packet info, and see if the received cookie matches the regenerated one
                            Span<byte> regenCookie = stackalloc byte[CookieByteSize];
                            
                            GenerateCookie(address, secretId, timestamp, regenCookie);

                            bChallengeSuccess = cookie.SequenceEqual(regenCookie);

                            if (bChallengeSuccess)
                            {
                                if (bRestartHandshake)
                                {
                                    origCookie.CopyTo(_authorisedCookie);
                                }
                                else
                                {
                                    var seqA = BinaryPrimitives.ReadInt16LittleEndian(cookie);
                                    var seqB = BinaryPrimitives.ReadInt16LittleEndian(cookie.Slice(2));

                                    _lastServerSequence = seqA & (UNetConnection.MaxPacketId - 1);
                                    _lastClientSequence = seqB & (UNetConnection.MaxPacketId - 1);
                                    
                                    cookie.CopyTo(_authorisedCookie);
                                }

                                _bRestartedHandshake = bRestartHandshake;
                                _lastChallengeSuccessAddress = address;

                                // Now ack the challenge response - the cookie is stored in AuthorisedCookie, to enable retries
                                SendChallengeAck(address, _authorisedCookie);
                            }
                        }
                    }
                }
            }
            else
            {
                packet.SetError();
                
                Logger.Error("Error reading handshake packet");
            }
        }
        else if (packet.IsError())
        {
            Logger.Error("Error reading handshake bit from packet");
        }
        // Late packets from recently disconnected clients may incorrectly trigger this code path, so detect and exclude those packets
        else if (!packet.IsError() && !packetRef.Traits.FromRecentlyDisconnected)
        {
            // The packet was fine but not a handshake packet - an existing client might suddenly be communicating on a different address.
            // If we get them to resend their cookie, we can update the connection's info with their new address.
            SendRestartHandshakeRequest(address);
        }
    }

    private bool ParseHandshakePacket(
        FBitReader packet, 
        ref bool bOutRestartHandshake, 
        ref byte outSecretId,
        ref double outTimestamp, Span<byte> outCookie, Span<byte> outOrigCookie)
    {
        var bValidPacket = false;
        var bitsLeft = packet.GetBitsLeft();
        var bHandshakePacketSize = bitsLeft == (HandshakePacketSizeBits - 1);
        var bRestartResponsePacketSize = bitsLeft == (RestartHandshakePacketSizeBits - 1);

        if (bHandshakePacketSize || bRestartResponsePacketSize)
        {
            bOutRestartHandshake = packet.ReadBit();
            outSecretId = (byte)(packet.ReadBit() ? 1 : 0);
            outTimestamp = packet.ReadDouble();
            packet.Serialize(outCookie, CookieByteSize);

            if (bRestartResponsePacketSize)
            {
                packet.Serialize(outOrigCookie, CookieByteSize);
            }

            bValidPacket = !packet.IsError();
        } 
        else if (bitsLeft == (RestartHandshakePacketSizeBits - 1))
        {
            bOutRestartHandshake = packet.ReadBit();
            bValidPacket = !packet.IsError() && bOutRestartHandshake && Handler.Mode == HandlerMode.Client;
        }

        return bValidPacket;
    }

    private void GenerateCookie(IPEndPoint clientAddress, byte secretId, double timestamp, Span<byte> outCookie)
    {
        using var writer = new FBitWriter(64 * 8);

        writer.WriteDouble(timestamp);
        writer.WriteString(clientAddress.ToString());

        HMACSHA1.HashData(_handshakeSecret[secretId], writer.GetData().AsSpan((int)writer.GetNumBytes()), outCookie);
    }

    public override void NotifyHandshakeBegin()
    {
        if (Handler.Mode != HandlerMode.Client) return;

        var ServerConn = _driver?.ServerConnection;

        if (_driver == null || ServerConn == null)
        {
            Logger.Warning("Tried to send handshake connect packet without a server connection.");
            return;
        }

        using var ackPacket = new FBitWriter(GetAdjustedSizeBits(HandshakePacketSizeBits) + 1);
        var bHandshakePacket = (byte)1;
        var bRestartHandshake = (byte)(_bRestartedHandshake ? 1 : 0);
        var secretIdPad = (byte)0;

        //if (MagicHeader.Num() > 0) challengePacket.SerializeBits(MagicHeader.GetData(), MagicHeader.Num());

        ackPacket.WriteBit(bHandshakePacket);
        // In order to prevent DRDoS reflection amplification attacks, clients must pad the packet to match server packet size
        ackPacket.WriteBit(bRestartHandshake);
        ackPacket.WriteBit(secretIdPad);
        ackPacket.Serialize(new Byte[28], 28);

        Logger.Verbose("NotifyHandshakeBegin");

        CapHandshakePacket(ackPacket);

        // Disable PacketHandler parsing, and send the raw packet
        ServerConn.Handler?.SetRawSend(true);

        if (_driver.IsNetResourceValid())
        {
            ServerConn.LowLevelSend(ackPacket.GetData(), (int)ackPacket.GetNumBits(), new FOutPacketTraits());
        }

        ServerConn.Handler?.SetRawSend(false);

        _lastClientSendTimestamp = FPlatformTime.Seconds();
    }

    private void SendConnectChallenge(IPEndPoint address)
    {
        if (_driver == null)
        {
            Logger.Warning("Tried to send connect challenge without driver");
            return;
        }

        using var challengePacket = new FBitWriter(GetAdjustedSizeBits(HandshakePacketSizeBits) + 1);
        var bHandshakePacket = (byte)1;
        var bRestartHandshake = (byte)0;
        var timestamp = _driver.GetElapsedTime();
        Span<byte> cookie = stackalloc byte[CookieByteSize];

        GenerateCookie(address, _activeSecret, timestamp, cookie);

        if (_magicHeader.Length > 0)
        {
            challengePacket.SerializeBits(_magicHeader, _magicHeader.Count);
        }

        challengePacket.WriteBit(bHandshakePacket);
        challengePacket.WriteBit(bRestartHandshake);
        challengePacket.WriteBit(_activeSecret);
        challengePacket.WriteDouble(timestamp);
        challengePacket.Serialize(cookie, cookie.Length);
        
        Logger.Verbose("SendConnectChallenge. Timestamp: {Timestamp}, Cookie: {Cookie}", timestamp, Convert.ToHexString(cookie));

        CapHandshakePacket(challengePacket);

        var connectionlessHandler = _driver.ConnectionlessHandler;
        
        connectionlessHandler?.SetRawSend(true);

        if (_driver.IsNetResourceValid())
        {
            _driver.LowLevelSend(address, challengePacket.GetData(), (int)challengePacket.GetNumBits(), new FOutPacketTraits());
        }

        connectionlessHandler?.SetRawSend(false);
    }

    private void SendChallengeResponse(byte inSecretId, double timestamp, Span<byte> cookie)
    {
        var ServerConn = _driver?.ServerConnection;

        if (ServerConn == null)
        {
            Logger.Warning("Tried to send connect challenge without driver");
            return;
        }
        using var challengePacket = new FBitWriter(GetAdjustedSizeBits(_bRestartedHandshake ? RestartResponseSizeBits : HandshakePacketSizeBits) + 1);
        var bHandshakePacket = (byte)1;
        var bRestartHandshake = (byte)(_bRestartedHandshake ? 1 : 0);

        //if (MagicHeader.Num() > 0)
        {
            //challengePacket.SerializeBits(MagicHeader.GetData(), MagicHeader.Num());
        }

        challengePacket.WriteBit(bHandshakePacket);
        challengePacket.WriteBit(bRestartHandshake);
        challengePacket.WriteBit(inSecretId);
        challengePacket.WriteDouble(timestamp);
        challengePacket.Serialize(cookie, cookie.Length);

        if (_bRestartedHandshake)
        {
            //challengePacket.Serialize(AuthorisedCookie, COOKIE_BYTE_SIZE);
        }

        Logger.Verbose("SendChallengeResponse. Timestamp: {Timestamp}, Cookie: {Cookie}", timestamp, Convert.ToHexString(cookie));

        CapHandshakePacket(challengePacket);

        ServerConn.Handler?.SetRawSend(true);

        if (_driver.IsNetResourceValid())
        {
            ServerConn.LowLevelSend(challengePacket.GetData(), (int)challengePacket.GetNumBits(), new FOutPacketTraits());
        }

        ServerConn.Handler?.SetRawSend(false);

        var CurSequence = cookie;

        _lastClientSendTimestamp = FPlatformTime.Seconds();
        _lastSecretId = inSecretId;
        _lastTimestamp = timestamp;

        _lastServerSequence = BinaryPrimitives.ReadInt16LittleEndian(cookie) & (UNetConnection.MaxPacketId - 1);
        _lastClientSequence = BinaryPrimitives.ReadInt16LittleEndian(cookie.Slice(2)) & (UNetConnection.MaxPacketId - 1);

        _lastCookie = cookie.ToArray();
    }

    private void SendChallengeAck(IPEndPoint address, byte[] inCookie)
    {
        if (_driver == null)
        {
            Logger.Warning("Tried to send challenge ack without driver");
            return;
        }

        using var ackPacket = new FBitWriter(GetAdjustedSizeBits(HandshakePacketSizeBits) + 1);
        var bHandshakePacket = (byte)1;
        var bRestartHandshake = (byte)0;
        var timestamp = -1.0d;

        if (_magicHeader.Length > 0)
        {
            ackPacket.SerializeBits(_magicHeader, _magicHeader.Count);
        }
        
        ackPacket.WriteBit(bHandshakePacket);
        ackPacket.WriteBit(bRestartHandshake);
        ackPacket.WriteBit(bHandshakePacket);
        ackPacket.WriteDouble(timestamp);
        ackPacket.Serialize(inCookie, CookieByteSize);
        
        Logger.Verbose("SendChallengeAck. InCookie: {Cookie}", inCookie);
        
        CapHandshakePacket(ackPacket);

        var connectionlessHandler = _driver.ConnectionlessHandler;
        
        connectionlessHandler?.SetRawSend(true);

        if (_driver.IsNetResourceValid())
        {
            _driver.LowLevelSend(address, ackPacket.GetData(), (int)ackPacket.GetNumBits(), new FOutPacketTraits());
        }

        connectionlessHandler?.SetRawSend(false);
    }

    private void SendRestartHandshakeRequest(IPEndPoint address)
    {
        if (_driver == null)
        {
            Logger.Warning("Tried to send restart handshake without driver");
            return;
        }

        using var restartPacket = new FBitWriter(GetAdjustedSizeBits(RestartHandshakePacketSizeBits) + 1);
        var bHandshakePacket = (byte)1;
        var bRestartHandshake = (byte)1;

        if (_magicHeader.Length > 0)
        {
            restartPacket.SerializeBits(_magicHeader, _magicHeader.Count);
        }
        
        restartPacket.WriteBit(bHandshakePacket);
        restartPacket.WriteBit(bRestartHandshake);
        
        CapHandshakePacket(restartPacket);

        var connectionlessHandler = _driver.ConnectionlessHandler;
        
        connectionlessHandler?.SetRawSend(true);

        if (_driver.IsNetResourceValid())
        {
            _driver.LowLevelSend(address, restartPacket.GetData(), (int)restartPacket.GetNumBits(), new FOutPacketTraits());
        }

        connectionlessHandler?.SetRawSend(false);
    }

    public override bool CanReadUnaligned()
    {
        return true;
    }

    private void CapHandshakePacket(FBitWriter handshakePacket)
    {
        var numBits = handshakePacket.GetNumBits() - GetAdjustedSizeBits(0);

        if (numBits != HandshakePacketSizeBits && 
            numBits != RestartHandshakePacketSizeBits &&
            numBits != RestartResponseSizeBits)
        {
            Logger.Warning("Invalid handshake packet size bits");
        }
        
        // Termination bit.
        handshakePacket.WriteBit(1);
    }

    public override bool IsValid()
    {
        return true;
    }

    private int GetAdjustedSizeBits(int inSizeBits)
    {
        return _magicHeader.Length + inSizeBits;
    }

    public bool HasPassedChallenge(IPEndPoint address, out bool bOutRestartedHandshake)
    {
        bOutRestartedHandshake = _bRestartedHandshake;

        return _lastChallengeSuccessAddress != null && 
               _lastChallengeSuccessAddress.Equals(address);
    }

    public void UpdateSecret()
    {
        _lastSecretUpdateTimestamp = _driver?.GetElapsedTime() ?? 0.0;

        if (_activeSecret == byte.MaxValue)
        {
            _handshakeSecret[0] = new byte[SecretByteSize];
            _handshakeSecret[1] = new byte[SecretByteSize];

            // Randomize other secret.
            var arr = _handshakeSecret[1];
            
            for (var i = 0; i < SecretByteSize; i++)
            {
                arr[i] = (byte)(Random.Shared.Next() % 255);
            }

            _activeSecret = 0;
        }
        else
        {
            _activeSecret = (byte)(_activeSecret == 1 ? 0 : 1);
        }
        
        // Randomize current secret.
        var curArray = _handshakeSecret[_activeSecret];
            
        for (var i = 0; i < SecretByteSize; i++)
        {
            curArray[i] = (byte)(Random.Shared.Next() % 255);
        }
    }
    
    public override int GetReservedPacketBits()
    {
        return _magicHeader.Length + 1;
    }

    public override void Tick(float deltaTime)
    {
        if (Handler.Mode == HandlerMode.Client)
        {
            if (State != HandlerComponentState.Initialized && _lastClientSendTimestamp != 0.0)
            {
                double LastSendTimeDiff = FPlatformTime.Seconds() - _lastClientSendTimestamp;

                if (LastSendTimeDiff > 1.0)
                {
                    var bRestartChallenge = _driver != null && ((_driver.GetElapsedTime() - _lastChallengeTimestamp) > MinCookieLifetime);

                    if (bRestartChallenge)
                    {
                        SetState(HandlerComponentState.UnInitialized);
                    }

                    if (State == HandlerComponentState.UnInitialized)
                    {
                        Logger.Verbose("Initial handshake packet timeout - resending.");

                        NotifyHandshakeBegin();
                    }
                    else if (State == HandlerComponentState.InitializedOnLocal && _lastTimestamp != 0.0)
                    {
                        Logger.Verbose("Challenge response packet timeout - resending.");

                        SendChallengeResponse(_lastSecretId, _lastTimestamp, _lastCookie);
                    }
                }
            }
        }
        else
        {
            var bConnectionlessHandler = _driver != null && _driver.StatelessConnectComponent == this;
            if (bConnectionlessHandler)
            {
                var curVariance = FMath.FRandRange(0, SecretUpdateTimeVariance);
                if (((_driver!.GetElapsedTime() - _lastSecretUpdateTimestamp) - (SecretUpdateTime + curVariance)) > 0.0)
                {
                    UpdateSecret();
                }
            }
        }
    }
}