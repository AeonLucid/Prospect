using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Net.Packets.Control;
using Serilog;

namespace Prospect.Unreal.Net.Channels.Control;

public class UControlChannel : UChannel
{
    private static readonly ILogger Logger = Log.ForContext<UControlChannel>();
    
    public UControlChannel()
    {
        ChType = EChannelType.CHTYPE_Control;
        ChName = EName.Control;
    }

    protected override void ReceivedBunch(FInBunch bunch)
    {
        // if (Connection != null && bNeedsEndianInspection && !CheckEndianess(bunch))
        // {
        //     // Send close bunch and shutdown this connection
        // }
        
        // bunch.()

        while (!bunch.AtEnd() && Connection != null && Connection.State != EConnectionState.USOCK_Closed)
        {
            var messageType = (NMT) bunch.ReadByte();

            if (bunch.IsError())
            {
                break;
            }

            var pos = bunch.GetPosBits();

            if (messageType == NMT.ActorChannelFailure)
            {
                throw new NotImplementedException();   
            } 
            else if (messageType == NMT.GameSpecific)
            {
                // the most common Notify handlers do not support subclasses by default and
                // so we redirect the game specific messaging to the GameInstance instead
                throw new NotImplementedException();   
            } 
            else if (messageType == NMT.SecurityViolation)
            {
                throw new NotImplementedException();
            }
            else if (messageType == NMT.DestructionInfo)
            {
                throw new NotImplementedException();
            }
            else
            {
                // Process control message on client/server connection
                Connection.Driver!.Notify.NotifyControlMessage(Connection, messageType, bunch);
            }
            
            // if the message was not handled, eat it ourselves
            if (pos == bunch.GetPosBits() && !bunch.IsError())
            {
                switch (messageType)
                {
                    case NMT.Hello:
                        NMT_Hello.Discard(bunch);
                        break;
                    case NMT.Welcome:
                        NMT_Welcome.Discard(bunch);
                        break;
                    case NMT.Upgrade:
                        NMT_Upgrade.Discard(bunch);
                        break;
                    case NMT.Challenge:
                        NMT_Challenge.Discard(bunch);
                        break;
                    case NMT.Netspeed:
                        NMT_Netspeed.Discard(bunch);
                        break;
                    case NMT.Login:
                        NMT_Login.Discard(bunch);
                        break;
                    case NMT.Failure:
                        NMT_Failure.Discard(bunch);
                        break;
                    case NMT.Join:
                        NMT_Join.Discard(bunch);
                        break;
                    case NMT.JoinSplit:
                        NMT_JoinSplit.Discard(bunch);
                        break;
                    case NMT.Skip:
                        NMT_Skip.Discard(bunch);
                        break;
                    case NMT.Abort:
                        NMT_Abort.Discard(bunch);
                        break;
                    case NMT.PCSwap:
                        NMT_PCSwap.Discard(bunch);
                        break;
                    case NMT.ActorChannelFailure:
                        NMT_ActorChannelFailure.Discard(bunch);
                        break;
                    case NMT.DebugText:
                        NMT_DebugText.Discard(bunch);
                        break;
                    case NMT.NetGUIDAssign:
                        NMT_NetGUIDAssign.Discard(bunch);
                        break;
                    case NMT.EncryptionAck:
                        NMT_EncryptionAck.Discard(bunch);
                        break;
                    case NMT.BeaconWelcome:
                        NMT_BeaconWelcome.Discard(bunch);
                        break;
                    case NMT.BeaconJoin:
                        NMT_BeaconJoin.Discard(bunch);
                        break;
                    case NMT.BeaconAssignGUID:
                        NMT_BeaconAssignGUID.Discard(bunch);
                        break;
                    case NMT.BeaconNetGUIDAck:
                        NMT_BeaconNetGUIDAck.Discard(bunch);
                        break;
                    default:
                        // if this fails, a case is missing above for an implemented message type
                        // or the connection is being sent potentially malformed packets
                        // @PotentialDOSAttackDetection
                        Logger.Error("Received unknown control channel message {MessageType}. Closing connection", (int)messageType);
                        Connection.Close();
                        return;
                }
            }

            if (bunch.IsError())
            {
                Logger.Error("Failed to read control channel message '{MessageType}'", messageType);
                break;
            }
        }

        if (bunch.IsError())
        {
            Logger.Error("Failed to read control channel message");

            if (Connection != null)
            {
                Connection.Close();
            }
        }

        // throw new NotImplementedException();
    }

    public override FPacketIdRange SendBunch(FOutBunch bunch, bool merge)
    {
        // TODO: Queue.
        if (!bunch.IsError())
        {
            return base.SendBunch(bunch, merge);
        }
        else
        {
            // an error here most likely indicates an unfixable error, such as the text using more than the maximum packet size
            // so there is no point in queueing it as it will just fail again
            Logger.Error("Control channel bunch overflowed");
            Connection!.Close();
            return new FPacketIdRange();
        }
    }
}