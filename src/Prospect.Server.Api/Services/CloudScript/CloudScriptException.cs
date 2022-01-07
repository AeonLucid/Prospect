using System.Runtime.Serialization;

namespace Prospect.Server.Api.Services.CloudScript;

public class CloudScriptException : Exception
{
    public CloudScriptException()
    {
    }

    protected CloudScriptException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public CloudScriptException(string? message) : base(message)
    {
    }

    public CloudScriptException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}