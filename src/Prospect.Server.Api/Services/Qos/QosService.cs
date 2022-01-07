namespace Prospect.Server.Api.Services.Qos;

/// <summary>
///     Implementation of a QoS beacon.
///     https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/using-qos-beacons-to-measure-player-latency-to-azure#quality-of-service-beacons
/// </summary>
public class QosService : IHostedService
{
    private readonly ILogger<QosService> _logger;
    private readonly QosServer _server;

    public QosService(ILogger<QosService> logger, QosServer server)
    {
        _logger = logger;
        _server = server;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting QoS beacon");
        _server.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping QoS beacon");
        await _server.StopAsync();
    }
}