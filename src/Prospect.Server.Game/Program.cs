using Prospect.Unreal.Net;
using Serilog;

namespace Prospect.Server.Game;

internal static class Program
{
    private const float TickRate = (1000.0f / 60.0f) / 1000.0f;
    
    private static readonly ILogger Logger = Log.ForContext(typeof(Program));
    private static readonly PeriodicTimer Tick = new PeriodicTimer(TimeSpan.FromSeconds(TickRate));
    
    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Tick.Dispose();
            e.Cancel = true;
        };
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceContext,-52}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        Logger.Information("Starting Prospect.Server.Game");
        
        await using (var driver = new UIpNetDriver())
        {
            driver.Init();
        
            // Tick all components.
            while (await Tick.WaitForNextTickAsync())
            {
                var deltaTime = TickRate;
                
                driver.TickDispatch(deltaTime);
                // driver.PostTickDispatch();

                if (driver.ConnectionlessHandler != null)
                {
                    driver.ConnectionlessHandler.Tick(deltaTime);
                }
            }
        }
        
        Logger.Information("Shutting down");
    }
}