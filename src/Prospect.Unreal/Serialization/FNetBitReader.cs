using Prospect.Unreal.Net;

namespace Prospect.Unreal.Serialization;

public class FNetBitReader : FBitReader
{
    public FNetBitReader(byte[] src) : base(src)
    {
        throw new NotSupportedException();
    }

    public FNetBitReader(UPackageMapClient? inPackageMap, byte[]? src, int num) : base(src, num)
    {
        PackageMap = inPackageMap;
    }
    
    public UPackageMapClient? PackageMap { get; }
}