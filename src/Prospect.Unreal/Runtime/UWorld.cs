using Prospect.Unreal.Core;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Control;
using Serilog;

namespace Prospect.Unreal.Runtime;

public abstract class UWorld : FNetworkNotify, IAsyncDisposable
{
    private static readonly ILogger Logger = Log.ForContext<UWorld>();

    public UWorld(FUrl url)
    {
        Url = url;
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
                    if (NMT_Hello.Receive(bunch, out var isLittleEndian, out var remoteNetworkVersion, out var encryptionToken))
                    {
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

                case NMT.Login:
                {
                    throw new NotImplementedException();
                }

                default:
                {
                    throw new NotImplementedException($"Unhandled control message {messageType}");
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (NetDriver != null)
        {
            await NetDriver.DisposeAsync();
        }
    }
}