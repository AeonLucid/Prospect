using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Core.Objects;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Actors;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Control;
using Serilog;

namespace Prospect.Unreal.Runtime;

public abstract partial class UWorld : FNetworkNotify, IAsyncDisposable
{
    private static readonly ILogger Logger = Log.ForContext<UWorld>();

    private UGameInstance? _owningGameInstance;
    private AGameModeBase? _authorityGameMode;
    
    /// <summary>
    ///     Array of levels currently in this world. Not serialized to disk to avoid hard references.
    /// </summary>
    private List<ULevel> _levels;
    
    /// <summary>
    ///     Pointer to the current level being edited.
    ///     Level has to be in the Levels array and == PersistentLevel in the game.
    /// </summary>
    private ULevel? _currentLevel;

    public UWorld()
    {
        _owningGameInstance = null;
        _authorityGameMode = null;
        _levels = new List<ULevel>();
    }
    
    /// <summary>
    ///     Persistent level containing the world info, default brush and actors spawned during gameplay among other things
    /// </summary>
    public ULevel? PersistentLevel { get; private set; }
    
    /// <summary>
    ///     The NAME_GameNetDriver game connection(s) for client/server communication
    /// </summary>
    public UNetDriver? NetDriver { get; private set; }
    
    /// <summary>
    ///     Whether actors have been initialized for play
    /// </summary>
    public bool bActorsInitialized { get; private set; }
    
    /// <summary>
    ///     Is the world in its actor initialization phase.
    /// </summary>
    public bool bStartup { get; private set; }
    
    /// <summary>
    ///     Whether BeginPlay has been called on actors
    /// </summary>
    public bool bBegunPlay { get; private set; }
    
    /// <summary>
    ///     The URL that was used when loading this World.
    /// </summary>
    public FUrl? Url { get; private set; }
    
    /// <summary>
    ///     Time in seconds since level began play, but IS paused when the game is paused, and IS dilated/clamped.
    /// </summary>
    public float TimeSeconds { get; private set; }
    
    /// <summary>
    ///     Time in seconds since level began play, but IS NOT paused when the game is paused, and IS dilated/clamped.
    /// </summary>
    public float UnpausedTimeSeconds { get; private set; }
    
    /// <summary>
    ///     Time in seconds since level began play, but IS NOT paused when the game is paused, and IS NOT dilated/clamped.
    /// </summary>
    public float RealTimeSeconds { get; private set; }
    
    /// <summary>
    ///     Time in seconds since level began play, but IS paused when the game is paused, and IS NOT dilated/clamped.
    /// </summary>
    public float AudioTimeSeconds { get; private set; }
    
    /// <summary>
    ///     Frame delta time in seconds adjusted by e.g. time dilation.
    /// </summary>
    public float DeltaTimeSeconds { get; private set; }
    
    public void Tick(float deltaTime)
    {
        if (NetDriver != null)
        {
            NetDriver.TickDispatch(deltaTime);
            NetDriver.PostTickDispatch();
            
            NetDriver.TickFlush(deltaTime);
            NetDriver.PostTickFlush();
        }
    }

    public void SetGameInstance(UGameInstance instance)
    {
        _owningGameInstance = instance;
    }

    public UGameInstance GetGameInstance()
    {
        if (_owningGameInstance == null)
        {
            throw new UnrealException($"Attempted to retrieve null {nameof(UGameInstance)}");
        }

        return _owningGameInstance;
    }
    
    public bool SetGameMode(FUrl worldUrl)
    {
        if (IsServer() && _authorityGameMode == null)
        {
            _authorityGameMode = GetGameInstance().CreateGameModeForURL(worldUrl, this);
            
            if (_authorityGameMode != null)
            {
                return true;
            }

            Logger.Error("Failed to spawn GameMode actor");
            return false;
        }

        return false;
    }

    public AGameModeBase? GetAuthGameMode()
    {
        return _authorityGameMode;
    }

    public void InitializeActorsForPlay(FUrl inUrl, bool bResetTime)
    {
        // Don't reset time for seamless world transitions.
        if (bResetTime)
        {
            TimeSeconds = 0.0f;
            UnpausedTimeSeconds = 0.0f;
            RealTimeSeconds = 0.0f;
            AudioTimeSeconds = 0.0f;
        }

        // Get URL Options
        var options = inUrl.OptionsToString();

        // Set level info.
        if (!string.IsNullOrEmpty(inUrl.GetOption("load", null)))
        {
            Url = inUrl;
        }
        
        // Init level gameplay info.
        if (!AreActorsInitialized())
        {
            // Initialize network actors and start execution.
            foreach (var level in _levels)
            {
                level.InitializeNetworkActors();
            }

            // Enable actor script calls.
            bStartup = true;
            bActorsInitialized = true;

            // Spawn server actors
            // TODO: GEngine SpawnServerActors.
            
            // Init the game mode.
            if (_authorityGameMode != null && !_authorityGameMode.IsActorInitialized())
            {
                _authorityGameMode.InitGame(inUrl.Map /* TODO: FPaths.GetBaseFilename */, options, out _);
            }
        }
    }

