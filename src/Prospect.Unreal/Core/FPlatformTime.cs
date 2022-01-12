namespace Prospect.Unreal.Core;

public static class FPlatformTime
{
    public static double Seconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}