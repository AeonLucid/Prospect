using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Prospect.Server.Api.Services.Qos;

public class QosServer : IAsyncDisposable
{
    private readonly ILogger<QosServer> _logger;
    private readonly UdpClient _client;

    private bool _isStopping;
    private Task? _runTask;
    
    public QosServer(ILogger<QosServer> logger)
    {
        _logger = logger;
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, 3075));
    }

    public void Start()
    {
        _runTask = RunAsync();
    }

    public async Task StopAsync()
    {
        _isStopping = true;
        _client.Dispose();

        if (_runTask != null)
        {
            await _runTask;
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                var result = await _client.ReceiveAsync();
            
                _logger.LogInformation("Received {Data} from {Address}", result.Buffer, result.RemoteEndPoint);

                if (result.Buffer.Length == 22 && BinaryPrimitives.ReadUInt64LittleEndian(result.Buffer) == 0xFFFFFFFF)
                {
                    // The server will reply with a single datagram, with the message contents having the
                    // first 2 bytes "flipped" to 0x0000 (0000 0000 0000 0000).
                    // The rest of the datagram contents will be copied from the initial ping.
                    result.Buffer[0] = 0x00;
                    result.Buffer[1] = 0x00;
                    result.Buffer[2] = 0x00;
                    result.Buffer[3] = 0x00;

                    await _client.SendAsync(result.Buffer, result.RemoteEndPoint);
                }
            }
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted || 
                                        e.SocketErrorCode == SocketError.Interrupted ||
                                        e.SocketErrorCode == SocketError.InvalidArgument && !OperatingSystem.IsWindows())
        {
            if (!_isStopping)
            {
                _logger.LogError(e, "SocketException caught in QosServer");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught in QosServer");
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}