    public bool Listen()
    {
        if (NetDriver != null)
        {
            Logger.Error("NetDriver already exists");
            return false;
        }
        
        NetDriver = new UIpNetDriver(Url.Host, Url.Port, IsServer());
        NetDriver.SetWorld(this);

        if (!((UIpNetDriver)NetDriver).InitListen(this))
        {
            Logger.Error("Failed to listen");
            NetDriver = null;
            return false;
        }
        
        return true;
    }

    public EAcceptConnection NotifyAcceptingConnection()
    {
        return EAcceptConnection.Accept;
    }

    public void NotifyAcceptedConnection(UNetConnection connection)
    {
        
    }

    public bool NotifyAcceptingChannel(UChannel channel)
    {
        if (channel.Connection?.Driver == null)
        {
            throw new UnrealNetException();
        }
        
        var driver = channel.Connection.Driver;
        if (!driver.IsServer())
        {
            throw new NotSupportedException("Client code");
        }
        else
        {
            // We are the server.
            if (driver.ChannelDefinitionMap[channel.ChName].ClientOpen)
            {
                // The client has opened initial channel.
                Logger.Verbose("NotifyAcceptingChannel {ChName} {ChIndex} server {FullName}: Accepted", channel.ChName, channel.ChIndex, typeof(UWorld).FullName);
                return true;
            }

            // Client can't open any other kinds of channels.
            Logger.Verbose("NotifyAcceptingChannel {ChName} {ChIndex} server {FullName}: Refused", channel.ChName, channel.ChIndex, typeof(UWorld).FullName);
            return false;
        }
    }

