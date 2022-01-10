namespace Prospect.Unreal.Core.Names;

public class FName
{
    public FName()
    {
        NameIndex = -1; // ?
        Number = -1; // ?
        Str = "None";
    }

    public FName(string str)
    {
        NameIndex = -1;
        Number = -1;
        Str = str;
    }

    public FName(string str, int number)
    {
        NameIndex = -1;
        Number = number;
        Str = str;
    }

    public Int32 NameIndex { get; set; }

    public Int32 Number { get; set; }

    public string Str { get; set; }

    public override string ToString()
    {
        return Str;
    }
}