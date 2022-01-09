using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Serilog;

namespace Prospect.Unreal.Runtime;

public abstract class UWorld : IAsyncDisposable
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

        if (!NetDriver.Init())
        {
            Logger.Error("Failed to listen");
            NetDriver = null;
            return false;
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