    public void NotifyControlMessage(UNetConnection connection, NMT messageType, FInBunch bunch)
    {
        if (NetDriver == null)
        {
            throw new UnrealNetException();
        }
        
        if (!NetDriver.IsServer())
        {
            throw new NotSupportedException("Client code");
        }
        else
        {
            Logger.Verbose("Level server received: {MessageType}", messageType);

            if (!connection.IsClientMsgTypeValid(messageType))
            {
                Logger.Error("IsClientMsgTypeValid FAILED ({MessageType}): Remote Address = {Address}", (int)messageType, connection.LowLevelGetRemoteAddress());
                bunch.SetError();
                return;
            }

            switch (messageType)
            {
                case NMT.Hello:
                {
                    const int localNetworkVersion = 0;
                    
                    if (NMT_Hello.Receive(bunch, out var isLittleEndian, out var remoteNetworkVersion, out var encryptionToken))
                    {
                        Logger.Information("Client connecting with version. LocalNetworkVersion: {Local}, RemoteNetworkVersion: {Remote}", localNetworkVersion, remoteNetworkVersion);
                        
                        // TODO: Version check.

                        if (string.IsNullOrEmpty(encryptionToken))
                        {
                            connection.SendChallengeControlMessage();
                        }
                        else
                        {
                            throw new NotImplementedException("Encryption");
                        }
                    }
                    break;
                }

                case NMT.Netspeed:
                {
                    if (NMT_Netspeed.Receive(bunch, out var rate))
                    {
                        connection.CurrentNetSpeed = Math.Clamp(rate, 1800, NetDriver.MaxClientRate);
                        Logger.Debug("Client netspeed is {Num}", connection.CurrentNetSpeed);
                    }

                    break;
                }

                case NMT.Abort:
                {
                    break;
                }

                case NMT.Skip:
                {
                    break;
                }

                case NMT.Login:
                {
                    // Admit or deny the player here.
                    if (NMT_Login.Receive(bunch, out var clientResponse, out var tmpRequestUrl, out var uniqueIdRepl, out var onlinePlatformName))
                    {
                        connection.ClientResponse = clientResponse;
                        connection.RequestURL = tmpRequestUrl;
                        
                        // Only the options/portal for the URL should be used during join
                        var newRequestUrl = connection.RequestURL;
                        
                        var oneIndex = newRequestUrl.IndexOf('?');
                        var twoIndex = newRequestUrl.IndexOf('#');

                        if (oneIndex != -1 && twoIndex != -1)
                        {
                            newRequestUrl = newRequestUrl.Substring(Math.Min(oneIndex, twoIndex));
                        } 
                        else if (oneIndex != -1)
                        {
                            newRequestUrl = newRequestUrl.Substring(oneIndex);
                        } 
                        else if (twoIndex != -1)
                        {
                            newRequestUrl = newRequestUrl.Substring(twoIndex);
                        }
                        else
                        {
                            newRequestUrl = string.Empty;
                        }
                        
                        Logger.Debug("Login request: {RequestUrl} userId: {UserId} platform: {Platform}", newRequestUrl, uniqueIdRepl.ToDebugString(), onlinePlatformName);
                        
                        // Compromise for passing splitscreen playercount through to gameplay login code,
                        // without adding a lot of extra unnecessary complexity throughout the login code.
                        // NOTE: This code differs from NMT_JoinSplit, by counting + 1 for SplitscreenCount
                        //			(since this is the primary connection, not counted in Children)
                        // TODO: Implement proper FUrl constructor
                        var inUrl = new FUrl
                        {
                            Map = Url.Map + newRequestUrl
                        };

                        if (!inUrl.Valid)
                        {
                            connection.RequestURL = newRequestUrl;
                            Logger.Error("NMT_Login: Invalid URL {Url}", connection.RequestURL);
                            bunch.SetError();
                            break;
                        }

                        var splitscreenCount = Math.Min(connection.Children.Count + 1, 255);
                        
                        // Don't allow clients to specify this value
                        inUrl.Options.Remove("SplitscreenCount");
                        inUrl.Options.Add($"SplitscreenCount={splitscreenCount}");

                        connection.RequestURL = inUrl.ToString();
                        
                        // skip to the first option in the URL
                        var tmp = connection.RequestURL.Substring(connection.RequestURL.IndexOf('?'));

                        // keep track of net id for player associated with remote connection
                        connection.PlayerId = uniqueIdRepl;

                        // keep track of the online platform the player associated with this connection is using.
                        connection.SetPlayerOnlinePlatformName(new FName(onlinePlatformName));
                        
                        // ask the game code if this player can join
                        string? errorMsg = null;
                        
                        var gameMode = GetAuthGameMode();
                        if (gameMode != null)
                        {
                            gameMode.PreLogin(tmp, connection.LowLevelGetRemoteAddress(), connection.PlayerId, out errorMsg);
                        }

                        if (!string.IsNullOrEmpty(errorMsg))
                        {
                            Logger.Debug("PreLogin failure: {Error}", errorMsg);
                            NMT_Failure.Send(connection, errorMsg);
                            connection.FlushNet(true);
                        }
                        else
                        {
                            WelcomePlayer(connection);
                        }
                    }
                    else
                    {
                        connection.ClientResponse = string.Empty;
                    }

                    break;
                }

                case NMT.Join:
                {
                    if (connection.PlayerController == null)
                    {
                        // Spawn the player-actor for this network player.
                        Logger.Debug("Join request: {Request}", connection.RequestURL);

                        // TODO: Proper constructor
                        var inURL = new FUrl();

                        connection.PlayerController = SpawnPlayActor(connection, ENetRole.ROLE_AutonomousProxy, inURL, connection.PlayerId, out var errorMsg);
                    }
                    break;
                }

                default:
                {
                    throw new NotImplementedException($"Unhandled control message {messageType}");
                }
            }
        }
    }

    private void WelcomePlayer(UNetConnection connection)
    {
        // TODO: Properly fetch level name from CurrentLevel
        var levelName = "/Game/ThirdPersonCPP/Maps/ThirdPersonExampleMap";
        
        // TODO: Properly fetch from AuthorityGameMode
        var gameName = "/Script/ThirdPersonMP.ThirdPersonMPGameMode";
        var redirectUrl = string.Empty;
        
        NMT_Welcome.Send(connection, levelName, gameName, redirectUrl);

        connection.FlushNet();
        // connection.QueuedBits = 0;
        connection.SetClientLoginState(EClientLoginState.Welcomed);
    }

    public bool IsServer()
    {
        if (NetDriver != null)
        {
            return NetDriver.IsServer();
        }

        return true;
    }

    public bool HasBegunPlay()
    {
        return bBegunPlay && PersistentLevel != null && PersistentLevel.Actors.Count != 0;
    }

    public bool AreActorsInitialized()
    {
        return bActorsInitialized && PersistentLevel != null && PersistentLevel.Actors.Count != 0;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (NetDriver != null)
        {
            await NetDriver.DisposeAsync();
        }
    }
}