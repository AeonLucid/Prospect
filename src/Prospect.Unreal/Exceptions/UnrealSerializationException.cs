using System.Runtime.Serialization;

namespace Prospect.Unreal.Exceptions;

public class UnrealSerializationException : UnrealException
{
    public UnrealSerializationException()
    {
    }

    protected UnrealSerializationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public UnrealSerializationException(string? message) : base(message)
    {
    }

    public UnrealSerializationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}