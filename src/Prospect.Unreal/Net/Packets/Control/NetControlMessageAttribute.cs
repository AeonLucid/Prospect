namespace Prospect.Unreal.Net.Packets.Control;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal class NetControlMessageAttribute : Attribute
{
    public NetControlMessageAttribute(string name, int index, params Type[] args)
    {
        Name = name;
        Index = index;
        Args = args;
    }

    public string Name { get; }
    public int Index { get; }
    public Type[] Args { get; }
}
