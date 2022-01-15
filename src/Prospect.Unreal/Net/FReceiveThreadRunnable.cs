using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Serilog;

namespace Prospect.Unreal.Net;

public record FReceivedPacket(IPEndPoint Address, byte[] Buffer, DateTimeOffset Timestamp);

public class FReceiveThreadRunnable : IAsyncDisposable
{
    private static readonly ILogger Logger = Log.ForContext<FReceiveThreadRunnable>();
    
    private readonly UIpNetDriver _driver;
    private readonly CancellationTokenSource _cancellation;
    private readonly ConcurrentQueue<FReceivedPacket> _receiveQueue;

    private Task? _receiveTask;

    public FReceiveThreadRunnable(UIpNetDriver driver)
    {
        _driver = driver;
        _cancellation = new CancellationTokenSource();
        _receiveQueue = new ConcurrentQueue<FReceivedPacket>();
    }
    
    public void Start()
    {
        _receiveTask = ReceiveAsync();
    }

    public bool TryReceive([MaybeNullWhen(false)] out FReceivedPacketView result)
    {
        if (_receiveQueue.TryDequeue(out var packet))
        {
            result = new FReceivedPacketView(
                new FPacketDataView(packet.Buffer, packet.Buffer.Length, ECountUnits.Bytes),
                packet.Address, 
                new FInPacketTraits());
            
            return true;
        }

        result = null;
        return false;
    }
    
    private async Task ReceiveAsync()
    {
        Logger.Information("Started listening on {ServerIp}", _driver.ServerIp);
        
        try
        {
            while (!_cancellation.IsCancellationRequested)
            {
                var result = await _driver.Socket.ReceiveAsync(_cancellation.Token);

                if (result.Buffer.Length == 0)
                {
                    continue;
                }
            
                if (result.Buffer.Length > UNetConnection.MaxPacketSize)
                {
                    Logger.Warning("Received packet exceeding MaxPacketSize ({Size} > {Max}) from {Ip}", 
                        result.Buffer.Length, UNetConnection.MaxPacketSize, result.RemoteEndPoint);
                    continue;
                }

                _receiveQueue.Enqueue(new FReceivedPacket(result.RemoteEndPoint, result.Buffer, DateTimeOffset.UtcNow));
            }
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException && !_cancellation.IsCancellationRequested)
            {
                Logger.Error(e, "Exception caught");
            }
        }
        
        Logger.Information("Stopped listening");
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        
        if (_receiveTask != null)
        {
            await _receiveTask;
        }
        
        _cancellation.Dispose();
    }
}