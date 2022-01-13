namespace Prospect.Unreal.Net;

public class FUniqueNetId
{
    public FUniqueNetId(string contents)
    {
        Contents = contents;
    }
    
    public string Contents { get; }

    public bool IsValid()
    {
        return true;
    }
}