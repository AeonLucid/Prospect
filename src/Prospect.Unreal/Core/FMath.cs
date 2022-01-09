namespace Prospect.Unreal.Core;

public static class FMath
{
    public static int DivideAndRoundUp(int dividend, int divisor)
    {
        return (dividend + divisor - 1) / divisor;
    }

    public static float FRandRange(float min, float max)
    {
        return min + (max - min) * Random.Shared.NextSingle();
    }
}