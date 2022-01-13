using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Actors;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Control;
using Serilog;

namespace Prospect.Unreal.Runtime;

public abstract class UWorld : FNetworkNotify, IAsyncDisposable
{
    private static readonly ILogger Logger = Log.ForContext<UWorld>();

    private UGameInstance? _owningGameInstance;
    private AGameModeBase? _authorityGameMode;

    public UWorld(FUrl url)
    {
        Url = url;
        _owningGameInstance = null;
        _authorityGameMode = null;
    }
    
    public FUrl Url { get; }
    public UNetDriver? NetDriver { get; private set; }
    
    public void Tick(float deltaTime)
    {
        if (NetDriver != null)
        {
            NetDriver.TickDispatch(deltaTime);
            NetDriver.ConnectionlessHandler?.Tick(deltaTime);
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
    
    public bool Listen()
    {
        if (NetDriver != null)
        {
            Logger.Error("NetDriver already exists");
            return false;
        }
        
        NetDriver = new UIpNetDriver(Url.Host, Url.Port);
        NetDriver.SetWorld(this);

        if (!NetDriver.Init(this))
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
                    if (NMT_Login.Receive(bunch, out var clientResponse, out var requestUrl, out var uniqueIdRepl, out var onlinePlatformName))
                    {
                        connection.ClientResponse = clientResponse;
                        
                        // Only the options/portal for the URL should be used during join
                        var newRequestUrl = requestUrl;
                        
                        var oneIndex = requestUrl.IndexOf('?');
                        var twoIndex = requestUrl.IndexOf('#');

                        if (oneIndex != -1 && twoIndex != -1)
                        {
                            newRequestUrl = requestUrl.Substring(Math.Min(oneIndex, twoIndex));
                        } 
                        else if (oneIndex != -1)
                        {
                            newRequestUrl = requestUrl.Substring(oneIndex);
                        } 
                        else if (twoIndex != -1)
                        {
                            newRequestUrl = requestUrl.Substring(twoIndex);
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
                            requestUrl = newRequestUrl;
                            Logger.Error("NMT_Login: Invalid URL {Url}", requestUrl);
                            bunch.SetError();
                            break;
                        }

                        var splitscreenCount = Math.Min(connection.Children.Count + 1, 255);
                        
                        // Don't allow clients to specify this value
                        inUrl.Options.Remove("SplitscreenCount");
                        inUrl.Options.Add($"SplitscreenCount={splitscreenCount}");

                        requestUrl = inUrl.ToString();
                        
                        // skip to the first option in the URL
                        var tmp = requestUrl.Substring(requestUrl.IndexOf('?'));

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
    
    public async ValueTask DisposeAsync()
    {
        if (NetDriver != null)
        {
            await NetDriver.DisposeAsync();
        }
    }
}