using System.Collections.Concurrent;
using System.Net;

namespace Prospect.Unreal.Net;

public abstract class UNetDriver : IAsyncDisposable
{
    private double _elapsedTime;
    
    protected UNetDriver()
    {
        MappedClientConnections = new ConcurrentDictionary<IPEndPoint, UNetConnection>();
    }
    
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
        ConnectionlessHandler.Initialize(true);

        StatelessConnectComponent = (StatelessConnectHandlerComponent) ConnectionlessHandler.AddHandler<StatelessConnectHandlerComponent>();
        StatelessConnectComponent.SetDriver(this);

        ConnectionlessHandler.InitializeComponents();
    }
    
    public virtual void TickDispatch(float deltaTime)
    {
        _elapsedTime += deltaTime;
    }

    public virtual void LowLevelSend(IPEndPoint address, byte[] data, int countBits, FOutPacketTraits traits)
    {
        throw new NotImplementedException();
    }
    
    public abstract bool IsNetResourceValid();

    public double GetElapsedTime()
    {
        return _elapsedTime;
    }

    public void ResetElapsedTime()
    {
        _elapsedTime = 0.0;
    }
    
    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}