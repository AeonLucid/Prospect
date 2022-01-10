namespace Prospect.Unreal.Serialization;

public struct FBitReaderMark
{
    private long _pos;
    
    public FBitReaderMark(FBitReader reader)
    {
        _pos = reader.Pos;
    }

    public long GetPos()
    {
        return _pos;
    }

    public void Pop(FBitReader reader)
    {
        reader.Pos = _pos;
    }
}