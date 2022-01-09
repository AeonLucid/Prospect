using System.Buffers;

namespace Prospect.Unreal.Serialization;

public class FMemoryWriter : FArchive
{
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create();

    public FMemoryWriter()
    {
        
    }
    
    
    
    public override void Dispose()
    {
        
    }
}