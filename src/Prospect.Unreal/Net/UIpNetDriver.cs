using System.Net;
using System.Net.Sockets;
using Prospect.Unreal.Core;
using Serilog;

namespace Prospect.Unreal.Net;

public class UIpNetDriver : UNetDriver
{
    private static readonly ILogger Logger = Log.ForContext<UIpNetDriver>();

    private bool _isDisposed;
    
    public UIpNetDriver(IPAddress host, int port)
    {
        ServerIp = new IPEndPoint(host, port);
        Socket = new UdpClient(ServerIp);
        ReceiveThread = new FReceiveThreadRunnable(this);
    }

    public IPEndPoint ServerIp { get; }
    public UdpClient Socket { get; }
    public FReceiveThreadRunnable ReceiveThread { get; }

    public override bool Init(FNetworkNotify notify)
    {
        if (!base.Init(notify))
        {
            return false;
        }
        
        // Initialize connectionless packet handler.
        InitConnectionlessHandler();
        
        // Start receiving packets.
        ReceiveThread.Start();

        return true;
    }
    
    public override void TickDispatch(float deltaTime)
    {
        base.TickDispatch(deltaTime);
        
        while (ReceiveThread.TryReceive(out var packet))
        {
            Logger.Information("Received from {Adress} data {Buffer}", packet.Address, packet.DataView.GetData());

            UNetConnection? connection = null;

            if (Equals(packet.Address, ServerIp))
            {
                // TODO: Assign connection.
                throw new NotImplementedException();
            }

            if (connection == null)
            {
                MappedClientConnections.TryGetValue(packet.Address, out connection);
            }

            var bIgnorePacket = false;
            
            // If we didn't find a client connection, maybe create a new one.
            if (connection == null)
            {
                connection = ProcessConnectionlessPacket(packet);
                bIgnorePacket = packet.DataView.NumBytes() == 0;
            }
            
            // Send the packet to the connection for processing.
            if (connection != null && !bIgnorePacket)
            {
                connection.ReceivedRawPacket(packet);
            }
        }
    }

    private UNetConnection? ProcessConnectionlessPacket(FReceivedPacketView packet)
    {
        UNetConnection? returnVal = null;
        var statelessConnect = StatelessConnectComponent;
        var address = packet.Address;
        var bPassedChallenge = false;
        var bRestartedHandshake = false;
        var bIgnorePacket = true;
        
        if (ConnectionlessHandler != null && statelessConnect != null)
        {
            var result = ConnectionlessHandler.IncomingConnectionless(packet);
            if (result)
            {
                bPassedChallenge = statelessConnect.HasPassedChallenge(address, out bRestartedHandshake);

                if (bPassedChallenge)
                {
                    if (bRestartedHandshake)
                    {
                        throw new NotImplementedException();
                    }

                    var newCountBytes = packet.DataView.NumBytes();
                    var workingData = new byte[newCountBytes];

                    if (newCountBytes > 0)
                    {
                        Buffer.BlockCopy(packet.DataView.GetData(), 0, workingData, 0, newCountBytes);
                        bIgnorePacket = false;
                    }

                    packet.DataView = new FPacketDataView(workingData, newCountBytes, ECountUnits.Bytes);
                }
            }
        }
        else
        {
            Logger.Warning("Invalid ConnectionlessHandler or StatelessConnectComponent, can't accept connections");    
        }

        if (bPassedChallenge)
        {
            if (!bRestartedHandshake)
            {
                Logger.Verbose("Server accepting post-challenge connection from: {Address}", address);

                returnVal = new UIpConnection();

                if (statelessConnect != null)
                {
                    statelessConnect.GetChallengeSequences(out var serverSequence, out var clientSequence);

                    returnVal.InitSequence(clientSequence, serverSequence);
                }

                returnVal.InitRemoteConnection(this, Socket, World != null ? World.Url : new FUrl(), address, EConnectionState.USOCK_Open);

                if (returnVal.Handler != null)
                {
                    returnVal.Handler.BeginHandshaking();
                }

                AddClientConnection(returnVal);
            }

            if (statelessConnect != null)
            {
                statelessConnect.ResetChallengeData();
            }
        }

        if (bIgnorePacket)
        {
            packet.DataView = new FPacketDataView(packet.DataView.GetData(), 0, ECountUnits.Bits);
        }
        
        return returnVal;
    }

    public override void LowLevelSend(IPEndPoint address, byte[] data, int countBits, FOutPacketTraits traits)
    {
        if (ConnectionlessHandler != null)
        {
            var processedData = ConnectionlessHandler.OutgoingConnectionless(address, data, countBits, traits);
            if (!processedData.Error)
            {
                data = processedData.Data;
                countBits = processedData.CountBits;
            }
            else
            {
                countBits = 0;
            }
        }

        if (countBits > 0)
        {
            Socket.Send(data, FMath.DivideAndRoundUp(countBits, 8), address);
        }
    }

    public override bool IsNetResourceValid()
    {
        return !_isDisposed;
    }

    public override async ValueTask DisposeAsync()
    {
        _isDisposed = true;
        
        await base.DisposeAsync();
        
        Socket.Dispose();
        
        await ReceiveThread.DisposeAsync();
    }
}