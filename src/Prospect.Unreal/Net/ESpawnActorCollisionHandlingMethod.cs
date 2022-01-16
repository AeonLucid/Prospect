namespace Prospect.Unreal.Net;

public enum ESpawnActorCollisionHandlingMethod
{
    Undefined,
    AlwaysSpawn,
    AdjustIfPossibleButAlwaysSpawn,
    AdjustIfPossibleButDontSpawnIfColliding,
    DontSpawnIfColliding
}