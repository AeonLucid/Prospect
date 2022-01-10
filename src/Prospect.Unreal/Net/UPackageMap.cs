using System.Diagnostics.CodeAnalysis;
using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Net;

public class UPackageMap
{
    public static unsafe bool StaticSerializeName(FBitReader ar, [MaybeNullWhen(false)] out FName name)
    {
        if (!ar.IsLoading())
        {
            throw new NotImplementedException();
        }

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

        return true;
    }
}