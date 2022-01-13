namespace Prospect.Unreal.Net;

public class UChildConnection : UNetConnection
{
    public override void LowLevelSend(byte[] data, int countBits, FOutPacketTraits traits)
    {
        throw new NotImplementedException();
    }

    public override string LowLevelGetRemoteAddress(bool bAppendPort = false)
    {
        throw new NotImplementedException();
    }

    public override string LowLevelDescribe()
    {
        throw new NotImplementedException();
    }

    public override void Tick(float deltaSeconds)
    {
        throw new NotImplementedException();
    }

    public override void CleanUp()
    {
        throw new NotImplementedException();
    }

    public override float GetTimeoutValue()
    {
        throw new NotImplementedException();
    }
}