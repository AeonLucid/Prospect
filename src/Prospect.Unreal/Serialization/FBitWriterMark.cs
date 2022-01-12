namespace Prospect.Unreal.Serialization;

public struct FBitWriterMark
{
    private bool _overflowed;
    private long _num;

    public FBitWriterMark()
    {
        _overflowed = false;
        _num = 0;
    }

    public FBitWriterMark(FBitWriter writer)
    {
        _overflowed = writer.IsError();
        _num = writer.GetNumBits();
    }

    public long GetPos()
    {
        return _num;
    }

    public void Pop(FBitReader reader)
    {
        reader.Pos = _num;
    }

    public void Init(FBitWriter writer)
    {
        _num = writer.Num;
        _overflowed = writer.IsError();
    }

    public void Reset()
    {
        _overflowed = false;
        _num = 0;
    }

    public void PopWithoutClear(FBitWriter writer)
    {
        writer.Num = _num;
    }
}