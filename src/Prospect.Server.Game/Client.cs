using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Prospect.Unreal.Runtime;
using Prospect.Unreal.Net.Packets.Control;
using Prospect.Unreal.Net.Packets.Bunch;
using Serilog;

namespace Prospect.Server.Game;

internal class Client
{
    private const float TickRate = (1000.0f / 60.0f) / 1000.0f;
    private readonly PeriodicTimer ClientTick = new PeriodicTimer(TimeSpan.FromSeconds(TickRate));
    UNetConnection UnitConn;
    public async Task<UIpNetDriver> Connect(string ipAddr, int port, FUrl worldUrl)
    {
        var connection = new UIpNetDriver(System.Net.IPAddress.Parse(ipAddr), port, false);
        await using (var world = new ProspectWorld())
            connection.InitConnect(world, new FUrl { Host = System.Net.IPAddress.Parse(ipAddr), Port = port });
        UnitConn = connection.ServerConnection;
        connection.ServerConnection.Handler?.BeginHandshaking(SendInitialJoin);

        while (await ClientTick.WaitForNextTickAsync())
        {
            if (connection != null)
            {
                connection.TickDispatch(TickRate);
                connection.PostTickDispatch();

                connection.TickFlush(TickRate);
                connection.PostTickFlush();
            }
        }

        return connection;
    }
    public void SendInitialJoin()
    //public static void SendInitialJoin(UNetConnection UnitConn)
    {
        var channel = UnitConn.CreateChannelByName(new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control), Unreal.Net.Channels.EChannelCreateFlags.None, 0);
        var BunchSequence = ++UnitConn.OutReliable[0];
        FOutBunch ControlChanBunch = new FOutBunch(channel, false);
        ControlChanBunch.Time = 0.0;
        ControlChanBunch.ReceivedAck = false;
        ControlChanBunch.PacketId = 0;
        //ControlChanBunch.Channel = nullptr;
        ControlChanBunch.ChIndex = 0;
        ControlChanBunch.ChName = new Unreal.Core.Names.FName(Unreal.Core.Names.EName.Control);
        ControlChanBunch.bReliable = true;
        ControlChanBunch.ChSequence = BunchSequence;
        ControlChanBunch.bOpen = true;
        // NOTE: Might not cover all bOpen or 'channel already open' cases
        if (UnitConn.Channels[0] != null && UnitConn.Channels[0].OpenPacketId.First == 0)
        {
            UnitConn.Channels[0].OpenPacketId = new FPacketIdRange(BunchSequence, BunchSequence);
        }

        // Need to send 'NMT_Hello' to start off the connection (the challenge is not replied to)
        byte IsLittleEndian = 0;

        // We need to construct the NMT_Hello packet manually, for the initial connection
        byte MessageType = (byte)NMT.Hello;

        // Allow the bunch to resize, and be split into partial bunches in SendRawBunch - for Fortnite
        ControlChanBunch.SetAllowResize(true);
        ControlChanBunch.WriteByte(MessageType);
        ControlChanBunch.WriteByte(IsLittleEndian);
        ControlChanBunch.WriteInt32(0); //FNetworkVersion::GetLocalNetworkVersion();
        // TODO hello encryption token

        /*bool bSkipControlJoin = !!(MinClientFlags & EMinClientFlags::SkipControlJoin);
        bool bBeaconConnect = !!(MinClientFlags & EMinClientFlags::BeaconConnect);

        if (bBeaconConnect)
        {
            if (!bSkipControlJoin)
            {
                MessageType = NMT_BeaconJoin;
                *ControlChanBunch << MessageType;
                *ControlChanBunch << BeaconType;

                uint8 EncType = 0;

                *ControlChanBunch << EncType;
                *ControlChanBunch << JoinUID;

                // Also immediately ack the beacon GUID setup; we're just going to let the server setup the client beacon,
                // through the actor channel
                MessageType = NMT_BeaconNetGUIDAck;
                *ControlChanBunch << MessageType;
                *ControlChanBunch << BeaconType;
            }
        }
        else
        {
            // Then send NMT_Login
            WriteControlLogin(ControlChanBunch);

            // Now send NMT_Join, which will spawn the PlayerController, which should then trigger replication of basic actor channels
            if (!bSkipControlJoin)
            {
                MessageType = NMT_Join;
                *ControlChanBunch << MessageType;
            }
        }*/


        UnitConn.SendRawBunch(ControlChanBunch, false);
        // Immediately flush, so that Fortnite doesn't trigger an overflow
        UnitConn.FlushNet();


        // At this point, fire of notification that we are connected
        //bConnected = true;

        //ConnectedDel.ExecuteIfBound();
    }
}
