using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Serialization;
using Serilog;

namespace Prospect.Unreal.Net;

public class FUniqueNetIdRepl
{
    private static readonly ILogger Logger = Log.ForContext<FUniqueNetIdRepl>();
    private const int TypeHash_Other = 31;
    
    public FUniqueNetId? UniqueNetId { get; private set; }

    public bool IsValid()
    {
        return UniqueNetId != null && UniqueNetId.IsValid();
    }

    public string ToDebugString()
    {
        // TODO: Add type
        return IsValid() ? $"{UniqueNetId!.Contents}" : "INVALID";
    }
    
    public static void Write(FArchive ar, FUniqueNetIdRepl value)
    {
        Serialize(ar, value);
    }

    public static FUniqueNetIdRepl Read(FArchive ar)
    {
        var result = new FUniqueNetIdRepl();
        Serialize(ar, result);
        return result;
    }

    private void NetSerialize(FArchive ar, UPackageMap? packageMap, out bool bOutSuccess)
    {
        // TODO: Get back to this later when we understand FName / FNamePool and UOnlineEngineInterface better.
        // FUniqueNetIdRepl::NetSerialize
        
        bOutSuccess = false;
        
        if (ar.IsSaving())
        {
            throw new NotImplementedException();
        } 
        else if (ar.IsLoading())
        {
            UniqueNetId = null;
            
            var encodingFlags = (EUniqueIdEncodingFlags) ar.ReadByte();

            if (!ar.IsError())
            {
                if ((encodingFlags & EUniqueIdEncodingFlags.IsEncoded) != 0)
                {
                    if ((encodingFlags & EUniqueIdEncodingFlags.IsEmpty) == 0)
                    {
                        // Non empty and hex encoded
                        var typeHash = GetTypeHashFromEncoding(encodingFlags);
                        if (typeHash == 0)
                        {
                            // If no type was encoded, assume default
                            throw new NotImplementedException();
                            // TypeHash = UOnlineEngineInterface::Get()->GetReplicationHashForSubsystem(UOnlineEngineInterface::Get()->GetDefaultOnlineSubsystemName());
                        }

                        var bValidTypeHash = typeHash != 0;
                        
                        if (typeHash == TypeHash_Other)
                        {
                            var typeString = ar.ReadString();
                            var type = new FName(typeString);
                            // TODO: Add FName into FNamePool
                            throw new NotImplementedException();
                            if (ar.IsError() || type.Number == (int)UnrealNameKey.None)
                            {
                                bValidTypeHash = false;
                            }
                        }
                        else
                        {
                            // Type = UOnlineEngineInterface::Get()->GetSubsystemFromReplicationHash(TypeHash);
                        }

                        throw new NotImplementedException();
                    }
                    else
                    {
                        bOutSuccess = true;
                    }
                }
                else
                {
                    // Original FString serialization goes here
                    var typeHash = GetTypeHashFromEncoding(encodingFlags);
                    if (typeHash == 0)
                    {
                        // If no type was encoded, assume default
                        throw new NotImplementedException();
                        // TypeHash = UOnlineEngineInterface::Get()->GetReplicationHashForSubsystem(UOnlineEngineInterface::Get()->GetDefaultOnlineSubsystemName());
                    }

                    var bValidTypeHash = typeHash != 0;
                    if (typeHash == TypeHash_Other)
                    {
                        
                    }
                    else
                    {
                        // TODO: Type = UOnlineEngineInterface::Get()->GetSubsystemFromReplicationHash(TypeHash);
                    }

                    if (bValidTypeHash)
                    {
                        var contents = ar.ReadString();
                        if (!ar.IsError())
                        {
                            // TODO: Check if type != none
                            UniqueNetId = new FUniqueNetId(contents);
                            bOutSuccess = true;
                        }
                    }
                    else
                    {
                        Logger.Warning("Error with encoded type hash");
                    }
                }
            }
            else
            {
                Logger.Warning("Error serializing unique id");
            }
        }
    }
    
    private static void Serialize(FArchive ar, FUniqueNetIdRepl uniqueNetId)
    {
        if (!ar.IsPersistent() || ar._arIsNetArchive)
        {
            uniqueNetId.NetSerialize(ar, null, out _);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private static byte GetTypeHashFromEncoding(EUniqueIdEncodingFlags inFlags)
    {
        var typeHash = (byte) ((byte) (inFlags & EUniqueIdEncodingFlags.TypeMask) >> 3);
        return (byte)(typeHash < 32 ? typeHash : 0);
    }
}
