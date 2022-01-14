using System.Runtime.CompilerServices;

namespace Prospect.Unreal.Core.Names;

internal static class FNameHelper
{
    /// <summary>
    ///     Externally, the instance number to represent no instance number is NAME_NO_NUMBER, 
    ///     but internally, we add 1 to indices, so we use this #define internally for 
    ///     zero'd memory initialization will still make NAME_None as expected
    /// </summary>
    public const int NAME_NO_NUMBER_INTERNAL = 0;

    /// <summary>
    ///     Special value for an FName with no number
    /// </summary>
    public const int NAME_NO_NUMBER = NAME_NO_NUMBER_INTERNAL - 1;

    /// <summary>
    ///     Maximum size of name.
    /// </summary>
    public const int NAME_SIZE = 1024;
    
    public static FName MakeDetectNumber(string name, EFindName findType)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new FName();
        }

        var nameLen = name.Length;
        var internalNumber = ParseNumber(name, ref nameLen);
        return MakeWithNumber(name.Substring(0, nameLen), findType, (int)internalNumber);
    }

    private static uint ParseNumber(ReadOnlySpan<char> name, ref int nameLength)
    {
        var len = nameLength;
        var digits = 0;
        
        for (var i = len - 1; i >= 0 && name[i] >= '0' && name[i] <= '9'; --i)
        {
            ++digits;
        }

        var firstDigit = len - digits;
        const int maxDigitsInt32 = 10;
        if (digits != 0 && digits < len && name[firstDigit - 1] == '_' && digits <= maxDigitsInt32)
        {
            // check for the case where there are multiple digits after the _ and the first one
            // is a 0 ("Rocket_04"). Can't split this case. (So, we check if the first char
            // is not 0 or the length of the number is 1 (since ROcket_0 is valid)
            if (digits == 1 || name[firstDigit] != '0')
            {
                var number = long.Parse(name.Slice(len - digits, digits));
                if (number < int.MaxValue)
                {
                    nameLength -= 1 + digits;
                    return NAME_EXTERNAL_TO_INTERNAL((uint)number);
                }
            }
        }

        return NAME_NO_NUMBER_INTERNAL;
    }

    public static FName MakeWithNumber(string name, EFindName findType, int internalNumber)
    {
        if (name.Length == 0)
        {
            return new FName();
        }

        return Make(name, findType, internalNumber);
    }

    private static FName Make(string name, EFindName findType, int internalNumber)
    {
        if (name.Length >= NAME_SIZE)
        {
            return new FName("ERROR_NAME_SIZE_EXCEEDED");
        }

        FNameEntryId index;
        
        if (findType == EFindName.FNAME_Add)
        {
            index = FNamePool.Store(name);
        } 
        else if (findType == EFindName.FNAME_Find)
        {
            index = FNamePool.Find(name);
        }
        else
        {
            throw new NotImplementedException();
        }

        return new FName(index, internalNumber);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint NAME_EXTERNAL_TO_INTERNAL(uint number)
    {
        return number + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NAME_EXTERNAL_TO_INTERNAL(int number)
    {
        return number + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint NAME_INTERNAL_TO_EXTERNAL(uint number)
    {
        return number - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NAME_INTERNAL_TO_EXTERNAL(int number)
    {
        return number - 1;
    }
}