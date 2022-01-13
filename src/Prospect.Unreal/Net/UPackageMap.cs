using System.Diagnostics.CodeAnalysis;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Net;

public class UPackageMap
{
    public static unsafe bool StaticSerializeName(FArchive ar, [MaybeNullWhen(false)] ref FName name)
    {
        if (ar.IsLoading())
        {
            name = null;
        
            bool bHardcoded;
            ar.SerializeBits(&bHardcoded, 1);
            
            if (bHardcoded)
            {
                // replicated by hardcoded index
                uint nameIndex;

                if (ar.EngineNetVer() < EEngineNetworkVersionHistory.HISTORY_CHANNEL_NAMES)
                {
                    ar.SerializeInt(&nameIndex, UnrealNames.MaxNetworkedHardcodedName);
                }
                else
                {
                    ar.SerializeIntPacked(&nameIndex);
                }

                if (nameIndex < UnrealNames.MaxHardcodedNameIndex)
                {
                    // hardcoded names never have a Number
                    name = UnrealNames.FNames[(UnrealNameKey) nameIndex];
                }
                else
                {
                    ar.SetError();
                }
            }
            else
            {
                // replicated by string
                var inString = FString.Deserialize(ar);
                var inNumber = ar.ReadInt32();
                name = new FName(inString, inNumber);
            }
        }
        else
        {
            var bHardcoded = (byte)(ShouldReplicateAsInteger(name) ? 1 : 0);
            ar.SerializeBits(&bHardcoded, 1); // 25
            if (bHardcoded != 0)
            {
                ar.SerializeIntPacked((uint)name.Number);
            }
            else
            {
                // send by string
                var outString = name.Str;
                var outNumber = name.Number;
                
                ar.WriteString(outString);
                ar.WriteInt32(outNumber);
            }
        }

        return true;
    }

    private static bool ShouldReplicateAsInteger(FName name)
    {
        return name.Number <= UnrealNames.MaxNetworkedHardcodedName;
    }

    public void NotifyBunchCommit(int bunchPacketId, FOutBunch bunch)
    {
        throw new NotImplementedException();
    }

    public void ReceivedAck(int ackPacketId)
    {
        // TODO: Implement
    }

    public void ReceivedNak(int nakPacketId)
    {
        // TODO: Implement
    }
}