using System.Net;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net;

public record FIncomingPacketRef(FBitReader Packet, IPEndPoint Address, FInPacketTraits Traits);

public record ProcessedPacket(byte[] Data, int CountBits, bool Error = false);

public class PacketHandler
{
    private static readonly ILogger Logger = Log.ForContext<PacketHandler>();
    
    private readonly List<HandlerComponent> _handlerComponents;
    
    private bool _bConnectionlessHandler;
    private bool _bRawSend;
    private HandlerState _state;
    private ReliabilityHandlerComponent? _reliabilityComponent;

    private FBitWriter _outgoingPacket;
    private FBitReader _incomingPacket;

    public PacketHandler()
    {
        _bConnectionlessHandler = false;
        _state = HandlerState.Uninitialized;
        _handlerComponents = new List<HandlerComponent>();
        _reliabilityComponent = null;
        _outgoingPacket = new FBitWriter(0);
        _incomingPacket = new FBitReader(Array.Empty<byte>());

        Mode = HandlerMode.Server;
    }

    public HandlerMode Mode { get; }

    public void Tick(float deltaTime)
    {
        foreach (var component in _handlerComponents)
        {
            component.Tick(deltaTime);
        }
    }

    public void Initialize(bool bConnectionlessOnly)
    {
        _bConnectionlessHandler = bConnectionlessOnly;

        if (!_bConnectionlessHandler)
        {
            // TODO: Load from .ini file (GEngineIni)
            //  %s PacketHandlerProfileConfig (Driver..)
            //  Components
            
            // If no matches, load from PacketHandlerComponents / Components in GEngineIni
        }
        
        // TODO: FEncryptionComponent.
        
        // TODO: ReliabilityHandlerComponent.
    }

    public void InitializeComponents()
    {
        if (_state == HandlerState.Uninitialized)
        {
            if (_handlerComponents.Count > 0)
            {
                SetState(HandlerState.InitializingComponents);
            }
            else
            {
                HandlerInitialized();
            }
        }

        foreach (var component in _handlerComponents)
        {
            if (component.IsValid() && !component.IsInitialized())
            {
                component.Initialize();
            }
        }
    }

    public bool IncomingConnectionless(FReceivedPacketView packetView)
    {
        packetView.Traits.ConnectionlessPacket = true;

        return Incoming_Internal(packetView);
    }

    public ProcessedPacket OutgoingConnectionless(IPEndPoint address, byte[] packet, int countBits, FOutPacketTraits traits)
    {
        return Outgoing_Internal(packet, countBits, traits, true, address);
    }

    public void BeginHandshaking()
    {
        // bBeganHandshaking = true;

        foreach (var component in _handlerComponents)
        {
            if (component.RequiresHandshake() && !component.IsInitialized())
            {
                component.NotifiyHandshakeBegin();
            }
        }
    }

    public HandlerComponent AddHandler<T>() where T : HandlerComponent
    {
        var result = (HandlerComponent) Activator.CreateInstance(typeof(T), this)!;

        _handlerComponents.Add(result);
        
        return result;
    }

    public bool Incoming(FReceivedPacketView packetView)
    {
        return Incoming_Internal(packetView);
    }

    public void IncomingHigh(FBitReader reader)
    {
        // NO-OP
    }

    private bool Incoming_Internal(FReceivedPacketView packetView)
    {
        var returnVal = true;
        var dataView = packetView.DataView;
        var countBits = dataView.NumBits();

        if (_handlerComponents.Count > 0)
        {
            var data = dataView.GetData();
            var lastByte = data[dataView.NumBytes() - 1];
            if (lastByte != 0)
            {
                countBits--;

                while ((lastByte & 0x80) == 0)
                {
                    lastByte *= 2;
                    countBits--;
                }
            }
            else
            {
                returnVal = false;
            }
        }

        if (returnVal)
        {
            var processPacketReader = new FBitReader(dataView.GetData(), countBits);
            var packetRef = new FIncomingPacketRef(processPacketReader, packetView.Address, packetView.Traits);
            
            if (_state == HandlerState.Uninitialized)
            {
                UpdateInitialState();
            }
            
            foreach (var component in _handlerComponents)
            {
                if (processPacketReader.GetPosBits() != 0 && !component.CanReadUnaligned())
                {
                    RealignPacket(processPacketReader);
                }

                if (packetView.Traits.ConnectionlessPacket)
                {
                    component.IncomingConnectionless(packetRef);
                }
                else
                {
                    component.Incoming(processPacketReader);
                }
            }

            if (!processPacketReader.IsError())
            {
                ReplaceIncomingPacket(processPacketReader);

                packetView.DataView = new FPacketDataView(_incomingPacket.GetBuffer(), _incomingPacket.GetBitsLeft(), ECountUnits.Bits);
            }
            else
            {
                returnVal = false;
            }
        }

        return returnVal;
    }

    private ProcessedPacket Outgoing_Internal(byte[] packet, int countBits, FOutPacketTraits traits, bool bConnectionLess, IPEndPoint address)
    {
        if (!_bRawSend)
        {
            throw new NotImplementedException();
        }
        else
        {
            return new ProcessedPacket(packet, countBits);
        }
    }

    private void SetState(HandlerState state)
    {
        if (state == _state)
        {
            Logger.Fatal("Set PacketHandler state to the state it is currently in ({State})", state);
        }
        else
        {
            _state = state;
        }
    }

    private void UpdateInitialState()
    {
        if (_state == HandlerState.Uninitialized)
        {
            if (_handlerComponents.Count > 0)
            {
                InitializeComponents();
            }
            else
            {
                HandlerInitialized();
            }
        }
    }

    private void HandlerInitialized()
    {
        if (_reliabilityComponent != null)
        {
            throw new NotImplementedException();
        }
        
        SetState(HandlerState.Initialized);
    }

    private void ReplaceIncomingPacket(FBitReader replacementPacket)
    {
        if (replacementPacket.GetPosBits() == 0 || replacementPacket.GetBitsLeft() == 0)
        {
            _incomingPacket = replacementPacket;
        }
        else
        {
            var tempPacketData = new byte[replacementPacket.GetBytesLeft()];
            var newPacketSizeBits = replacementPacket.GetBitsLeft();
            
            replacementPacket.SerializeBits(tempPacketData, newPacketSizeBits);
            
            _incomingPacket = new FBitReader(tempPacketData, newPacketSizeBits);
        }
    }

    private void RealignPacket(FBitReader packet)
    {
        Logger.Warning("Realigning packet, which is untested");
        
        if (packet.GetPosBits() != 0)
        {
            var bitsLeft = packet.GetBitsLeft();
            if (bitsLeft > 0)
            {
                var tempPacketData = new byte[packet.GetBytesLeft()];

                packet.SerializeBits(tempPacketData, bitsLeft);
                packet.SetData(tempPacketData, bitsLeft);
            }
        }
    }

    public void SetRawSend(bool enabled)
    {
        _bRawSend = enabled;
    }

    public bool GetRawSend()
    {
        return _bRawSend;
    }
}