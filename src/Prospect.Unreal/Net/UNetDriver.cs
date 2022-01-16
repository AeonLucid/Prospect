using System.Collections.Concurrent;
using System.Net;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Exceptions;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Net.Channels.Actor;
using Prospect.Unreal.Net.Channels.Control;
using Prospect.Unreal.Net.Channels.Voice;
using Prospect.Unreal.Runtime;

namespace Prospect.Unreal.Net;

public abstract class UNetDriver : IAsyncDisposable
{
    private double _elapsedTime;
    
    protected UNetDriver()
    {
        GuidCache = new FNetGUIDCache(this);
        // TODO: Load from Engine ini
        ChannelDefinitionMap = new Dictionary<FName, FChannelDefinition>();
        ChannelDefinitions = new List<FChannelDefinition>
        {
            new FChannelDefinition(EName.Control, typeof(string), 0, true, false, true, false, true),
            new FChannelDefinition(EName.Voice, typeof(string), 1, true, true, true, true, true),
            new FChannelDefinition(EName.Actor, typeof(string), -1, false, true, false, false, false)
        };
        ClientConnections = new List<UNetConnection>();
        MappedClientConnections = new ConcurrentDictionary<IPEndPoint, UNetConnection>();
        
        foreach (var channel in ChannelDefinitions)
        {
            ChannelDefinitionMap[channel.Name] = channel;
        }
    }

    // TODO: From Engine ini
    public float KeepAliveTime { get; } = 0.2f;

    // TODO: From Engine ini
    public int MaxClientRate { get; } = 100000;
    
    /// <summary>
    ///     World this net driver is associated with
    /// </summary>
    public UWorld? World { get; private set; }
    
    public FNetworkNotify Notify { get; private set; }
    
    public FNetGUIDCache GuidCache { get; }
    
    /// <summary>
    ///     Used to specify available channel types and their associated UClass
    /// </summary>
    public List<FChannelDefinition> ChannelDefinitions { get; } 
    
    /// <summary>
    ///     Used for faster lookup of channel definitions by name.
    /// </summary>
    public Dictionary<FName, FChannelDefinition> ChannelDefinitionMap { get; }

    /// <summary>
    ///     AConnection to the server (this net driver is a client)
    /// </summary>
    public UNetConnection ServerConnection { get; set; }

    /// <summary>
    ///     Array of connections to clients (this net driver is a host) - unsorted, and ordering changes depending on actor replication
    /// </summary>
    public List<UNetConnection> ClientConnections { get; }

    /// <summary>
    ///     Map of <see cref="IPEndPoint"/> to <see cref="UNetConnection"/>.
    /// </summary>
    public ConcurrentDictionary<IPEndPoint, UNetConnection> MappedClientConnections { get; }
    
    /// <summary>
    ///     Serverside PacketHandler for managing connectionless packets
    /// </summary>
    public PacketHandler? ConnectionlessHandler { get; private set; }
    
    /// <summary>
    ///     Reference to the PacketHandler component, for managing stateless connection handshakes
    /// </summary>
    public StatelessConnectHandlerComponent? StatelessConnectComponent { get; private set; }

    public void InitConnectionlessHandler()
    {
        ConnectionlessHandler = new PacketHandler();
        ConnectionlessHandler.Initialize(HandlerMode.Server, UNetConnection.MaxPacketSize, true);

        StatelessConnectComponent = (StatelessConnectHandlerComponent) ConnectionlessHandler.AddHandler<StatelessConnectHandlerComponent>();
        StatelessConnectComponent.SetDriver(this);

        ConnectionlessHandler.InitializeComponents();
    }

    public virtual bool Init(FNetworkNotify notify)
    {
        Notify = notify;
        return true;
    }
    
