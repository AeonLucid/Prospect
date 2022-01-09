using System.Runtime.Serialization;

namespace Prospect.Unreal.Exceptions;

public class UnrealException : Exception
{
    public UnrealException()
    {
    }

    protected UnrealException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public UnrealException(string? message) : base(message)
    {
    }

    public UnrealException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}