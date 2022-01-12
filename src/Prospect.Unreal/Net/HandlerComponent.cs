using System.Net;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Net;

public abstract class HandlerComponent
{
    private bool _bActive;
    private bool _bInitialized;
    private string _name;

    protected HandlerComponent(PacketHandler handler, string name)
    {
        _name = name;
        RequiresHandshake = false;
        RequiresReliability = false;
        _bActive = false;
        _bInitialized = false;

        Handler = handler;
        State = HandlerComponentState.UnInitialized;
    }
    
    protected PacketHandler Handler { get; }
    protected HandlerComponentState State { get; private set; }
    
    public bool RequiresHandshake { get; protected set; }
    public bool RequiresReliability { get; protected set; }
    
    /// <summary>
    ///     Maximum number of Outgoing packet bits supported (automatically calculated to factor in other HandlerComponent reserved bits)
    /// </summary>
    public uint MaxOutgoingBits { get; set; }
    
    public virtual bool IsActive()
    {
        return _bActive;
    }

    public virtual bool IsValid()
    {
        return false;
    }

    public bool IsInitialized()
    {
        return _bInitialized;
    }

    public virtual void Incoming(FBitReader packet)
    {
    }

    public virtual void Outgoing(ref FBitWriter packet, FOutPacketTraits traits)
    {
    }

    public virtual void IncomingConnectionless(FIncomingPacketRef packetRef)
    {
    }

    public virtual void OutgoingConnectionless(IPEndPoint address, FBitWriter packet, FOutPacketTraits traits)
    {
    }

    public virtual bool CanReadUnaligned()
    {
        return false;
    }

    public abstract void Initialize();

    public virtual void NotifiyHandshakeBegin()
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void SetActive(bool active)
    {
        _bActive = active;
    }

    public virtual int GetReservedPacketBits()
    {
        return 0;
    }

    public virtual void CountBytes(FArchive ar)
    {
    }

    protected void SetState(HandlerComponentState state)
    {
        State = state;
    }
    
    protected void Initialized()
    {
        _bInitialized = true;
        Handler.HandlerComponentInitialized(this);
    }
}