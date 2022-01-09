using System.Runtime.Serialization;

namespace Prospect.Unreal.Exceptions;

public class UnrealNetException : UnrealException
{
    public UnrealNetException()
    {
    }

    protected UnrealNetException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public UnrealNetException(string? message) : base(message)
    {
    }

    public UnrealNetException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}