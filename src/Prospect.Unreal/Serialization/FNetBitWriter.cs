using Prospect.Unreal.Core;
using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net;

namespace Prospect.Unreal.Serialization;

public class FNetBitWriter : FBitWriter
{
    public FNetBitWriter() : base(0)
    {
        PackageMap = null;
    }

    public FNetBitWriter(long inMaxBits) : base(inMaxBits, true)
    {
        PackageMap = null;
    }

    public FNetBitWriter(UPackageMap inPackageMap, long inMaxBits) : base(inMaxBits, true)
    {
        PackageMap = inPackageMap;
    }

    public FNetBitWriter(FNetBitWriter writer) : base(writer)
    {
        PackageMap = writer.PackageMap;
    }

    public UPackageMap? PackageMap { get; }

    public virtual void WriteFName(FName name)
    {
        throw new NotImplementedException();
    }

    public virtual void WriteUObject(UObject obj)
    {
        throw new NotImplementedException();
    }
    
    // TODO: FSoftObjectPath
    // TODO: FSoftObjectPtr
    // TODO: FWeakObjectPtr
}