namespace Prospect.Unreal.Core.Properties;

public class UStruct : UField
{
    private UStruct? _superStruct;
    
    public bool IsChildOf(UStruct? someBase)
    {
        if (someBase == null)
        {
            return false;
        }

        var bOldResult = false;
        
        for (var tempStruct = this; tempStruct != null; tempStruct = tempStruct.GetSuperStruct())
        {
            if (tempStruct == someBase)
            {
                bOldResult = true;
                break;
            }
        }

        return bOldResult;
    }

    public UStruct? GetSuperStruct()
    {
        return _superStruct;
    }

    public virtual void SetSuperStruct(UStruct newSuperStruct)
    {
        _superStruct = newSuperStruct;
    }
}