    /// <summary>
    ///     handle time update: read and process packets
    /// </summary>
    public virtual void TickDispatch(float deltaTime)
    {
        _elapsedTime += deltaTime;
        
        // Delete closed connections.
        for (var i = ClientConnections.Count - 1; i >= 0; i--)
        {
            if (ClientConnections[i].State == EConnectionState.USOCK_Closed)
            {
                ClientConnections[i].CleanUp();
            }
        }
    }

    /// <summary>
    ///     PostTickDispatch actions
    /// </summary>
    public virtual void PostTickDispatch()
    {
        foreach (var connection in ClientConnections)
        {
            /* TODO: (When UObject) if (!connection.IsPendingKill()) */
            {
                connection.PostTickDispatch();
            }
        }
    }

    /// <summary>
    ///     ReplicateActors and Flush
    /// </summary>
    public virtual void TickFlush(float deltaTime)
    {
        if (IsServer() && ClientConnections.Count > 0)
        {
            // TODO: (When actors are implemented) ServerReplicateActors
        }

        foreach (var connection in ClientConnections)
        {
            connection.Tick(deltaTime);
        }

        if (ConnectionlessHandler != null)
        {
            ConnectionlessHandler.Tick(deltaTime);
            
            // TODO: FlushHandler
        }
        
        // TODO: (When actors are implemented) CleanupStaleDormantReplicators
    }

    /// <summary>
    ///     PostTick actions
    /// </summary>
    public virtual void PostTickFlush()
    {
        // ClearVoicePackets?
    }

    public virtual void LowLevelSend(IPEndPoint address, byte[] data, int countBits, FOutPacketTraits traits)
    {
        throw new NotImplementedException();
    }
    
    public abstract bool IsNetResourceValid();
    
    public void SetWorld(UWorld? inWorld)
    {
        if (World != null)
        {
            World = null;
        }

        if (inWorld != null)
        {
            World = inWorld;
            
            // TODO: AddInitialObjects?
        }
    }

    public bool IsServer()
    {
        return ServerConnection == null;
    }

    public bool IsKnownChannelName(FName name)
    {
        return ChannelDefinitionMap.ContainsKey(name);
    }

    public virtual bool ShouldIgnoreRPCs()
    {
        return false;
    }
    
    public double GetElapsedTime()
    {
        return _elapsedTime;
    }

    public void ResetElapsedTime()
    {
        _elapsedTime = 0.0;
    }

    private protected void AddClientConnection(UNetConnection newConnection)
    {
        ClientConnections.Add(newConnection);

        if (newConnection.RemoteAddr != null)
        {
            MappedClientConnections[newConnection.RemoteAddr] = newConnection;
            
            // TODO: RecentlyDisconnectedClients ?
        }

        CreateInitialServerChannels(newConnection);

        // TODO: NetworkObjectList > HandleConnectionAdded
    }

    protected void CreateInitialClientChannels()
    {
        foreach (var channelDef in ChannelDefinitions)
        {
            if (channelDef.InitialServer)
            {
                ServerConnection.CreateChannelByName(channelDef.Name, EChannelCreateFlags.OpenedLocally, channelDef.StaticChannelIndex);
            }
        }
    }

    private void CreateInitialServerChannels(UNetConnection clientConnection)
    {
        foreach (var channelDef in ChannelDefinitions)
        {
            if (channelDef.InitialServer)
            {
                clientConnection.CreateChannelByName(channelDef.Name, EChannelCreateFlags.OpenedLocally, channelDef.StaticChannelIndex);
            }
        }
    }

    public UChannel GetOrCreateChannelByName(FName chName)
    {
        // TODO: Pool actor channels (?)

        var name = chName.ToEName();
        if (name == null)
        {
            throw new UnrealNetException($"Unsupported channel name specified {chName}");
        }
        
        return name switch
        {
            EName.Actor => new UActorChannel(),
            EName.Control => new UControlChannel(),
            EName.Voice => new UVoiceChannel(),
            _ => throw new UnrealNetException($"Attempted to create unknown channel {chName}")
        };
    }

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}