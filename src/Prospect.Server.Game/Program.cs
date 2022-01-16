using Prospect.Unreal.Core;
using Prospect.Unreal.Runtime;
using Serilog;

namespace Prospect.Server.Game;

internal static class Program
{
    private const float TickRate = (1000.0f / 60.0f) / 1000.0f;
    
    private static readonly ILogger Logger = Log.ForContext(typeof(Program));
    private static readonly PeriodicTimer Tick = new PeriodicTimer(TimeSpan.FromSeconds(TickRate));
    
    public static async Task Main()
    {
        Console.CancelKeyPress += (_, e) =>
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

        // Prospect:
        //  Map:        /Game/Maps/MP/Station/Station_P
        //  GameMode:   /Script/Prospect/YGameMode_Station
        
        var worldUrl = new FUrl
        {
            Map = "/Game/ThirdPersonCPP/Maps/ThirdPersonExampleMap"
        };
        
        await using (var world = new ProspectWorld())
        {
            world.SetGameInstance(new UGameInstance());
            world.SetGameMode(worldUrl);
            world.InitializeActorsForPlay(worldUrl, true);
            world.Listen();
        
            while (await Tick.WaitForNextTickAsync())
            {
                world.Tick(TickRate);
            }
        }
        
        Logger.Information("Shutting down");
    }
}