using Prospect.Unreal.Net;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Core;

public class FEngineVersion
{
    public ushort Major { get; private set; }

    public ushort Minor { get; private set; }

    public ushort Patch { get; private set; }

    public uint Changelist { get; private set; }

    public string Branch { get; private set; }

    public void Set(ushort major, ushort minor, ushort patch, uint changelist, string branch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Changelist = changelist;
        Branch = branch;
    }

    public void Deserialize(FArchive archive)
    {
        Major = archive.ReadUInt16();
        Minor = archive.ReadUInt16();
        Patch = archive.ReadUInt16();
        Changelist = archive.ReadUInt32();
        Branch = FString.Deserialize(archive);
    }
}